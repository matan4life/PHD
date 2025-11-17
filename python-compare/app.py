import json
import os
import base64
import logging
import time
import traceback

import boto3
from boto3.dynamodb.conditions import Key
import numpy as np
from minutia_types import MINUTIA_DTYPE_BASE, assign_unique_ids
from minutia_comparison import compare_images
from score_aggregation import aggregate_group_scores, evaluate_thresholds

# Configure logging
logger = logging.getLogger(__name__)
logger.setLevel(logging.INFO)
handler = logging.StreamHandler()
handler.setFormatter(logging.Formatter('%(asctime)s %(levelname)s %(message)s'))
logger.addHandler(handler)

def validate_binary_data(binary_data: bytes, dtype, field_name: str) -> np.ndarray:
    """
    Validates and creates NumPy array from binary data with detailed error reporting.
    
    Args:
        binary_data: Raw binary data
        dtype: NumPy dtype for the array
        field_name: Name for error reporting
        
    Returns:
        NumPy array created from binary data
        
    Raises:
        ValueError: If binary data is invalid
    """
    if not binary_data:
        raise ValueError(f"{field_name}: Binary data is empty")
    
    element_size = dtype.itemsize
    data_size = len(binary_data)
    
    logger.info(f"{field_name}: Data size = {data_size} bytes, Element size = {element_size} bytes")
    logger.info(f"{field_name}: dtype details = {dtype}")
    
    if data_size % element_size != 0:
        expected_elements = data_size // element_size
        remainder = data_size % element_size
        
        error_msg = (f"{field_name}: Buffer size mismatch. "
                    f"Data size: {data_size} bytes, "
                    f"Element size: {element_size} bytes, "
                    f"Expected elements: {expected_elements}, "
                    f"Remainder: {remainder} bytes")
        
        logger.error(error_msg)
        
        # Попытка обрезать данные до корректного размера
        if remainder > 0 and expected_elements > 0:
            corrected_size = expected_elements * element_size
            logger.warning(f"{field_name}: Attempting to truncate data from {data_size} to {corrected_size} bytes")
            binary_data = binary_data[:corrected_size]
            logger.info(f"{field_name}: Truncated data size = {len(binary_data)} bytes")
        else:
            raise ValueError(error_msg)
    
    try:
        array = np.frombuffer(binary_data, dtype=dtype)
        logger.info(f"{field_name}: Successfully created array with {len(array)} elements")
        return array
    except Exception as e:
        logger.error(f"{field_name}: Failed to create NumPy array: {str(e)}")
        raise ValueError(f"{field_name}: Failed to create NumPy array: {str(e)}")


def safe_decode_base64(data_b64: str, field_name: str) -> bytes:
    """
    Safely decodes base64 data with error handling.
    
    Args:
        data_b64: Base64 encoded string
        field_name: Name for error reporting
        
    Returns:
        Decoded binary data
    """
    try:
        if not data_b64:
            raise ValueError(f"{field_name}: Base64 data is empty")
        
        # Проверка на корректность base64
        if len(data_b64) % 4 != 0:
            logger.warning(f"{field_name}: Base64 string length is not multiple of 4, padding may be missing")
        
        decoded = base64.b64decode(data_b64)
        logger.info(f"{field_name}: Successfully decoded {len(data_b64)} base64 chars to {len(decoded)} bytes")
        return decoded
    except Exception as e:
        logger.error(f"{field_name}: Failed to decode base64 data: {str(e)}")
        raise ValueError(f"{field_name}: Failed to decode base64 data: {str(e)}")


def lambda_handler(event, context):
    """
    AWS Lambda handler for processing DynamoDB stream inserts.

    Processes probe insertion, compares against all groups, aggregates scores,
    evaluates thresholds, writes "YES"/"NO" to ResultsTable, and deletes probe.
    """
    logger.info(f"Lambda handler started. Request ID: {context.aws_request_id}")
    logger.info(f"Event contains {len(event.get('Records', []))} records")
    
    # Логируем информацию о dtype
    logger.info(f"MINUTIA_DTYPE_BASE itemsize: {MINUTIA_DTYPE_BASE.itemsize} bytes")
    logger.info(f"MINUTIA_DTYPE_BASE fields: {MINUTIA_DTYPE_BASE.names}")
    logger.info(f"MINUTIA_DTYPE_BASE descr: {MINUTIA_DTYPE_BASE.descr}")

    dynamodb = boto3.resource('dynamodb')
    minutiae_table = dynamodb.Table(os.environ['INPUT_TABLE_NAME'])
    results_table = dynamodb.Table(os.environ['RESULT_TABLE_NAME'])

    logger.info(f"Connected to tables: {os.environ['INPUT_TABLE_NAME']}, {os.environ['RESULT_TABLE_NAME']}")

    try:
        for record_idx, record in enumerate(event['Records']):
            logger.info(f"Processing record {record_idx + 1}/{len(event['Records'])}")
            if record['eventName'] != 'INSERT':
                logger.info(f"Skipping record {record_idx + 1}: eventName = {record['eventName']}")
                continue
                
            new_image = record['dynamodb']['NewImage']
            probe_image_id = new_image['ImageId']['S']

            logger.info(f"Processing probe image: {probe_image_id}")

            # Безопасное декодирование данных probe
            try:
                probe_binary_b64 = new_image['MinutiaeBinary']['B']
                probe_binary = safe_decode_base64(probe_binary_b64, f"Probe {probe_image_id}")
                probe_minutiae = validate_binary_data(probe_binary, MINUTIA_DTYPE_BASE, f"Probe {probe_image_id}")
            except Exception as e:
                logger.error(f"Failed to process probe binary data for {probe_image_id}: {str(e)}")
                continue  # Пропускаем этот probe и переходим к следующему

            # Обработка метаданных
            try:
                metadata = new_image['Metadata']['M']
                probe_center = (int(metadata['center_x']['N']), int(metadata['center_y']['N']))
                logger.info(f"Probe center coordinates: {probe_center}")
            except Exception as e:
                logger.error(f"Failed to parse metadata for {probe_image_id}: {str(e)}")
                continue

            # Assign IDs to probe
            try:
                probe_minutiae, current_id = assign_unique_ids(probe_minutiae, 0)
                logger.info(f"Assigned IDs to probe minutiae. Next ID: {current_id}")
            except Exception as e:
                logger.error(f"Failed to assign IDs to probe minutiae: {str(e)}")
                continue

            # Initialize global caches (by ImageId)
            global_dist_cache = {}
            global_angle_cache = {}
            logger.info("Initialized global caches")
            logger.info("Starting scan for unique group IDs")

            # Scan unique group IDs (optimize with GSI if available)
            try:
                scan_response = minutiae_table.scan(ProjectionExpression='GroupId')
                group_ids = set(item['GroupId'] for item in scan_response['Items'] if 'GroupId' in item)
                while 'LastEvaluatedKey' in scan_response:
                    scan_response = minutiae_table.scan(ProjectionExpression='GroupId',
                                                        ExclusiveStartKey=scan_response['LastEvaluatedKey'])
                    group_ids.update(item['GroupId'] for item in scan_response['Items'] if 'GroupId' in item)

                logger.info(f"Found {len(group_ids)} unique groups")
            except Exception as e:
                logger.error(f"Failed to scan for group IDs: {str(e)}")
                continue

            for group_id in group_ids:
                try:
                    # Query galleries in group
                    query_response = minutiae_table.query(KeyConditionExpression=Key('GroupId').eq(group_id))
                    galleries = query_response['Items']
                    while 'LastEvaluatedKey' in query_response:
                        query_response = minutiae_table.query(KeyConditionExpression=Key('GroupId').eq(group_id),
                                                              ExclusiveStartKey=query_response['LastEvaluatedKey'])
                        galleries.extend(query_response['Items'])

                    logger.info(f"Processing group {group_id} with {len(galleries)} galleries")
                    
                    group_scores = []
                    group_size = 0
                    
                    for gallery in galleries:
                        if gallery['ImageId'] == probe_image_id:
                            continue
                            
                        group_size += 1
                        gallery_image_id = gallery['ImageId']
                        
                        try:
                            # Безопасное декодирование данных gallery
                            gallery_binary_b64 = gallery['MinutiaeBinary']['B']
                            gallery_binary = safe_decode_base64(gallery_binary_b64, f"Gallery {gallery_image_id}")
                            gallery_minutiae = validate_binary_data(gallery_binary, MINUTIA_DTYPE_BASE, f"Gallery {gallery_image_id}")
                            
                            gallery_metadata = gallery['Metadata']['M']
                            gallery_center = (int(gallery_metadata['center_x']['N']), int(gallery_metadata['center_y']['N']))
                            gallery_minutiae, current_id = assign_unique_ids(gallery_minutiae, current_id)
                            
                            score, global_dist_cache, global_angle_cache = compare_images(
                                probe_minutiae=probe_minutiae,
                                probe_center=probe_center,
                                probe_image_id=probe_image_id,
                                gallery_minutiae=gallery_minutiae,
                                gallery_center=gallery_center,
                                gallery_image_id=gallery_image_id,
                                global_dist_cache=global_dist_cache,
                                global_angle_cache=global_angle_cache
                            )
                            group_scores.append(score)
                            
                        except Exception as e:
                            logger.error(f"Failed to process gallery {gallery_image_id}: {str(e)}")
                            continue  # Пропускаем эту gallery и продолжаем

                    # Aggregate scores and evaluate
                    normalized_pos, normalized_mea = aggregate_group_scores(group_scores, group_size)
                    result = "YES" if evaluate_thresholds(normalized_pos, normalized_mea) else "NO"
                    
                    results_table.put_item(Item={
                        'ImageId': probe_image_id,
                        'GroupId': group_id,
                        'Result': result
                    })
                    
                    logger.info(f"Group {group_id}: {len(group_scores)} successful comparisons, result = {result}")
                    
                except Exception as e:
                    logger.error(f"Failed to process group {group_id}: {str(e)}")
                    continue  # Пропускаем эту группу и продолжаем

            # Delete probe entry
            try:
                minutiae_table.delete_item(Key={'ImageId': probe_image_id})
                logger.info(f"Deleted probe entry: {probe_image_id}")
            except Exception as e:
                logger.error(f"Failed to delete probe entry {probe_image_id}: {str(e)}")

    except Exception as e:
        error_details = {
            'error': str(e),
            'type': type(e).__name__,
            'traceback': traceback.format_exc(),
            'probe_image_id': probe_image_id if 'probe_image_id' in locals() else 'unknown',
            'request_id': context.aws_request_id
        }
        logger.error(f"Critical error in lambda handler: {json.dumps(error_details, indent=2)}")
        return {'statusCode': 500, 'body': json.dumps(error_details)}

    return {'statusCode': 200}

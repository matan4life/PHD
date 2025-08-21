import json
import os
import boto3
import numpy as np
import logging
import time
from fs_utils import read_file_as_opencv
from extract_steps import crop_image, create_enhanced_version, get_centroid, shift_minutiae
from cv_filter_utils import get_image_skeletons, calculate_minutiae
from dynamo_utils import save_minutiae_to_dynamo, save_group_to_dynamo

# Configure logging
logger = logging.getLogger(__name__)
logger.setLevel(logging.INFO)
handler = logging.StreamHandler()
handler.setFormatter(logging.Formatter('%(asctime)s %(levelname)s %(message)s'))
logger.addHandler(handler)

s3_client = boto3.client('s3')


def handler(event, context):
    """Lambda handler for minutiae extraction from dataset S3 events.

    Reuse: TABLE_NAME env for dataset/query, TTL_SECONDS for query.
    Optimizations: Batch processing, compressed binary, numba in utils.
    Logging: Structured logs to CloudWatch with timings and request ID.

    Args:
        event: S3 event {'Records': [{'s3': {'bucket': {'name': str}, 'object': {'key': str}}}]}
        context: Lambda context

    Returns:
        JSON with per-image results
    """
    table_name = os.environ['TABLE_NAME']
    service = os.environ['SERVICE']
    groups_table_name = os.environ.get('GROUPS_TABLE_NAME', 'FingerprintGroups')
    ttl_seconds = int(os.environ.get('TTL_SECONDS', 0))
    request_id = context.aws_request_id

    logger.info(f"Starting handler for request_id={request_id}, records={len(event.get('Records', []))}")

    results = []
    for record in event.get('Records', []):
        try:
            bucket = record['s3']['bucket']['name']
            key = record['s3']['object']['key']
            logger.info(f"Processing image={key}, request_id={request_id}")

            # Extract GroupId from filename (e.g., 101_1.tif -> GroupId=101)
            logger.info(f"Extracting GroupId from key={key} for service={service}")
            group_id = key.split('_')[0] if service == "dataset-extract" else None
            if service == "dataset-extract":
                logger.info(f"GroupId extracted: {group_id}")

            start_time = time.time()
            img = read_file_as_opencv(bucket, key)
            logger.info(f"Read image in {time.time() - start_time:.3f}s")

            start_time = time.time()
            crop, row, column, row_offset, column_offset = crop_image(img)
            logger.info(f"Crop image in {time.time() - start_time:.3f}s")

            start_time = time.time()
            enhanced, mask = create_enhanced_version(crop)
            logger.info(f"Enhance image in {time.time() - start_time:.3f}s")

            start_time = time.time()
            skeleton, binary_skeleton = get_image_skeletons(enhanced)
            logger.info(f"Generate skeletons in {time.time() - start_time:.3f}s")

            start_time = time.time()
            minutiae = calculate_minutiae(mask, skeleton, binary_skeleton)
            logger.info(f"Calculate minutiae in {time.time() - start_time:.3f}s, count={len(minutiae)}")

            center_x, center_y = get_centroid(minutiae)
            logger.info(f"Centroid: center_x={center_x}, center_y={center_y}")

            minutiae_array = np.array([(x, y, int(1 if term else 0), theta)
                                       for x, y, term, theta in minutiae], dtype=np.float32)
            minutiae_array = shift_minutiae(minutiae_array, int(column), int(row))
            center_x -= int(column)
            center_y -= int(row)

            metadata = {
                'width_shift': int(column),  # Convert numpy.int64 to int
                'height_shift': int(row),
                'offset_row': int(row_offset),
                'offset_col': int(column_offset),
                'center_x': center_x,  # Save centroid
                'center_y': center_y
            }

            start_time = time.time()
            save_minutiae_to_dynamo(table_name, key, minutiae_array, metadata, group_id,
                                    ttl_seconds if ttl_seconds > 0 else None)
            logger.info(f"Saved to DynamoDB in {time.time() - start_time:.3f}s")

            if group_id is not None:
                # Save GroupId to FingerprintGroups
                save_group_to_dynamo(groups_table_name, group_id)

            s3_client.delete_object(Bucket=bucket, Key=key)
            logger.info(f"Deleted image={key} from bucket={bucket}")

            results.append({'image': key, 'status': 'success', 'count': len(minutiae)})
        except Exception as e:
            logger.error(f"Error processing {key}, request_id={request_id}: {str(e)}")
            results.append({'image': key, 'status': 'error', 'message': str(e)})

    logger.info(f"Completed handler for request_id={request_id}, results={len(results)}")
    return {'statusCode': 200, 'body': json.dumps(results)}
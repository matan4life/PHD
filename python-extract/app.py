import json
import os
import time
import logging
from typing import Dict, List, Tuple, Optional, NamedTuple
from dataclasses import dataclass
import boto3
import numpy as np

# Импорт ОРИГИНАЛЬНЫХ проверенных алгоритмов (без изменений)
from fs_utils import read_file_as_opencv
from extract_steps import crop_image, create_enhanced_version, get_centroid, shift_minutiae
from cv_filter_utils import get_image_skeletons, calculate_minutiae
from dynamo_utils import save_minutiae_to_dynamo, save_group_to_dynamo

logger = logging.getLogger(__name__)
logger.setLevel(logging.INFO)
handler = logging.StreamHandler()
handler.setFormatter(logging.Formatter('%(asctime)s %(levelname)s %(message)s'))
logger.addHandler(handler)


class ProcessingMetrics(NamedTuple):
    load_time_ms: float
    crop_time_ms: float
    enhance_time_ms: float
    skeleton_time_ms: float
    minutiae_time_ms: float
    save_time_ms: float
    total_time_ms: float


@dataclass
class ProcessingResult:
    image_key: str
    success: bool
    minutiae_count: int = 0
    group_id: Optional[str] = None
    metrics: Optional[ProcessingMetrics] = None
    error_message: Optional[str] = None


def extract_group_id(filename: str, service_type: str) -> Optional[str]:
    if service_type != "dataset-extract":
        return None

    try:
        return filename.split('_')[0]
    except (IndexError, ValueError):
        logger.warning(f"Cannot extract group ID from filename: {filename}")
        return None


def process_fingerprint_image(bucket: str, key: str, service_type: str,
                             table_name: str, groups_table: str) -> ProcessingResult:
    start_total = time.time()
    s3_client = boto3.client('s3')

    try:
        logger.info(f"Processing image: {key}")

        group_id = extract_group_id(key, service_type)
        if group_id:
            logger.info(f"Extracted GroupId: {group_id}")

        step_start = time.time()
        img = read_file_as_opencv(bucket, key)
        load_time = (time.time() - step_start) * 1000
        logger.debug(f"Read image in {load_time:.1f}ms")

        step_start = time.time()
        crop, row, column, row_offset, column_offset = crop_image(img)
        crop_time = (time.time() - step_start) * 1000
        logger.debug(f"Crop image in {crop_time:.1f}ms")

        step_start = time.time()
        enhanced, mask = create_enhanced_version(crop)
        enhance_time = (time.time() - step_start) * 1000
        logger.debug(f"Enhance image in {enhance_time:.1f}ms")

        step_start = time.time()
        skeleton, binary_skeleton = get_image_skeletons(enhanced)
        skeleton_time = (time.time() - step_start) * 1000
        logger.debug(f"Generate skeletons in {skeleton_time:.1f}ms")

        step_start = time.time()
        minutiae = calculate_minutiae(mask, skeleton, binary_skeleton)
        minutiae_time = (time.time() - step_start) * 1000
        logger.debug(f"Calculate minutiae in {minutiae_time:.1f}ms, count={len(minutiae)}")

        center_x, center_y = get_centroid(minutiae)
        logger.debug(f"Centroid: center_x={center_x}, center_y={center_y}")

        minutiae_array = np.array([
            (x, y, int(1 if term else 0), theta)
            for x, y, term, theta in minutiae
        ], dtype=np.float32)

        minutiae_array = shift_minutiae(minutiae_array, int(column), int(row))
        center_x -= int(column)
        center_y -= int(row)

        metadata = {
            'width_shift': int(column),
            'height_shift': int(row),
            'offset_row': int(row_offset),
            'offset_col': int(column_offset),
            'center_x': center_x,
            'center_y': center_y
        }

        step_start = time.time()
        save_minutiae_to_dynamo(
            table_name, key, minutiae_array, metadata, group_id
        )

        if group_id:
            save_group_to_dynamo(groups_table, group_id)

        save_time = (time.time() - step_start) * 1000
        logger.debug(f"Saved to DynamoDB in {save_time:.1f}ms")

        s3_client.delete_object(Bucket=bucket, Key=key)
        logger.debug(f"Deleted {key} from bucket {bucket}")

        total_time = (time.time() - start_total) * 1000

        metrics = ProcessingMetrics(
            load_time_ms=load_time,
            crop_time_ms=crop_time,
            enhance_time_ms=enhance_time,
            skeleton_time_ms=skeleton_time,
            minutiae_time_ms=minutiae_time,
            save_time_ms=save_time,
            total_time_ms=total_time
        )

        logger.info(f"Successfully processed {key}: {len(minutiae)} minutiae in {total_time:.1f}ms")

        return ProcessingResult(
            image_key=key,
            success=True,
            minutiae_count=len(minutiae),
            group_id=group_id,
            metrics=metrics
        )

    except Exception as e:
        total_time = (time.time() - start_total) * 1000
        error_msg = str(e)
        logger.error(f"Error processing {key}: {error_msg}", exc_info=True)

        return ProcessingResult(
            image_key=key,
            success=False,
            error_message=error_msg,
            metrics=ProcessingMetrics(0, 0, 0, 0, 0, 0, total_time)
        )


def lambda_handler(event: Dict, context) -> Dict:
    table_name = os.environ['MINUTIAE_TABLE']
    groups_table = os.environ['GROUPS_TABLE']
    service_type = os.environ['SERVICE_TYPE']
    ttl_hours = int(os.environ.get('TTL_HOURS', '0'))
    ttl_seconds = ttl_hours * 3600 if ttl_hours > 0 else 0

    request_id = context.aws_request_id
    logger.info(f"Handler started: service={service_type}, request_id={request_id}")
    logger.info(f"Processing {len(event.get('Records', []))} S3 records")

    results: List[ProcessingResult] = []

    for record in event.get('Records', []):
        try:
            s3_data = record['s3']
            bucket = s3_data['bucket']['name']
            key = s3_data['object']['key']

            result = process_fingerprint_image(
                bucket=bucket,
                key=key,
                service_type=service_type,
                table_name=table_name,
                groups_table=groups_table
            )

            results.append(result)

        except Exception as e:
            logger.error(f"Error processing S3 record: {str(e)}", exc_info=True)
            results.append(ProcessingResult(
                image_key="unknown",
                success=False,
                error_message=f"Failed to parse S3 record: {str(e)}"
            ))

    successful = sum(1 for r in results if r.success)
    total_minutiae = sum(r.minutiae_count for r in results if r.success)
    avg_time = sum(r.metrics.total_time_ms for r in results if r.metrics) / len(results) if results else 0

    logger.info(f"Handler completed: {successful}/{len(results)} successful, "
               f"total_minutiae={total_minutiae}, avg_time={avg_time:.1f}ms")

    response_body = []
    for r in results:
        item = {
            'image': r.image_key,
            'success': r.success,
            'minutiae_count': r.minutiae_count
        }

        if r.group_id:
            item['group_id'] = r.group_id

        if r.metrics:
            item['processing_time_ms'] = round(r.metrics.total_time_ms, 1)
            item['performance'] = {
                'load_ms': round(r.metrics.load_time_ms, 1),
                'crop_ms': round(r.metrics.crop_time_ms, 1),
                'enhance_ms': round(r.metrics.enhance_time_ms, 1),
                'skeleton_ms': round(r.metrics.skeleton_time_ms, 1),
                'minutiae_ms': round(r.metrics.minutiae_time_ms, 1),
                'save_ms': round(r.metrics.save_time_ms, 1)
            }

        if r.error_message:
            item['error'] = r.error_message

        response_body.append(item)

    return {
        'statusCode': 200,
        'body': json.dumps({
            'summary': {
                'processed': len(results),
                'successful': successful,
                'total_minutiae': total_minutiae,
                'avg_processing_time_ms': round(avg_time, 1)
            },
            'results': response_body
        })
    }

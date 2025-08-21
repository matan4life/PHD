import json
import os

import boto3
from boto3.dynamodb.conditions import Key
import numpy as np
from minutia_types import MINUTIA_DTYPE_BASE, assign_unique_ids
from minutia_comparison import compare_images
from score_aggregation import aggregate_group_scores, evaluate_thresholds


def lambda_handler(event, context):
    """
    AWS Lambda handler for processing DynamoDB stream inserts.

    Processes probe insertion, compares against all groups, aggregates scores,
    evaluates thresholds, writes "YES"/"NO" to ResultsTable, and deletes probe.
    """
    dynamodb = boto3.resource('dynamodb')
    minutiae_table = os.environ['INPUT_TABLE_NAME']
    dataset_table = os.environ['DATASET_TABLE_NAME']
    group_table = os.environ['GROUP_TABLE_NAME']
    results_table = os.environ['RESULT_TABLE_NAME']

    try:
        for record in event['Records']:
            if record['eventName'] != 'INSERT':
                continue
            new_image = record['dynamodb']['NewImage']
            probe_image_id = new_image['ImageId']['S']
            probe_binary = new_image['MinutiaeBinary']['B']
            metadata = new_image['Metadata']['M']
            probe_center = (int(metadata['center_x']['N']), int(metadata['center_y']['N']))
            probe_minutiae = np.frombuffer(probe_binary, dtype=MINUTIA_DTYPE_BASE)

            # Assign IDs to probe
            probe_minutiae, current_id = assign_unique_ids(probe_minutiae, 0)

            # Initialize global caches (by ImageId)
            global_dist_cache = {}
            global_angle_cache = {}

            # Scan unique group IDs (optimize with GSI if available)
            scan_response = minutiae_table.scan(ProjectionExpression='GroupId')
            group_ids = set(item['GroupId'] for item in scan_response['Items'] if 'GroupId' in item)
            while 'LastEvaluatedKey' in scan_response:
                scan_response = minutiae_table.scan(ProjectionExpression='GroupId',
                                                    ExclusiveStartKey=scan_response['LastEvaluatedKey'])
                group_ids.update(item['GroupId'] for item in scan_response['Items'] if 'GroupId' in item)

            for group_id in group_ids:
                # Query galleries in group
                query_response = minutiae_table.query(KeyConditionExpression=Key('GroupId').eq(group_id))
                galleries = query_response['Items']
                while 'LastEvaluatedKey' in query_response:
                    query_response = minutiae_table.query(KeyConditionExpression=Key('GroupId').eq(group_id),
                                                          ExclusiveStartKey=query_response['LastEvaluatedKey'])
                    galleries.extend(query_response['Items'])

                group_scores = []
                group_size = 0
                for gallery in galleries:
                    if gallery['ImageId'] == probe_image_id:
                        continue
                    group_size += 1
                    gallery_image_id = gallery['ImageId']
                    gallery_binary = gallery['MinutiaeBinary']['B']
                    gallery_metadata = gallery['Metadata']['M']
                    gallery_center = (int(gallery_metadata['center_x']['N']), int(gallery_metadata['center_y']['N']))
                    gallery_minutiae = np.frombuffer(gallery_binary, dtype=MINUTIA_DTYPE_BASE)
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

                # Aggregate scores and evaluate
                normalized_pos, normalized_mea = aggregate_group_scores(group_scores, group_size)
                result = evaluate_thresholds(normalized_pos, normalized_mea)
                results_table.put_item(Item={
                    'ImageId': probe_image_id,
                    'GroupId': group_id,
                    'Result': result
                })

            # Delete probe entry
            minutiae_table.delete_item(Key={'ImageId': probe_image_id})

    except Exception as e:
        print(f"Error processing event: {str(e)}")
        return {'statusCode': 500, 'body': f"Error: {str(e)}"}

    return {'statusCode': 200}
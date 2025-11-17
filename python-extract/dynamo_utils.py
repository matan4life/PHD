import io
import time
import numpy as np
import boto3
from typing import Dict, Optional

dynamodb = boto3.resource('dynamodb')


def save_minutiae_to_dynamo(table_name: str, image_id: str, array: np.ndarray, metadata: Dict, group_id: Optional[str] = None, ttl_seconds: Optional[int] = None) -> None:
    """Save or update minutiae as compressed binary to DynamoDB.
    
    For publication: Optimized for size (compressed), reusable for dataset (persistent) or query (TTL).
    Updates existing item if ImageId exists, creates new otherwise.
    
    Args:
        table_name: DynamoDB table name (e.g., DatasetMinutiae, QueryMinutiae).
        image_id: Partition key (image filename).
        array: np.array [n,4] (X,Y,IsTermination,Theta).
        metadata: Dict with shifts/offsets/centroid (converted to Decimal for floats).
        ttl_seconds: Expire time for query table (None for persistent).
    """
    buffer = io.BytesIO()
    np.savez_compressed(buffer, data=array)
    buffer.seek(0)
    
    item = {
        'ImageId': image_id,
        'MinutiaeBinary': buffer.getvalue(),
        'Metadata': metadata,
        'Timestamp': int(time.time())
    }

    if group_id is not None:
        item['GroupId'] = group_id

    if ttl_seconds:
        item['TTL'] = int(time.time() + ttl_seconds)
    
    try:
        table = dynamodb.Table(table_name)
        table.update_item(
            Key={'ImageId': image_id},
            UpdateExpression="SET MinutiaeBinary = :mb, GroupId = :gi, Metadata = :md, #ts = :ts" + (", TTL = :ttl" if ttl_seconds else ""),
            ExpressionAttributeValues={
                ':mb': item['MinutiaeBinary'],
                ':md': item['Metadata'],
                ':ts': item['Timestamp'],
                ':gi': item.get('GroupId', None),
                **({':ttl': item['TTL']} if ttl_seconds else {})
            },
            ExpressionAttributeNames={'#ts': 'Timestamp'},
        )
    except Exception as e:
        raise RuntimeError(f"Failed to save/update {image_id} to {table_name}: {str(e)}")

def save_group_to_dynamo(table_name: str, group_id: Optional[str] = None) -> None:    
    try:
        dynamodb.Table(table_name).update_item(
            Key={'GroupId': group_id},
            UpdateExpression="SET #ts = :ts",
            ExpressionAttributeValues={':ts': int(time.time())},
            ExpressionAttributeNames={'#ts': 'Timestamp'}
        )
    except Exception as e:
        raise RuntimeError(f"Failed to save/update {group_id} to {table_name}: {str(e)}")

def load_minutiae_from_dynamo(table_name: str, image_id: str) -> np.ndarray:
    """Load and decompress minutiae. Reuse for comparator."""
    try:
        response = dynamodb.Table(table_name).get_item(Key={'ImageId': image_id})
        if 'Item' not in response:
            raise ValueError(f"No item for {image_id} in {table_name}")
        buffer = io.BytesIO(response['Item']['MinutiaeBinary'])
        return np.load(buffer)['data']
    except Exception as e:
        raise RuntimeError(f"Failed to load {image_id}: {str(e)}")
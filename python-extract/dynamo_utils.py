import io
import time
import numpy as np
import boto3
from typing import Dict, Optional

dynamodb = boto3.resource('dynamodb')

def save_minutiae_to_dynamo(table_name: str, image_id: str, array: np.ndarray, metadata: Dict, ttl_seconds: Optional[int] = None) -> None:
    """Save minutiae as compressed binary to DynamoDB.
    
    For publication: Optimized for size (compressed), reusable for dataset (persistent) or query (TTL).
    
    Args:
        table_name: DynamoDB table name (e.g., DatasetMinutiae, QueryMinutiae).
        image_id: Partition key (image filename).
        array: np.ndarray [n,4] (X,Y,IsTermination,Theta).
        metadata: Dict with shifts/offsets.
        ttl_seconds: Expire time for query table (None for persistent).
    """
    buffer = io.BytesIO()
    np.savez_compressed(buffer, data=array)
    buffer.seek(0)
    
    item = {
        'ImageId': image_id,
        'MinutiaeBinary': buffer.getvalue(),  # Binary auto-detected
        'Metadata': metadata,
        'Timestamp': int(time.time())
    }
    if ttl_seconds:
        item['TTL'] = int(time.time() + ttl_seconds)
    
    try:
        dynamodb.Table(table_name).put_item(Item=item)
    except Exception as e:
        raise RuntimeError(f"Failed to save {image_id} to {table_name}: {str(e)}")

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
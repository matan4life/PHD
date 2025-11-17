import os
import cv2
import numpy as np
import boto3

s3_client = boto3.client('s3')

def read_file_as_opencv(bucket: str, key: str) -> np.ndarray:
    """Read image from S3 as grayscale. Reusable for dataset/input."""
    temp_path = f"/tmp/{os.path.basename(key)}"
    try:
        s3_client.download_file(bucket, key, temp_path)
        img = cv2.imread(temp_path, cv2.IMREAD_GRAYSCALE)
        if img is None:
            raise ValueError(f"Failed to read {key}")
        return img
    finally:
        if os.path.exists(temp_path):
            os.remove(temp_path)
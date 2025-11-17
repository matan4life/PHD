"""
Module related to file-related jobs with opencv and provided docker volumes
"""
import os
import cv2
import numpy as np


def read_file_as_opencv(file_name: str, input_volume: str) -> np.ndarray:
    """
    Function to read a file and return an opencv image
    :param file_name: Name of file to read
    :param input_volume: Docker volume to read from
    :return: File in opencv format
    """
    path = f'{input_volume}/{file_name}'
    file = cv2.imread(path, cv2.IMREAD_GRAYSCALE)
    return file


def upload_opencv_file_to_fs(file_name: str, output_volume: str, file: np.ndarray) -> None:
    """
    Function to upload an opencv image to filesystem volume
    :param file_name: Name of file to upload
    :param output_volume: Volume to upload to
    :param file: File to upload
    """
    path = f'{output_volume}/{file_name}'
    cv2.imwrite(path, file)

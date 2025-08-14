import json
import os
import time

import numpy as np
import seaborn as sns
import pandas as pd

import pyodbc
from flask import Flask, request, g, current_app
from matplotlib import pyplot as plt
from simpleicp import PointCloud, SimpleICP
from sklearn import preprocessing, linear_model
from sklearn.cluster import KMeans
from sklearn.metrics import silhouette_score
from sklearn.model_selection import train_test_split

from cv_filter_utils import get_image_skeletons, calculate_minutiae
from extract_steps import crop_image, create_enhanced_version
from fs_utils import read_file_as_opencv, upload_opencv_file_to_fs

app = Flask(__name__)
conn_string = ('Driver={ODBC Driver 18 for SQL Server};SERVER=localhost;'
               'Database=Fingerprint;TrustServerCertificate=yes;Trusted_Connection=yes;Integrated Security=yes;')


@app.route('/image', methods=['POST'])
def process():
    image_id = request.args.get('image_id')
    test_run_id = request.args.get('test_run_id')
    context = pyodbc.connect(conn_string)
    cursor = context.cursor()
    image_name = cursor.execute(f"SELECT FileName FROM Fingerprint.dbo.Images WHERE Id = {image_id}").fetchone()[0]
    img = read_file_as_opencv(image_name, f'{os.environ["FLASK_INPUT"]}/{test_run_id}')
    crop, row, column, row_offset, column_offset = crop_image(img)
    cursor.execute(f"UPDATE Fingerprint.dbo.Images SET WidthShift={column}, HeightShift={row}, WidthOffset={column_offset}, HeightOffset={row_offset} WHERE Id={image_id}")
    enhanced, mask = create_enhanced_version(crop)
    upload_opencv_file_to_fs(image_name, f'{os.environ["FLASK_ENHANCED_OUTPUT"]}/{test_run_id}', enhanced)
    skeleton, binary_skeleton = get_image_skeletons(enhanced)
    upload_opencv_file_to_fs(image_name, f'{os.environ["FLASK_SKELETON_OUTPUT"]}/{test_run_id}', skeleton)
    minutiae = calculate_minutiae(mask, skeleton, binary_skeleton)
    for x, y, termination, theta in minutiae:
        termination_value = 1 if termination else 0
        cursor.execute(f"INSERT INTO Fingerprint.dbo.Minutiae (X, Y, IsTermination, Theta, ImageId) VALUES ({x+column}, {y+row}, {termination_value}, {theta}, '{image_id}')")
    cursor.execute(f"UPDATE Fingerprint.dbo.Images SET ProcessedCorrectly=1 WHERE Id={image_id}")
    context.commit()
    context.close()
    return json.dumps({'success': True}), 200, {'ContentType': 'application/json'}


if __name__ == '__main__':
    app.run(host='0.0.0.0')

FROM public.ecr.aws/lambda/python:3.12

# Install OpenCV runtime dependencies
RUN yum install -y libgl1-mesa-glx libglib2.0-0 libsm6 libxext6 libxrender1 && \
    yum clean all

COPY requirements.txt ${LAMBDA_TASK_ROOT}
COPY ../python-extract-utils/requirements.txt /tmp/utils-requirements.txt
RUN pip install --no-cache-dir -r requirements.txt -r /tmp/utils-requirements.txt

COPY . ${LAMBDA_TASK_ROOT}
COPY ../python-extract-utils/*.py ${LAMBDA_TASK_ROOT}/utils/

CMD ["app.handler"]
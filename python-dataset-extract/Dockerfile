FROM public.ecr.aws/lambda/python:3.12

# Install runtime dependencies for opencv-contrib-python-headless
RUN dnf install -y mesa-libGL mesa-libGL-devel libpng libjpeg-turbo libtiff libwebp libopenjp2 libxcb libxcb-devel libX11 libX11-devel libXrender libXrender-devel libXext libXext-devel libSM libSM-devel glib2 glib2-devel && \
    dnf clean all && \
    rm -rf /var/cache/dnf

# Copy requirements
COPY requirements.txt ${LAMBDA_TASK_ROOT}/requirements.txt
RUN pip install --no-cache-dir -r ${LAMBDA_TASK_ROOT}/requirements.txt

# Copy application code
COPY . ${LAMBDA_TASK_ROOT}/

# Set Lambda handler
CMD ["app.handler"]
FROM public.ecr.aws/lambda/python:3.12

# Install runtime dependencies for opencv-contrib-python-headless
RUN dnf install -y libglvnd-glx glib2 libSM libXext libXrender && \
    dnf clean all && \
    rm -rf /var/cache/dnf

# Copy and install requirements
COPY requirements.txt ${LAMBDA_TASK_ROOT}
RUN pip install --no-cache-dir -r requirements.txt

# Copy application code
COPY . ${LAMBDA_TASK_ROOT}
COPY ../python-extract-utils/*.py ${LAMBDA_TASK_ROOT}/utils/

# Set Lambda handler
CMD ["app.handler"]
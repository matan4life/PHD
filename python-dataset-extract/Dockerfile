FROM public.ecr.aws/lambda/python:3.12

# Install runtime dependencies for opencv-contrib-python-headless
RUN dnf install -y libglvnd-glx glib2 libSM libXext libXrender && \
    dnf clean all && \
    rm -rf /var/cache/dnf

# Copy requirements
COPY requirements.txt ${LAMBDA_TASK_ROOT}/requirements.txt
RUN pip install --no-cache-dir -r ${LAMBDA_TASK_ROOT}/requirements.txt

# Copy application code
COPY . ${LAMBDA_TASK_ROOT}/

# Set Lambda handler
CMD ["app.handler"]
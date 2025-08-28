# Stage 1: Install dependencies in amazonlinux:2
FROM amazonlinux:2 AS builder

# Install runtime dependencies for opencv-contrib-python-headless
RUN yum install -y libglvnd-glx glib2 libSM libXext libXrender libpng libjpeg-turbo libtiff openjpeg2 && \
    yum clean all && \
    rm -rf /var/cache/yum

# Debug: List installed packages
RUN yum list installed > /tmp/installed_packages.txt

# Stage 2: Build Lambda image
FROM public.ecr.aws/lambda/python:3.11

# Copy dependencies from builder
COPY --from=builder /usr/lib64/libglvnd-glx.so* /usr/lib64/
COPY --from=builder /usr/lib64/libglib-2.0.so* /usr/lib64/
COPY --from=builder /usr/lib64/libSM.so* /usr/lib64/
COPY --from=builder /usr/lib64/libXext.so* /usr/lib64/
COPY --from=builder /usr/lib64/libXrender.so* /usr/lib64/
COPY --from=builder /usr/lib64/libpng*.so* /usr/lib64/
COPY --from=builder /usr/lib64/libjpeg.so* /usr/lib64/
COPY --from=builder /usr/lib64/libtiff.so* /usr/lib64/
COPY --from=builder /usr/lib64/libopenjp2.so* /usr/lib64/
COPY --from=builder /tmp/installed_packages.txt /tmp/installed_packages.txt

# Copy requirements
COPY requirements.txt ${LAMBDA_TASK_ROOT}/requirements.txt
RUN pip install --no-cache-dir -r ${LAMBDA_TASK_ROOT}/requirements.txt

# Copy application code
COPY . ${LAMBDA_TASK_ROOT}/

# Set Lambda handler
CMD ["app.lambda_handler"]
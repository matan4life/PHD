FROM public.ecr.aws/lambda/python:3.11

# Установка системных зависимостей для OpenCV (оптимизированная версия)
RUN dnf update -y && \
    dnf install -y \
        libGL \
        glib2 \
        libgomp \
        mesa-libGL && \
    dnf clean all

# Копируем и устанавливаем Python зависимости
COPY requirements.txt ${LAMBDA_TASK_ROOT}
RUN pip install --no-cache-dir --upgrade pip && \
    pip install --no-cache-dir -r requirements.txt

# Копируем все модули приложения
COPY app.py ${LAMBDA_TASK_ROOT}
COPY cv_filter_utils.py ${LAMBDA_TASK_ROOT}
COPY extract_steps.py ${LAMBDA_TASK_ROOT}
COPY fs_utils.py ${LAMBDA_TASK_ROOT}
COPY dynamo_utils.py ${LAMBDA_TASK_ROOT}

# Устанавливаем Lambda handler
CMD ["app.lambda_handler"]

FROM python:3.12

RUN curl -fsSL https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor -o /usr/share/keyrings/microsoft-prod.gpg
RUN curl https://packages.microsoft.com/config/debian/12/prod.list | tee /etc/apt/sources.list.d/mssql-release.list
RUN apt-get update
RUN ACCEPT_EULA=Y apt-get install -y msodbcsql18
RUN ACCEPT_EULA=Y apt-get install -y mssql-tools18
RUN echo 'export PATH="$PATH:/opt/mssql-tools18/bin"' >> ~/.bashrc
RUN . ~/.bashrc
RUN apt-get install -y unixodbc-dev
RUN apt-get install -y libgssapi-krb5-2

RUN pip install Flask opencv-contrib-python-headless numpy numexpr pyodbc gunicorn

COPY app.py /app.py
COPY fs_utils.py /fs_utils.py
COPY cv_filter_utils.py /cv_filter_utils.py
COPY extract_steps.py /extract_steps.py

ENV FLASK_APP=app

CMD ["gunicorn", "--bind", "0.0.0.0:3000", "app:app"]
FROM python:3.11-slim
WORKDIR /app
COPY Tools/LearningApp/Online/requirements-production.txt ./requirements.txt
RUN pip install --no-cache-dir -r requirements.txt && useradd --uid 10001 --create-home deprem && mkdir /data && chown deprem:deprem /data
COPY Tools/LearningApp/Online/server.py ./server.py
COPY Assets/LearningApp/Resources/LearningApp/catalog.json ./catalog.json
ENV DEPREM_ONLINE_DB=/data/online.sqlite3 DEPREM_ONLINE_CATALOG=/app/catalog.json
USER deprem
EXPOSE 8765
CMD ["gunicorn", "--bind", "0.0.0.0:8765", "--workers", "1", "--threads", "8", "--timeout", "30", "server:application"]

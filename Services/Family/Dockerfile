FROM python:3.11-slim
ENV PYTHONDONTWRITEBYTECODE=1 PYTHONUNBUFFERED=1 FAMILY_DATABASE=/data/family.sqlite3
WORKDIR /app
COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt && useradd --uid 10001 --create-home family && mkdir /data && chown family:family /data
COPY server.py .
USER family
EXPOSE 8787
HEALTHCHECK --interval=30s --timeout=5s CMD python -c "import urllib.request; urllib.request.urlopen('http://127.0.0.1:8787/health',timeout=3)"
CMD ["python", "server.py", "--host", "0.0.0.0", "--port", "8787", "--trusted-proxy", "*"]

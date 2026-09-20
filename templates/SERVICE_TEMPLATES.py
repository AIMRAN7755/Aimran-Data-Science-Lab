"""
SERVICE TEMPLATES FOR AIMRAN DATA SCIENCE LAB
Deployment and orchestration configurations
"""

# ============================================================================
# DOCKER SERVICE TEMPLATES
# ============================================================================

DOCKER_SERVICE_TEMPLATE = """
version: '3.8'

services:
  # Main API Service
  api:
    build:
      context: .
      dockerfile: Dockerfile
    container_name: aimran-api
    ports:
      - "8000:8000"
    environment:
      - ENVIRONMENT=production
      - LOG_LEVEL=INFO
      - PYTHONUNBUFFERED=1
      - DATABASE_URL=postgresql://user:password@postgres:5432/aimran
    volumes:
      - ./data:/app/data
      - ./models:/app/models
      - ./logs:/app/logs
    depends_on:
      - postgres
      - redis
    networks:
      - aimran-network
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8000/health"]
      interval: 30s
      timeout: 10s
      retries: 3

  # Streamlit Dashboard
  dashboard:
    build:
      context: .
      dockerfile: Dockerfile.dashboard
    container_name: aimran-dashboard
    ports:
      - "8501:8501"
    environment:
      - PYTHONUNBUFFERED=1
    volumes:
      - ./data:/app/data
      - ./logs:/app/logs
    depends_on:
      - api
    networks:
      - aimran-network
    restart: unless-stopped

  # PostgreSQL Database
  postgres:
    image: postgres:15-alpine
    container_name: aimran-postgres
    environment:
      POSTGRES_USER: aimran
      POSTGRES_PASSWORD: secure_password_here
      POSTGRES_DB: aimran
    volumes:
      - postgres_data:/var/lib/postgresql/data
    networks:
      - aimran-network
    restart: unless-stopped
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U aimran"]
      interval: 10s
      timeout: 5s
      retries: 5

  # Redis Cache
  redis:
    image: redis:7-alpine
    container_name: aimran-redis
    ports:
      - "6379:6379"
    volumes:
      - redis_data:/data
    networks:
      - aimran-network
    restart: unless-stopped
    command: redis-server --appendonly yes

  # MLflow Tracking Server
  mlflow:
    image: ghcr.io/mlflow/mlflow:latest
    container_name: aimran-mlflow
    ports:
      - "5000:5000"
    volumes:
      - ./mlflow_data:/mlflow
    networks:
      - aimran-network
    restart: unless-stopped
    command: mlflow server --host 0.0.0.0 --backend-store-uri sqlite:////mlflow/mlflow.db

  # Nginx Reverse Proxy
  nginx:
    image: nginx:alpine
    container_name: aimran-nginx
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./nginx.conf:/etc/nginx/nginx.conf:ro
      - ./ssl:/etc/nginx/ssl:ro
    depends_on:
      - api
      - dashboard
    networks:
      - aimran-network
    restart: unless-stopped

volumes:
  postgres_data:
  redis_data:

networks:
  aimran-network:
    driver: bridge
"""

# ============================================================================
# KUBERNETES DEPLOYMENT TEMPLATES
# ============================================================================

K8S_API_DEPLOYMENT = """
apiVersion: apps/v1
kind: Deployment
metadata:
  name: aimran-api
  namespace: aimran-lab
spec:
  replicas: 3
  selector:
    matchLabels:
      app: aimran-api
  template:
    metadata:
      labels:
        app: aimran-api
    spec:
      containers:
      - name: api
        image: aimran-lab/api:0.2.0
        ports:
        - containerPort: 8000
        env:
        - name: ENVIRONMENT
          value: "production"
        - name: LOG_LEVEL
          value: "INFO"
        - name: DATABASE_URL
          valueFrom:
            secretKeyRef:
              name: aimran-secrets
              key: database-url
        - name: REDIS_URL
          valueFrom:
            secretKeyRef:
              name: aimran-secrets
              key: redis-url
        resources:
          requests:
            memory: "256Mi"
            cpu: "250m"
          limits:
            memory: "512Mi"
            cpu: "500m"
        livenessProbe:
          httpGet:
            path: /health
            port: 8000
          initialDelaySeconds: 30
          periodSeconds: 10
        readinessProbe:
          httpGet:
            path: /health
            port: 8000
          initialDelaySeconds: 5
          periodSeconds: 5
        volumeMounts:
        - name: data
          mountPath: /app/data
        - name: logs
          mountPath: /app/logs
      volumes:
      - name: data
        persistentVolumeClaim:
          claimName: aimran-data-pvc
      - name: logs
        persistentVolumeClaim:
          claimName: aimran-logs-pvc
---
apiVersion: v1
kind: Service
metadata:
  name: aimran-api-service
  namespace: aimran-lab
spec:
  type: LoadBalancer
  selector:
    app: aimran-api
  ports:
  - port: 80
    targetPort: 8000
---
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: aimran-api-hpa
  namespace: aimran-lab
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: aimran-api
  minReplicas: 2
  maxReplicas: 10
  metrics:
  - type: Resource
    resource:
      name: cpu
      target:
        type: Utilization
        averageUtilization: 70
  - type: Resource
    resource:
      name: memory
      target:
        type: Utilization
        averageUtilization: 80
"""

K8S_DASHBOARD_DEPLOYMENT = """
apiVersion: apps/v1
kind: Deployment
metadata:
  name: aimran-dashboard
  namespace: aimran-lab
spec:
  replicas: 2
  selector:
    matchLabels:
      app: aimran-dashboard
  template:
    metadata:
      labels:
        app: aimran-dashboard
    spec:
      containers:
      - name: dashboard
        image: aimran-lab/dashboard:0.2.0
        ports:
        - containerPort: 8501
        resources:
          requests:
            memory: "512Mi"
            cpu: "250m"
          limits:
            memory: "1Gi"
            cpu: "500m"
        livenessProbe:
          tcpSocket:
            port: 8501
          initialDelaySeconds: 30
          periodSeconds: 10
        volumeMounts:
        - name: data
          mountPath: /app/data
      volumes:
      - name: data
        persistentVolumeClaim:
          claimName: aimran-data-pvc
---
apiVersion: v1
kind: Service
metadata:
  name: aimran-dashboard-service
  namespace: aimran-lab
spec:
  type: LoadBalancer
  selector:
    app: aimran-dashboard
  ports:
  - port: 8501
    targetPort: 8501
"""

K8S_INGRESS = """
apiVersion: networking.k8s.io/v1
kind: Ingress
metadata:
  name: aimran-ingress
  namespace: aimran-lab
spec:
  ingressClassName: nginx
  tls:
  - hosts:
    - api.aimran-lab.com
    - dashboard.aimran-lab.com
    secretName: aimran-tls
  rules:
  - host: api.aimran-lab.com
    http:
      paths:
      - path: /
        pathType: Prefix
        backend:
          service:
            name: aimran-api-service
            port:
              number: 80
  - host: dashboard.aimran-lab.com
    http:
      paths:
      - path: /
        pathType: Prefix
        backend:
          service:
            name: aimran-dashboard-service
            port:
              number: 8501
"""

# ============================================================================
# SYSTEMD SERVICE TEMPLATES (LINUX)
# ============================================================================

SYSTEMD_API_SERVICE = """[Unit]
Description=Aimran Data Science Lab API Service
After=network.target
Wants=aimran-postgres.service aimran-redis.service

[Service]
Type=notify
User=aimran
WorkingDirectory=/opt/aimran-lab
Environment="PYTHONUNBUFFERED=1"
Environment="ENVIRONMENT=production"
Environment="LOG_LEVEL=INFO"
ExecStart=/opt/aimran-lab/venv/bin/python -m uvicorn src.api.main:app --host 0.0.0.0 --port 8000 --workers 4
Restart=always
RestartSec=10
StandardOutput=journal
StandardError=journal

[Install]
WantedBy=multi-user.target
"""

SYSTEMD_DASHBOARD_SERVICE = """[Unit]
Description=Aimran Data Science Lab Dashboard
After=network.target aimran-api.service

[Service]
Type=simple
User=aimran
WorkingDirectory=/opt/aimran-lab
Environment="PYTHONUNBUFFERED=1"
ExecStart=/opt/aimran-lab/venv/bin/python -m streamlit run ui/dashboard.py --server.port=8501
Restart=always
RestartSec=10
StandardOutput=journal
StandardError=journal

[Install]
WantedBy=multi-user.target
"""

SYSTEMD_POSTGRES_SERVICE = """[Unit]
Description=Aimran PostgreSQL Database
After=network.target

[Service]
Type=simple
User=postgres
ExecStart=/usr/lib/postgresql/15/bin/postgres -D /var/lib/postgresql/15/main -c config_file=/etc/postgresql/15/main/postgresql.conf
Restart=always
RestartSec=10

[Install]
WantedBy=multi-user.target
"""

# ============================================================================
# WINDOWS SERVICE TEMPLATE
# ============================================================================

WINDOWS_SERVICE_TEMPLATE = """
# Create Windows Service for Aimran Lab API
# Run as Administrator PowerShell

$ServiceName = "AimranLabAPI"
$DisplayName = "Aimran Data Science Lab API"
$BinaryPath = "C:\\aimran-lab\\venv\\Scripts\\python.exe -m uvicorn src.api.main:app --host 0.0.0.0 --port 8000"
$WorkingDirectory = "C:\\aimran-lab"

# Create the service
New-Service -Name $ServiceName `
    -DisplayName $DisplayName `
    -BinaryPathName $BinaryPath `
    -StartupType Automatic `
    -Description "Aimran Data Science Lab API Service"

# Set working directory
$ServiceObject = Get-WmiObject Win32_Service -Filter "Name='$ServiceName'"
$ServiceObject.PathName = "$BinaryPath"
$ServiceObject.Put() | Out-Null

# Start the service
Start-Service -Name $ServiceName

# Verify
Get-Service -Name $ServiceName
"""

# ============================================================================
# AWS DEPLOYMENT TEMPLATES
# ============================================================================

AWS_CLOUDFORMATION_TEMPLATE = """
AWSTemplateFormatVersion: '2010-09-09'
Description: 'Aimran Data Science Lab on AWS'

Parameters:
  InstanceType:
    Type: String
    Default: t3.large
    Description: EC2 instance type
  EnvironmentName:
    Type: String
    Default: production
    Description: Environment name

Resources:
  # EC2 Instance
  AimranLabInstance:
    Type: AWS::EC2::Instance
    Properties:
      ImageId: ami-0c55b159cbfafe1f0  # Ubuntu 22.04
      InstanceType: !Ref InstanceType
      KeyName: aimran-lab-key
      SecurityGroups:
        - !Ref AimranLabSecurityGroup
      UserData:
        Fn::Base64: |
          #!/bin/bash
          set -e
          apt-get update
          apt-get install -y python3.11 python3.11-venv docker.io
          cd /opt
          git clone https://github.com/your-org/aimran-lab.git
          cd aimran-lab
          python3.11 -m venv venv
          source venv/bin/activate
          pip install -r requirements.txt
          docker-compose up -d
      Tags:
        - Key: Name
          Value: aimran-lab-api
        - Key: Environment
          Value: !Ref EnvironmentName

  # Security Group
  AimranLabSecurityGroup:
    Type: AWS::EC2::SecurityGroup
    Properties:
      GroupDescription: Aimran Lab security group
      SecurityGroupIngress:
        - IpProtocol: tcp
          FromPort: 80
          ToPort: 80
          CidrIp: 0.0.0.0/0
        - IpProtocol: tcp
          FromPort: 443
          ToPort: 443
          CidrIp: 0.0.0.0/0
        - IpProtocol: tcp
          FromPort: 8000
          ToPort: 8000
          CidrIp: 0.0.0.0/0
        - IpProtocol: tcp
          FromPort: 8501
          ToPort: 8501
          CidrIp: 0.0.0.0/0

  # RDS PostgreSQL
  AimranDatabase:
    Type: AWS::RDS::DBInstance
    Properties:
      DBName: aimran
      Engine: postgres
      EngineVersion: '15.1'
      MasterUsername: aimran
      MasterUserPassword: !Sub '{{resolve:secretsmanager:aimran-db-password:SecretString:password}}'
      DBInstanceClass: db.t3.micro
      AllocatedStorage: '20'
      StorageType: gp2

  # ElastiCache Redis
  AimranCache:
    Type: AWS::ElastiCache::CacheCluster
    Properties:
      CacheNodeType: cache.t3.micro
      Engine: redis
      NumCacheNodes: 1
      Port: 6379

Outputs:
  InstancePublicIP:
    Value: !GetAtt AimranLabInstance.PublicIp
    Description: Public IP of Aimran Lab instance
  DatabaseEndpoint:
    Value: !GetAtt AimranDatabase.Endpoint.Address
    Description: RDS Database endpoint
"""

# ============================================================================
# DOCKER COMPOSE PRODUCTION TEMPLATE
# ============================================================================

DOCKER_COMPOSE_PRODUCTION = """
version: '3.8'

services:
  api:
    image: aimran-lab-api:0.2.0
    restart: always
    ports:
      - "8000:8000"
    environment:
      - ENVIRONMENT=production
      - WORKERS=4
      - LOG_LEVEL=WARNING
    depends_on:
      - db
      - cache
    networks:
      - aimran
    deploy:
      resources:
        limits:
          cpus: '1'
          memory: 1G
        reservations:
          cpus: '0.5'
          memory: 512M

  dashboard:
    image: aimran-lab-dashboard:0.2.0
    restart: always
    ports:
      - "8501:8501"
    depends_on:
      - api
    networks:
      - aimran
    deploy:
      resources:
        limits:
          cpus: '0.5'
          memory: 512M

  db:
    image: postgres:15-alpine
    restart: always
    environment:
      POSTGRES_DB: aimran
      POSTGRES_USER: aimran
      POSTGRES_PASSWORD_FILE: /run/secrets/db_password
    secrets:
      - db_password
    volumes:
      - db_data:/var/lib/postgresql/data
    networks:
      - aimran

  cache:
    image: redis:7-alpine
    restart: always
    ports:
      - "6379:6379"
    volumes:
      - cache_data:/data
    networks:
      - aimran

volumes:
  db_data:
  cache_data:

networks:
  aimran:
    driver: bridge

secrets:
  db_password:
    file: ./secrets/db_password.txt
"""

# ============================================================================
# MICROSERVICE TEMPLATES
# ============================================================================

DATA_PROCESSING_SERVICE = """
from fastapi import FastAPI, BackgroundTasks
from typing import Dict, Any
import asyncio

app = FastAPI(title="Data Processing Service")

# Service configuration
SERVICE_CONFIG = {
    "name": "data-processing",
    "version": "0.2.0",
    "port": 8001,
    "workers": 4,
    "timeout": 300
}

@app.get("/health")
async def health():
    return {"status": "healthy", "service": SERVICE_CONFIG["name"]}

@app.post("/process")
async def process_data(data_path: str, background_tasks: BackgroundTasks):
    '''Process data file asynchronously'''
    background_tasks.add_task(process_file_background, data_path)
    return {"status": "processing", "data_path": data_path}

async def process_file_background(data_path: str):
    '''Background processing task'''
    try:
        # Process file
        result = await process_async(data_path)
        return result
    except Exception as e:
        print(f"Processing error: {str(e)}")

async def process_async(path: str) -> Dict[str, Any]:
    '''Async data processing'''
    # Implementation
    pass

if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="0.0.0.0", port=SERVICE_CONFIG["port"])
"""

MODEL_SERVING_SERVICE = """
from fastapi import FastAPI
from typing import List
import numpy as np

app = FastAPI(title="Model Serving Service")

SERVICE_CONFIG = {
    "name": "model-serving",
    "version": "0.2.0",
    "port": 8002,
    "models": ["classifier", "regressor", "ensemble"]
}

class ModelCache:
    def __init__(self):
        self.models = {}

    def load_model(self, model_name: str):
        if model_name not in self.models:
            # Load model logic
            pass
        return self.models.get(model_name)

model_cache = ModelCache()

@app.get("/health")
async def health():
    return {"status": "healthy", "service": SERVICE_CONFIG["name"]}

@app.get("/models")
async def list_models():
    return {"available_models": SERVICE_CONFIG["models"]}

@app.post("/predict")
async def predict(model_name: str, features: List[float]):
    '''Make prediction using specified model'''
    model = model_cache.load_model(model_name)
    prediction = model.predict(np.array([features]))
    return {"prediction": prediction[0]}

if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="0.0.0.0", port=SERVICE_CONFIG["port"])
"""

# ============================================================================
# BACKGROUND WORKER TEMPLATE
# ============================================================================

BACKGROUND_WORKER_TEMPLATE = """
import asyncio
from celery import Celery
from src.data.loaders import DataLoader
from src.models.base_model import BaseModel

# Celery configuration
app = Celery(
    'aimran_workers',
    broker='redis://redis:6379/0',
    backend='redis://redis:6379/0'
)

app.conf.update(
    task_serializer='json',
    accept_content=['json'],
    result_serializer='json',
    timezone='UTC',
    enable_utc=True,
    task_track_started=True,
    task_time_limit=30 * 60,  # 30 minutes hard limit
)

# ============ TASKS ============

@app.task(bind=True, max_retries=3)
def process_dataset(self, dataset_path: str, output_path: str):
    '''Background task: Process dataset'''
    try:
        loader = DataLoader()
        df = loader.load_dataset(dataset_path, use_chunking=True)
        # Processing logic
        return {"status": "completed", "output": output_path}
    except Exception as exc:
        raise self.retry(exc=exc, countdown=60)

@app.task(bind=True, max_retries=3)
def train_model(self, config_dict: dict):
    '''Background task: Train model'''
    try:
        # Train model with given config
        return {"status": "trained", "config": config_dict}
    except Exception as exc:
        raise self.retry(exc=exc, countdown=60)

@app.task(bind=True)
def periodic_health_check(self):
    '''Periodic health check task'''
    from src.health_check import health_manager
    result = health_manager.full_report()
    return result

# Run periodic tasks
from celery.schedules import crontab

app.conf.beat_schedule = {
    'health-check-every-5-minutes': {
        'task': 'worker.periodic_health_check',
        'schedule': crontab(minute='*/5'),
    },
}
"""

# ============================================================================
# NGINX CONFIGURATION TEMPLATE
# ============================================================================

NGINX_CONFIG_TEMPLATE = """
user nginx;
worker_processes auto;
error_log /var/log/nginx/error.log warn;
pid /var/run/nginx.pid;

events {
    worker_connections 1024;
}

http {
    include /etc/nginx/mime.types;
    default_type application/octet-stream;

    log_format main '$remote_addr - $remote_user [$time_local] "$request" '
                    '$status $body_bytes_sent "$http_referer" '
                    '"$http_user_agent" "$http_x_forwarded_for"';

    access_log /var/log/nginx/access.log main;

    sendfile on;
    tcp_nopush on;
    tcp_nodelay on;
    keepalive_timeout 65;
    types_hash_max_size 2048;
    server_tokens off;

    # Gzip compression
    gzip on;
    gzip_vary on;
    gzip_proxied any;
    gzip_comp_level 6;
    gzip_types text/plain text/css text/xml text/javascript 
               application/json application/javascript application/xml+rss 
               application/rss+xml font/truetype font/opentype 
               application/vnd.ms-fontobject image/svg+xml;

    # Upstream API
    upstream api_backend {
        least_conn;
        server api:8000 max_fails=3 fail_timeout=30s;
        server api:8000 backup;
    }

    # Upstream Dashboard
    upstream dashboard_backend {
        least_conn;
        server dashboard:8501 max_fails=3 fail_timeout=30s;
    }

    # Redirect HTTP to HTTPS
    server {
        listen 80;
        server_name _;
        return 301 https://$host$request_uri;
    }

    # HTTPS Server
    server {
        listen 443 ssl http2;
        server_name api.aimran-lab.com;

        # SSL Configuration
        ssl_certificate /etc/nginx/ssl/cert.pem;
        ssl_certificate_key /etc/nginx/ssl/key.pem;
        ssl_protocols TLSv1.2 TLSv1.3;
        ssl_ciphers HIGH:!aNULL:!MD5;
        ssl_prefer_server_ciphers on;

        # API Proxy
        location / {
            proxy_pass http://api_backend;
            proxy_set_header Host $host;
            proxy_set_header X-Real-IP $remote_addr;
            proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
            proxy_set_header X-Forwarded-Proto $scheme;
            proxy_read_timeout 300s;
            proxy_connect_timeout 75s;
        }
    }

    server {
        listen 443 ssl http2;
        server_name dashboard.aimran-lab.com;

        ssl_certificate /etc/nginx/ssl/cert.pem;
        ssl_certificate_key /etc/nginx/ssl/key.pem;

        # Dashboard Proxy
        location / {
            proxy_pass http://dashboard_backend;
            proxy_set_header Host $host;
            proxy_set_header X-Real-IP $remote_addr;
            proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
            proxy_set_header X-Forwarded-Proto $scheme;
            proxy_http_version 1.1;
            proxy_set_header Upgrade $http_upgrade;
            proxy_set_header Connection "upgrade";
        }
    }
}
"""

if __name__ == "__main__":
    print("""
    SERVICE TEMPLATES FOR AIMRAN DATA SCIENCE LAB
    ============================================

    Available Templates:
    1. Docker Compose (Production) - docker-compose.yml
    2. Kubernetes Deployment - k8s_deployment.yaml
    3. Systemd Services (Linux) - systemd/*.service
    4. Windows Services - windows_service.ps1
    5. AWS CloudFormation - cloudformation.yaml
    6. Microservices - data-processing, model-serving
    7. Background Workers (Celery) - worker.py
    8. Nginx Proxy - nginx.conf

    To use these templates:
    1. Copy the template from this file
    2. Customize for your environment
    3. Deploy using appropriate tool
    4. Monitor and maintain
    """)

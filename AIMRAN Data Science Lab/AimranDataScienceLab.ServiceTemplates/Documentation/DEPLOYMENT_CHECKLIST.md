# AIMRAN Service Deployment Checklist

## Pre-Deployment Planning

### Requirements Gathering
- [ ] Identify service role (Analyst, Scientist, Engineer, AI Engineer, or Infrastructure)
- [ ] Document service dependencies
- [ ] Determine resource requirements
- [ ] Identify configuration needs (secrets, ConfigMaps)
- [ ] Plan high availability strategy
- [ ] Define monitoring and alerting requirements

### Architecture Review
- [ ] Review service design and architecture
- [ ] Validate service dependencies chain
- [ ] Plan capacity for peak load
- [ ] Review security implications
- [ ] Plan backup and recovery strategy
- [ ] Document operational procedures

---

## Template Generation

### Generate Service Templates
- [ ] Install service template generator: `dotnet tool install --global AimranDataScienceLab.ServiceTemplates`
- [ ] Create output directory: `mkdir -p deployment/generated`
- [ ] Generate templates:
  ```bash
  dotnet ServiceTemplateGenerator.dll generate \
	--name MyService \
	--label my-service \
	--role analyst \
	--output ./deployment/generated
  ```
- [ ] Review generated files
- [ ] Customize templates for your environment
- [ ] Verify template syntax

### Validate Generated Templates
- [ ] Run template validator: `dotnet ServiceTemplateValidator.dll validate ./deployment/generated`
- [ ] Check for errors and warnings
- [ ] Fix any identified issues
- [ ] Re-run validation to confirm

---

## Docker Deployment

### Local Development

#### Setup
- [ ] Install Docker: `docker --version`
- [ ] Install Docker Compose: `docker-compose --version`
- [ ] Create `.env` file from template
- [ ] Update environment variables

#### Build
- [ ] Build Docker image: `docker build -t aimran/my-service:latest .`
- [ ] Tag for local registry: `docker tag aimran/my-service:latest localhost:5000/aimran/my-service:v1.0.0`
- [ ] Verify image: `docker images | grep my-service`

#### Test
- [ ] Start containers: `docker-compose -f docker-compose.local.yml up -d`
- [ ] Wait for services to start: `sleep 10`
- [ ] Check logs: `docker-compose logs -f my-service`
- [ ] Test endpoints: `curl http://localhost:5010/health`
- [ ] Verify connectivity between services
- [ ] Run integration tests
- [ ] Stop containers: `docker-compose down`

### Production Deployment

#### Build and Push
- [ ] Build production image: `docker build -t aimran/my-service:v1.0.0 -f Dockerfile .`
- [ ] Test production image locally
- [ ] Tag for registry: `docker tag aimran/my-service:v1.0.0 registry.example.com/aimran/my-service:v1.0.0`
- [ ] Login to registry: `docker login registry.example.com`
- [ ] Push image: `docker push registry.example.com/aimran/my-service:v1.0.0`
- [ ] Verify push: `docker pull registry.example.com/aimran/my-service:v1.0.0`

#### Deploy Stack
- [ ] Update docker-compose.production.yml with production registry
- [ ] Initialize Docker Swarm: `docker swarm init` (if needed)
- [ ] Deploy stack: `docker stack deploy -c docker-compose.production.yml aimran`
- [ ] Verify deployment: `docker service ls`
- [ ] Check service status: `docker service ps aimran_my-service`
- [ ] Monitor logs: `docker service logs aimran_my-service`

#### Health Checks
- [ ] Access service health endpoint
- [ ] Verify all replicas are running
- [ ] Test service-to-service communication
- [ ] Verify external access (if applicable)
- [ ] Monitor resource usage

---

## Kubernetes Deployment

### Prerequisites
- [ ] Install kubectl: `kubectl version --client`
- [ ] Configure kubeconfig: `kubectl config use-context your-context`
- [ ] Verify cluster access: `kubectl get nodes`
- [ ] Verify Kubernetes version compatibility

### Infrastructure Setup
- [ ] Create namespace: `kubectl apply -f deployment/kubernetes/namespace-ingress-rbac.yaml`
- [ ] Verify namespace: `kubectl get namespace aimran`
- [ ] Create RBAC resources: `kubectl apply -f deployment/kubernetes/namespace-ingress-rbac.yaml`
- [ ] Verify RBAC: `kubectl get serviceaccounts -n aimran`

### Configuration Setup
- [ ] Create ConfigMaps: `kubectl apply -f deployment/kubernetes/secrets-configmaps.yaml`
- [ ] Verify ConfigMaps: `kubectl get configmaps -n aimran`
- [ ] Create Secrets: `kubectl apply -f deployment/kubernetes/secrets-configmaps.yaml`
- [ ] Verify Secrets: `kubectl get secrets -n aimran`

### Service Deployment
- [ ] Deploy service: `kubectl apply -f deployment/kubernetes/kubernetes-my-service.yaml`
- [ ] Verify deployment: `kubectl get deployments -n aimran`
- [ ] Check pod status: `kubectl get pods -n aimran -l app=my-service`
- [ ] Verify service: `kubectl get svc -n aimran`

### Advanced Features Setup
- [ ] Deploy Horizontal Pod Autoscaler: `kubectl apply -f deployment/kubernetes/hpa-template.yaml`
- [ ] Verify HPA: `kubectl get hpa -n aimran`
- [ ] Deploy Network Policies: `kubectl apply -f deployment/kubernetes/network-policy-template.yaml`
- [ ] Verify Network Policies: `kubectl get networkpolicies -n aimran`
- [ ] Deploy Pod Disruption Budget: `kubectl apply -f deployment/kubernetes/pod-disruption-budget.yaml`
- [ ] Setup Ingress: `kubectl apply -f deployment/kubernetes/ingress-template.yaml`

### Verification
- [ ] Check pod logs: `kubectl logs deployment/my-service -n aimran`
- [ ] Describe deployment: `kubectl describe deployment my-service -n aimran`
- [ ] Get pod details: `kubectl get pods -n aimran -o wide`
- [ ] Test service endpoint: `kubectl port-forward svc/my-service 5000:5000 -n aimran`
- [ ] Curl endpoint: `curl http://localhost:5000/health`
- [ ] Check events: `kubectl get events -n aimran`

---

## Windows Service Deployment

### Build
- [ ] Build release: `dotnet build -c Release`
- [ ] Publish: `dotnet publish -c Release -o ./bin/Release/publish`
- [ ] Verify output

### Setup
- [ ] Copy publish files to target directory: `Copy-Item ./bin/Release/publish -Destination 'C:\Services\MyService' -Recurse`
- [ ] Create Windows Service:
  ```powershell
  New-Service -Name "AimranMyService" `
	-DisplayName "AIMRAN My Service" `
	-BinaryPathName "C:\Services\MyService\MyService.exe"
  ```
- [ ] Verify service created: `Get-Service AimranMyService`

### Configuration
- [ ] Copy appsettings.Production.json to service directory
- [ ] Update configuration for Windows environment
- [ ] Set service to start automatically: `Set-Service AimranMyService -StartupType Automatic`
- [ ] Setup event log source: `New-EventLog -LogName Application -Source AimranMyService` (if needed)

### Testing
- [ ] Start service: `Start-Service AimranMyService`
- [ ] Verify running: `Get-Service AimranMyService`
- [ ] Check logs: `Get-EventLog -LogName Application -Source AimranMyService`
- [ ] Test connectivity
- [ ] Stop service: `Stop-Service AimranMyService`
- [ ] Remove test service: `Remove-Service AimranMyService`

### Production Deployment
- [ ] Deploy to production server
- [ ] Follow testing steps
- [ ] Setup monitoring and alerting
- [ ] Document recovery procedures

---

## Post-Deployment Verification

### Health Checks
- [ ] Service is responding to health endpoint
- [ ] All replicas/instances are running
- [ ] Service can reach its dependencies
- [ ] Service is discoverable in registry

### Connectivity Tests
- [ ] Service-to-service communication works
- [ ] External API access (if needed)
- [ ] Database connectivity
- [ ] Message queue connectivity

### Configuration Verification
- [ ] Environment variables are loaded correctly
- [ ] Configuration matches expectations
- [ ] Secrets are properly mounted/injected
- [ ] Feature flags are set correctly

### Monitoring Setup
- [ ] Metrics collection is working
- [ ] Logs are being aggregated
- [ ] Health checks are being monitored
- [ ] Alerts are configured
- [ ] Dashboards are displaying data

### Performance Baseline
- [ ] Record baseline performance metrics
- [ ] No unusual error rates
- [ ] Resource usage is within expectations
- [ ] Response times are acceptable

---

## Operation and Maintenance

### Daily Operations
- [ ] Monitor service health
- [ ] Check error rates and logs
- [ ] Verify resource usage
- [ ] Monitor external dependencies

### Regular Maintenance
- [ ] Update dependencies (weekly)
- [ ] Review and optimize logs (weekly)
- [ ] Update security patches (as released)
- [ ] Review performance metrics (weekly)
- [ ] Backup state if applicable (daily/weekly)

### Incident Response
- [ ] Document any errors or issues
- [ ] Capture logs and diagnostics
- [ ] Follow incident procedures
- [ ] Post-mortem analysis
- [ ] Update procedures based on learnings

### Version Updates
- [ ] Build new version
- [ ] Test in staging
- [ ] Plan deployment window
- [ ] Deploy gradually (canary/blue-green)
- [ ] Monitor new version
- [ ] Rollback procedure ready

### Scaling Operations
- [ ] Monitor resource utilization
- [ ] Adjust replica counts as needed
- [ ] Monitor HPA metrics and scaling
- [ ] Update resource limits if needed
- [ ] Document any permanent changes

---

## Rollback Procedures

### Docker Rollback
```bash
# Stop current deployment
docker service rm aimran_my-service

# Deploy previous version
docker stack deploy -c docker-compose.production.<prev-version>.yml aimran

# Verify
docker service ps aimran_my-service
```

### Kubernetes Rollback
```bash
# View rollout history
kubectl rollout history deployment/my-service -n aimran

# Rollback to previous revision
kubectl rollout undo deployment/my-service -n aimran

# Or rollback to specific revision
kubectl rollout undo deployment/my-service --to-revision=2 -n aimran

# Verify rollback
kubectl rollout status deployment/my-service -n aimran
```

### Windows Service Rollback
```powershell
# Stop service
Stop-Service AimranMyService

# Replace with previous version
Copy-Item 'C:\Services\MyService.Backup\*' -Destination 'C:\Services\MyService' -Recurse -Force

# Start service
Start-Service AimranMyService

# Verify
Get-Service AimranMyService
```

---

## Decommissioning

### Docker Shutdown
- [ ] Drain service of connections
- [ ] Stop service: `docker service rm aimran_my-service`
- [ ] Remove volumes if persistent: `docker volume rm aimran_my-service-data`
- [ ] Remove image if no longer needed

### Kubernetes Shutdown
- [ ] Scale down: `kubectl scale deployment my-service --replicas=0 -n aimran`
- [ ] Delete deployment: `kubectl delete deployment my-service -n aimran`
- [ ] Delete service: `kubectl delete svc my-service -n aimran`
- [ ] Delete PVC if applicable: `kubectl delete pvc -n aimran -l app=my-service`

### Windows Service Shutdown
- [ ] Stop service: `Stop-Service AimranMyService`
- [ ] Disable startup: `Set-Service AimranMyService -StartupType Disabled`
- [ ] Remove service: `Remove-Service AimranMyService`
- [ ] Archive publish files for backup
- [ ] Clean up directory

---

## Sign-off

- [ ] Deployment verified and working
- [ ] All checks passed
- [ ] Monitoring and alerting configured
- [ ] Documentation updated
- [ ] Team trained on new service
- [ ] Stakeholder approval obtained
- [ ] Date and time of deployment: _______________
- [ ] Deployed by: _______________
- [ ] Verified by: _______________

---

## Notes and Issues

Document any issues encountered or special considerations:

```
_____________________________________________________________________
_____________________________________________________________________
_____________________________________________________________________
_____________________________________________________________________
```

# Workflow Service with Durable Tasks

This project demonstrates a workflow service using Azure Durable Tasks in a WebHost environment (not Azure Functions). It provides REST API endpoints to manage workflow orchestrations.

## Features

- **Timer-based processing** with Service Bus integration
- **Durable Task orchestrations** with multiple activities
- **REST API endpoints** for workflow management
- **Swagger documentation** for API exploration
- **Health checks** for monitoring

## API Endpoints

### Start Workflow
```
POST /api/workflow/start
Content-Type: application/json

{
  "input": "Test workflow",
  "instanceId": "optional-custom-id"
}
```

### Get Workflow Status
```
GET /api/workflow/status/{instanceId}
```

### Terminate Workflow
```
POST /api/workflow/terminate/{instanceId}
Content-Type: application/json

{
  "reason": "User requested termination"
}
```

### List Workflow Instances
```
GET /api/workflow/instances?runtimeStatus=Running&top=10
```

## Configuration

Update your `appsettings.json` or environment variables:

```json
{
  "ConnectionStrings": {
    "AzureWebJobsStorage": "DefaultEndpointsProtocol=https;AccountName=...;AccountKey=...;EndpointSuffix=core.windows.net",
    "ServiceBus": "Endpoint=sb://your-namespace.servicebus.windows.net/;SharedAccessKeyName=...;SharedAccessKey=..."
  }
}
```

## Usage Examples

### Using curl:

```bash
# Start a workflow
curl -X POST "https://localhost:5001/api/workflow/start" \
  -H "Content-Type: application/json" \
  -d '{"input": "Hello World"}'

# Check workflow status
curl "https://localhost:5001/api/workflow/status/your-instance-id"

# List running workflows
curl "https://localhost:5001/api/workflow/instances?runtimeStatus=Running"
```

### Using PowerShell:

```powershell
# Start a workflow
$body = @{
    input = "Hello World"
    instanceId = "my-custom-id"
} | ConvertTo-Json

Invoke-RestMethod -Uri "https://localhost:5001/api/workflow/start" `
  -Method POST `
  -Body $body `
  -ContentType "application/json"

# Check status
Invoke-RestMethod -Uri "https://localhost:5001/api/workflow/status/my-custom-id"
```

## Development

1. **Setup local storage emulator** (Azurite) for development
2. **Configure Service Bus** connection string
3. **Run the application**: `dotnet run`
4. **Access Swagger UI**: `https://localhost:5001/swagger`

## Workflow Activities

The orchestrator `RunOrchestrator` executes three activities in sequence:
1. `SayHello` - Greets with the input
2. `ProcessData` - Processes the input data
3. `CompleteWorkflow` - Finalizes the workflow

Each activity can be extended with actual business logic as needed.

## Monitoring

- Health checks available at `/health`
- Structured logging with configurable levels
- Service Bus message processing with error handling
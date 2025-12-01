# API Access Guide

## Local Development

When running locally with `dotnet run`, the API is accessible at:

- **API Base URL**: `http://localhost:5166/api`
- **Swagger UI**: `http://localhost:5166/swagger/index.html`

## Production Deployment

After deploying with `./deploy.sh`, the API is accessible at:

- **API Base URL**: `https://api.your-domain.com/api`
- **Swagger UI**: `https://api.your-domain.com/swagger/index.html`

Replace `your-domain.com` with your actual domain configured in `deploy.config`.

## Example API Endpoints

### Get Service List
```
GET https://your-domain.com/api/ServiceList
```

### Validate XML
```
POST https://your-domain.com/api/Validator/validate
Content-Type: application/json

{
  "service": "Inventory",
  "version": "2.0.0",
  "operation": "getInventoryLevels",
  "xmlContent": "<your-xml-here>",
  "endpoint": "https://example.com/api"
}
```

### Get Sample Request
```
GET https://your-domain.com/api/Validator/sample-request?serviceName=Inventory&version=2.0.0&operationName=getInventoryLevels
```

### Get Response Schema
```
GET https://your-domain.com/api/Validator/response-schema?serviceName=Inventory&version=2.0.0&operationName=getInventoryLevels
```

## Notes

- Swagger is enabled in all environments for easy API documentation access
- All API requests are proxied through nginx at the `/api/` path
- HTTPS is enforced in production via Let's Encrypt certificates

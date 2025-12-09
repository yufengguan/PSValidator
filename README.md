# PSValidator
This application is a demo for PS Validator.
It was built using Antigravity AI according to Docs\PS_WebServiceValidator_Requirements_V02.md.

## How to Run the Application

### 1. Clone the Repository
```bash
git clone <repository-url>
cd PSValidator
```

### 2. Frontend App
```bash
cd App
npm install
npm run dev
```
Open the browser and navigate to http://localhost:5173

### 3. Backend API
```bash
cd Api
dotnet restore
dotnet build
dotnet run
```
API will be available at http://localhost:5166
Swagger UI: http://localhost:5166/swagger/index.html

### 4. StubServer (Mock PromoStandards Service)
The StubServer provides mock SOAP responses for testing the validator.

```bash
cd Tests/StubServer
dotnet restore
dotnet build
dotnet run
```
StubServer will be available at http://localhost:5086
Swagger UI: http://localhost:5086

**Endpoint Format:** `POST /api/{service}/{errorCode}`
- Example: `POST /api/PPC/ERR_MISSING_PRODUCTID` - Returns a mock response for PPC service with error code ERR_MISSING_PRODUCTID

### 5. Integration Tests
Integration tests validate the entire flow: Validator → StubServer → Validation Results

#### Run Tests Against Local Services (Default)
```bash
cd Tests/Integration
dotnet test
```

#### Run Tests Against Public Servers
```bash
cd Tests/Integration
dotnet test --environment Production
```

**Configuration:**
- **Local**: Uses `appsettings.Development.json`
  - API: `http://localhost:5166`
  - StubServer: `http://localhost:5086`
- **Production**: Uses `appsettings.Production.json`
  - API: `https://api.{domain}.com`
  - StubServer: `https://stubserver.{domain}.com`

## Deployment

### Deploy to AWS
```bash
# Copy the example to create your actual config
cp deploy.config.example deploy.config

# Edit with your real values
nano deploy.config

# Run the deploy script
./deploy.sh
```

### GitHub Actions
Push to `main` branch to trigger automatic deployment:
```bash
git add .
git commit -m "Your commit message"
git push origin main
```

## Project Structure
- **App/** - React frontend application
- **Api/** - .NET 8 backend API for validation
- **Tests/StubServer/** - Mock PromoStandards SOAP service
- **Tests/Integration/** - Integration tests
- **Docs/** - Documentation and mock XML responses
- **nginx/** - Nginx configuration templates

## Temporary Public URLs
- Frontend: https://demo18.com
- API: https://api.demo18.com
- StubServer: https://stubserver.demo18.com


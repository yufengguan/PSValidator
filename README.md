# PromoStandards SOAP Web Service Validator

The PromoStandards SOAP Web Service Validator is a testing tool designed to help developers validate their PromoStandards web service implementations. It provides real-time validation of XML requests and responses against official PromoStandards XSD schemas.

It was built using Antigravity AI according to [PS_WebServiceValidator_Requirements_V02.md](Docs/PS_WebServiceValidator_Requirements_V02.md). Not a single line of code was written by hand. The code might not be the best or fully DRY, but it works and passes all tests, which were written by Antigravity AI as well.

## Architecture

The system consists of five main components:
*   **Validator API (.NET 8)**: The core validation engine that parses XML/SOAP messages and checks them against standard schemas.
*   **Frontend (React/Vite)**: A user-friendly web interface for real-time validation and feedback.
*   **StubServer**: A mock PromoStandards service capable of simulating various API responses (both success and error scenarios) for testing purposes.
*   **Integration Tests**: A comprehensive suite that verifies the end-to-end flow (Validator → StubServer).
*   **Seq**: Centralized logging server for real-time diagnostics and performance monitoring. Note: This will be upgraded to AWS CloudWatch due to licensing costs.

---

## Getting Started

### Prerequisites For Local Development
  * Git
  * Node.js (v18+)
  * .NET 8 SDK
  * Docker

### Frontend App
The User Interface for interacting with the validator.
```bash
cd App
npm install
npm run dev
```
*   **URL**: `http://localhost:5173`

### Backend API
The core validation service.
```bash
cd Api
dotnet restore
dotnet run
```
*   **URL**: `http://localhost:5166`
*   **Swagger**: `http://localhost:5166/swagger`

### Logging (Seq)
The centralized logging server.
```bash
docker compose up -d seq
```
*   **URL**: `http://localhost:5341`

---

## Testing

### Frontend Tests

#### Unit Tests (Vitest)
Located in `App/tests/unit`. These tests check individual components and logic.
```bash
cd App
npm run test          # Run tests once
npm run test:ui       # Open interactive test UI
npm run test:coverage # Generate coverage report
```

#### E2E Tests (Playwright)
Located in `App/tests/e2e`. These tests simulate real user interactions.
```bash
cd App
npx playwright test
```


### Backend Integration Tests
Integration tests verify the end-to-end flow: **Validator → Simulated API (StubServer) → Validation Results**.

#### Run Locally
##### StubServer (Mock Service)
The StubServer simulates PromoStandards endpoints for testing. 
It can be run independently:
```bash
cd Tests/StubServer
dotnet restore
dotnet run
```
*   **URL**: `http://localhost:5086`
*   **Swagger**: `http://localhost:5086/swagger`

##### Run Integration Tests
```bash
cd Tests/Integration
dotnet test
```

## Docker Support

You can run App and Api services using Docker. This ensures an environment identical to production.

### Run Full Stack (App + API)
```bash
docker compose up --build
```
*   **Frontend**: `http://localhost` (Port 80)
*   **API**: `http://localhost:5000/swagger/index.html`
*   **Seq (Logs)**: `http://localhost:5341`

> [!IMPORTANT]
> **Docker Networking**: When the Validator API (in Docker) calls the StubServer (in Docker), you **cannot** use `localhost`. 
> Use the internal service name and internal port instead:
> *   **Use**: `http://stubserver:8080/api/PD/2.0.0/success`
> *   **Avoid**: `http://localhost:5001/...` (Connection Refused)
*   **StubServer**: `http://localhost:5001/index.html`
---
### Run Individually (Docker)
Frontend (App):
```bash
docker compose up --build -d app
```
API:
```bash
docker compose up --build -d api
```
SEQ:
```bash
docker compose up --build -d seq
```
StubServer:
```bash
docker compose up --build -d stubserver
```

## Deployment

The project includes an inspection-ready automated deployment script `deploy.sh` for Linux/AWS environments.

1.  **Configure**:
    ```bash
    cp deploy.config.example deploy.config
    # Edit 'deploy.config' with your server details (Domain, Email, etc.)
    ```

2.  **Deploy**:
    ```bash
    ./deploy.sh
    ```

---

## Project Structure

| Directory | Description |
| :--- | :--- |
| **Api/** | Core .NET 8 Web API for validation logic. |
| **App/** | React + Vite frontend application. |
| **Docs/** | Project documentation, mock XMLs, and schema references. |
| **Tests/Integration/** | End-to-end integration tests. |
| **Tests/StubServer/** | Mock web service for simulation. |
| **nginx/** | Nginx configuration templates for reverse proxy. |

---

## Demo Environment

*   **Frontend**: [https://demo18.com](https://demo18.com)
*   **API**: [https://api.demo18.com](https://api.demo18.com)
*   **StubServer**: [https://stubserver.demo18.com](https://stubserver.demo18.com)
*   **Seq Logs**: [https://logs.demo18.com](https://logs.demo18.com)

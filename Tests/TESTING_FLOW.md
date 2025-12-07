# Integration Testing Strategy

## Overview

We use a **Mock Service Provider (StubServer)** to simulate real PromoStandards suppliers. This allows us to test the Validator's logic end-to-end without relying on external third-party services.

## The Testing Flow

```
┌─────────────────────────────────────────────────────────────┐
│                   Integration Test                          │
│        (in Tests.Integration Project)                       │
└─────────────────────────────────────────────────────────────┘
                            │
                            │ 1. POST /api/Validator/validate
                            │    Payload:
                            │    {
                            │      "Service": "PPC",
                            │      "Endpoint": "http://localhost:5000/PPC/INVALID-WSVERSION"
                            │    }
                            ▼
┌─────────────────────────────────────────────────────────────┐
│                   Validator API                             │
│             (in Api Project)                                │
└─────────────────────────────────────────────────────────────┘
                            │
                            │ 2. API sees "Endpoint", wraps XML in SOAP,
                            │    and calls the StubServer URL.
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│                   Stub Server                               │
│           (in Tests.StubServer Project)                     │
│        POST /PPC/INVALID-WSVERSION                          │
└─────────────────────────────────────────────────────────────┘
                            │
                            │ 3. MockController reads "Docs/MockXMLResponses/PPC/mock_responses.json"
                            │    Finds entry for "INVALID-WSVERSION"
                            │    Returns: <GetConfigurationAndPricingResponse>... (Missing wsVersion)
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│                   Validator API                             │
└─────────────────────────────────────────────────────────────┘
                            │
                            │ 4. Validates the response against Official XSDs.
                            │ 5. Returns ValidationResult (isValid: false)
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│                   Integration Test                          │
└─────────────────────────────────────────────────────────────┘
      6. Asserts that result.IsValid == false
      7. Asserts that result.Messages contains expected error.
```

## Implementation Details

### 1. Mock Controller (`Tests.StubServer`)

The `MockController` is a standalone ASP.NET Core app that mimicks a SOAP service.
- **Route**: `/{service}/{errorCode?}`
- **Logic**:
  - Looks up `errorCode` in `Docs\MockXMLResponses\{service}\mock_responses.json`.
  - Returns the specific XML file associated with that error code (e.g., `PPC-1.0.0-GetConfig-ErrorVersion.xml`).
  - If no error code is provided, it tries to parse the operation from the body and return a default "happy path" response.

### 2. Integration Tests (`Tests.Integration`)

The tests drive the entire process.
- **Project**: `PromoStandards.Validator.Tests.Integration`
- **Key Test Class**: `ValidatorWithMockResponsesTests.cs` (or `ValidatorWithMockServiceTests.cs`)
- **Methodology**:
  - Reads the *same* `mock_responses.json` config that the StubServer uses.
  - Iterates through every defined test case.
  - Sends a standard validation request to the API, pointing the `Endpoint` to the StubServer with the specific `errorCode`.
  - Verifies that the API correctly identifies the defect introduced in the mock XML.

## How to Run

1.  **Start the StubServer**:
    ```bash
    dotnet run --project Tests/StubServer/PromoStandards.Validator.Tests.StubServer.csproj
    ```
    (Ensure it runs on the port expected by tests, e.g., 5000)

2.  **Run the API**:
    (Usually the Integration Test spins this up via `WebApplicationFactory` or you run it manually)

3.  **Run Tests**:
    ```bash
    dotnet test Tests/Integration/PromoStandards.Validator.Tests.Integration.csproj
    ```

## Adding New Test Cases

1.  **Create Bad XML**: Add a new XML file in `Docs\MockXMLResponses\{Service}` (e.g., `MyService-Error-MissingField.xml`).
2.  **Update Config**: Add an entry to `mock_responses.json`:
    ```json
    {
      "ErrorCode": "MISSING-FIELD",
      "StubResponseFile": "MyService-Error-MissingField.xml",
      "ExpectedError": "Element 'wsVersion' is missing"
    }
    ```
3.  **Run Tests**: The integration test suite will automatically pick up the new case and execute it.
   dotnet test Tests\Integration\PromoStandards.Validator.Tests.Integration.csproj --no-build > test_output.txt 2>&1
   

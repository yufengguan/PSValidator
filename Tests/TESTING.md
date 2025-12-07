# PromoStandards Validator - Testing Projects

This document describes the testing structure for the PromoStandards Web Service Validator application, aligned with **Section 5** of the requirements document.

## Testing Architecture

We have **two separate testing projects**:

1. **Api.Tests** - Backend/API testing (.NET xUnit)
2. **App/src/tests** - Frontend/UI testing (Vitest + React Testing Library)

This separation allows for:
- Independent test execution
- Technology-appropriate testing frameworks
- Clear separation of concerns
- Easier CI/CD integration

---

## 1. API Testing (Api.Tests)

### Technology Stack
- **xUnit** - Test framework
- **FluentAssertions** - Assertion library
- **Moq** - Mocking framework
- **Microsoft.AspNetCore.Mvc.Testing** - Integration testing

### Test Structure

```
Api.Tests/
├── Controllers/
│   ├── ServiceListControllerTests.cs      # Section 5.1.1, 5.1.2
│   └── ValidatorControllerTests.cs        # Section 5.2, 5.3
├── Services/
│   └── XmlValidationServiceTests.cs       # Section 5.2
└── PromoStandards.Validator.Api.Tests.csproj
```

### Running API Tests

```bash
# Run all tests
cd Api.Tests
dotnet test

# Run with detailed output
dotnet test --logger "console;verbosity=detailed"

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Test Coverage

#### ServiceListController Tests (Section 5.1.1, 5.1.2)
- ✅ Service list retrieval
- ✅ Cascading dropdown data structure
- ✅ JSON format validation

#### ValidatorController Tests (Section 5.2, 5.3)
- ✅ XML schema validation
- ✅ Recursive schema validation (Section 3.8.3)
- ✅ Validation error reporting with line numbers and positions (Section 5.2.2)
- ✅ Error handling for invalid requests (Section 5.3.1)
- ✅ Client-side validation (Section 5.3.2)

#### XmlValidationService Tests (Section 5.2)
- ✅ Valid XML validation
- ✅ Invalid XML error detection
- ✅ Imported/included schema validation
- ✅ Nested element validation

---

## 2. Frontend Testing (App/src/tests)

### Technology Stack
- **Vitest** - Test runner
- **React Testing Library** - Component testing
- **@testing-library/user-event** - User interaction simulation
- **jsdom** - DOM environment

### Test Structure

```
App/src/tests/
├── setup.js                      # Test configuration
├── ServiceSelector.test.jsx      # Section 5.1.1
├── App.test.jsx                  # Section 5.1, 5.3, 5.4
├── ValidationPanel.test.jsx      # Section 5.2.2, 3.9
└── EndpointInput.test.jsx        # Section 3.5, 5.3.2
```

### Running Frontend Tests

```bash
# Run all tests
cd App
npm test

# Run tests in watch mode
npm test -- --watch

# Run tests with UI
npm run test:ui

# Run tests with coverage
npm run test:coverage
```

### Test Coverage

#### ServiceSelector Tests (Section 5.1.1)
- ✅ Cascading dropdown rendering
- ✅ Version filtering when service is selected
- ✅ Operation filtering when version is selected
- ✅ Selection change callbacks
- ✅ Reset behavior when service changes (Section 3.3.2)

#### App Integration Tests (Section 5.1, 5.3, 5.4)
- ✅ Service list fetching on mount (Section 5.1.3)
- ✅ Request and response XML display (Section 5.1.4)
- ✅ Validate button disabled state (Section 3.8.1, 5.3.2)
- ✅ Validation error display (Section 5.2.2)
- ✅ Unreachable endpoint error handling (Section 5.3.1)

#### ValidationPanel Tests (Section 5.2.2, 3.9)
- ✅ Success message display
- ✅ Validation errors with line numbers and positions (Section 3.9.2)
- ✅ Overall status display (Section 3.9.1)
- ✅ Multiple error handling

#### EndpointInput Tests (Section 3.5, 5.3.2)
- ✅ Endpoint input rendering
- ✅ User input handling
- ✅ Empty endpoint validation (Section 3.5.2)
- ✅ Unreachable endpoint error display (Section 3.5.2)
- ✅ Valid URL format acceptance

---

## Requirements Coverage Matrix

| Requirement | Test Location | Status |
|-------------|---------------|--------|
| 5.1.1 - Cascading dropdowns | `ServiceSelector.test.jsx` | ✅ |
| 5.1.2 - Example XML generation | `ServiceListControllerTests.cs` | ✅ |
| 5.1.3 - SOAP request/response | `App.test.jsx` | ✅ |
| 5.1.4 - UI panel display | `App.test.jsx` | ✅ |
| 5.2.1 - Schema validation | `ValidatorControllerTests.cs` | ✅ |
| 5.2.2 - Error details (line/position) | `ValidationPanel.test.jsx` | ✅ |
| 5.3.1 - Unreachable endpoint errors | `App.test.jsx` | ✅ |
| 5.3.2 - Client-side validation | `EndpointInput.test.jsx` | ✅ |
| 5.4.1 - Export functionality | TODO | ⏳ |
| 5.4.2 - Import functionality | TODO | ⏳ |
| 5.5.1 - PSServiceList.json updates | TODO | ⏳ |
| 5.5.2 - App page updates | TODO | ⏳ |
| 5.6.1 - Additional testing | Ongoing | ⏳ |

---

## CI/CD Integration

### GitHub Actions Example

```yaml
name: Tests

on: [push, pull_request]

jobs:
  api-tests:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: 8.0.x
      - name: Run API Tests
        run: |
          cd Api.Tests
          dotnet test --logger "trx;LogFileName=test-results.trx"

  frontend-tests:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - name: Setup Node
        uses: actions/setup-node@v3
        with:
          node-version: 20
      - name: Install dependencies
        run: |
          cd App
          npm ci
      - name: Run Frontend Tests
        run: |
          cd App
          npm test
```

---

## Best Practices

1. **Test Naming**: Use descriptive names that reference requirement sections
2. **Arrange-Act-Assert**: Follow AAA pattern for clarity
3. **Mocking**: Mock external dependencies (API calls, file system)
4. **Coverage**: Aim for >80% code coverage
5. **Documentation**: Comment tests with requirement section references

---

## Next Steps

1. ✅ Create API test project structure
2. ✅ Create Frontend test project structure
3. ✅ Implement core test cases for Section 5.1-5.3
4. ⏳ Implement export/import tests (Section 5.4)
5. ⏳ Implement update reflection tests (Section 5.5)
6. ⏳ Add E2E tests for complete workflows
7. ⏳ Set up CI/CD pipeline
8. ⏳ Add test coverage reporting

---

## Questions?

For questions about testing or to report issues, please refer to the main project README or contact the development team.

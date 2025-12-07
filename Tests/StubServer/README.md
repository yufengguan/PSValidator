# Stub Server Project
Tests.StubServer = a tiny “fake PromoStandards service”.
It should expose the same HTTP‑SOAP contract that a real PromoStandards service would expose, but instead of doing any real business logic it simply reads the appropriate file from Docs\MockXMLResponses\PPC and returns it.

Tests.Api then calls the real validator API exactly the way a client would call a real PromoStandards service by pointing the endpoint parameter at the stub server URL.


## Features

- **Dynamic Endpoints**: Automatically supports all services and operations defined in `PSServiceList.json`.
- **Code-Based Responses**: Returns different XML responses based on a `code` query parameter.
- **Validation**: Ensures that requested services and operations actually exist in the standard.
- **Swagger UI**: Explore and test endpoints interactively.

## Project Structure

```
StubServer/
├── Controllers/
│   └── StubController.cs       # Handles api/{service}/{operation} requests
├── Services/
│   ├── ServiceListProvider.cs  # Reads PSServiceList.json
│   └── MockResponseProvider.cs # Generates XML responses
├── Program.cs                  # App configuration
└── PromoStandards.StubServer.csproj
```

## How to Run

```bash
cd StubServer
dotnet run
```

The service will start (usually on http://localhost:5xxx). Check the console output for the URL.

## Usage

### Endpoint Format
```
POST /api/{Service}/{Operation}?code={code}
```

### Examples

**1. Get a Valid Response**
```
POST /api/OrderStatus/getOrderStatus?code=valid
```

**2. Get an Error Response (Missing Field)**
```
POST /api/OrderStatus/getOrderStatus?code=error-missing-wsversion
```

**3. Get a Timeout Simulation**
```
POST /api/OrderStatus/getOrderStatus?code=timeout
```

## Supported Codes

| Code | Description |
|------|-------------|
| `valid` | Returns a valid XML response for the operation |
| `error-missing-wsversion` | Returns XML missing the required `wsVersion` element |
| `error-wrong-namespace` | Returns XML with an incorrect namespace |
| `error-wrong-root` | Returns XML with an incorrect root element |
| `error-malformed` | Returns malformed (not well-formed) XML |
| `server-error` | Returns HTTP 500 Internal Server Error |
| `timeout` | Waits 30 seconds before responding |

## Extending

To add more specific responses, edit `Services/MockResponseProvider.cs`. You can add new codes or specific logic for different services.

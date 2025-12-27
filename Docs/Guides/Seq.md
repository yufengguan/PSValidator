# Using Seq for Logging

To verify the Seq log:

1. Start Docker Desktop.
2. Run `docker-compose up -d seq` in `PSValidator`.
3. Go to [http://localhost:5341](http://localhost:5341).

# Troubleshooting
    docker ps 
    docker logs --tail 20 psvalidator-seq

    curl http://localhost:5341/api/events/raw

# Filtering Logs

To filter logs in the Seq Query bar:

- **Errors only**: `@Level = 'Error'` or `@Level = 'Fatal'`
- **Exceptions**: `@Exception is not null`
- **Specific text**: `"some text"` (quotes required for phrases)
- **Combine filters**: `@Level = 'Error' and Application = 'PSValidator.Api'`

Common properties you can filter on include `@Message`, `@Level`, `@Timestamp`, and any structured property attached to the log event (e.g., `SourceContext`, `RequestId`).

# Distinguishing Issues

To tell the difference between a validation issue and a system crash:

## 1. Validation Failures (Bad Input)
- **Log Level**: `Warning`
- **Message**: "Response Validation Failed for..."
- **Meaning**: The API worked correctly, but the input data was invalid according to the schema.
- **Action**: Check the input data against the schema.

## 2. System Errors (API Crash/Exception)
- **Log Level**: `Error`
- **Message**: "System Error during validation..." or "FormatXml Failed..."
- **Meaning**: The API code encountered an unexpected exception (e.g., NullReference, XML Parsing crash).
- **Action**: Debug the API code. For `FormatXml Failed`, check the `XmlContent` property to see what input caused the crash.

# Analysis Report
## Dashboards:
    Open Seq (http://localhost:5341).
    Click Dashboards in the top navigation bar.
    Click + New Dashboard and name it "Usage Analytics".
    Click + Add Chart and copy-paste the queries from Docs/Seq_Dashboards.md

## Quick Reference (Queries)
### Active Users:
    select count(distinct(RefUserId)) from stream where RefUserId is not null group by time(1d)
### Traffic Volume:
    select count(*) from stream where EventType = 'ValidationCompleted' group by time(1h)
### Success vs Failure:
    select count(*) from stream where EventType = 'ValidationCompleted' group by IsValid, time(1h)

# ValidationCompleted Event Json payload:
{
  "@t": "2025-12-26T12:55:00.1234567Z",
  "@mt": "ValidationCompleted: {Service} {Version} {Operation} IsValid:{IsValid} Errors:{ErrorCount} DurationMs:{DurationMs} ExternalDurationMs:{ExternalDurationMs}",
  "Service": "ProductData",
  "Version": "2.0.0",
  "Operation": "getProduct",
  "IsValid": true,
  "ErrorCount": 0,
  "DurationMs": 150.5,
  "ExternalDurationMs": 45.2,
  "SessionId": "a1b2c3d4-...",
  "RefUserId": "ab12...",
  "SourceContext": "PromoStandards.Validator.Api.Services.ValidationResponseService",
  "RequestId": "0HMV...",
  "ConnectionId": "0HMV..."
}
## Key Fields Breakdown:
   1. @mt: The message template used for rendering the log text.
   2. C# Service, Version, Operation: The validation context context.
   3. IsValid: true or false.
   4. DurationMs: Total time (in milliseconds) the validator took.
   5. ExternalDurationMs: Time spent waiting for the HTTP POST to your endpoint (will be -1 if no endpoint was called).
   6. RefUserId: The hashed user ID (from your IP).
   7. SessionId: The header value from the frontend session.


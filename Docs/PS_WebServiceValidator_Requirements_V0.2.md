---
**Author:** Yufeng Guan, Senior Software Engineer for Starline Industries Inc.  
**Date:** October 20, 2025  
**Version:** V0.2  
**Contributors:** Erica Griffitt, Senior Software Engineer for Staples Promo
---

# PromoStandards Web Service Validator – Redevelopment Requirements Document

**Prepared for:** PromoStandards.org  
**Purpose:** Redevelop the existing Web Service Validator to improve usability, maintainability, and developer integration.

---

## 1. Overview

The purpose of this project is to rebuild the existing [PromoStandards Web Service Validator](https://promostandards.org/web-service-validator/) into a modern, extensible web application. The new validator will allow PromoStandards members to:
- Test their web service implementations for compliance with PromoStandards specifications
- Provide a trusted, authoritative source to resolve data disputes between members regarding service responses

This redevelopment aims to improve usability, maintainability, and integration for developers and organizations using PromoStandards.


## 2. System Architecture
The new system will be composed of two primary layers:

### 2.1 Frontend
2.1.1 Use a modern responsive web framework (React, Angular, Vue, etc.) with a modern web UI.
2.1.2 Allow users to select services, enter endpoints, edit requests, view responses, and review validation results.

### 2.2 Backend
2.2.1 Provide a RESTful API (Node.js, .NET, Python, etc.)
2.2.2 Process validation requests
2.2.3 Invoke SOAP endpoints to obtain XML responses
2.2.4 Validate XML responses against the official PromoStandards schema specified in the request
2.2.5 Provide Swagger/OpenAPI documentation

## 3. Basic Feature Requirements

### 3.1 Layout Overview
3.1.1 The application interface must provide a modern, developer‑friendly experience with the following layout:
3.1.2 Panel Structure:
  - Top row: Three cascading dropdowns for Web Service, Version, and Operation selection.
  - Endpoint input field directly below the dropdowns (full‑width).
  - Main content area: Vertically stacked panels for Request Body, Response Body, and Validation Result:
    +--------------------------------------------------------------------------+
    |  [Web Service ▼]     [Version ▼]     [Operation ▼]                       |
    +--------------------------------------------------------------------------+
    |  Endpoint: [ https://supplier.example.com/... ]                          |
    +--------------------------------------------------------------------------+
    |                             Request Body                                 |
    |  (XML editor with horizontal scroll)                                     |
    +--------------------------------------------------------------------------+
    |                             Response Body                                |
    |  (XML viewer with horizontal scroll)                                     |
    +--------------------------------------------------------------------------+
    |                           Validation Result                              |
    |  (Validation messages and status)                                        |
    +--------------------------------------------------------------------------+

### 3.2 General UI Requirements
    3.2.1 Full‑Width Editors: Use the full browser width for maximum readability and editing comfort.
    3.2.2 Independent Panels: Each main section (Request, Response, Validation Result) scrolls independently with clear, persistent labels.
    3.2.3 Auto‑Scroll & Resizable Panels: Enable auto‑scroll for long content and allow users to resize panels.
    3.2.4 Error Display: Return errors in a clear, consistent manner, displayed directly beneath the relevant panel or input.

### 3.3 Cascading Dropdowns (Web Service, Version, Operation)
    3.3.1 Three cascading dropdowns where options are dynamically filtered by the prior selection.
    3.3.2 When Service changes: clear all panels and Endpoint input textbox.
    3.3.3 When Operation changes: clear Request Body, Response Body, and Validation Result. Keep Endpoint unchanged.

### 3.4 Service List and Schema Structure
    3.4.1 The available services, versions, and operations are defined in attached PSServiceList.json.
    3.4.2 Authorized admins will use Bitbucket pipeline to push any changes (service additions/updates) to the PSServiceList.json file
    3.4.3 PromoStandards request and response schemas follow a pattern and are located at: https://promostandards.org/standards-services/{service}-{version}-{operation}.xsd.
          Example: https://promostandards.org/standards-services/OrderStatus/1-0-0/GetOrderStatusResponse.xsd

### 3.5 Endpoint Input Validation
    3.5.1 Text input for a user‑provided endpoint URL.
    3.5.2 Validation rules:
      - If empty: "Endpoint is required."
      - If unreachable or service call fails: display the service error message.

### 3.6 Request Body
    3.6.1 Provide an editable code window for users to modify XML requests.
    3.6.2 When the Operation changes, automatically generate and display a sample XML request for the selected operation based on its schema.
          For example, for OrderStatus V2 GetOrderStatusRequest, the generated XML should resemble:
            <GetOrderStatusRequest xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns="http://www.promostandards.org/WSDL/OrderStatus/2.0.0/">
              <wsVersion xmlns="http://www.promostandards.org/WSDL/OrderStatus/2.0.0/SharedObjects/">Token1</wsVersion>
              <id xmlns="http://www.promostandards.org/WSDL/OrderStatus/2.0.0/SharedObjects/">Token1</id>
              <password xmlns="http://www.promostandards.org/WSDL/OrderStatus/2.0.0/SharedObjects/">Token1</password>
              <queryType xmlns="http://www.promostandards.org/WSDL/OrderStatus/2.0.0/SharedObjects/">poSearch</queryType>
              <referenceNumber xmlns="http://www.promostandards.org/WSDL/OrderStatus/2.0.0/SharedObjects/">referenceNumber1</referenceNumber>
              <statusTimeStamp xmlns="http://www.promostandards.org/WSDL/OrderStatus/2.0.0/SharedObjects/">1900-01-01T01:01:01.0000000-05:00</statusTimeStamp>
              <returnIssueDetailType xmlns="http://www.promostandards.org/WSDL/OrderStatus/2.0.0/SharedObjects/">noIssues</returnIssueDetailType>
              <returnProductDetail xmlns="http://www.promostandards.org/WSDL/OrderStatus/2.0.0/SharedObjects/">true</returnProductDetail>
            </GetOrderStatusRequest>

### 3.7 Response Body Panel
    3.7.1 Display the raw XML response
    3.7.2 Generate an example XML response based on the selected service, version, and operation, and display it in a new window or modal. The contents should be similar to what is shown in the existing app when clicking the "example response body" link.

### 3.8 Validate Button Functionality
    3.8.1 The **Validate** button must be disabled if there are client-side validation errors.
    3.8.2 When clicked, validate the response XML returned from the endpoint against the selected response schema and display the results (errors or warnings) in the validation results panel. If valid, show a success message.
    3.8.2.1 API Process
      - Call the endpoint with the request to obtain a raw XML response for the selected operation.
      - Validate the XML response against the expected XML schema (XSD).
      - Return a `ValidationResult` object that must contain at least:
        - `isValid` (boolean)
        - `validationResultMessages` (array of messages)
    3.8.2.2 Error Handling
      - Catch and log all exceptions during validation and display messages to the user.
    3.8.3 Shared Schema Files and Comprehensive Recursive Validation
      - Many PromoStandards XSD files import or include shared element definitions from separate shared XSD files (e.g., `SharedObjectsOrderStatusV2.xsd`) using XML Schema namespaces. These shared XSDs define common elements and types that are referenced throughout the main service schemas. The validator must provide full, multi-level, recursive schema validation for all XML content, ensuring that every element—no matter how deeply nested or where it is defined—is validated against the correct XSD definition:
        - It must resolve and validate all imported and included schemas, not just the top-level XSD, but also validate every nested element and type, regardless of how deeply it is embedded or which XSD file defines it.
        - Validation must be performed deeply into all embedded, nested, and shared elements as defined in the sharedObjects XSDs.
        - All constraints, types, and structures—whether defined in the main schema or in any referenced shared schema—must be enforced.

### 3.9 Validation Result Panel
    3.9.1 Display overall success/failure status.
    3.9.2 Show schema validation errors (line number, position, description).  

## 4. Optional New / Improved Features
### 4.1 Provide an XML viewer in the Response Body Panel (see 3.7)
### 4.2 Export/Import validations for convenience
    4.2.1 When the user clicks the export button, all XML contents currently displayed (e.g., services, request, response, validation results) are downloaded as a .txt or .zip file.
    4.2.2 When the user clicks the import button, the application prompts the user to select a file to upload, and the contents are displayed in all the panels respectively.
### 4.3 Implement error logs for troubleshooting, diagnostics, and system reliability
### 4.4 Implement usage logs for monitoring application activity, user behavior, and performance analytics
### 4.5 Validate the request body against the expected XML request schema (XSD) for the selected operation before sending

## 5. Testing / Verification
### 5.1 Functional Testing
    5.1.1 Test the cascading dropdowns to ensure correct filtering and clearing behavior.
    5.1.2 Verify that the validator generates example Request and Response XML correctly.
    5.1.3 Verify that the validator can successfully send SOAP requests to all supported PromoStandards services (e.g., Product Data, Pricing, Inventory) and receive responses.
    5.1.4 Confirm that the UI correctly displays request and response XML in their respective panels.

### 5.2 Schema Validation
    5.2.1 Validate that the application correctly checks XML responses against the official PromoStandards XSD schemas, including recursive validation for imported/included schemas.
    5.2.2 Ensure that schema validation errors are displayed with line number, position, and description in the Validation Result panel.

### 5.3 Error Handling
    5.3.1 Simulate unreachable endpoints and invalid service calls to verify that error messages are shown in the appropriate panel.
    5.3.2 Test client-side validation for endpoint input and request body, ensuring the Validate button is disabled when errors are present.

### 5.4 Export/Import Functionality
    5.4.1 Confirm that exporting downloads all relevant XML and validation results as a .txt or .zip file.
    5.4.2 Test importing a file to ensure its contents populate the correct panels.

### 5.5 Update Reflection
    5.5.1 After updating PSServiceList.json in the GitHub repository, verify that the web app displays the latest version automatically.
    5.5.2 After updating an app page in the GitHub repository, verify that the web app reflects the changes automatically.

### 5.6 Additional Testing
    5.6.1 Any additional testing or verification activities necessary to ensure the reliability, usability, and compliance of the application—beyond those explicitly described in this document—should be performed as appropriate. This includes, but is not limited to, edge cases and unforeseen user scenarios.

## 6. Deliverables
### 6.1 Source code and sample .env and configuration files must be committed to the official GitHub repository
### 6.2 Administrator guides and requirements documents must be committed to the official GitHub repository. Any updates to these documents (new commits) should automatically reflect in the web app, ensuring users always see the latest version.
### 6.3 Deployment package or container, hostable on PromoStandards.org infrastructure

## 7. Backend API opensource
### 7.1 The backend API code must be open-sourced and hosted in the official PromoStandards Bitbucket repository: https://bitbucket.org/promostandards/web-service-validator-backend


## History- V0.1 Initial draft (October 20, 2025)
- V0.2 Updated based on Committee Meeting feedback (October 25, 2025)
  - Update 3.4.2 from Github to Bitbucket repository
  - No remove Direct RESTful API Access, then no Authorization needed


# Project Roadmap & Todo

## 🚀 Current Focus (v1.0 Release Candidate)

### Integration & Reliability
- [x] **Fix Integration Tests (PPC)**
  - [x] Update `ExpectedError` values in `Docs/MockXmlResponses/PPC/mock_responses.json`
  - [x] Verify all PPC test cases pass with correct error matching
- [ ] **Expand Test Coverage**
  - [ ] Add invalid mock response cases for other services (Inventory, OrderStatus, etc.)
  - [ ] Verify validation logic for all new services

### Deployment & DevOps
- [ ] **Automated Deployment**
  - [ ] Configure CI/CD pipeline in Bitbucket
  - [ ] Finalize deployment scripts for PromoStandards.org environment
- [x] **StubServer Configuration**
  - [x] Ensure correct version routing (`api/{Service}/{Version}/{ErrorCode}`)
  - [x] Secure sensitive configuration (gitignore)

## 📋 Backlog (Future Enhancements)

### Documentation
- [ ] Review and update `Docs/` to reflect latest architecture
- [ ] Create guide: "How to add a new PromoStandards Service/Version"

### Application Features
- [ ] **Error Handling**: Improve user-facing error messages for edge cases
- [ ] **Analytics**: Implement logging for usage tracking (optional)
- [ ] **Performance**: Add response time monitoring and alerting (optional)

### Security & Optimization
- [ ] Review and optimize Docker image sizes
- [ ] Implement API rate limiting
- [ ] Add access control/authentication layers if public

## ✅ Completed
- [x] **Core Validation Logic**: Validates XML against XSD schemas
- [x] **StubServer**: Functional mock server for local testing
- [x] **Robustness Tests**: Handling of malformed XML, invalid types, and empty bodies
- [x] **Project Structure**: Organized Tests, Docs, and Source code

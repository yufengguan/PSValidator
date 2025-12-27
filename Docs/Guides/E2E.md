# End-to-End (E2E) Testing Guide

This project uses **[Playwright](https://playwright.dev/)** for functional End-to-End (E2E) testing. These tests ensure that critical user workflows (such as validation, exporting, and importing) continue to function correctly as the application evolves.

## 🚀 Quick Start

### 1. Prerequisites
Ensure you have the project dependencies installed:
```bash
cd App
npm install
```

### 2. Install Playwright Browsers
If this is your first time running tests, install the browser binaries:
```bash
npx playwright install --with-deps
```

### 3. Run Tests
Run the entire suite in headless mode:
```bash
npx playwright test
```

Run tests with the UI Mode (interactive debugger):
```bash
npx playwright test --ui
```

---

## 📂 Project Structure

Tests are located in `App/tests/e2e`.

```text
App/
├── playwright.config.js      # Global configuration (base URL, timeouts, etc.)
└── tests/
    └── e2e/
        └── functional.spec.js # Core functional tests
```

## 🧪 What We Test

Our functional tests focus on **behavior**, not strict visual styling. We verify:

1.  **Page Load Integrity**:
    *   Title is "WSV".
    *   Header "Web Service Validator" is present.
2.  **Initial UI State**:
    *   Export button and "with Credentials" checkbox are disabled on load.
3.  **Validation Workflow**:
    *   User can select a Service, Version, and Operation.
    *   User can enter an XML payload.
    *   Clicking **Validate Request** enables the **Export** button.
4.  **Control Attributes**:
    *   Buttons and checkboxes have correct `title` tooltips for accessibility.

## 📝 Writing New Tests

Create new `.spec.js` files in `App/tests/e2e/`.

### Example Template
```javascript
import { test, expect } from '@playwright/test';

test('should perform specific action', async ({ page }) => {
  await page.goto('/');
  
  // Interaction
  await page.locator('button.my-btn').click();
  
  // Assertion
  await expect(page.locator('.result')).toBeVisible();
});
```

## ⚠️ Troubleshooting

**Tests failing with timeouts?**
*   Ensure the local development server (`npm run dev`) and the backend API (`dotnet run`) are running.
*   Playwright attempts to start the dev server automatically via `webServer` config in `playwright.config.js`, but backend dependency must be met manually in this environment.

**"Export" button not enabling?**
*   The test uses a real XML payload `<root>test</root>` to ensure the backend validator accepts the request. If the backend is down or returns a 500 Error, the button won't enable, and the test will fail.

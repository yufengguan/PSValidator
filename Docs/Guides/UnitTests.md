# Unit Testing Guide

This project uses **[Vitest](https://vitest.dev/)** and **[React Testing Library](https://testing-library.com/docs/react-testing-library/intro/)** for unit and integration testing of React components.

## 🚀 Quick Start

### Run Tests
Execute all unit tests in watch mode:
```bash
cd App
npm run test
```
*Note: This is configured to exclude E2E tests (`tests/e2e/`).*

### Run with UI
Launch the Vitest UI for a visual dashboard of test results:
```bash
npm run test:ui
```

### Coverage
Generate a code coverage report:
```bash
npm run test:coverage
```

---

## 🧪 Test Suite Breakdown

Our unit tests are located in `App/src/tests/`. Detailed "Remarks" are included at the top of each test file.

### 1. `ServiceSelector.test.jsx`
*   **Target**: `<ServiceSelector />`
*   **Type**: Controlled Component.
*   **Focus**: Verifies the cascading dropdown logic (Service -> Version -> Operation).
*   **Strategy**: Since the component is controlled by props (`selection`), tests verify that selecting an option calls the `onSelectionChange` callback with the correct new state.

### 2. `ValidationPanel.test.jsx`
*   **Target**: `<ValidationPanel />`
*   **Type**: Presentational Component.
*   **Focus**: Verifies visual feedback for validation results.
*   **Scenarios**:
    *   **Success**: Checks for Green Alert and success message.
    *   **Failure**: Checks for Red Alert and list of errors.
    *   **Cleaning**: Verifies that redundant "Error:" prefixes are removed from messages.
    *   **Structure Warnings**: Checks for the conditional "Structural Error" note.

### 3. `EndpointInput.test.jsx`
*   **Target**: `<EndpointInput />`
*   **Type**: Controlled Component.
*   **Focus**: Verifies input rendering, user typing (calling `onChange`), and error state display.
*   **Technique**: Uses a local `TestWrapper` or mock callbacks to verify the controlled input behavior.

### 4. `App.test.jsx`
*   **Target**: `<App />` (Integration)
*   **Type**: Smart Container / Integration.
*   **Focus**: Verifies the "Happy Path" workflow and Error Handling.
*   **Mocking**: Uses `global.fetch = vi.fn()` to mock API calls (`/ServiceList`, `/validate-request`) to test the application logic without a real backend.

---

## 📝 Best Practices

1.  **Mocking API calls**: Always mock `fetch` in component tests. Do not assert against real backend data, as it makes tests flaky.
2.  **Controlled Components**: When testing components that rely on parent state (like inputs), either:
    *   Mock the callback (`vi.fn()`) and check if it was called.
    *   Use a simple `<TestWrapper />` that uses `useState` to simulate the parent.
3.  **Behavior over Implementation**: Test what the user sees (text, buttons) rather than internal state or class names, whenever possible.

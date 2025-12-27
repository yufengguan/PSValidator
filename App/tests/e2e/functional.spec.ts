import { test, expect } from '@playwright/test';

test.describe('Validator Functional Tests', () => {

    test('should load the home page correctly', async ({ page }) => {
        await page.goto('/');
        await expect(page).toHaveTitle('WSV');
        await expect(page.locator('h3')).toContainText('Web Service Validator');
    });

    test('should disable Export and Checkbox initially', async ({ page }) => {
        await page.goto('/');
        const exportBtn = page.getByRole('button', { name: 'Export' });
        const checkbox = page.locator('#credentials-check');

        await expect(exportBtn).toBeDisabled();
        await expect(checkbox).toBeDisabled();
    });

    test('should enable Export and Checkbox after validation', async ({ page }) => {
        await page.goto('/');

        // 1. Select Service (Index 1)
        const serviceSelect = page.locator('select').first();
        await serviceSelect.selectOption({ index: 1 });

        // 2. Select Version (Index 1) - wait for update
        const versionSelect = page.locator('select').nth(1);
        await expect(versionSelect).toBeVisible();
        await page.waitForTimeout(1000);
        await versionSelect.selectOption({ index: 1 });

        // 3. Select Operation (Index 1)
        const opSelect = page.locator('select').nth(2);
        await expect(opSelect).toBeVisible();
        await page.waitForTimeout(1000);
        await opSelect.selectOption({ index: 1 });

        // 4. Enter Request Body - Valid XML
        const textarea = page.locator('textarea');
        await textarea.fill('<root>test</root>');

        // 5. Click Validate
        await page.getByRole('button', { name: 'Validate Request' }).click();

        // 6. Wait for Result
        // Matches "Validation Results" or "Request Validation Results", etc.
        const resultPanel = page.locator('.card-header', { hasText: 'Validation Results' });
        await expect(resultPanel).toBeVisible({ timeout: 15000 });

        // 7. Verify Buttons Enabled
        // Instead of hard wait, we wait for the button to be enabled
        const exportBtn = page.getByRole('button', { name: /Export/ });
        await expect(exportBtn).toBeEnabled({ timeout: 15000 });
        const checkbox = page.locator('#credentials-check');
        await expect(checkbox).toBeEnabled();
    });

    test('should have titles on controls', async ({ page }) => {
        await page.goto('/');

        const importBtn = page.getByRole('button', { name: 'Import' });
        const exportBtn = page.getByRole('button', { name: 'Export' });
        const checkboxLabel = page.locator('label', { hasText: 'with Credentials' });

        await expect(importBtn).toHaveAttribute('title', /Import/);
        await expect(exportBtn).toHaveAttribute('title', /Export/);
        await expect(checkboxLabel).toHaveAttribute('title', /Include sensitive credentials/);
    });

});

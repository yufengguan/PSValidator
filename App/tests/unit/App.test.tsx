import { render, screen, waitFor, act, fireEvent } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import App from '../../src/App';

/**
 * UNIT TEST REMARKS:
 * Component: App (Integration)
 * See previous remarks. 
 * Updated to handle Sample Request fetch asynchronous behavior correctly.
 */

describe('App Integration Tests', () => {

    // Correct Mock Data mimicking PromoStandards structure
    const mockServices = [
        {
            ServiceId: 'S1',
            ServiceName: 'OrderStatus',
            Versions: [
                {
                    Major: 2, Minor: 0, Patch: 0,
                    Operations: [
                        { OperationName: 'GetOrderStatusRequest' }
                    ]
                }
            ]
        }
    ];

    beforeEach(() => {
        global.fetch = vi.fn() as any;
        window.sessionStorage.clear();
    });

    afterEach(() => {
        vi.restoreAllMocks();
    });

    it('should fetch and display service list on mount', async () => {
        (global.fetch as any).mockResolvedValueOnce({
            ok: true,
            json: async () => mockServices
        });

        await act(async () => {
            render(<App />);
        });

        await waitFor(() => {
            expect(screen.getByRole('combobox', { name: /Select Service/i })).toBeInTheDocument();
        });
    });

    it('should run a full validation flow (Happy Path)', async () => {
        const user = userEvent.setup();

        // 1. Service List
        (global.fetch as any).mockResolvedValueOnce({
            ok: true,
            json: async () => mockServices
        });

        // 2. Sample Request (Triggered by Operation Selection)
        (global.fetch as any).mockResolvedValueOnce({
            ok: true,
            json: async () => ({ xmlContent: '<Sample>Request</Sample>' })
        });

        // 3. Validation API Call
        (global.fetch as any).mockResolvedValueOnce({
            ok: true,
            headers: { get: () => 'application/json' },
            json: async () => ({
                isValid: true,
                validationResultMessages: ['Validation Successful']
            })
        });

        render(<App />);

        await waitFor(() => {
            expect(screen.getByRole('combobox', { name: /Select Service/i })).not.toBeDisabled();
        });

        // Select Service
        fireEvent.change(screen.getByRole('combobox', { name: /Select Service/i }), { target: { value: 'OrderStatus' } });

        // Wait for Version
        await waitFor(() => {
            expect(screen.getByRole('combobox', { name: /Select Version/i })).not.toBeDisabled();
        });

        // Select Version
        fireEvent.change(screen.getByRole('combobox', { name: /Select Version/i }), { target: { value: '2.0.0' } });

        // Wait for Operation
        await waitFor(() => {
            expect(screen.getByRole('combobox', { name: /Select Operation/i })).not.toBeDisabled();
        });

        // Select Operation - This triggers Sample Request Fetch!
        fireEvent.change(screen.getByRole('combobox', { name: /Select Operation/i }), { target: { value: 'GetOrderStatusRequest' } });

        // Wait for Sample Request to populate
        const requestInput = screen.getByRole('textbox', { name: /Request Body/i });
        await waitFor(() => {
            expect(requestInput).toHaveValue('<Sample>Request</Sample>');
        });

        // Clear and Enter XML
        await user.clear(requestInput);
        await user.type(requestInput, '<Test>XML</Test>');

        // Click Validate
        const validateBtn = screen.getByRole('button', { name: /Validate Request/i });
        await user.click(validateBtn);

        // Expect Validation Results
        await waitFor(() => {
            expect(screen.getByText('Validation Successful')).toBeInTheDocument();
        });
    });

    it('should handle API Network Errors gracefully', async () => {
        const user = userEvent.setup();

        // 1. Service List
        (global.fetch as any).mockResolvedValueOnce({
            ok: true,
            json: async () => mockServices
        });

        // 2. Sample Request
        (global.fetch as any).mockResolvedValueOnce({
            ok: true,
            json: async () => ({ xmlContent: '<Sample/>' })
        });

        // 3. Validation API Failure
        (global.fetch as any).mockRejectedValueOnce(new Error('Network Error'));

        render(<App />);

        await waitFor(() => screen.getByRole('combobox', { name: /Select Service/i }));

        fireEvent.change(screen.getByRole('combobox', { name: /Select Service/i }), { target: { value: 'OrderStatus' } });

        await waitFor(() => expect(screen.getByRole('combobox', { name: /Select Version/i })).not.toBeDisabled());
        fireEvent.change(screen.getByRole('combobox', { name: /Select Version/i }), { target: { value: '2.0.0' } });

        await waitFor(() => expect(screen.getByRole('combobox', { name: /Select Operation/i })).not.toBeDisabled());
        fireEvent.change(screen.getByRole('combobox', { name: /Select Operation/i }), { target: { value: 'GetOrderStatusRequest' } });

        // Wait for sample
        const input = screen.getByRole('textbox', { name: /Request Body/i });
        await waitFor(() => expect(input).toHaveValue('<Sample/>'));

        await user.clear(input);
        await user.type(input, '<Test/>');

        await user.click(screen.getByRole('button', { name: /Validate Request/i }));

        await waitFor(() => {
            expect(screen.getByText(/Network Error/i)).toBeInTheDocument();
        });
    });

    it('should validate Endpoint URL format', async () => {
        const user = userEvent.setup();

        // 1. Service List
        (global.fetch as any).mockResolvedValueOnce({ ok: true, json: async () => mockServices });

        render(<App />);

        // Wait for initial render
        await waitFor(() => screen.getByRole('combobox', { name: /Select Service/i }));

        const endpointInput = screen.getByLabelText(/Endpoint URL/i);

        // Invalid URL
        await user.clear(endpointInput);
        await user.type(endpointInput, 'invalid-url');

        expect(screen.getByText(/Invalid URL format/i)).toBeInTheDocument();

        // Fix it
        await user.clear(endpointInput);
        await user.type(endpointInput, 'https://example.com');
        expect(screen.queryByText(/Invalid URL/i)).not.toBeInTheDocument();
    });

    it('should run a full response validation flow', async () => {
        const user = userEvent.setup();

        // 1. Service List
        (global.fetch as any).mockResolvedValueOnce({ ok: true, json: async () => mockServices });

        // 2. Sample Request (Operation Selection)
        (global.fetch as any).mockResolvedValueOnce({ ok: true, json: async () => ({ xmlContent: '<Sample/>' }) });

        // 3. Response Validation API
        (global.fetch as any).mockResolvedValueOnce({
            ok: true,
            headers: { get: () => 'application/json' },
            json: async () => ({
                isValid: true,
                validationResultMessages: ['Response Validated'],
                responseContent: '<Response>OK</Response>'
            })
        });

        render(<App />);

        // Navigation to Operation
        await waitFor(() => screen.getByRole('combobox', { name: /Select Service/i }));
        fireEvent.change(screen.getByRole('combobox', { name: /Select Service/i }), { target: { value: 'OrderStatus' } });

        await waitFor(() => expect(screen.getByRole('combobox', { name: /Select Version/i })).not.toBeDisabled());
        fireEvent.change(screen.getByRole('combobox', { name: /Select Version/i }), { target: { value: '2.0.0' } });

        await waitFor(() => expect(screen.getByRole('combobox', { name: /Select Operation/i })).not.toBeDisabled());
        fireEvent.change(screen.getByRole('combobox', { name: /Select Operation/i }), { target: { value: 'GetOrderStatusRequest' } });

        // Input Endpoint
        const endpointInput = screen.getByLabelText(/Endpoint URL/i);
        await user.type(endpointInput, 'https://example.com');

        // Wait for sample request to ensure Request Body is not empty
        await waitFor(() => {
            expect(screen.getByRole('textbox', { name: /Request Body/i })).not.toHaveValue('');
        });

        // Click Validate Response
        const validateBtn = screen.getByRole('button', { name: /Validate Response/i });
        await user.click(validateBtn);

        await waitFor(() => {
            expect(screen.getByText('Validation Successful')).toBeInTheDocument();
        });
    });

    it('should export session to JSON', async () => {
        const user = userEvent.setup();

        // Setup state suitable for export
        (global.fetch as any).mockResolvedValueOnce({ ok: true, json: async () => mockServices });
        (global.fetch as any).mockResolvedValueOnce({ ok: true, json: async () => ({ xmlContent: '<Sample/>' }) });
        (global.fetch as any).mockResolvedValueOnce({
            ok: true,
            headers: { get: () => 'application/json' },
            json: async () => ({ isValid: true, validationResultMessages: [] })
        });

        render(<App />);

        // Select Op
        await waitFor(() => screen.getByRole('combobox', { name: /Select Service/i }));
        fireEvent.change(screen.getByRole('combobox', { name: /Select Service/i }), { target: { value: 'OrderStatus' } });
        await waitFor(() => screen.getByRole('combobox', { name: /Select Version/i }));
        fireEvent.change(screen.getByRole('combobox', { name: /Select Version/i }), { target: { value: '2.0.0' } });
        await waitFor(() => screen.getByRole('combobox', { name: /Select Operation/i }));
        fireEvent.change(screen.getByRole('combobox', { name: /Select Operation/i }), { target: { value: 'GetOrderStatusRequest' } });

        await user.type(screen.getByLabelText(/Endpoint URL/i), 'https://test.com');
        await user.click(screen.getByRole('button', { name: /Validate Request/i }));

        await waitFor(() => expect(screen.getByRole('button', { name: /Export/i })).not.toBeDisabled());

        // Mock download anchor
        const clickMock = vi.fn();
        const linkMock = { href: '', download: '', click: clickMock, style: {} } as any;

        vi.spyOn(document, 'createElement').mockImplementation(((tagName: string) => {
            if (tagName === 'a') return linkMock;
            return document.createElement(tagName);
        }) as any);

        vi.spyOn(document.body, 'appendChild').mockImplementation(() => null as any);
        vi.spyOn(document.body, 'removeChild').mockImplementation(() => null as any);

        await user.click(screen.getByRole('button', { name: /Export/i }));

        expect(clickMock).toHaveBeenCalled();
        expect(linkMock.download).toContain('.json');

        vi.restoreAllMocks();
    });

    it('should import session from JSON', async () => {
        const user = userEvent.setup();
        (global.fetch as any).mockResolvedValueOnce({ ok: true, json: async () => mockServices });

        const { container } = render(<App />);
        await waitFor(() => screen.getByRole('combobox', { name: /Select Service/i }));

        const fileContent = JSON.stringify({
            serviceSelection: { service: 'OrderStatus', version: '2.0.0', operation: 'GetOrderStatusRequest' },
            endpoint: 'https://imported.com',
            requestXml: '<Imported>XML</Imported>'
        });

        const file = new File([fileContent], 'session.json', { type: 'application/json' });

        const input = container.querySelector('input[type="file"]');
        expect(input).toBeInTheDocument();

        await user.upload(input as HTMLInputElement, file);

        await waitFor(() => {
            expect(screen.getByLabelText(/Endpoint URL/i)).toHaveValue('https://imported.com');
            expect(screen.getByRole('textbox', { name: /Request Body/i })).toHaveValue('<Imported>XML</Imported>');
            expect(screen.getByRole('combobox', { name: /Select Service/i })).toHaveValue('OrderStatus');
        });
    });

    it('should toggle export options and trigger import click', async () => {
        const user = userEvent.setup();
        (global.fetch as any).mockResolvedValueOnce({ ok: true, json: async () => mockServices });

        // Mock validation result to enable export buttons
        (global.fetch as any).mockResolvedValueOnce({ ok: true, json: async () => ({ xmlContent: '<Sample/>' }) });
        (global.fetch as any).mockResolvedValueOnce({
            ok: true,
            headers: { get: () => 'application/json' },
            json: async () => ({ isValid: true, validationResultMessages: [] })
        });

        const { container } = render(<App />);

        // Test Import Click (Always enabled)
        const fileInput = container.querySelector('input[type="file"]') as HTMLInputElement;
        const clickSpy = vi.spyOn(fileInput, 'click');
        const importBtn = screen.getByRole('button', { name: /Import/i });

        await user.click(importBtn);
        expect(clickSpy).toHaveBeenCalled();

        // Enable Credentials Checkbox by running validation
        await waitFor(() => screen.getByRole('combobox', { name: /Select Service/i }));
        fireEvent.change(screen.getByRole('combobox', { name: /Select Service/i }), { target: { value: 'OrderStatus' } });
        await waitFor(() => screen.getByRole('combobox', { name: /Select Version/i }));
        fireEvent.change(screen.getByRole('combobox', { name: /Select Version/i }), { target: { value: '2.0.0' } });
        await waitFor(() => screen.getByRole('combobox', { name: /Select Operation/i }));
        fireEvent.change(screen.getByRole('combobox', { name: /Select Operation/i }), { target: { value: 'GetOrderStatusRequest' } });

        // Wait for sample request to populate (ensure body not empty)
        await waitFor(() => {
            expect(screen.getByRole('textbox', { name: /Request Body/i })).toHaveValue('<Sample/>');
        });

        await user.type(screen.getByLabelText(/Endpoint URL/i), 'https://test.com');
        await user.click(screen.getByRole('button', { name: /Validate Request/i }));

        // Wait for validation to complete (export buttons enable)
        await waitFor(() => expect(screen.getByRole('checkbox', { name: /with Credentials/i })).not.toBeDisabled());

        const checkbox = screen.getByRole('checkbox', { name: /with Credentials/i });
        expect(checkbox).not.toBeChecked();
        await user.click(checkbox);
        expect(checkbox).toBeChecked();
    });

    it('should log error when Service List fetch fails', async () => {
        const consoleSpy = vi.spyOn(console, 'error').mockImplementation(() => { });
        (global.fetch as any).mockRejectedValueOnce(new Error('Service List Failed'));

        render(<App />);

        await waitFor(() => {
            expect(consoleSpy).toHaveBeenCalledWith('Error fetching services:', expect.any(Error));
        });
        consoleSpy.mockRestore();
    });

    it('should handle Sample Request fetch error', async () => {
        (global.fetch as any).mockResolvedValueOnce({ ok: true, json: async () => mockServices });
        // Sample request fails
        (global.fetch as any).mockRejectedValueOnce(new Error('Sample Failed'));

        render(<App />);

        await waitFor(() => screen.getByRole('combobox', { name: /Select Service/i }));
        fireEvent.change(screen.getByRole('combobox', { name: /Select Service/i }), { target: { value: 'OrderStatus' } });
        await waitFor(() => screen.getByRole('combobox', { name: /Select Version/i }));
        fireEvent.change(screen.getByRole('combobox', { name: /Select Version/i }), { target: { value: '2.0.0' } });

        // Select op triggers fetch
        fireEvent.change(screen.getByRole('combobox', { name: /Select Operation/i }), { target: { value: 'GetOrderStatusRequest' } });

        await waitFor(() => {
            expect(screen.getByRole('textbox', { name: /Request Body/i })).toHaveValue('<!-- Error generating sample: Sample Failed -->');
        });
    });

    it('should clear validation results and response panel immediately upon clicking Validate', async () => {
        const user = userEvent.setup();
        // 1. Initial Setup Mocks
        (global.fetch as any).mockResolvedValueOnce({ ok: true, json: async () => mockServices });
        (global.fetch as any).mockResolvedValueOnce({ ok: true, json: async () => ({ xmlContent: '<Sample/>' }) });
        // 2. First Validation Success
        (global.fetch as any).mockResolvedValueOnce({
            ok: true,
            headers: { get: () => 'application/json' },
            json: async () => ({ isValid: true, validationResultMessages: ['Success'], responseContent: '<Response>Old</Response>' })
        });

        render(<App />);

        // Navigate & First Validation
        await waitFor(() => screen.getByRole('combobox', { name: /Select Service/i }));
        fireEvent.change(screen.getByRole('combobox', { name: /Select Service/i }), { target: { value: 'OrderStatus' } });
        await waitFor(() => screen.getByRole('combobox', { name: /Select Version/i }));
        fireEvent.change(screen.getByRole('combobox', { name: /Select Version/i }), { target: { value: '2.0.0' } });
        await waitFor(() => screen.getByRole('combobox', { name: /Select Operation/i }));
        fireEvent.change(screen.getByRole('combobox', { name: /Select Operation/i }), { target: { value: 'GetOrderStatusRequest' } });

        await user.type(screen.getByLabelText(/Endpoint URL/i), 'https://test.com');
        await waitFor(() => expect(screen.getByRole('textbox', { name: /Request Body/i })).toHaveValue('<Sample/>'));

        const validateBtn = screen.getByRole('button', { name: /Validate Request/i });
        await user.click(validateBtn);

        // Confirm Old Results Visible
        await waitFor(() => {
            expect(screen.getByText('Validation Successful')).toBeInTheDocument();
        });

        // 3. Second Validation - Slow Response
        let resolveSecondCall: (value: any) => void;
        const secondCallPromise = new Promise((resolve) => { resolveSecondCall = resolve; });
        (global.fetch as any).mockReturnValue(secondCallPromise);

        // Click Validate Again
        await user.click(validateBtn);

        // IMMEDIATE ASSERTION: Old results should be gone!
        // We do wait for the "loading" state though, but the text should be gone.
        await waitFor(() => {
            expect(screen.queryByText('Validation Successful')).not.toBeInTheDocument();
        });

        // Cleanup: Resolve the pending promise to avoid open handles
        resolveSecondCall!({
            ok: true,
            headers: { get: () => 'application/json' },
            json: async () => ({ isValid: false, validationResultMessages: ['New Error'] })
        });
    });

});

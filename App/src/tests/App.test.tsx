import { render, screen, waitFor, act, fireEvent } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import App from '../App';

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

        // Wait for Sample Request to populate (Wait for fetch to complete and state update)
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

        // Use Type without clear? Just appending or overwrite doesn't matter for this fail test, 
        // as long as something is there. But clearing is safer.
        await user.clear(input);
        await user.type(input, '<Test/>');

        await user.click(screen.getByRole('button', { name: /Validate Request/i }));

        await waitFor(() => {
            expect(screen.getByText(/Network Error/i)).toBeInTheDocument();
        });
    });
});

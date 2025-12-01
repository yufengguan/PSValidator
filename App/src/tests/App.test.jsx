import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, expect, beforeEach, vi } from 'vitest';
import App from '../App';

/**
 * Integration Tests for App Component - Section 5.1, 5.3, 5.4
 * Verifies end-to-end functionality including validation and error handling
 */
describe('App - Integration Tests', () => {
    beforeEach(() => {
        // Mock fetch for API calls
        global.fetch = vi.fn();
    });

    afterEach(() => {
        vi.restoreAllMocks();
    });

    it('should fetch and display service list on mount (Section 5.1.3)', async () => {
        const mockServices = [
            {
                service: 'OrderStatus',
                versions: [
                    {
                        version: '2.0.0',
                        operations: ['GetOrderStatusRequest', 'GetOrderStatusResponse']
                    }
                ]
            }
        ];

        global.fetch.mockResolvedValueOnce({
            ok: true,
            json: async () => mockServices
        });

        render(<App />);

        await waitFor(() => {
            expect(screen.getByLabelText(/web service/i)).toBeInTheDocument();
        });
    });

    it('should display request and response XML in panels (Section 5.1.4)', async () => {
        const user = userEvent.setup();

        global.fetch.mockResolvedValueOnce({
            ok: true,
            json: async () => [
                {
                    service: 'OrderStatus',
                    versions: [
                        {
                            version: '2.0.0',
                            operations: ['GetOrderStatusRequest']
                        }
                    ]
                }
            ]
        });

        render(<App />);

        await waitFor(() => {
            expect(screen.getByText(/request body/i)).toBeInTheDocument();
            expect(screen.getByText(/response body/i)).toBeInTheDocument();
        });
    });

    it('should disable Validate button when endpoint is empty (Section 3.8.1, 5.3.2)', async () => {
        global.fetch.mockResolvedValueOnce({
            ok: true,
            json: async () => []
        });

        render(<App />);

        await waitFor(() => {
            const validateButton = screen.getByRole('button', { name: /validate/i });
            expect(validateButton).toBeDisabled();
        });
    });

    it('should show validation errors in validation panel (Section 5.2.2)', async () => {
        const user = userEvent.setup();

        const mockServices = [
            {
                service: 'OrderStatus',
                versions: [
                    {
                        version: '2.0.0',
                        operations: ['GetOrderStatusResponse']
                    }
                ]
            }
        ];

        global.fetch
            .mockResolvedValueOnce({
                ok: true,
                json: async () => mockServices
            })
            .mockResolvedValueOnce({
                ok: true,
                json: async () => ({
                    isValid: false,
                    validationResultMessages: [
                        'Line 1, Position 5: Element "InvalidRoot" is not valid'
                    ]
                })
            });

        render(<App />);

        await waitFor(() => {
            expect(screen.getByLabelText(/web service/i)).toBeInTheDocument();
        });

        // Select service, version, operation
        const serviceDropdown = screen.getByLabelText(/web service/i);
        await user.selectOptions(serviceDropdown, 'OrderStatus');

        const versionDropdown = screen.getByLabelText(/version/i);
        await user.selectOptions(versionDropdown, '2.0.0');

        const operationDropdown = screen.getByLabelText(/operation/i);
        await user.selectOptions(operationDropdown, 'GetOrderStatusResponse');

        // Enter endpoint and XML
        const endpointInput = screen.getByLabelText(/endpoint/i);
        await user.type(endpointInput, 'https://example.com/service');

        const requestTextarea = screen.getByRole('textbox', { name: /request/i });
        await user.type(requestTextarea, '<InvalidRoot>Test</InvalidRoot>');

        // Click validate
        const validateButton = screen.getByRole('button', { name: /validate/i });
        await user.click(validateButton);

        // Check validation result
        await waitFor(() => {
            expect(screen.getByText(/not valid/i)).toBeInTheDocument();
        });
    });

    it('should handle unreachable endpoint errors (Section 5.3.1)', async () => {
        const user = userEvent.setup();

        global.fetch
            .mockResolvedValueOnce({
                ok: true,
                json: async () => [
                    {
                        service: 'OrderStatus',
                        versions: [{ version: '2.0.0', operations: ['GetOrderStatusResponse'] }]
                    }
                ]
            })
            .mockRejectedValueOnce(new Error('Network error'));

        render(<App />);

        await waitFor(() => {
            expect(screen.getByLabelText(/web service/i)).toBeInTheDocument();
        });

        // Make selections and validate
        const serviceDropdown = screen.getByLabelText(/web service/i);
        await user.selectOptions(serviceDropdown, 'OrderStatus');

        const versionDropdown = screen.getByLabelText(/version/i);
        await user.selectOptions(versionDropdown, '2.0.0');

        const operationDropdown = screen.getByLabelText(/operation/i);
        await user.selectOptions(operationDropdown, 'GetOrderStatusResponse');

        const endpointInput = screen.getByLabelText(/endpoint/i);
        await user.type(endpointInput, 'https://unreachable.example.com');

        const requestTextarea = screen.getByRole('textbox', { name: /request/i });
        await user.type(requestTextarea, '<Test/>');

        const validateButton = screen.getByRole('button', { name: /validate/i });
        await user.click(validateButton);

        await waitFor(() => {
            expect(screen.getByText(/network.*error/i)).toBeInTheDocument();
        });
    });
});

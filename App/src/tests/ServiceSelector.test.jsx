import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, expect, beforeEach, vi } from 'vitest';
import ServiceSelector from '../components/ServiceSelector';

/**
 * Tests for ServiceSelector Component - Section 5.1.1
 * Verifies cascading dropdown functionality and filtering behavior
 */
describe('ServiceSelector - Cascading Dropdowns (Section 5.1.1)', () => {
    const mockServices = [
        {
            service: 'OrderStatus',
            versions: [
                {
                    version: '2.0.0',
                    operations: ['GetOrderStatusRequest', 'GetOrderStatusResponse']
                },
                {
                    version: '1.0.0',
                    operations: ['GetOrderStatusRequest', 'GetOrderStatusResponse']
                }
            ]
        },
        {
            service: 'ProductData',
            versions: [
                {
                    version: '1.0.0',
                    operations: ['GetProductRequest', 'GetProductResponse']
                }
            ]
        }
    ];

    const mockOnSelectionChange = vi.fn();

    beforeEach(() => {
        mockOnSelectionChange.mockClear();
    });

    it('should render all three dropdowns', () => {
        render(
            <ServiceSelector
                services={mockServices}
                onSelectionChange={mockOnSelectionChange}
            />
        );

        expect(screen.getByLabelText(/web service/i)).toBeInTheDocument();
        expect(screen.getByLabelText(/version/i)).toBeInTheDocument();
        expect(screen.getByLabelText(/operation/i)).toBeInTheDocument();
    });

    it('should filter versions when service is selected', async () => {
        const user = userEvent.setup();
        render(
            <ServiceSelector
                services={mockServices}
                onSelectionChange={mockOnSelectionChange}
            />
        );

        const serviceDropdown = screen.getByLabelText(/web service/i);
        await user.selectOptions(serviceDropdown, 'OrderStatus');

        await waitFor(() => {
            const versionDropdown = screen.getByLabelText(/version/i);
            expect(versionDropdown).not.toBeDisabled();
        });
    });

    it('should filter operations when version is selected', async () => {
        const user = userEvent.setup();
        render(
            <ServiceSelector
                services={mockServices}
                onSelectionChange={mockOnSelectionChange}
            />
        );

        const serviceDropdown = screen.getByLabelText(/web service/i);
        await user.selectOptions(serviceDropdown, 'OrderStatus');

        const versionDropdown = screen.getByLabelText(/version/i);
        await user.selectOptions(versionDropdown, '2.0.0');

        await waitFor(() => {
            const operationDropdown = screen.getByLabelText(/operation/i);
            expect(operationDropdown).not.toBeDisabled();
        });
    });

    it('should call onSelectionChange when all selections are made', async () => {
        const user = userEvent.setup();
        render(
            <ServiceSelector
                services={mockServices}
                onSelectionChange={mockOnSelectionChange}
            />
        );

        const serviceDropdown = screen.getByLabelText(/web service/i);
        await user.selectOptions(serviceDropdown, 'OrderStatus');

        const versionDropdown = screen.getByLabelText(/version/i);
        await user.selectOptions(versionDropdown, '2.0.0');

        const operationDropdown = screen.getByLabelText(/operation/i);
        await user.selectOptions(operationDropdown, 'GetOrderStatusRequest');

        expect(mockOnSelectionChange).toHaveBeenCalledWith({
            service: 'OrderStatus',
            version: '2.0.0',
            operation: 'GetOrderStatusRequest'
        });
    });

    it('should reset version and operation when service changes (Section 3.3.2)', async () => {
        const user = userEvent.setup();
        render(
            <ServiceSelector
                services={mockServices}
                onSelectionChange={mockOnSelectionChange}
            />
        );

        // Select OrderStatus
        const serviceDropdown = screen.getByLabelText(/web service/i);
        await user.selectOptions(serviceDropdown, 'OrderStatus');

        const versionDropdown = screen.getByLabelText(/version/i);
        await user.selectOptions(versionDropdown, '2.0.0');

        // Change service to ProductData
        await user.selectOptions(serviceDropdown, 'ProductData');

        // Version and operation should be reset
        expect(versionDropdown.value).toBe('');
    });
});

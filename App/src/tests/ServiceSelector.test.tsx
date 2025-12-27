import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import { describe, it, expect } from 'vitest';
import ServiceSelector from '../components/ServiceSelector';
import { useState } from 'react';

/**
 * UNIT TEST REMARKS:
 * Component: ServiceSelector
 * Type: Controlled Component (Presentational)
 * 
 * Purpose:
 * 1. Verify that the component renders the 3 dropdowns (Service, Version, Operation) based on props.
 * 2. Ensure that changing a dropdown calls the `onSelectionChange` callback with the NEW selection state.
 * 3. Verify that the component creates a proper waterfall experience.
 * 
 * Key Updates:
 * - Uses getByRole('combobox', { name: ... }) for robust accessibility selection.
 * - Uses fireEvent.change for reliable interaction.
 * - Uses waitFor/await for disabled state assertions.
 * - Corrected Mock Data structure to match legacy .NET casing used in component (ServiceName, Versions, Major...).
 */

// Wrapper to simulate App.jsx state management
const TestWrapper = ({ initialServices }: { initialServices: any[] }) => {
    const [selection, setSelection] = useState({ service: '', version: '', operation: '' });

    const handleChange = (newSelection: any) => {
        setSelection(newSelection);
    };

    return (
        <ServiceSelector
            services={initialServices}
            selection={selection}
            onSelectionChange={handleChange}
        />
    );
};

describe('ServiceSelector Unit Tests', () => {
    // Correct Mock Data mimicking PromoStandards structure
    const mockServices = [
        {
            ServiceId: 'S1',
            ServiceName: 'OrderStatus',
            Versions: [
                {
                    Major: 2, Minor: 0, Patch: 0,
                    Operations: [
                        { OperationName: 'GetOrderStatusRequest' },
                        { OperationName: 'GetOrderStatusResponse' }
                    ]
                }
            ]
        },
        {
            ServiceId: 'S2',
            ServiceName: 'ProductData',
            Versions: [
                {
                    Major: 1, Minor: 0, Patch: 0,
                    Operations: [
                        { OperationName: 'GetProductRequest' }
                    ]
                }
            ]
        }
    ];

    it('should render all dropdowns initially', () => {
        render(
            <ServiceSelector
                services={mockServices}
                selection={{ service: '', version: '', operation: '' }}
                onSelectionChange={() => { }}
            />
        );

        expect(screen.getByRole('combobox', { name: /Select Service/i })).toBeInTheDocument();
        expect(screen.getByRole('combobox', { name: /Select Version/i })).toBeInTheDocument();
        expect(screen.getByRole('combobox', { name: /Select Operation/i })).toBeInTheDocument();
    });

    it('should allow selecting a service, which then enables version', async () => {
        render(<TestWrapper initialServices={mockServices} />);

        // 1. Select Service
        const serviceSelect = screen.getByRole('combobox', { name: /Select Service/i });
        fireEvent.change(serviceSelect, { target: { value: 'OrderStatus' } });

        // 2. Verify Version is now enabled
        await waitFor(() => {
            expect(screen.getByRole('combobox', { name: /Select Version/i })).not.toBeDisabled();
        });

        // 2.0.0 is constructed from Major.Minor.Patch
        expect(screen.getByText('2.0.0')).toBeInTheDocument();
    });

    it('should cascade selection from Service -> Version -> Operation', async () => {
        render(<TestWrapper initialServices={mockServices} />);

        // 1. Select Service
        fireEvent.change(screen.getByRole('combobox', { name: /Select Service/i }), { target: { value: 'OrderStatus' } });

        // Wait for Version to enable
        await waitFor(() => {
            expect(screen.getByRole('combobox', { name: /Select Version/i })).not.toBeDisabled();
        });

        // 2. Select Version
        fireEvent.change(screen.getByRole('combobox', { name: /Select Version/i }), { target: { value: '2.0.0' } });

        // Wait for Operation to enable
        await waitFor(() => {
            expect(screen.getByRole('combobox', { name: /Select Operation/i })).not.toBeDisabled();
        });

        // 3. Select Operation
        const opSelect = screen.getByRole('combobox', { name: /Select Operation/i });
        fireEvent.change(opSelect, { target: { value: 'GetOrderStatusRequest' } });

        // Verify value is set
        expect(opSelect).toHaveValue('GetOrderStatusRequest');
    });

    it('should reset downstream fields when upstream field changes', async () => {
        render(<TestWrapper initialServices={mockServices} />);

        // Fully select everything for OrderStatus
        fireEvent.change(screen.getByRole('combobox', { name: /Select Service/i }), { target: { value: 'OrderStatus' } });
        fireEvent.change(screen.getByRole('combobox', { name: /Select Version/i }), { target: { value: '2.0.0' } });
        fireEvent.change(screen.getByRole('combobox', { name: /Select Operation/i }), { target: { value: 'GetOrderStatusRequest' } });

        // Now change Service to ProductData
        fireEvent.change(screen.getByRole('combobox', { name: /Select Service/i }), { target: { value: 'ProductData' } });

        // Version should be reset (empty)
        // Wait for verify
        await waitFor(() => {
            expect(screen.getByRole('combobox', { name: /Select Version/i })).toHaveValue('');
        });
    });
});

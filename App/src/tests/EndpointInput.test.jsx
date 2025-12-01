import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, expect, vi } from 'vitest';
import EndpointInput from '../components/EndpointInput';

/**
 * Tests for EndpointInput Component - Section 3.5, 5.3.2
 * Verifies endpoint validation and error handling
 */
describe('EndpointInput - Endpoint Validation (Section 3.5)', () => {
    it('should render endpoint input field', () => {
        const mockOnChange = vi.fn();
        render(<EndpointInput endpoint="" onChange={mockOnChange} />);

        expect(screen.getByLabelText(/endpoint/i)).toBeInTheDocument();
    });

    it('should call onChange when user types', async () => {
        const user = userEvent.setup();
        const mockOnChange = vi.fn();

        render(<EndpointInput endpoint="" onChange={mockOnChange} />);

        const input = screen.getByLabelText(/endpoint/i);
        await user.type(input, 'https://example.com/service');

        expect(mockOnChange).toHaveBeenCalled();
    });

    it('should display validation error when empty (Section 3.5.2)', () => {
        const mockOnChange = vi.fn();
        render(<EndpointInput endpoint="" onChange={mockOnChange} error="Endpoint is required." />);

        expect(screen.getByText(/endpoint is required/i)).toBeInTheDocument();
    });

    it('should display service error message when endpoint is unreachable (Section 3.5.2)', () => {
        const mockOnChange = vi.fn();
        const errorMessage = 'Unable to connect to the service endpoint';

        render(<EndpointInput endpoint="https://unreachable.com" onChange={mockOnChange} error={errorMessage} />);

        expect(screen.getByText(/unable to connect/i)).toBeInTheDocument();
    });

    it('should accept valid URL format', async () => {
        const user = userEvent.setup();
        const mockOnChange = vi.fn();

        render(<EndpointInput endpoint="" onChange={mockOnChange} />);

        const input = screen.getByLabelText(/endpoint/i);
        await user.type(input, 'https://supplier.example.com/promostandards/OrderStatus');

        expect(input.value).toBe('https://supplier.example.com/promostandards/OrderStatus');
    });
});

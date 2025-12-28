import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, expect, vi } from 'vitest';
import EndpointInput from '../../src/components/EndpointInput';
import { useState } from 'react';

/**
 * UNIT TEST REMARKS:
 * Component: EndpointInput
 * Type: Controlled Component (Presentational)
 * 
 * Purpose:
 * 1. Verify that the component renders a Label and Input field.
 * 2. Ensure that typing into the input calls the `onChange` callback.
 * 3. Verify that the component displays the value passed via the `endpoint` prop (Controlled behavior).
 * 4. Verify that error messages are displayed when the `error` prop is present.
 */

// Simple wrapper for testing controlled input behavior
const TestWrapper = () => {
    const [val, setVal] = useState('');
    return <EndpointInput endpoint={val} onChange={setVal} />;
};

describe('EndpointInput Unit Tests', () => {

    it('should render the endpoint label and input', () => {
        render(<EndpointInput endpoint="" onChange={() => { }} />);
        expect(screen.getByLabelText(/Endpoint URL/i)).toBeInTheDocument();
        expect(screen.getByRole('textbox', { name: /Endpoint URL/i })).toBeInTheDocument();
    });

    it('should display the value from props', () => {
        render(<EndpointInput endpoint="http://test.com" onChange={() => { }} />);
        const input = screen.getByRole('textbox', { name: /Endpoint URL/i });
        expect(input).toHaveValue('http://test.com');
    });

    it('should call onChange when user types', async () => {
        const mockOnChange = vi.fn();
        const user = userEvent.setup();

        render(<EndpointInput endpoint="" onChange={mockOnChange} />);

        const input = screen.getByRole('textbox', { name: /Endpoint URL/i });
        await user.type(input, 'https://test.com');

        expect(mockOnChange).toHaveBeenCalled();
    });

    it('should update value correctly in a controlled setup', async () => {
        const user = userEvent.setup();
        render(<TestWrapper />);

        const input = screen.getByRole('textbox', { name: /Endpoint URL/i });
        await user.type(input, 'https://new-url.com');

        expect(input).toHaveValue('https://new-url.com');
    });

    it('should display error message when error prop is provided', () => {
        render(<EndpointInput endpoint="" onChange={() => { }} error="Invalid URL format" />);
        expect(screen.getByText('Invalid URL format')).toBeInTheDocument();
    });
});

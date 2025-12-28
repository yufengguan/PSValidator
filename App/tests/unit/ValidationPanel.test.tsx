import { render, screen } from '@testing-library/react';
import { describe, it, expect } from 'vitest';
import ValidationPanel from '../../src/components/ValidationPanel';

/**
 * UNIT TEST REMARKS:
 * Component: ValidationPanel
 * Type: Stateless Presentational Component
 * 
 * Purpose:
 * 1. Verify display of "Success" state (Green Alert).
 * 2. Verify display of "Error" state (Red Alert).
 * 3. Verify that error messages are "cleaned" (removing redundant "Error:" prefix).
 * 4. Verify the conditional "Note" block for structural errors.
 * 5. Verify the Empty/Initial state.
 */

describe('ValidationPanel Unit Tests', () => {

    it('should display initial empty state when result is null', () => {
        render(<ValidationPanel result={null} />);
        expect(screen.getByText('No validation performed yet.')).toBeInTheDocument();
    });

    it('should display Success Alert when result is valid', () => {
        const result = {
            type: 'Request',
            isValid: true,
            validationResultMessages: ['Validation Successful']
        };

        render(<ValidationPanel result={result} />);

        // Header
        expect(screen.getByText(/Request Validation Results: Success/i)).toBeInTheDocument();
        // Body
        expect(screen.getByText('Validation Successful')).toBeInTheDocument();
        // Alert Class (Bootstrap)
        expect(screen.getByRole('alert')).toHaveClass('alert-success');
    });

    it('should display Failure Alert and list errors', () => {
        const result = {
            isValid: false,
            validationResultMessages: [
                'Line 1: Tag mismatch',
                'Position 5: Invalid Attribute'
            ]
        };

        render(<ValidationPanel result={result} />);

        // Header
        expect(screen.getByText(/Validation Results: Failed/i)).toBeInTheDocument();
        // Errors
        expect(screen.getByText('Line 1: Tag mismatch')).toBeInTheDocument();
        expect(screen.getByText('Position 5: Invalid Attribute')).toBeInTheDocument();
        // Alert Class
        expect(screen.getByRole('alert')).toHaveClass('alert-danger');
    });

    it('should clean "Error:" prefix from messages', () => {
        const result = {
            isValid: false,
            validationResultMessages: [
                'Error: Invalid XML structure',
                'error: lowercase prefix check'
            ]
        };

        render(<ValidationPanel result={result} />);

        // Should find "Invalid XML structure" without "Error: "
        expect(screen.getByText('Invalid XML structure')).toBeInTheDocument();
        expect(screen.queryByText('Error: Invalid XML structure')).not.toBeInTheDocument();

        // Regex /i in component handles case insensitivity? 
        // Component uses: msg.replace(/^Error:\s*/i, '') -> Yes.
        expect(screen.getByText('lowercase prefix check')).toBeInTheDocument();
    });

    it('should show Note for structural errors (when not empty body error)', () => {
        const result = {
            isValid: false,
            validationResultMessages: ['Some structural error occurred']
        };

        render(<ValidationPanel result={result} />);

        expect(screen.getByText(/Some structural errors may prevent further validation/i)).toBeInTheDocument();
    });

    it('should NOT show Note if the only error is "Request Body cannot be empty."', () => {
        const result = {
            isValid: false,
            validationResultMessages: ['Request Body cannot be empty.']
        };

        render(<ValidationPanel result={result} />);

        expect(screen.getByText('Request Body cannot be empty.')).toBeInTheDocument();
        expect(screen.queryByText(/Some structural errors/i)).not.toBeInTheDocument();
    });
});

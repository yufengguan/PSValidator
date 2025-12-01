import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, expect, vi } from 'vitest';
import ValidationPanel from '../components/ValidationPanel';

/**
 * Tests for ValidationPanel Component - Section 5.2.2, 3.9
 * Verifies validation result display with line numbers, positions, and descriptions
 */
describe('ValidationPanel - Validation Results Display (Section 5.2.2)', () => {
    it('should display success message when validation passes', () => {
        const result = {
            isValid: true,
            validationResultMessages: []
        };

        render(<ValidationPanel result={result} />);

        expect(screen.getByText(/valid/i)).toBeInTheDocument();
        expect(screen.queryByText(/error/i)).not.toBeInTheDocument();
    });

    it('should display validation errors with details (Section 3.9.2)', () => {
        const result = {
            isValid: false,
            validationResultMessages: [
                'Line 5, Position 12: Element "InvalidElement" is not valid according to schema',
                'Line 10, Position 3: Required element "wsVersion" is missing'
            ]
        };

        render(<ValidationPanel result={result} />);

        expect(screen.getByText(/line 5/i)).toBeInTheDocument();
        expect(screen.getByText(/position 12/i)).toBeInTheDocument();
        expect(screen.getByText(/invalidelement/i)).toBeInTheDocument();
        expect(screen.getByText(/line 10/i)).toBeInTheDocument();
        expect(screen.getByText(/wsversion/i)).toBeInTheDocument();
    });

    it('should display overall failure status (Section 3.9.1)', () => {
        const result = {
            isValid: false,
            validationResultMessages: ['Validation failed']
        };

        render(<ValidationPanel result={result} />);

        expect(screen.getByText(/failed|invalid/i)).toBeInTheDocument();
    });

    it('should handle empty result gracefully', () => {
        render(<ValidationPanel result={null} />);

        expect(screen.queryByText(/valid/i)).not.toBeInTheDocument();
    });

    it('should display multiple validation errors', () => {
        const result = {
            isValid: false,
            validationResultMessages: [
                'Error 1: Invalid namespace',
                'Error 2: Missing required attribute',
                'Error 3: Invalid data type'
            ]
        };

        render(<ValidationPanel result={result} />);

        expect(screen.getByText(/error 1/i)).toBeInTheDocument();
        expect(screen.getByText(/error 2/i)).toBeInTheDocument();
        expect(screen.getByText(/error 3/i)).toBeInTheDocument();
    });
});

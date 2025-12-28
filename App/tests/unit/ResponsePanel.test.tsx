import { render, screen } from '@testing-library/react';
import { describe, it, expect } from 'vitest';
import ResponsePanel from '../../src/components/ResponsePanel';

/**
 * UNIT TEST REMARKS:
 * Component: ResponsePanel
 * 
 * Test Cases:
 * 1. Renders "Response Body" header.
 * 2. Auto-formats displayed XML content (since it calls formatXml on render).
 * 3. Renders empty gracefully.
 * 4. Verify read-only state.
 */

describe('ResponsePanel Component', () => {

    it('renders header and textarea', () => {
        render(<ResponsePanel xmlContent="" />);
        expect(screen.getByText('Response Body')).toBeInTheDocument();
        expect(screen.getByRole('textbox')).toBeInTheDocument();
    });

    it('is read-only', () => {
        render(<ResponsePanel xmlContent="test" />);
        const input = screen.getByRole('textbox');
        expect(input).toHaveAttribute('readonly');
    });

    it('automatically formats XML content on render', () => {
        const rawXml = '<Root><Child>Content</Child></Root>';
        // Logic splits by \r\n and pads with 2 spaces
        // Regex: ()(<)(\/*) -> $1\r\n$2$3
        // <Root>\r\n<Child>Content</Child>\r\n</Root> -> then loop pads it.
        // It's a visual format. 

        render(<ResponsePanel xmlContent={rawXml} />);

        const input = screen.getByRole('textbox');
        // We expect it to NOT be the raw one line string
        expect(input).not.toHaveValue(rawXml);
        const val = (input as HTMLTextAreaElement).value;
        expect(val).toContain('<Root>');
        expect(val).toContain('  <Child>'); // Check for indentation
    });

    it('handles empty content safely', () => {
        render(<ResponsePanel xmlContent="" />);
        const input = screen.getByRole('textbox');
        expect(input).toHaveValue('');
    });

    it('handles invalid or plain text content gracefully', () => {
        // If it fails regex or logic, it returns original (based on catch block)
        // actually existing logic is surprisingly robust/permissive regex.
        // Let's pass null to see if it handles empty string guard from prop types? 
        // Typescript prevents null, but empty string is handled.

        // Logic parses XML tags. Plain text without tags is returned as is by regex or catch?
        // Actually the regex /(>)(<)(\/*)/g won't match "Not XML content", so it returns original.
        // But the split loop might still run. 
        // Let's just check it contains the text.
        const plainText = 'Not XML content';
        render(<ResponsePanel xmlContent={plainText} />);
        expect((screen.getByRole('textbox') as HTMLTextAreaElement).value).toContain(plainText);
    });

});

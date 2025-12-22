import React, { useRef } from 'react';
import { Form, Card } from 'react-bootstrap';

const RequestPanel = ({ xmlContent, onChange, error }) => {
    const textareaRef = useRef(null);

    const formatXml = () => {
        if (!xmlContent) return;
        try {
            let formatted = '';
            let reg = /(>)(<)(\/*)/g;
            let xml = xmlContent.replace(reg, '$1\r\n$2$3');
            let pad = 0;
            const nodes = xml.split('\r\n');

            nodes.forEach((node) => {
                let indent = 0;
                if (node.match(/.+<\/\w[^>]*>$/)) {
                    indent = 0;
                } else if (node.match(/^<\/\w/)) {
                    if (pad !== 0) {
                        pad -= 1;
                    }
                } else if (node.match(/^<\w[^>]*[^\/]>.*$/)) {
                    indent = 1;
                } else {
                    indent = 0;
                }

                let padding = '';
                for (let i = 0; i < pad; i++) {
                    padding += '  ';
                }

                formatted += padding + node + '\r\n';
                pad += indent;
            });

            onChange(formatted.trim());

            // Reset scroll position to top-left
            if (textareaRef.current) {
                textareaRef.current.scrollTop = 0;
                textareaRef.current.scrollLeft = 0;
            }
        } catch (e) {
            console.error("Format error", e);
        }
    };

    return (
        <Card className="mb-3">
            <Card.Header className="d-flex justify-content-between align-items-center">
                <span>Request Body</span>
                <button
                    className="btn btn-sm btn-outline-secondary"
                    onClick={formatXml}
                    title="Format XML"
                    style={{ lineHeight: 1, padding: '0.2rem 0.5rem' }}
                >
                    Format
                </button>
            </Card.Header>
            <Card.Body style={{ padding: 0 }}>
                <Form.Control
                    as="textarea"
                    ref={textareaRef}
                    value={xmlContent}
                    onChange={(e) => onChange(e.target.value)}
                    style={{
                        fontFamily: 'monospace',
                        whiteSpace: 'pre',
                        height: '300px',
                        maxHeight: '500px',
                        resize: 'vertical',
                        overflowY: 'auto',
                        border: 'none',
                        borderRadius: 0
                    }}
                    isInvalid={!!error}
                />
                <Form.Control.Feedback type="invalid">
                    {error}
                </Form.Control.Feedback>
            </Card.Body>
        </Card>
    );
};

export default RequestPanel;

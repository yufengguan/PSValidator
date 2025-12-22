import React from 'react';
import { Form, Card } from 'react-bootstrap';

const ResponsePanel = ({ xmlContent, className = "mb-3" }) => {
    const formatXml = (xml) => {
        if (!xml) return '';
        try {
            let formatted = '';
            let reg = /(>)(<)(\/*)/g;
            xml = xml.replace(reg, '$1\r\n$2$3');
            let pad = 0;
            xml.split('\r\n').forEach((node) => {
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
            return formatted;
        } catch (e) {
            return xml;
        }
    };

    return (
        <Card className={className}>
            <Card.Header>Response Body</Card.Header>
            <Card.Body style={{ padding: 0 }}>
                <Form.Control
                    as="textarea"
                    value={formatXml(xmlContent)}
                    readOnly
                    style={{
                        fontFamily: 'monospace',
                        whiteSpace: 'pre',
                        backgroundColor: '#f8f9fa',
                        height: '400px',
                        maxHeight: '600px',
                        resize: 'vertical',
                        overflowY: 'auto',
                        border: 'none',
                        borderRadius: 0
                    }}
                />
            </Card.Body>
        </Card>
    );
};

export default ResponsePanel;

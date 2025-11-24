import React from 'react';
import { Form, Card } from 'react-bootstrap';

const ResponseSchemaPanel = ({ schemaContent }) => {
    return (
        <Card className="mb-3">
            <Card.Header>Testing -- Response Schema</Card.Header>
            <Card.Body style={{ padding: 0 }}>
                <Form.Control
                    as="textarea"
                    value={schemaContent || ''}
                    readOnly
                    style={{
                        fontFamily: 'monospace',
                        whiteSpace: 'pre',
                        height: '300px',
                        maxHeight: '500px',
                        resize: 'vertical',
                        overflowY: 'auto',
                        border: 'none',
                        borderRadius: 0,
                        backgroundColor: '#f8f9fa' // Light gray for read-only
                    }}
                />
            </Card.Body>
        </Card>
    );
};

export default ResponseSchemaPanel;

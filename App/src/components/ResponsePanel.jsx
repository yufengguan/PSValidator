import React from 'react';
import { Form, Card } from 'react-bootstrap';

const ResponsePanel = ({ xmlContent }) => {
    return (
        <Card className="mb-3">
            <Card.Header>Response Body</Card.Header>
            <Card.Body>
                <Form.Control
                    as="textarea"
                    rows={10}
                    value={xmlContent}
                    readOnly
                    style={{ fontFamily: 'monospace', whiteSpace: 'pre', backgroundColor: '#f8f9fa' }}
                />
            </Card.Body>
        </Card>
    );
};

export default ResponsePanel;

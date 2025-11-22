import React from 'react';
import { Form, Card } from 'react-bootstrap';

const RequestPanel = ({ xmlContent, onChange }) => {
    return (
        <Card className="mb-3">
            <Card.Header>Request Body</Card.Header>
            <Card.Body>
                <Form.Control
                    as="textarea"
                    rows={10}
                    value={xmlContent}
                    onChange={(e) => onChange(e.target.value)}
                    style={{ fontFamily: 'monospace', whiteSpace: 'pre' }}
                />
            </Card.Body>
        </Card>
    );
};

export default RequestPanel;

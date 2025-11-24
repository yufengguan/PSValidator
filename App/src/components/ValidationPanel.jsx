import React from 'react';
import { Card, Alert } from 'react-bootstrap';

const ValidationPanel = ({ result }) => {
    return (
        <Card className="mb-3">
            <Card.Header>Validation Result</Card.Header>
            <Card.Body style={{ maxHeight: '300px', overflowY: 'auto' }}>
                {!result ? (
                    <div className="text-muted">No validation performed yet.</div>
                ) : result.isValid ? (
                    <Alert variant="success">Validation Successful</Alert>
                ) : (
                    <Alert variant="danger">
                        <Alert.Heading>Validation Failed</Alert.Heading>
                        <ul style={{ marginBottom: 0 }}>
                            {result.validationResultMessages.map((msg, idx) => (
                                <li key={idx}>{msg}</li>
                            ))}
                        </ul>
                    </Alert>
                )}
            </Card.Body>
        </Card>
    );
};

export default ValidationPanel;

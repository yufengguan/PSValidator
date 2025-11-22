import React from 'react';
import { Card, Alert } from 'react-bootstrap';

const ValidationPanel = ({ result }) => {
    if (!result) return null;

    return (
        <Card className="mb-3">
            <Card.Header>Validation Result</Card.Header>
            <Card.Body>
                {result.isValid ? (
                    <Alert variant="success">Validation Successful</Alert>
                ) : (
                    <Alert variant="danger">
                        <Alert.Heading>Validation Failed</Alert.Heading>
                        <ul>
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

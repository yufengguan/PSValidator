import React from 'react';
import { Card, Alert } from 'react-bootstrap';
import { ValidationResult } from '../types';

interface ValidationPanelProps {
    result: ValidationResult | null;
}

const ValidationPanel: React.FC<ValidationPanelProps> = ({ result }) => {
    // Debug log
    console.log("ValidationPanel Result:", result);

    const getTitle = () => {
        if (!result) return "Validation Results";
        const baseTitle = result.type ? `${result.type} Validation Results` : "Validation Results";
        const status = result.isValid ? "Success" : "Failed";
        return `${baseTitle}: ${status}`;
    };

    const cleanMessage = (msg: string) => {
        if (!msg) return "";
        // Remove "Error:" prefix if present
        return msg.replace(/^Error:\s*/i, '');
    };

    const getHeaderClass = () => {
        if (!result) return "";
        return result.isValid ? "bg-success text-white" : "bg-danger text-white";
    };

    return (
        <Card className="mb-3">
            <Card.Header className={getHeaderClass()}>
                <strong>{getTitle()}</strong>
            </Card.Header>
            <Card.Body style={{ maxHeight: '300px', overflowY: 'auto' }}>
                {!result ? (
                    <div className="text-muted">No validation performed yet.</div>
                ) : result.isValid ? (
                    <Alert variant="success">Validation Successful</Alert>
                ) : (
                    <Alert variant="danger">
                        <ul style={{ marginBottom: '1rem', paddingLeft: '1.2rem' }}>
                            {result.validationResultMessages.map((msg, idx) => (
                                <li key={idx}>{cleanMessage(msg)}</li>
                            ))}
                        </ul>
                        {result.validationResultMessages.some(msg => msg !== "Request Body cannot be empty.") && (
                            <div style={{ fontSize: '0.9rem', borderTop: '1px solid #f5c6cb', paddingTop: '0.5rem', marginTop: '0.5rem' }}>
                                <small>
                                    <strong>Note:</strong> Some structural errors may prevent further validation. If you still have issues after fixing the above, please re-validate to uncover deeper errors.
                                </small>
                            </div>
                        )}
                    </Alert>
                )}
            </Card.Body>
        </Card>
    );
};

export default ValidationPanel;

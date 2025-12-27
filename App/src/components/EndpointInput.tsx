import React from 'react';
import { Form, Row, Col, InputGroup } from 'react-bootstrap';

interface EndpointInputProps {
    endpoint: string;
    onChange: (endpoint: string) => void;
    error?: string;
}

const EndpointInput: React.FC<EndpointInputProps> = ({ endpoint, onChange, error }) => {
    return (
        <Row className="mb-3">
            <Col>
                <InputGroup hasValidation>
                    <InputGroup.Text>Endpoint URL</InputGroup.Text>
                    <Form.Control
                        type="text"
                        placeholder="https://..."
                        value={endpoint}
                        onChange={(e: React.ChangeEvent<HTMLInputElement>) => onChange(e.target.value)}
                        isInvalid={!!error}
                        aria-label="Endpoint URL"
                    />
                    <Form.Control.Feedback type="invalid">
                        {error}
                    </Form.Control.Feedback>
                </InputGroup>
            </Col>
        </Row>
    );
};

export default EndpointInput;

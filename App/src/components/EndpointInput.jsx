import React from 'react';
import { Form, Row, Col, InputGroup } from 'react-bootstrap';

const EndpointInput = ({ endpoint, onChange, error }) => {
    return (
        <Row className="mb-3">
            <Col>
                <InputGroup hasValidation>
                    <InputGroup.Text>Endpoint URL</InputGroup.Text>
                    <Form.Control
                        type="text"
                        placeholder="https://..."
                        value={endpoint}
                        onChange={(e) => onChange(e.target.value)}
                        isInvalid={!!error}
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

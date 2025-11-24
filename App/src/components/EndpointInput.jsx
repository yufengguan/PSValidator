import React from 'react';
import { Form, Row, Col } from 'react-bootstrap';

const EndpointInput = ({ endpoint, onChange, error }) => {
    return (
        <Row className="mb-3">
            <Col>
                <Form.Group>
                    <Form.Label>Endpoint URL</Form.Label>
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
                </Form.Group>
            </Col>
        </Row>
    );
};

export default EndpointInput;

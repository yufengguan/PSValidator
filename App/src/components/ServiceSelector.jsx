import React, { useState, useEffect } from 'react';
import { Form, Row, Col, InputGroup } from 'react-bootstrap';

const ServiceSelector = ({ services, selection, onSelectionChange, error }) => {
    // Derived state from props
    const selectedService = selection?.service || '';
    const selectedVersion = selection?.version || '';
    const selectedOperation = selection?.operation || '';

    const handleServiceChange = (e) => {
        const serviceName = e.target.value;
        onSelectionChange({ service: serviceName, version: '', operation: '' });
    };

    const handleVersionChange = (e) => {
        const version = e.target.value;
        onSelectionChange({ service: selectedService, version: version, operation: '' });
    };

    const handleOperationChange = (e) => {
        const operation = e.target.value;
        onSelectionChange({ service: selectedService, version: selectedVersion, operation: operation });
    };

    const currentService = services.find(s => s.ServiceName === selectedService);

    // Helper to format version string
    const getVersionString = (v) => `${v.Major}.${v.Minor}.${v.Patch}`;

    const currentVersion = currentService?.Versions.find(v => getVersionString(v) === selectedVersion);

    return (
        <Row className="mb-3">
            <Col md={3}>
                <InputGroup>
                    <InputGroup.Text>Service</InputGroup.Text>
                    <Form.Select value={selectedService} onChange={handleServiceChange}>
                        <option value="">Select...</option>
                        {services.map(s => (
                            <option key={s.ServiceId} value={s.ServiceName}>{s.ServiceName}</option>
                        ))}
                    </Form.Select>
                </InputGroup>
            </Col>
            <Col md={2}>
                <InputGroup>
                    <InputGroup.Text>Version</InputGroup.Text>
                    <Form.Select value={selectedVersion} onChange={handleVersionChange} disabled={!selectedService}>
                        <option value="">...</option>
                        {currentService?.Versions.map((v, idx) => (
                            <option key={idx} value={getVersionString(v)}>{getVersionString(v)}</option>
                        ))}
                    </Form.Select>
                </InputGroup>
            </Col>
            <Col md={7}>
                <InputGroup hasValidation>
                    <InputGroup.Text>Operation</InputGroup.Text>
                    <Form.Select
                        value={selectedOperation}
                        onChange={handleOperationChange}
                        disabled={!selectedVersion}
                        isInvalid={!!error}
                    >
                        <option value="">Select Operation...</option>
                        {currentVersion?.Operations.map((op, idx) => (
                            <option key={idx} value={op.OperationName}>{op.OperationName}</option>
                        ))}
                    </Form.Select>
                    <Form.Control.Feedback type="invalid">
                        {error}
                    </Form.Control.Feedback>
                </InputGroup>
            </Col>
        </Row>
    );
};

export default ServiceSelector;

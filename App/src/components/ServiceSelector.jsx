import React, { useState, useEffect } from 'react';
import { Form, Row, Col } from 'react-bootstrap';

const ServiceSelector = ({ services, onSelectionChange }) => {
    const [selectedService, setSelectedService] = useState('');
    const [selectedVersion, setSelectedVersion] = useState('');
    const [selectedOperation, setSelectedOperation] = useState('');

    const handleServiceChange = (e) => {
        const serviceName = e.target.value;
        setSelectedService(serviceName);
        setSelectedVersion('');
        setSelectedOperation('');
        onSelectionChange({ service: serviceName, version: '', operation: '' });
    };

    const handleVersionChange = (e) => {
        const version = e.target.value;
        setSelectedVersion(version);
        setSelectedOperation('');
        onSelectionChange({ service: selectedService, version: version, operation: '' });
    };

    const handleOperationChange = (e) => {
        const operation = e.target.value;
        setSelectedOperation(operation);
        onSelectionChange({ service: selectedService, version: selectedVersion, operation: operation });
    };

    const currentService = services.find(s => s.ServiceName === selectedService);

    // Helper to format version string
    const getVersionString = (v) => `${v.Major}.${v.Minor}.${v.Patch}`;

    const currentVersion = currentService?.Versions.find(v => getVersionString(v) === selectedVersion);

    return (
        <Row className="mb-3">
            <Col md={4}>
                <Form.Group>
                    <Form.Label>Web Service</Form.Label>
                    <Form.Select value={selectedService} onChange={handleServiceChange}>
                        <option value="">Select Service...</option>
                        {services.map(s => (
                            <option key={s.ServiceId} value={s.ServiceName}>{s.ServiceName}</option>
                        ))}
                    </Form.Select>
                </Form.Group>
            </Col>
            <Col md={4}>
                <Form.Group>
                    <Form.Label>Version</Form.Label>
                    <Form.Select value={selectedVersion} onChange={handleVersionChange} disabled={!selectedService}>
                        <option value="">Select Version...</option>
                        {currentService?.Versions.map((v, idx) => (
                            <option key={idx} value={getVersionString(v)}>{getVersionString(v)}</option>
                        ))}
                    </Form.Select>
                </Form.Group>
            </Col>
            <Col md={4}>
                <Form.Group>
                    <Form.Label>Operation</Form.Label>
                    <Form.Select value={selectedOperation} onChange={handleOperationChange} disabled={!selectedVersion}>
                        <option value="">Select Operation...</option>
                        {currentVersion?.Operations.map((op, idx) => (
                            <option key={idx} value={op.OperationName}>{op.OperationName}</option>
                        ))}
                    </Form.Select>
                </Form.Group>
            </Col>
        </Row>
    );
};

export default ServiceSelector;

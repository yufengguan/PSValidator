import React from 'react';
import { Modal, Button } from 'react-bootstrap';

const AboutModal = ({ show, onHide }) => {
    return (
        <Modal show={show} onHide={onHide} size="lg" centered>
            <Modal.Header closeButton style={{ background: 'linear-gradient(90deg, #1e3a8a 0%, #2563eb 50%, #3b82f6 100%)', color: '#ffffff', borderBottom: '3px solid #1e40af' }}>
                <Modal.Title>About PromoStandards Web Service Validator</Modal.Title>
            </Modal.Header>
            <Modal.Body style={{ padding: '2rem' }}>
                <h5 style={{ color: '#2563eb', marginBottom: '1rem' }}>What is this tool?</h5>
                <p style={{ lineHeight: '1.6', color: '#333' }}>
                    The PromoStandards Web Service Validator is a testing tool designed to help developers validate their
                    PromoStandards web service implementations. It provides real-time validation of XML requests and responses
                    against official PromoStandards XSD schemas.
                </p>

                <h5 style={{ color: '#2563eb', marginTop: '2rem', marginBottom: '1rem' }}>Key Features</h5>
                <ul style={{ lineHeight: '1.8', color: '#333' }}>
                    <li>Validate XML against PromoStandards schemas</li>
                    <li>Generate sample requests from XSD definitions</li>
                    <li>Test live endpoints with SOAP requests</li>
                    <li>View response schemas for validation</li>
                    <li>Support for multiple PromoStandards versions</li>
                </ul>

                <h5 style={{ color: '#2563eb', marginTop: '2rem', marginBottom: '1rem' }}>About PromoStandards</h5>
                <p style={{ lineHeight: '1.6', color: '#333' }}>
                    PromoStandards is a nonprofit organization that creates and maintains technology standards for the
                    promotional products industry. These standards enable seamless integration between suppliers,
                    distributors, and service providers.
                </p>

                <p style={{ lineHeight: '1.6', color: '#333', marginTop: '1rem' }}>
                    Learn more at <a href="https://promostandards.org" target="_blank" rel="noopener noreferrer" style={{ color: '#2563eb' }}>PromoStandards.org</a>
                </p>
            </Modal.Body>
            <Modal.Footer>
                <Button variant="primary" onClick={onHide} style={{ background: '#2563eb', borderColor: '#2563eb' }}>
                    Close
                </Button>
            </Modal.Footer>
        </Modal>
    );
};

export default AboutModal;

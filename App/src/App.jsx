import React, { useState, useEffect } from 'react';
import { Container, Button, Navbar, Spinner } from 'react-bootstrap';
import ServiceSelector from './components/ServiceSelector';
import EndpointInput from './components/EndpointInput';
import RequestPanel from './components/RequestPanel';
import ResponsePanel from './components/ResponsePanel';
import ValidationPanel from './components/ValidationPanel';
import './App.css';

function App() {
  const [services, setServices] = useState([]);
  const [selection, setSelection] = useState({ service: '', version: '', operation: '' });
  const [endpoint, setEndpoint] = useState('');
  const [requestXml, setRequestXml] = useState('');
  const [responseXml, setResponseXml] = useState('');
  const [validationResult, setValidationResult] = useState(null);
  const [loading, setLoading] = useState(false);

  const API_BASE_URL = import.meta.env.VITE_API_URL || '/api';

  useEffect(() => {
    fetch(`${API_BASE_URL}/ServiceList`)
      .then(res => res.json())
      .then(data => setServices(data))
      .catch(err => console.error('Error fetching services:', err));
  }, []);

  const handleSelectionChange = (newSelection) => {
    setSelection(newSelection);
    // Reset other fields when selection changes
    if (newSelection.operation !== selection.operation) {
      setRequestXml(''); // In real app, generate sample XML here
      setResponseXml('');
      setValidationResult(null);
    }
  };

  const handleValidate = async () => {
    setLoading(true);
    setValidationResult(null);

    try {
      const res = await fetch(`${API_BASE_URL}/Validator/validate`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          service: selection.service,
          version: selection.version,
          operation: selection.operation,
          xmlContent: requestXml // Using request XML for testing validation
        })
      });
      const result = await res.json();
      setValidationResult(result);
      // For demo, copy request to response panel to show "what was validated"
      setResponseXml(requestXml);
    } catch (err) {
      console.error('Validation error:', err);
      setValidationResult({ isValid: false, validationResultMessages: ['Network or Server Error'] });
    } finally {
      setLoading(false);
    }
  };

  return (
    <>
      <Navbar bg="dark" variant="dark" className="mb-4">
        <Container>
          <Navbar.Brand href="#home">PromoStandards Validator</Navbar.Brand>
        </Container>
      </Navbar>

      <Container>
        <ServiceSelector services={services} onSelectionChange={handleSelectionChange} />

        <EndpointInput endpoint={endpoint} onChange={setEndpoint} />

        <RequestPanel xmlContent={requestXml} onChange={setRequestXml} />

        <div className="d-grid gap-2 mb-3">
          <Button variant="primary" size="lg" onClick={handleValidate} disabled={!selection.operation || !requestXml || loading}>
            {loading ? <Spinner animation="border" size="sm" /> : 'Validate'}
          </Button>
        </div>

        <ResponsePanel xmlContent={responseXml} />

        <ValidationPanel result={validationResult} />
      </Container>
    </>
  );
}

export default App;

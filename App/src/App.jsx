import React, { useState, useEffect } from 'react';
import { Container, Row, Col, Button, Navbar, Spinner, Alert } from 'react-bootstrap';
import ServiceSelector from './components/ServiceSelector';
import EndpointInput from './components/EndpointInput';
import RequestPanel from './components/RequestPanel';
import ResponsePanel from './components/ResponsePanel';
import ValidationPanel from './components/ValidationPanel';
import ResponseSchemaPanel from './components/ResponseSchemaPanel';
import './App.css';

function App() {
  const [services, setServices] = useState([]);
  const [selection, setSelection] = useState({ service: '', version: '', operation: '' });
  const [endpoint, setEndpoint] = useState('');
  const [requestXml, setRequestXml] = useState('');
  const [responseXml, setResponseXml] = useState('');
  const [responseSchema, setResponseSchema] = useState('');
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
    // 3.3.2: When Service changes, clear all panels and Endpoint
    if (newSelection.service !== selection.service) {
      setSelection(newSelection);
      setEndpoint('');
      setRequestXml('');
      setResponseXml('');
      setResponseSchema('');
      setValidationResult(null);
      return;
    }

    // 3.3.3: When Operation changes, clear Request Body, Response Body, and Validation Result (keep Endpoint)
    // 3.6.2: When Operation changes, automatically generate and display a sample XML request
    if (newSelection.operation !== selection.operation) {
      setSelection(newSelection);

      if (newSelection.operation) {
        // Fetch sample request from API
        fetch(`${API_BASE_URL}/Validator/sample-request?service=${newSelection.service}&version=${newSelection.version}&operation=${newSelection.operation}`)
          .then(res => res.json())
          .then(data => {
            if (data.xmlContent) {
              setRequestXml(data.xmlContent);
            }
          })
          .catch(err => {
            console.error('Error fetching sample request:', err);
            setRequestXml(`<!-- Error generating sample: ${err.message} -->`);
          });

        // Fetch response schema from API
        fetch(`${API_BASE_URL}/Validator/response-schema?serviceName=${newSelection.service}&version=${newSelection.version}&operationName=${newSelection.operation}`)
          .then(res => res.text()) // It returns string
          .then(data => {
            setResponseSchema(data);
          })
          .catch(err => {
            console.error('Error fetching response schema:', err);
            setResponseSchema(`<!-- Error fetching schema: ${err.message} -->`);
          });

      } else {
        setRequestXml('');
        setResponseSchema('');
      }

      setResponseXml('');
      setValidationResult(null);
      return;
    }

    // For version changes or other updates
    setSelection(newSelection);
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
          xmlContent: requestXml,
          endpoint: endpoint
        })
      });
      const result = await res.json();
      setValidationResult(result);

      if (result.responseContent) {
        setResponseXml(result.responseContent);
      }
    } catch (err) {
      console.error('Validation error:', err);
      setValidationResult({ isValid: false, validationResultMessages: ['Network or Server Error'] });
    } finally {
      setLoading(false);
    }
  };

  const getUrlError = (string) => {
    if (!string) return null; // Don't show error if empty (unless touched, but keeping simple)
    try {
      const url = new URL(string);
      if (!url.hostname) {
        return "Invalid URL: Hostname is missing";
      }
      if (url.hostname.startsWith('.')) {
        return "Invalid URL: Hostname cannot start with a dot";
      }
      if (url.hostname.endsWith('.')) {
        return "Invalid URL: Hostname cannot end with a dot";
      }
      if (url.hostname !== 'localhost' && !url.hostname.includes('.')) {
        return "Invalid URL: Hostname must contain a domain extension (e.g. .com)";
      }
      if (url.protocol !== "http:" && url.protocol !== "https:") {
        return "Invalid URL: Protocol must be http or https";
      }
      return null;
    } catch (_) {
      return "Invalid URL format. Must start with http:// or https://";
    }
  };

  const getXmlError = (xmlString) => {
    if (!xmlString) return null;
    try {
      const parser = new DOMParser();
      const doc = parser.parseFromString(xmlString, "application/xml");
      const errorNode = doc.querySelector("parsererror");
      if (errorNode) {
        return "Invalid XML: " + errorNode.textContent;
      }
      return null;
    } catch (e) {
      return "Error parsing XML";
    }
  };

  const urlError = getUrlError(endpoint);
  const xmlError = getXmlError(requestXml);

  const httpErrorMessage = validationResult?.validationResultMessages?.find(m => m.startsWith('HTTP Error') || m.startsWith('Network Error'));

  const filteredValidationResult = validationResult ? {
    ...validationResult,
    validationResultMessages: validationResult.validationResultMessages.filter(m => !m.startsWith('HTTP Error') && !m.startsWith('Network Error'))
  } : null;

  // Only valid if fields are filled AND have no errors
  const isFormValid = selection.operation && endpoint && !urlError && requestXml && !xmlError;

  return (
    <>
      <Navbar bg="dark" variant="dark" className="mb-4">
        <Container>
          <Navbar.Brand href="#home">PromoStandards Web Service Validator</Navbar.Brand>
        </Container>
      </Navbar>

      <Container fluid style={{ paddingLeft: '2rem', paddingRight: '2rem' }}>
        <ServiceSelector services={services} onSelectionChange={handleSelectionChange} />

        <EndpointInput endpoint={endpoint} onChange={setEndpoint} error={urlError} />

        <RequestPanel xmlContent={requestXml} onChange={setRequestXml} error={xmlError} />

        <div className="d-grid gap-2 mb-3">
          <Button variant="primary" size="lg" onClick={handleValidate} disabled={!isFormValid || loading}>
            {loading ? <Spinner animation="border" size="sm" /> : 'Validate'}
          </Button>
        </div>

        {httpErrorMessage && (
          <Alert variant="danger" className="mb-3">
            {httpErrorMessage}
          </Alert>
        )}

        <Row>
          <Col md={12}>
            <ResponsePanel xmlContent={responseXml} />
          </Col>
        </Row>
        <Row>
          <Col md={12}>
            <ValidationPanel result={filteredValidationResult} />
          </Col>
        </Row>

        <Row>
          <Col md={12}>
            <ResponseSchemaPanel schemaContent={responseSchema} />
          </Col>
        </Row>
      </Container>
    </>
  );
}

export default App;

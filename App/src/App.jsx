import React, { useState, useEffect } from 'react';
import { Container, Navbar, Button, Spinner, Alert, Row, Col, Card } from 'react-bootstrap';
import ServiceSelector from './components/ServiceSelector';
import EndpointInput from './components/EndpointInput';
import RequestPanel from './components/RequestPanel';
import ResponsePanel from './components/ResponsePanel';
import ValidationPanel from './components/ValidationPanel';
import AboutModal from './components/AboutModal';
import './App.css';

function App() {
  const [services, setServices] = useState([]);
  const [selection, setSelection] = useState({ service: '', version: '', operation: '' });
  const [endpoint, setEndpoint] = useState('');
  const [requestXml, setRequestXml] = useState('');
  const [responseXml, setResponseXml] = useState('');
  const [requestSchema, setRequestSchema] = useState('');
  const [responseSchema, setResponseSchema] = useState('');
  const [activeSchema, setActiveSchema] = useState('none');

  const [showAboutModal, setShowAboutModal] = useState(false);
  const [validationResult, setValidationResult] = useState(null);
  const [loading, setLoading] = useState(false);

  // New error states for inline validation
  const [operationError, setOperationError] = useState('');
  const [endpointError, setEndpointError] = useState('');
  const [requestError, setRequestError] = useState('');

  const API_BASE_URL = import.meta.env.VITE_API_URL || '/api';

  useEffect(() => {
    // Session ID Generation
    let sessionId = sessionStorage.getItem('ps_validator_session_id');
    if (!sessionId) {
      sessionId = crypto.randomUUID();
      sessionStorage.setItem('ps_validator_session_id', sessionId);
    }

    fetch(`${API_BASE_URL}/ServiceList`)
      .then(res => res.json())
      .then(data => setServices(data))
      .catch(err => console.error('Error fetching services:', err));
  }, []);

  const handleSelectionChange = (newSelection) => {
    setOperationError(''); // Clear error on change
    setRequestError('');

    // 3.3.2: When Service changes, clear all panels and Endpoint
    if (newSelection.service !== selection.service) {
      setSelection(newSelection);
      setEndpoint('');
      setEndpointError('');
      setRequestXml('');
      setResponseXml('');
      setRequestSchema('');
      setResponseSchema('');
      setActiveSchema('none'); // Clear panel
      setValidationResult(null);
      return;
    }

    // 3.3.3: When Operation changes, clear Request Body, Response Body, and Validation Result (keep Endpoint)
    // 3.6.2: When Operation changes, automatically generate and display a sample XML request
    if (newSelection.operation !== selection.operation) {
      setSelection(newSelection);

      if (newSelection.operation) {
        // Fetch sample request from API
        fetch(`${API_BASE_URL}/Validator/sample-request?serviceName=${newSelection.service}&version=${newSelection.version}&operationName=${newSelection.operation}`)
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
      } else {
        setRequestXml('');
      }

      setResponseXml('');
      setValidationResult(null);
      return;
    }

    // For version changes or other updates
    setSelection(newSelection);
  };

  const handleValidateResponse = async () => {
    // Reset inline errors
    setOperationError('');
    setEndpointError('');
    setRequestError('');

    let hasError = false;

    // Validation checks
    if (!selection.operation) {
      setOperationError("Please select a Service Operation first.");
      hasError = true;
    }
    if (!endpoint) {
      setEndpointError("Please enter an Endpoint URL.");
      hasError = true;
    }

    // Check validation blocks
    if (!requestXml) {
      setRequestError("Request Body cannot be empty.");
      hasError = true;
    } else if (xmlError) {
      setRequestError(`Invalid Request XML: ${getXmlError(requestXml)}`);
      hasError = true;
    }

    // If inline errors exists, we stop here. 
    // If requestXml errors exists, validationResult is set, so we also stop.
    if (hasError) return;

    setLoading(true);
    setValidationResult(null);

    try {
      const res = await fetch(`${API_BASE_URL}/Validator/validate-response`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'X-Session-ID': sessionStorage.getItem('ps_validator_session_id') || 'unknown'
        },
        body: JSON.stringify({
          service: selection.service,
          version: selection.version,
          operation: selection.operation,
          xmlContent: requestXml, // Note: Usage of requestXml for response validation workflow if manual input
          endpoint: endpoint
        })
      });

      let result;
      const contentType = res.headers.get("content-type");
      if (contentType && contentType.indexOf("application/json") !== -1) {
        result = await res.json();
      } else {
        const text = await res.text();
        result = { isValid: false, validationResultMessages: [`Server Error: ${res.status} ${res.statusText}`, text] };
      }

      if (!res.ok) {
        // Handle 400/500 errors that might return ProblemDetails or plain text
        const messages = result.validationResultMessages || [];
        if (result.title) messages.push(`Error: ${result.title}`); // ASP.NET Core ProblemDetails
        if (messages.length === 0) messages.push(`HTTP Error: ${res.status}`);
        console.log("Validation Failed (HTTP Error):", result);
        setValidationResult({ type: 'Response', isValid: false, validationResultMessages: messages });
      } else {
        console.log("Validation API Response (Success):", result);
        setValidationResult({ ...result, type: 'Response' });
        if (result.responseContent) {
          setResponseXml(result.responseContent);
        }
        setActiveSchema('response');
      }

    } catch (err) {
      console.error('Validation error:', err);
      setValidationResult({ type: 'Response', isValid: false, validationResultMessages: [`Network Error: ${err.message}`] });
    } finally {
      setLoading(false);
    }
  };

  const handleValidateRequest = async () => {
    setOperationError('');
    setRequestError('');
    let hasError = false;

    // Validation checks
    if (!selection.operation) {
      setOperationError("Please select a Service Operation first.");
      hasError = true;
    }

    if (!requestXml) {
      setRequestError("Request Body cannot be empty.");
      hasError = true;
    } else if (xmlError) {
      setRequestError(`Invalid Request XML: ${getXmlError(requestXml)}`);
      hasError = true;
    }

    if (hasError) return;

    setLoading(true);
    setValidationResult(null);

    try {
      const res = await fetch(`${API_BASE_URL}/Validator/validate-request`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'X-Session-ID': sessionStorage.getItem('ps_validator_session_id') || 'unknown'
        },
        body: JSON.stringify({
          service: selection.service,
          version: selection.version,
          operation: selection.operation,
          xmlContent: requestXml
        })
      });

      let result;
      const contentType = res.headers.get("content-type");
      if (contentType && contentType.indexOf("application/json") !== -1) {
        result = await res.json();
      } else {
        const text = await res.text();
        result = { isValid: false, validationResultMessages: [`Server Error: ${res.status} ${res.statusText}`, text] };
      }

      if (!res.ok) {
        const messages = result.validationResultMessages || [];
        if (result.title) messages.push(`Error: ${result.title}`);
        if (messages.length === 0) messages.push(`HTTP Error: ${res.status}`);
        setValidationResult({ type: 'Request', isValid: false, validationResultMessages: messages });
      } else {
        setValidationResult({ ...result, type: 'Request' });
      }
    } catch (err) {
      console.error('Validation error:', err);
      setValidationResult({ type: 'Request', isValid: false, validationResultMessages: [`Network Error: ${err.message}`] });
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
    validationResultMessages: (validationResult.validationResultMessages || []).filter(m => !m.startsWith('HTTP Error') && !m.startsWith('Network Error'))
  } : null;

  return (
    <>
      <Container fluid style={{ paddingLeft: '2rem', paddingRight: '2rem', marginTop: '0' }}>
        <div style={{
          width: '100%',
          height: '60px',
          backgroundColor: '#f8f9fa',
          borderBottom: '1px solid #dee2e6',
          display: 'flex',
          justifyContent: 'center',
          alignItems: 'center',
          marginBottom: '1rem',
          borderRadius: '0.25rem'
        }}>
          <h3 style={{ margin: 0, fontSize: '1.5rem', fontWeight: 'bold', color: '#212529' }}>Web Service Validator</h3>
        </div>
        <ServiceSelector
          services={services}
          onSelectionChange={handleSelectionChange}
          error={operationError} // Pass error to selector
        />

        <EndpointInput
          endpoint={endpoint}
          onChange={(val) => { setEndpoint(val); setEndpointError(''); }} // Clear error on input
          error={urlError || endpointError} // Prefer urlError derived check, then required check
        />

        <RequestPanel xmlContent={requestXml} onChange={(val) => { setRequestXml(val); setRequestError(''); }} error={xmlError || requestError} />

        <div className="d-flex gap-2 mb-3">
          <Button variant="outline-primary" size="lg" onClick={handleValidateRequest} disabled={loading} className="flex-grow-1 btn-outline-custom">
            {loading ? <Spinner animation="border" size="sm" /> : 'Validate Request'}
          </Button>
          <Button variant="primary" size="lg" onClick={handleValidateResponse} disabled={loading} className="flex-grow-1 btn-primary-custom">
            {loading ? <Spinner animation="border" size="sm" /> : 'Validate Response'}
          </Button>
        </div>

        {httpErrorMessage && (
          <Alert variant="danger" className="mb-3">
            {httpErrorMessage}
          </Alert>
        )}

        <Row>
          <Col md={12}>
            {validationResult?.type === 'Response' && <ResponsePanel xmlContent={responseXml} className="mb-0" />}
          </Col>
        </Row>



        <Row>
          <Col md={12}>
            <ValidationPanel result={filteredValidationResult} />
          </Col>
        </Row>



      </Container>



      <AboutModal show={showAboutModal} onHide={() => setShowAboutModal(false)} />
    </>
  );
}

export default App;

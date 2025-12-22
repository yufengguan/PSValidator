import React, { useState, useEffect } from 'react';
import { Container, Navbar, Button, Spinner, Alert, Row, Col } from 'react-bootstrap';
import ServiceSelector from './components/ServiceSelector';
import EndpointInput from './components/EndpointInput';
import RequestPanel from './components/RequestPanel';
import ResponsePanel from './components/ResponsePanel';
import ValidationPanel from './components/ValidationPanel';
import AboutModal from './components/AboutModal';
import logo from './assets/logo.png';
import './App.css';

function App() {
  const [services, setServices] = useState([]);
  const [selection, setSelection] = useState({ service: '', version: '', operation: '' });
  const [endpoint, setEndpoint] = useState('');
  const [requestXml, setRequestXml] = useState('');
  const [responseXml, setResponseXml] = useState('');

  const [showAboutModal, setShowAboutModal] = useState(false);
  const [validationResult, setValidationResult] = useState(null);
  const [loading, setLoading] = useState(false);

  // New error states for inline validation
  const [operationError, setOperationError] = useState('');
  const [endpointError, setEndpointError] = useState('');
  const [requestError, setRequestError] = useState('');

  const API_BASE_URL = import.meta.env.VITE_API_URL || '/api';

  useEffect(() => {
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
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          service: selection.service,
          version: selection.version,
          operation: selection.operation,
          xmlContent: requestXml,
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
        setValidationResult({ type: 'Response', isValid: false, validationResultMessages: messages });
      } else {
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
        headers: { 'Content-Type': 'application/json' },
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
      {/* Header with PromoStandards Logo */}
      <header style={{
        background: 'rgb(38, 56, 128)',
        borderBottom: '3px solid #0066cc',
        boxShadow: '0 2px 8px rgba(0,0,0,0.2)'
      }}>
        <Container fluid className="py-3" style={{ position: 'relative' }}>
          <div className="d-flex align-items-center">
            <div style={{ position: 'absolute', left: '10rem', display: 'flex', alignItems: 'center' }}>
              <a href="https://promostandards.org" target="_blank" rel="noopener noreferrer" style={{ display: 'flex', alignItems: 'center' }}>
                <img
                  src={logo}
                  alt="PromoStandards"
                  style={{ height: '50px', marginRight: '2.5rem' }}
                />
              </a>
            </div>
            <div style={{ marginLeft: 'calc(10rem + 200px)', borderLeft: '2px solid rgba(255,255,255,0.3)', paddingLeft: '1.5rem', height: '40px', display: 'flex', alignItems: 'center' }}>
              <h1 style={{ margin: 0, fontSize: '1.5rem', fontWeight: '600', color: '#ffffff' }}>Web Service Validator</h1>
            </div>
          </div>
        </Container>
      </header>

      <Container fluid style={{ paddingLeft: '2rem', paddingRight: '2rem', marginTop: '2rem' }}>
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
            {validationResult?.type === 'Response' && <ResponsePanel xmlContent={responseXml} />}
          </Col>
        </Row>
        <Row>
          <Col md={12}>
            <ValidationPanel result={filteredValidationResult} />
          </Col>
        </Row>



      </Container>

      {/* Footer styled like PromoStandards.org */}
      <footer style={{ marginTop: '4rem' }}>
        {/* Main footer content */}
        <div style={{
          background: 'rgb(16, 31, 90)',
          borderTop: '3px solid rgba(255,255,255,0.1)'
        }}>
          <Container className="py-4">
            <Row className="mb-4">
              <Col md={4} className="mb-3 mb-md-0">
                <h5 style={{ color: '#ffffff', fontWeight: '600', marginBottom: '1rem', fontSize: '1.1rem' }}>About This Tool</h5>
                <p style={{ color: '#e8e8e8', fontSize: '0.9rem', lineHeight: '1.6' }}>
                  The PromoStandards Web Service Validator helps developers test and validate their PromoStandards web service implementations.
                </p>
                <a
                  href="#"
                  onClick={(e) => { e.preventDefault(); setShowAboutModal(true); }}
                  style={{ color: '#ffffff', textDecoration: 'underline', fontSize: '0.9rem' }}
                >
                  Learn more →
                </a>
              </Col>
              <Col md={4} className="mb-3 mb-md-0">
                <h5 style={{ color: '#ffffff', fontWeight: '600', marginBottom: '1rem', fontSize: '1.1rem' }}>Quick Links</h5>
                <ul style={{ listStyle: 'none', padding: 0, margin: 0 }}>
                  <li style={{ marginBottom: '0.5rem' }}>
                    <a href="https://promostandards.org/standards-services/" target="_blank" rel="noopener noreferrer" style={{ color: '#e8e8e8', textDecoration: 'none', fontSize: '0.9rem' }}>
                      Documentation
                    </a>
                  </li>
                  <li style={{ marginBottom: '0.5rem' }}>
                    <a href="https://github.com/yufengguan/PSValidator" target="_blank" rel="noopener noreferrer" style={{ color: '#e8e8e8', textDecoration: 'none', fontSize: '0.9rem' }}>
                      <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" fill="currentColor" viewBox="0 0 16 16" style={{ marginRight: '0.4rem', marginBottom: '2px' }}>
                        <path d="M8 0C3.58 0 0 3.58 0 8c0 3.54 2.29 6.53 5.47 7.59.4.07.55-.17.55-.38 0-.19-.01-.82-.01-1.49-2.01.37-2.53-.49-2.69-.94-.09-.23-.48-.94-.82-1.13-.28-.15-.68-.52-.01-.53.63-.01 1.08.58 1.23.82.72 1.21 1.87.87 2.33.66.07-.52.28-.87.51-1.07-1.78-.2-3.64-.89-3.64-3.95 0-.87.31-1.59.82-2.15-.08-.2-.36-1.02.08-2.12 0 0 .67-.21 2.2.82.64-.18 1.32-.27 2-.27.68 0 1.36.09 2 .27 1.53-1.04 2.2-.82 2.2-.82.44 1.1.16 1.92.08 2.12.51.56.82 1.27.82 2.15 0 3.07-1.87 3.75-3.65 3.95.29.25.54.73.54 1.48 0 1.07-.01 1.93-.01 2.2 0 .21.15.46.55.38A8.012 8.012 0 0 0 16 8c0-4.42-3.58-8-8-8z" />
                      </svg>
                      Source Code
                    </a>
                  </li>
                </ul>
              </Col>
              <Col md={4}>
                <h5 style={{ color: '#ffffff', fontWeight: '600', marginBottom: '1rem', fontSize: '1.1rem' }}>Contact</h5>
                <p style={{ color: '#e8e8e8', fontSize: '0.9rem', lineHeight: '1.6', marginBottom: '0.5rem' }}>
                  For questions about PromoStandards:
                </p>
                <p style={{ color: '#e8e8e8', fontSize: '0.9rem' }}>
                  <a href="mailto:admin@promostandards.org" style={{ color: '#ffffff', textDecoration: 'none' }}>admin@promostandards.org</a>
                </p>
              </Col>
            </Row>
          </Container>
        </div>

        {/* Copyright section with darker background */}
        <div style={{ background: 'rgb(16, 31, 90)', padding: '1.5rem 0', opacity: 0.9 }}>
          <Container>
            <Row>
              <Col className="text-center">
                <p style={{ color: 'rgba(255,255,255,0.9)', fontSize: '0.85rem', margin: 0 }}>
                  PromoStandards © {new Date().getFullYear()}
                </p>
              </Col>
            </Row>
          </Container>
        </div>
      </footer>

      <AboutModal show={showAboutModal} onHide={() => setShowAboutModal(false)} />
    </>
  );
}

export default App;

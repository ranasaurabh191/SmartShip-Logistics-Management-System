// features/shipment/CreateShipmentWizard.tsx
import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { apiClient } from '../../core/api/axios';
import { useNotificationStore } from '../../store/useNotificationStore';
import {
  validateAddressSection, validatePackageSection, hasErrors,
  type AddressErrors, type PackageErrors,
} from '../../utils/shipmentValidation';

const steps = [
  { key: 'sender', label: 'Sender Details' },
  { key: 'receiver', label: 'Receiver Details' },
  { key: 'package', label: 'Package Info' },
  { key: 'review', label: 'Review & Create' },
];

type AddressSection = {
  fullName: string; phone: string; street: string;
  city: string; state: string; postalCode: string; country: string;
};
type PackageSection = {
  weightKg: string; lengthCm: string; widthCm: string;
  heightCm: string; description: string;
};
type FormDataType = {
  sender: AddressSection; receiver: AddressSection;
  package: PackageSection; shipmentType: string; notes: string;
};
type ShipmentResponse = {
  id: number; trackingNumber: string; customerId: number;
  shipmentType: string; status: string; shippingRate: number; createdAt: string;
  senderAddress: AddressSection; receiverAddress: AddressSection;
  package: { weightKg: number; lengthCm: number; widthCm: number; heightCm: number; description: string };
};
type PaymentResponse = {
  id: number; shipmentId?: number | null; trackingNumber: string;
  amount: number; paymentMethod: string; paymentStatus: string;
  razorpayOrderId?: string | null; razorpayPaymentId?: string | null;
  createdAt: string; paidAt?: string | null; message?: string | null;
};

const PAYMENTMETHOD = { COD: 0, ONLINE: 1 };
const RAZORPAY_KEY_ID = import.meta.env.VITE_RAZORPAY_KEY_ID as string;
const COUNTRIES = ['India', 'United States', 'United Kingdom', 'Canada', 'Australia', 'Germany', 'UAE', 'Singapore', 'Other'];

const inputErrorStyle: React.CSSProperties = {
  color: '#ff6b6b', fontSize: 12, marginTop: 4, fontFamily: 'Inter, sans-serif',
};

export const CreateShipmentWizard = () => {
  const navigate = useNavigate();
  const addNotification = useNotificationStore(state => state.addNotification);
  const [activeStep, setActiveStep] = useState(0);
  const [submitting, setSubmitting] = useState(false);
  const [formData, setFormData] = useState<FormDataType>({
    sender: { fullName: '', phone: '', street: '', city: '', state: '', postalCode: '', country: 'India' },
    receiver: { fullName: '', phone: '', street: '', city: '', state: '', postalCode: '', country: 'India' },
    package: { weightKg: '', lengthCm: '', widthCm: '', heightCm: '', description: '' },
    shipmentType: 'Domestic', notes: '',
  });

  const [senderErrors, setSenderErrors] = useState<AddressErrors>({});
  const [receiverErrors, setReceiverErrors] = useState<AddressErrors>({});
  const [packageErrors, setPackageErrors] = useState<PackageErrors>({});

  const [estimatedRate, setEstimatedRate] = useState<number | null>(null);
  const [rateLoading, setRateLoading] = useState(false);
  const [rateError, setRateError] = useState('');
  const [createdShipment, setCreatedShipment] = useState<ShipmentResponse | null>(null);
  const [paymentStageOpen, setPaymentStageOpen] = useState(false);
  const [selectedPaymentMode, setSelectedPaymentMode] = useState<'COD' | 'ONLINE' | null>(null);
  const [paymentLoading, setPaymentLoading] = useState(false);
  const [paymentError, setPaymentError] = useState('');
  const [paymentSuccess, setPaymentSuccess] = useState('');
  const [paymentResponse, setPaymentResponse] = useState<PaymentResponse | null>(null);

  // Rate estimation
  useEffect(() => {
    const weight = Number(formData.package.weightKg);
    const type = formData.shipmentType?.trim();
    if (!weight || weight <= 0 || !type) { setEstimatedRate(null); setRateError(''); return; }
    const timer = setTimeout(async () => {
      setRateLoading(true); setRateError('');
      try {
        const res = await apiClient.get('/shipments/rate', { params: { weight, type } });
        const rate = res?.data?.rate;
        setEstimatedRate(typeof rate === 'number' ? rate : Number(rate) || null);
      } catch (err: any) {
        setEstimatedRate(null);
        setRateError(err?.response?.data?.message || err?.response?.data || 'Unable to calculate rate');
      } finally { setRateLoading(false); }
    }, 400);
    return () => clearTimeout(timer);
  }, [formData.package.weightKg, formData.shipmentType]);

  const handleChange = (section: 'sender' | 'receiver' | 'package', field: string, value: string) => {
    setFormData(prev => ({ ...prev, [section]: { ...prev[section], [field]: value } }));
    if (section === 'sender') setSenderErrors(p => ({ ...p, [field]: '' }));
    if (section === 'receiver') setReceiverErrors(p => ({ ...p, [field]: '' }));
    if (section === 'package') setPackageErrors(p => ({ ...p, [field]: '' }));
  };

  const handleNext = async () => {
    if (activeStep === 0) {
      const errs = validateAddressSection(formData.sender, 'Sender');
      setSenderErrors(errs);
      if (hasErrors(errs)) return;
    }
    if (activeStep === 1) {
      const errs = validateAddressSection(formData.receiver, 'Receiver');
      setReceiverErrors(errs);
      if (hasErrors(errs)) return;
    }
    if (activeStep === 2) {
      const errs = validatePackageSection(formData.package);
      setPackageErrors(errs);
      if (hasErrors(errs)) return;
    }
    if (activeStep === steps.length - 1) await submitShipment();
    else setActiveStep(prev => prev + 1);
  };

  const handleBack = () => setActiveStep(prev => prev - 1);

  const submitShipment = async () => {
    try {
      setSubmitting(true);
      const payload = {
        senderAddress: { ...formData.sender },
        receiverAddress: { ...formData.receiver },
        package: {
          weightKg: Number(formData.package.weightKg), lengthCm: Number(formData.package.lengthCm),
          widthCm: Number(formData.package.widthCm), heightCm: Number(formData.package.heightCm),
          description: formData.package.description,
        },
        shipmentType: formData.shipmentType, pickupScheduledAt: null,
        notes: formData.notes || 'Created via SmartShip frontend',
      };
      const res = await apiClient.post('/shipments', payload);
      setCreatedShipment(res.data);
      setPaymentStageOpen(true);
      setPaymentError(''); setPaymentSuccess(''); setPaymentResponse(null);
    } catch (err: any) {
      addNotification(err?.response?.data?.message || err?.response?.data || 'Failed to create shipment.', 'error');
    } finally { setSubmitting(false); }
  };

  // Launch real Razorpay checkout after order is created
  const launchRazorpay = (payment: PaymentResponse, shipmentId: number) => {
    if (!window.Razorpay) {
      setPaymentError('Razorpay SDK not loaded. Please refresh and try again.');
      return;
    }
    if (!payment.razorpayOrderId) {
      setPaymentError('No Razorpay Order ID returned. Please try again.');
      return;
    }

    setPaymentLoading(false); // We've handed off to Razorpay UI

    const options: RazorpayOptions = {
      key: RAZORPAY_KEY_ID,
      amount: payment.amount * 100,
      currency: 'INR',
      name: 'SmartShip',
      description: `Shipment ${payment.trackingNumber}`,
      order_id: payment.razorpayOrderId,

      handler: async (response: RazorpayPaymentResponse) => {
        setPaymentLoading(true);
        setPaymentError(''); setPaymentSuccess('');
        try {
          const res = await apiClient.post('/payment/verify', {
            razorpayOrderId: response.razorpay_order_id,
            razorpayPaymentId: response.razorpay_payment_id,
            signature: response.razorpay_signature,
            shipmentId,
            paymentMethod: 'Online',
          });
          setPaymentResponse(res.data);
          setPaymentSuccess('✅ Payment verified successfully! You can now schedule pickup.');
        } catch (err: any) {
          setPaymentError(
            err?.response?.data?.message ||
              'Payment made but verification failed. Go to My Payments → Retry Verify.'
          );
        } finally {
          setPaymentLoading(false);
        }
      },

      theme: { color: '#e0001a' },

      modal: {
        ondismiss: () => {
          setPaymentSuccess('');
          setPaymentError('Razorpay checkout was closed. Click "Pay Online" to retry.');
          setPaymentLoading(false);
        },
      },
    };

    const rzp = new window.Razorpay(options);
    rzp.open();
  };

  const handleCreateOrder = async (mode: 'COD' | 'ONLINE') => {
    if (!createdShipment?.id) return;
    setSelectedPaymentMode(mode);
    setPaymentLoading(true);
    setPaymentError(''); setPaymentSuccess(''); setPaymentResponse(null);
    try {
      const res = await apiClient.post('/payment/create-order', {
        shipmentId: createdShipment.id,
        paymentMethod: mode === 'COD' ? PAYMENTMETHOD.COD : PAYMENTMETHOD.ONLINE,
      });
      const payment = res.data as PaymentResponse;
      setPaymentResponse(payment);

      if (mode === 'COD') {
        setPaymentSuccess(payment.message || '✅ COD registered. You can now schedule pickup from Shipments.');
        setPaymentLoading(false);
        return;
      }

      // Online: open real Razorpay checkout (launchRazorpay handles setPaymentLoading)
      launchRazorpay(payment, createdShipment.id);
    } catch (err: any) {
      setPaymentError(err?.response?.data?.message || err?.response?.data || 'Unable to create payment order.');
      setPaymentLoading(false);
    }
  };

  const formatRate = () => {
    if (rateLoading) return 'CALCULATING...';
    if (rateError) return rateError.toUpperCase();
    if (estimatedRate !== null) return `₹${estimatedRate.toLocaleString('en-IN')}`;
    return '₹ 0';
  };

  const fieldLabel = (_text: string, hasError: boolean): React.CSSProperties => ({
    display: 'block', fontFamily: 'Orbitron, monospace', fontSize: 11,
    fontWeight: 600, letterSpacing: '0.14em', textTransform: 'uppercase',
    color: hasError ? '#ff6b6b' : '#e0001a', marginBottom: 6,
  });

  const renderAddressField = (
    section: 'sender' | 'receiver', field: string, label: string,
    placeholder: string, errors: AddressErrors, wide?: boolean, isSelect?: boolean,
  ) => {
    const err = (errors as any)[field];
    return (
      <div key={field} style={wide ? { gridColumn: '1 / -1' } : {}}>
        <label style={fieldLabel(label, !!err)}>{label}</label>
        {isSelect ? (
          <select className="ss-input" style={{ width: '100%', borderColor: err ? '#ff6b6b' : undefined }}
            value={(formData[section] as any)[field]}
            onChange={e => handleChange(section, field, e.target.value)}>
            {COUNTRIES.map(c => <option key={c} value={c}>{c}</option>)}
          </select>
        ) : (
          <input className="ss-input" style={{ width: '100%', borderColor: err ? '#ff6b6b' : undefined }}
            placeholder={placeholder}
            value={(formData[section] as any)[field]}
            onChange={e => handleChange(section, field, e.target.value)}
            onBlur={() => {
              if (section === 'sender') setSenderErrors(validateAddressSection(formData.sender, 'Sender'));
              if (section === 'receiver') setReceiverErrors(validateAddressSection(formData.receiver, 'Receiver'));
            }}
          />
        )}
        {err && <div style={inputErrorStyle}>⚠ {err}</div>}
      </div>
    );
  };

  const renderPkgField = (field: keyof PackageSection, label: string, placeholder: string) => {
    const err = packageErrors[field];
    return (
      <div key={field}>
        <label style={fieldLabel(label, !!err)}>{label}</label>
        <input className="ss-input" style={{ width: '100%', borderColor: err ? '#ff6b6b' : undefined }}
          placeholder={placeholder} value={formData.package[field]}
          type={field === 'description' ? 'text' : 'number'}
          onChange={e => handleChange('package', field, e.target.value)}
          onBlur={() => setPackageErrors(validatePackageSection(formData.package))}
        />
        {err && <div style={inputErrorStyle}>⚠ {err}</div>}
      </div>
    );
  };

  const renderStep = () => {
    if (activeStep === 0 || activeStep === 1) {
      const sec = activeStep === 0 ? 'sender' : 'receiver';
      const errors = activeStep === 0 ? senderErrors : receiverErrors;
      return (
        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 14 }}>
          {renderAddressField(sec, 'fullName', 'Full Name', 'Enter full name', errors)}
          {renderAddressField(sec, 'phone', 'Phone Number', 'Enter phone number', errors)}
          {renderAddressField(sec, 'street', 'Street Address', 'House no., building, street name', errors, true)}
          {renderAddressField(sec, 'city', 'City', 'Enter city', errors)}
          {renderAddressField(sec, 'state', 'State', 'Enter state', errors)}
          {renderAddressField(sec, 'postalCode', 'Postal Code', '6-digit PIN code', errors)}
          {renderAddressField(sec, 'country', 'Country', '', errors, false, true)}
        </div>
      );
    }

    if (activeStep === 2) {
      return (
        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 14 }}>
          <div style={{ gridColumn: '1 / -1' }}>
            <label style={fieldLabel('Shipment Type', false)}>Shipment Type</label>
            <select className="ss-input" style={{ width: '100%' }} value={formData.shipmentType}
              onChange={e => setFormData(p => ({ ...p, shipmentType: e.target.value }))}>
              <option value="Domestic">Domestic</option>
              <option value="International">International</option>
            </select>
          </div>
          {renderPkgField('weightKg', 'Weight (kg)', 'e.g. 2.5')}
          {renderPkgField('description', 'Description', 'Type of item')}
          {renderPkgField('lengthCm', 'Length (cm)', '30')}
          {renderPkgField('widthCm', 'Width (cm)', '20')}
          {renderPkgField('heightCm', 'Height (cm)', '15')}
          <div style={{ gridColumn: '1 / -1' }}>
            <label style={fieldLabel('Notes', false)}>Notes</label>
            <textarea className="ss-input" style={{ width: '100%', resize: 'vertical' }}
              placeholder="Optional handling instructions" value={formData.notes}
              onChange={e => setFormData(p => ({ ...p, notes: e.target.value }))} />
          </div>
          <div style={{ gridColumn: '1 / -1' }}>
            <label style={fieldLabel('Estimated Rate ₹', !!rateError)}>Estimated Rate ₹</label>
            <div className="ss-input" style={{ width: '100%', fontSize: 18, display: 'flex', alignItems: 'center', minHeight: 38, color: rateError ? 'var(--color-accent)' : '#fff', fontFamily: 'Orbitron, monospace', fontWeight: 900, letterSpacing: '0.06em' }}>
              {formatRate()}
            </div>
          </div>
        </div>
      );
    }

    // Step 3 — Review
    const items = [
      { label: 'From', value: `${formData.sender.city} — ${formData.sender.country}` },
      { label: 'To', value: `${formData.receiver.city} — ${formData.receiver.country}` },
      { label: 'Sender', value: `${formData.sender.fullName} · ${formData.sender.phone}` },
      { label: 'Receiver', value: `${formData.receiver.fullName} · ${formData.receiver.phone}` },
      { label: 'Weight', value: formData.package.weightKg ? `${formData.package.weightKg} kg` : '—' },
      { label: 'Type', value: formData.shipmentType || '—' },
      { label: 'Description', value: formData.package.description || '—' },
    ];

    return (
      <div style={{ display: 'flex', flexDirection: 'column', gap: 20 }}>
        <div style={{ background: 'var(--color-surface-2)', border: '1px solid var(--color-border)', borderRadius: 4, padding: 24 }}>
          <div style={{ fontFamily: 'Roboto, sans-serif', fontSize: 13, fontWeight: 700, letterSpacing: '0.08em', textTransform: 'uppercase', color: '#fff', marginBottom: 16 }}>
            Shipment Summary
          </div>
          {items.map(item => (
            <div key={item.label} style={{ display: 'flex', justifyContent: 'space-between', padding: '8px 0', borderBottom: '1px solid rgba(255,255,255,0.04)' }}>
              <span style={{ fontSize: 12, color: 'var(--color-text-muted)', fontFamily: 'Roboto, sans-serif', letterSpacing: '0.08em', textTransform: 'uppercase' }}>{item.label}</span>
              <span style={{ fontSize: 13, color: '#fff', fontFamily: 'Roboto, sans-serif', fontWeight: 600 }}>{item.value}</span>
            </div>
          ))}
          <div style={{ display: 'flex', justifyContent: 'space-between', padding: '12px 0', marginTop: 4 }}>
            <span style={{ fontFamily: 'Roboto, sans-serif', fontSize: 14, fontWeight: 700, letterSpacing: '0.08em', textTransform: 'uppercase', color: '#fff' }}>Estimated Cost</span>
            <span style={{ fontFamily: 'Orbitron, monospace', fontSize: 18, fontWeight: 700, color: rateError ? 'var(--color-warning)' : 'var(--color-accent)' }}>
              {rateLoading ? 'CALCULATING...' : estimatedRate !== null ? `₹${estimatedRate.toLocaleString('en-IN')}` : '—'}
            </span>
          </div>
        </div>
        <div style={{ fontSize: 12, color: 'var(--color-text-muted)', display: 'flex', alignItems: 'center', gap: 8 }}>
          <span style={{ color: 'var(--color-warning)', fontSize: 14 }}>⚠</span>
          Shipment will be created in Draft first. Payment happens next — COD or real Razorpay checkout.
        </div>
      </div>
    );
  };

  return (
    <div style={{ maxWidth: 860 }}>
      <div onClick={() => navigate(-1)} style={{ fontFamily: 'Orbitron, monospace', fontSize: 15, color: '#ff0000', fontWeight: 900, letterSpacing: '0.1em', cursor: 'pointer', marginBottom: 30 }}>
        ← BACK TO SHIPMENTS
      </div>
      <div className="accent-line" style={{ marginBottom: 8 }} />
      <h1 className="section-title">New Shipment</h1>
      <p className="section-sub" style={{ marginBottom: 24 }}>Create shipment, choose payment mode, then schedule pickup from shipments.</p>

      {!paymentStageOpen && (
        <>
          {/* Step Indicators */}
          <div style={{ display: 'flex', marginBottom: 32, gap: 0 }}>
            {steps.map((s, i) => (
              <div key={s.key} style={{ flex: 1, display: 'flex', flexDirection: 'column', alignItems: 'center', position: 'relative' }}>
                {i > 0 && (
                  <div style={{ position: 'absolute', left: '-50%', top: 14, width: '100%', height: 1, background: i <= activeStep ? 'var(--color-accent)' : 'var(--color-border)' }} />
                )}
                <div style={{
                  width: 28, height: 28, borderRadius: 10, display: 'flex', alignItems: 'center', justifyContent: 'center',
                  background: i <= activeStep ? 'var(--color-accent)' : 'var(--color-surface-2)',
                  border: `1px solid ${i <= activeStep ? 'var(--color-accent)' : 'var(--color-border)'}`,
                  fontFamily: 'Orbitron, monospace', fontSize: 15, fontWeight: 700,
                  color: i <= activeStep ? '#fff' : '#555', zIndex: 1,
                  boxShadow: i === activeStep ? '0 0 10px rgba(224,0,26,0.4)' : 'none',
                }}>
                  {i < activeStep ? '✓' : i + 1}
                </div>
                <div style={{ fontFamily: 'Orbitron, monospace', fontSize: 11, fontWeight: 700, letterSpacing: '0.12em', textTransform: 'uppercase', color: i === activeStep ? 'var(--color-accent)' : '#555', marginTop: 6 }}>
                  {s.label}
                </div>
              </div>
            ))}
          </div>

          <div className="ss-card" style={{ padding: 28, marginBottom: 20 }}>
            <div style={{ fontFamily: 'Orbitron, monospace', fontSize: 14, fontWeight: 700, letterSpacing: '0.08em', textTransform: 'uppercase', color: '#fff', marginBottom: 20, borderBottom: '1px solid var(--color-border)', paddingBottom: 12 }}>
              Step {activeStep + 1} — {steps[activeStep].label}
            </div>
            {renderStep()}
          </div>

          <div style={{ display: 'flex', justifyContent: 'space-between' }}>
            <button className="ss-btn ss-btn-outline" disabled={activeStep === 0 || submitting}
              onClick={handleBack} style={{ opacity: activeStep === 0 || submitting ? 0.3 : 1 }}>
              Back
            </button>
            <button className="ss-btn" onClick={handleNext} disabled={submitting} style={{ opacity: submitting ? 0.7 : 1 }}>
              {submitting ? 'CREATING...' : activeStep === steps.length - 1 ? 'Create Shipment →' : 'Continue →'}
            </button>
          </div>
        </>
      )}

      {/* Payment Stage */}
      {paymentStageOpen && createdShipment && (
        <div className="ss-card" style={{ padding: 28 }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', gap: 20, flexWrap: 'wrap', marginBottom: 20, borderBottom: '1px solid var(--color-border)', paddingBottom: 16 }}>
            <div>
              <div className="section-title" style={{ fontSize: 18 }}>Shipment Created ✓</div>
              <div className="section-sub">Tracking: <strong style={{ color: 'var(--color-accent)' }}>{createdShipment.trackingNumber}</strong> · Status: {createdShipment.status}</div>
            </div>
            <div style={{ textAlign: 'right' }}>
              <div className="kpi-label">Shipping Rate</div>
              <div className="kpi-value">₹{Number(createdShipment.shippingRate || 0).toLocaleString('en-IN')}</div>
            </div>
          </div>

          {/* Payment Buttons */}
          {/* Only show if not yet paid */}
          {paymentResponse?.paymentStatus !== 'Paid' && (
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 16, marginBottom: 16 }}>
              <button className="ss-btn"
                disabled={paymentLoading}
                onClick={() => handleCreateOrder('COD')}
                style={{ opacity: paymentLoading && selectedPaymentMode === 'COD' ? 0.7 : 1 }}>
                {paymentLoading && selectedPaymentMode === 'COD' ? 'CREATING...' : 'Cash on Delivery'}
              </button>
              <button className="ss-btn ss-btn-outline"
                disabled={paymentLoading}
                onClick={() => handleCreateOrder('ONLINE')}
                style={{ opacity: paymentLoading && selectedPaymentMode === 'ONLINE' ? 0.7 : 1 }}>
                {paymentLoading && selectedPaymentMode === 'ONLINE' ? 'OPENING RAZORPAY...' : 'Pay Online'}
              </button>
            </div>
          )}

          {paymentError && (
            <div style={{ padding: 12, border: '1px solid rgba(224,0,26,0.35)', background: 'rgba(224,0,26,0.08)', color: '#ff8d8d', borderRadius: 4, marginBottom: 12 }}>
              {paymentError}
            </div>
          )}
          {paymentSuccess && (
            <div style={{ padding: 12, border: '1px solid rgba(80,180,120,0.35)', background: 'rgba(80,180,120,0.08)', color: '#99e6b3', borderRadius: 4, marginBottom: 12 }}>
              {paymentSuccess}
            </div>
          )}

          {/* Payment Details Card */}
          {paymentResponse && (
            <div style={{ marginTop: 16, padding: 20, border: '1px solid var(--color-border)', background: 'var(--color-surface-2)', borderRadius: 4 }}>
              <div style={{ fontFamily: 'Orbitron, monospace', fontSize: 13, fontWeight: 700, letterSpacing: '0.1em', textTransform: 'uppercase', marginBottom: 14, color: '#fff' }}>
                Payment Details
              </div>
              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12 }}>
                <div><div className="kpi-label">Method</div><div>{paymentResponse.paymentMethod}</div></div>
                <div>
                  <div className="kpi-label">Status</div>
                  <span className={`ss-badge ${paymentResponse.paymentStatus === 'Paid' ? 'success' : ''}`}>
                    {paymentResponse.paymentStatus}
                  </span>
                </div>
                <div><div className="kpi-label">Amount</div><div>₹{Number(paymentResponse.amount || 0).toLocaleString('en-IN')}</div></div>
                <div><div className="kpi-label">Tracking</div><div style={{ color: 'var(--color-accent)' }}>{paymentResponse.trackingNumber}</div></div>
                {paymentResponse.razorpayOrderId && (
                  <div style={{ gridColumn: '1 / -1' }}>
                    <div className="kpi-label">Razorpay Order ID</div>
                    <div style={{ fontFamily: 'Orbitron, monospace', color: 'var(--color-text-muted)', wordBreak: 'break-all', fontSize: 12 }}>
                      {paymentResponse.razorpayOrderId}
                    </div>
                  </div>
                )}
                {paymentResponse.razorpayPaymentId && (
                  <div style={{ gridColumn: '1 / -1' }}>
                    <div className="kpi-label">Razorpay Payment ID</div>
                    <div style={{ fontFamily: 'Roboto, sans-serif', color: 'var(--color-success)', wordBreak: 'break-all', fontSize: 12 }}>
                      {paymentResponse.razorpayPaymentId}
                    </div>
                  </div>
                )}
              </div>
            </div>
          )}

          <div style={{ marginTop: 22, display: 'flex', justifyContent: 'space-between', gap: 12, flexWrap: 'wrap' }}>
            <button className="ss-btn ss-btn-outline" onClick={() => navigate('/customer/dashboard')}>Go to Dashboard</button>
            <button className="ss-btn" onClick={() => navigate('/customer/shipments')}>Open Shipments</button>
          </div>
        </div>
      )}
    </div>
  );
};
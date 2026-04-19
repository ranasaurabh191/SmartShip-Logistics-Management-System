import { useEffect, useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { apiClient } from '../../core/api/axios';

const steps = [
  { key: 'sender', label: 'Sender Details' },
  { key: 'receiver', label: 'Receiver Details' },
  { key: 'package', label: 'Package Info' },
  { key: 'review', label: 'Review & Create' },
];

type AddressSection = {
  fullName: string;
  phone: string;
  street: string;
  city: string;
  state: string;
  postalCode: string;
  country: string;
};

type PackageSection = {
  weightKg: string;
  lengthCm: string;
  widthCm: string;
  heightCm: string;
  description: string;
};

type FormDataType = {
  sender: AddressSection;
  receiver: AddressSection;
  package: PackageSection;
  shipmentType: string;
  notes: string;
};

type ShipmentResponse = {
  id: number;
  trackingNumber: string;
  customerId: number;
  shipmentType: string;
  status: string;
  shippingRate: number;
  createdAt: string;
  pickupScheduledAt?: string | null;
  deliveredAt?: string | null;
  senderAddress: AddressSection;
  receiverAddress: AddressSection;
  package: {
    weightKg: number;
    lengthCm: number;
    widthCm: number;
    heightCm: number;
    description: string;
  };
  notes?: string | null;
};

type PaymentResponse = {
  id: number;
  shipmentId?: number | null;
  trackingNumber: string;
  amount: number;
  paymentMethod: string;
  paymentStatus: string;
  razorpayOrderId?: string | null;
  razorpayPaymentId?: string | null;
  createdAt: string;
  paidAt?: string | null;
  message?: string | null;
};

const PAYMENT_METHOD = {
  COD: 0,
  ONLINE: 1,
};

export const CreateShipmentWizard = () => {
  const navigate = useNavigate();

  const [activeStep, setActiveStep] = useState(0);
  const [submitting, setSubmitting] = useState(false);

  const [formData, setFormData] = useState<FormDataType>({
    sender: {
      fullName: '',
      phone: '',
      street: '',
      city: '',
      state: '',
      postalCode: '',
      country: 'India',
    },
    receiver: {
      fullName: '',
      phone: '',
      street: '',
      city: '',
      state: '',
      postalCode: '',
      country: 'India',
    },
    package: {
      weightKg: '',
      lengthCm: '',
      widthCm: '',
      heightCm: '',
      description: '',
    },
    shipmentType: 'Domestic',
    notes: '',
  });

  const [estimatedRate, setEstimatedRate] = useState<number | null>(null);
  const [rateLoading, setRateLoading] = useState(false);
  const [rateError, setRateError] = useState('');

  const [createdShipment, setCreatedShipment] = useState<ShipmentResponse | null>(null);
  const [paymentStageOpen, setPaymentStageOpen] = useState(false);
  const [selectedPaymentMode, setSelectedPaymentMode] = useState<'COD' | 'ONLINE' | ''>('');
  const [paymentLoading, setPaymentLoading] = useState(false);
  const [paymentError, setPaymentError] = useState('');
  const [paymentSuccess, setPaymentSuccess] = useState('');
  const [paymentResponse, setPaymentResponse] = useState<PaymentResponse | null>(null);

  const [verifyForm, setVerifyForm] = useState({
    razorpayPaymentId: '',
    signature: '',
  });

  useEffect(() => {
    const weight = Number(formData.package.weightKg);
    const type = formData.shipmentType?.trim();

    if (!weight || weight <= 0 || !type) {
      setEstimatedRate(null);
      setRateError('');
      return;
    }

    const timer = setTimeout(async () => {
      setRateLoading(true);
      setRateError('');

      try {
        const res = await apiClient.get('/shipments/rate', {
          params: { weight, type },
        });

        const rate = res?.data?.rate;
        setEstimatedRate(typeof rate === 'number' ? rate : Number(rate) || null);
      } catch (err: any) {
        setEstimatedRate(null);
        setRateError(
          err?.response?.data?.message ||
          err?.response?.data ||
          'Unable to calculate rate'
        );
      } finally {
        setRateLoading(false);
      }
    }, 400);

    return () => clearTimeout(timer);
  }, [formData.package.weightKg, formData.shipmentType]);

  const canContinue = useMemo(() => {
    if (activeStep === 0) {
      return !!(
        formData.sender.fullName &&
        formData.sender.phone &&
        formData.sender.street &&
        formData.sender.city &&
        formData.sender.state &&
        formData.sender.postalCode
      );
    }

    if (activeStep === 1) {
      return !!(
        formData.receiver.fullName &&
        formData.receiver.phone &&
        formData.receiver.street &&
        formData.receiver.city &&
        formData.receiver.state &&
        formData.receiver.postalCode
      );
    }

    if (activeStep === 2) {
      return !!(
        formData.package.weightKg &&
        formData.package.lengthCm &&
        formData.package.widthCm &&
        formData.package.heightCm &&
        formData.package.description &&
        Number(formData.package.weightKg) > 0 &&
        Number(formData.package.lengthCm) > 0 &&
        Number(formData.package.widthCm) > 0 &&
        Number(formData.package.heightCm) > 0
      );
    }

    return true;
  }, [activeStep, formData]);

  const handleNext = async () => {
    if (activeStep === steps.length - 1) {
      await submitShipment();
    } else {
      setActiveStep((prev) => prev + 1);
    }
  };

  const handleBack = () => setActiveStep((prev) => prev - 1);

  const handleChange = (
    section: 'sender' | 'receiver' | 'package',
    field: string,
    value: string
  ) => {
    setFormData((prev) => ({
      ...prev,
      [section]: {
        ...prev[section],
        [field]: value,
      },
    }));
  };

  const handleShipmentTypeChange = (value: string) => {
    setFormData((prev) => ({
      ...prev,
      shipmentType: value,
    }));
  };

  const submitShipment = async () => {
    try {
      setSubmitting(true);

      const payload = {
        senderAddress: {
          fullName: formData.sender.fullName,
          phone: formData.sender.phone,
          street: formData.sender.street,
          city: formData.sender.city,
          state: formData.sender.state,
          postalCode: formData.sender.postalCode,
          country: formData.sender.country,
        },
        receiverAddress: {
          fullName: formData.receiver.fullName,
          phone: formData.receiver.phone,
          street: formData.receiver.street,
          city: formData.receiver.city,
          state: formData.receiver.state,
          postalCode: formData.receiver.postalCode,
          country: formData.receiver.country,
        },
        package: {
          weightKg: Number(formData.package.weightKg),
          lengthCm: Number(formData.package.lengthCm),
          widthCm: Number(formData.package.widthCm),
          heightCm: Number(formData.package.heightCm),
          description: formData.package.description,
        },
        shipmentType: formData.shipmentType,
        pickupScheduledAt: null,
        notes: formData.notes || 'Created via SmartShip frontend',
      };

      const res = await apiClient.post('/shipments', payload);
      setCreatedShipment(res.data);
      setPaymentStageOpen(true);
      setPaymentError('');
      setPaymentSuccess('');
    } catch (err: any) {
      alert(
        err?.response?.data?.message ||
        err?.response?.data ||
        'Failed to create shipment.'
      );
    } finally {
      setSubmitting(false);
    }
  };

  const handleCreateOrder = async (mode: 'COD' | 'ONLINE') => {
    if (!createdShipment?.id) return;

    try {
      setSelectedPaymentMode(mode);
      setPaymentLoading(true);
      setPaymentError('');
      setPaymentSuccess('');
      setPaymentResponse(null);

      const payload = {
        shipmentId: createdShipment.id,
        paymentMethod: mode === 'COD' ? PAYMENT_METHOD.COD : PAYMENT_METHOD.ONLINE,
      };

      const res = await apiClient.post('/payment/create-order', payload);
      const payment = res.data as PaymentResponse;
      setPaymentResponse(payment);

      if (mode === 'COD') {
        setPaymentSuccess(
          payment.message || 'COD registered. You can now schedule pickup from Shipments.'
        );
      } else {
        setPaymentSuccess(
          payment.message || 'Mock Razorpay order created. Complete dummy verification below.'
        );
      }
    } catch (err: any) {
      setPaymentError(
        err?.response?.data?.message ||
        err?.response?.data ||
        'Unable to create payment order.'
      );
    } finally {
      setPaymentLoading(false);
    }
  };

  const handleVerifyPayment = async () => {
    if (!createdShipment?.id || !paymentResponse?.razorpayOrderId) return;

    try {
      setPaymentLoading(true);
      setPaymentError('');
      setPaymentSuccess('');

      const payload = {
        razorpayOrderId: paymentResponse.razorpayOrderId,
        razorpayPaymentId: verifyForm.razorpayPaymentId.trim(),
        signature: verifyForm.signature.trim(),
        shipmentId: createdShipment.id,
        paymentMethod: 'Online',
      };

      const res = await apiClient.post('/payment/verify', payload);
      setPaymentResponse(res.data);
      setPaymentSuccess(res.data?.message || 'Payment verified successfully.');
    } catch (err: any) {
      setPaymentError(
        err?.response?.data?.message ||
        err?.response?.data ||
        'Payment verification failed.'
      );
    } finally {
      setPaymentLoading(false);
    }
  };

  const addressFields = (_section: 'sender' | 'receiver') => [
    { field: 'fullName', label: 'Full Name', placeholder: 'name' },
    { field: 'phone', label: 'Phone Number', placeholder: 'phone' },
    {
      field: 'street',
      label: 'Street Address',
      placeholder: 'address',
      wide: true,
    },
    { field: 'city', label: 'City', placeholder: 'city' },
    { field: 'state', label: 'State', placeholder: 'state' },
    { field: 'postalCode', label: 'Postal Code', placeholder: 'postal code' },
  ];

  const formatRate = () => {
    if (rateLoading) return 'CALCULATING...';
    if (rateError) return rateError.toUpperCase();
    if (estimatedRate != null) return `Rs. ${estimatedRate.toLocaleString('en-IN')}`;
    return 'ENTER WEIGHT AND SHIPMENT TYPE';
  };

  const renderStep = () => {
    if (activeStep === 0 || activeStep === 1) {
      const sec = activeStep === 0 ? 'sender' : 'receiver';

      return (
        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 14 }}>
          {addressFields(sec).map((f) => (
            <div key={f.field} style={f.wide ? { gridColumn: '1 / -1' } : {}}>
              <label
                style={{
                  display: 'block',
                  fontFamily: 'Rajdhani, sans-serif',
                  fontSize: 13,
                  fontWeight: 900,
                  letterSpacing: '0.14em',
                  textTransform: 'uppercase',
                  color: '#ef0000ff',
                  marginBottom: 6,
                }}
              >
                {f.label}
              </label>
              <input
                className="ss-input"
                style={{ width: '100%' }}
                placeholder={f.placeholder}
                value={formData[sec][f.field as keyof AddressSection]}
                onChange={(e) => handleChange(sec, f.field, e.target.value)}
              />
            </div>
          ))}
        </div>
      );
    }

    if (activeStep === 2) {
      const pkgFields = [
        { field: 'weightKg', label: 'Weight (kg)', placeholder: '4.5' },
        { field: 'description', label: 'Description', placeholder: 'Electronics' },
        { field: 'lengthCm', label: 'Length (cm)', placeholder: '30' },
        { field: 'widthCm', label: 'Width (cm)', placeholder: '20' },
        { field: 'heightCm', label: 'Height (cm)', placeholder: '15' },
      ];

      return (
        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 14 }}>
          <div style={{ gridColumn: '1 / -1' }}>
            <label
              style={{
                display: 'block',
                fontFamily: 'Rajdhani, sans-serif',
                fontSize: 12,
                fontWeight: 600,
                letterSpacing: '0.14em',
                textTransform: 'uppercase',
                color: 'var(--color-text-muted)',
                marginBottom: 6,
              }}
            >
              Shipment Type
            </label>
            <select
              className="ss-input"
              style={{ width: '100%' }}
              value={formData.shipmentType}
              onChange={(e) => handleShipmentTypeChange(e.target.value)}
            >
              <option value="Domestic">Domestic</option>
              <option value="International">International</option>
            </select>
          </div>

          {pkgFields.map((f) => (
            <div key={f.field}>
              <label
                style={{
                  display: 'block',
                  fontFamily: 'Rajdhani, sans-serif',
                  fontSize: 10,
                  fontWeight: 900,
                  letterSpacing: '0.14em',
                  textTransform: 'uppercase',
                  color: 'var(--color-text-muted)',
                  marginBottom: 6,
                }}
              >
                {f.label}
              </label>
              <input
                className="ss-input"
                style={{ width: '100%' }}
                placeholder={f.placeholder}
                value={formData.package[f.field as keyof PackageSection]}
                onChange={(e) => handleChange('package', f.field, e.target.value)}
                type={f.field === 'description' ? 'text' : 'number'}
              />
            </div>
          ))}

          <div style={{ gridColumn: '1 / -1' }}>
            <label
              style={{
                display: 'block',
                fontFamily: 'Rajdhani, sans-serif',
                fontSize: 10,
                fontWeight: 600,
                letterSpacing: '0.14em',
                textTransform: 'uppercase',
                color: 'var(--color-text-muted)',
                marginBottom: 6,
              }}
            >
              Notes
            </label>
            <textarea
              className="ss-input"
              style={{ width: '100%', resize: 'vertical' }}
              placeholder="Optional handling instructions"
              value={formData.notes}
              onChange={(e) =>
                setFormData((prev) => ({ ...prev, notes: e.target.value }))
              }
            />
          </div>

          <div style={{ gridColumn: '1 / -1' }}>
            <label
              style={{
                display: 'block',
                fontFamily: 'Rajdhani, sans-serif',
                fontSize: 12,
                fontWeight: 900,
                letterSpacing: '0.14em',
                textTransform: 'uppercase',
                color: 'var(--color-text-muted)',
                marginBottom: 6,
              }}
            >
              Estimated Rate (Rs.)
            </label>

            <div
              className="ss-input"
              style={{
                width: '100%',
                fontSize: 18,
                display: 'flex',
                alignItems: 'center',
                minHeight: 38,
                color: rateError ? 'var(--color-accent)' : '#fff',
                fontFamily: 'Rajdhani, sans-serif',
                fontWeight: 900,
                letterSpacing: '0.06em',
              }}
            >
              {formatRate()}
            </div>
          </div>
        </div>
      );
    }

    const items = [
      { label: 'From', value: formData.sender.city || '-' },
      { label: 'To', value: formData.receiver.city || '-' },
      { label: 'Receiver', value: formData.receiver.fullName || '-' },
      {
        label: 'Weight',
        value: formData.package.weightKg ? `${formData.package.weightKg} kg` : '-',
      },
      { label: 'Type', value: formData.shipmentType || '-' },

    ];

    return (
      <div style={{ display: 'flex', flexDirection: 'column', gap: 20 }}>
        <div
          style={{
            background: 'var(--color-surface-2)',
            border: '1px solid var(--color-border)',
            borderRadius: 4,
            padding: 24,
          }}
        >
          <div
            style={{
              fontFamily: 'Rajdhani, sans-serif',
              fontSize: 13,
              fontWeight: 700,
              letterSpacing: '0.08em',
              textTransform: 'uppercase',
              color: '#fff',
              marginBottom: 16,
            }}
          >
            Shipment Summary
          </div>

          {items.map((item) => (
            <div
              key={item.label}
              style={{
                display: 'flex',
                justifyContent: 'space-between',
                padding: '8px 0',
                borderBottom: '1px solid rgba(255,255,255,0.04)',
              }}
            >
              <span
                style={{
                  fontSize: 12,
                  color: 'var(--color-text-muted)',
                  fontFamily: 'Rajdhani, sans-serif',
                  letterSpacing: '0.08em',
                  textTransform: 'uppercase',
                }}
              >
                {item.label}
              </span>
              <span
                style={{
                  fontSize: 13,
                  color: '#fff',
                  fontFamily: 'Rajdhani, sans-serif',
                  fontWeight: 600,
                }}
              >
                {item.value}
              </span>
            </div>
          ))}

          <div
            style={{
              display: 'flex',
              justifyContent: 'space-between',
              padding: '12px 0',
              marginTop: 4,
            }}
          >
            <span
              style={{
                fontFamily: 'Rajdhani, sans-serif',
                fontSize: 14,
                fontWeight: 700,
                letterSpacing: '0.08em',
                textTransform: 'uppercase',
                color: '#fff',
              }}
            >
              Estimated Cost
            </span>
            <span
              style={{
                fontFamily: 'Rajdhani, sans-serif',
                fontSize: 18,
                fontWeight: 700,
                color: rateError ? 'var(--color-warning)' : 'var(--color-accent)',
              }}
            >
              {rateLoading
                ? 'CALCULATING...'
                : estimatedRate != null
                  ? `Rs.${estimatedRate.toLocaleString('en-IN')}`
                  : '-'}
            </span>
          </div>
        </div>

        <div
          style={{
            fontSize: 12,
            color: 'var(--color-text-muted)',
            display: 'flex',
            alignItems: 'center',
            gap: 8,
          }}
        >
          <span style={{ color: 'var(--color-warning)', fontSize: 14 }}>!</span>
          Shipment will be created in Draft first. Payment and pickup scheduling happen in the next step.
        </div>
      </div>
    );
  };

  return (
    <div style={{ maxWidth: 860 }}>
      <div
        onClick={() => navigate(-1)}
        style={{
          fontFamily: 'Rajdhani, sans-serif',
          fontSize: 15,
          color: '#ff0000ff',
          fontWeight: 900,
          letterSpacing: '0.1em',
          cursor: 'pointer',
          marginBottom: 30,
        }}
      >
        ← BACK TO SHIPMENTS
      </div>

      <div className="accent-line" style={{ marginBottom: 8 }} />
      <h1 className="section-title">New Shipment</h1>
      <p className="section-sub" style={{ marginBottom: 24 }}>
        Create shipment, choose payment mode, then schedule pickup from shipments.
      </p>

      {!paymentStageOpen && (
        <>
          <div style={{ display: 'flex', marginBottom: 32, gap: 0 }}>
            {steps.map((s, i) => (
              <div
                key={s.key}
                style={{
                  flex: 1,
                  display: 'flex',
                  flexDirection: 'column',
                  alignItems: 'center',
                  position: 'relative',
                }}
              >
                {i > 0 && (
                  <div
                    style={{
                      position: 'absolute',
                      left: '-50%',
                      top: 14,
                      width: '100%',
                      height: 1,
                      background:
                        i <= activeStep ? 'var(--color-accent)' : 'var(--color-border)',
                    }}
                  />
                )}

                <div
                  style={{
                    width: 28,
                    height: 28,
                    borderRadius: 9,
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'center',
                    background:
                      i <= activeStep ? 'var(--color-accent)' : 'var(--color-surface-2)',
                    border: `1px solid ${i <= activeStep ? 'var(--color-accent)' : 'var(--color-border)'
                      }`,
                    fontFamily: 'Rajdhani, sans-serif',
                    fontSize: 17,
                    fontWeight: 700,
                    color: i <= activeStep ? '#fff' : '#555',
                    zIndex: 1,
                    boxShadow:
                      i === activeStep ? '0 0 10px rgba(224,0,26,0.4)' : 'none',
                  }}
                >
                  {i < activeStep ? 'OK' : i + 1}
                </div>

                <div
                  style={{
                    fontFamily: 'Rajdhani, sans-serif',
                    fontSize: 13,
                    fontWeight: 900,
                    letterSpacing: '0.12em',
                    textTransform: 'uppercase',
                    color: i === activeStep ? 'var(--color-accent)' : '#555',
                    marginTop: 6,
                  }}
                >
                  {s.label}
                </div>
              </div>
            ))}
          </div>

          <div className="ss-card" style={{ padding: 28, marginBottom: 20 }}>
            <div
              style={{
                fontFamily: 'Rajdhani, sans-serif',
                fontSize: 14,
                fontWeight: 700,
                letterSpacing: '0.08em',
                textTransform: 'uppercase',
                color: '#fff',
                marginBottom: 20,
                borderBottom: '1px solid var(--color-border)',
                paddingBottom: 12,
              }}
            >
              Step {activeStep + 1} - {steps[activeStep].label}
            </div>

            {renderStep()}
          </div>

          <div style={{ display: 'flex', justifyContent: 'space-between' }}>
            <button
              className="ss-btn ss-btn-outline"
              disabled={activeStep === 0 || submitting}
              onClick={handleBack}
              style={{ opacity: activeStep === 0 || submitting ? 0.3 : 1 }}
            >
              Back
            </button>

            <button
              className="ss-btn"
              onClick={handleNext}
              disabled={submitting || !canContinue}
              style={{ opacity: submitting || !canContinue ? 0.7 : 1 }}
            >
              {submitting
                ? 'CREATING...'
                : activeStep === steps.length - 1
                  ? 'Create Shipment -> Payment'
                  : 'Continue ->'}
            </button>
          </div>
        </>
      )}

      {paymentStageOpen && createdShipment && (
        <div className="ss-card" style={{ padding: 28 }}>
          <div
            style={{
              display: 'flex',
              justifyContent: 'space-between',
              gap: 20,
              flexWrap: 'wrap',
              marginBottom: 20,
              borderBottom: '1px solid var(--color-border)',
              paddingBottom: 16,
            }}
          >
            <div>
              <div className="section-title" style={{ fontSize: 18 }}>
                Shipment Created
              </div>
              <div className="section-sub">
                Tracking: {createdShipment.trackingNumber} | Status: {createdShipment.status}
              </div>
            </div>
            <div style={{ textAlign: 'right' }}>
              <div className="kpi-label">Shipping Rate</div>
              <div className="kpi-value">
                Rs.{Number(createdShipment.shippingRate || 0).toLocaleString('en-IN')}
              </div>
            </div>
          </div>

          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 16 }}>
            <button
              className="ss-btn"
              disabled={paymentLoading}
              onClick={() => handleCreateOrder('COD')}
            >
              {paymentLoading && selectedPaymentMode === 'COD'
                ? 'CREATING COD ORDER...'
                : 'Cash on Delivery'}
            </button>

            <button
              className="ss-btn ss-btn-outline"
              disabled={paymentLoading}
              onClick={() => handleCreateOrder('ONLINE')}
            >
              {paymentLoading && selectedPaymentMode === 'ONLINE'
                ? 'CREATING ONLINE ORDER...'
                : 'Pay Online'}
            </button>
          </div>

          {paymentError && (
            <div
              style={{
                marginTop: 16,
                padding: 12,
                border: '1px solid rgba(224,0,26,0.35)',
                background: 'rgba(224,0,26,0.08)',
                color: '#ff8d8d',
                borderRadius: 4,
              }}
            >
              {paymentError}
            </div>
          )}

          {paymentSuccess && (
            <div
              style={{
                marginTop: 16,
                padding: 12,
                border: '1px solid rgba(80,180,120,0.35)',
                background: 'rgba(80,180,120,0.08)',
                color: '#99e6b3',
                borderRadius: 4,
              }}
            >
              {paymentSuccess}
            </div>
          )}

          {paymentResponse && (
            <div
              style={{
                marginTop: 20,
                padding: 20,
                border: '1px solid var(--color-border)',
                background: 'var(--color-surface-2)',
                borderRadius: 4,
              }}
            >
              <div
                style={{
                  fontFamily: 'Rajdhani, sans-serif',
                  fontSize: 13,
                  fontWeight: 700,
                  letterSpacing: '0.1em',
                  textTransform: 'uppercase',
                  marginBottom: 14,
                  color: '#fff',
                }}
              >
                Payment Details
              </div>

              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12 }}>
                <div>
                  <div className="kpi-label">Method</div>
                  <div>{paymentResponse.paymentMethod}</div>
                </div>
                <div>
                  <div className="kpi-label">Status</div>
                  <div>{paymentResponse.paymentStatus}</div>
                </div>
                <div>
                  <div className="kpi-label">Amount</div>
                  <div>Rs.{Number(paymentResponse.amount || 0).toLocaleString('en-IN')}</div>
                </div>
                <div>
                  <div className="kpi-label">Tracking Number</div>
                  <div>{paymentResponse.trackingNumber}</div>
                </div>
                {paymentResponse.razorpayOrderId && (
                  <div style={{ gridColumn: '1 / -1' }}>
                    <div className="kpi-label">Mock Razorpay Order ID</div>
                    <div
                      style={{
                        fontFamily: 'monospace',
                        color: 'var(--color-accent)',
                        wordBreak: 'break-all',
                      }}
                    >
                      {paymentResponse.razorpayOrderId}
                    </div>
                  </div>
                )}
              </div>

              {selectedPaymentMode === 'ONLINE' &&
                paymentResponse.razorpayOrderId &&
                paymentResponse.paymentStatus !== 'Paid' && (
                  <div style={{ marginTop: 20 }}>
                    <div
                      style={{
                        fontFamily: 'Rajdhani, sans-serif',
                        fontSize: 12,
                        fontWeight: 700,
                        letterSpacing: '0.1em',
                        textTransform: 'uppercase',
                        marginBottom: 12,
                        color: '#fff',
                      }}
                    >
                      Dummy Razorpay Verification
                    </div>

                    <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12 }}>
                      <div>
                        <label className="kpi-label">Razorpay Payment ID</label>
                        <input
                          className="ss-input"
                          style={{ width: '100%' }}
                          placeholder="pay_mock_123456"
                          value={verifyForm.razorpayPaymentId}
                          onChange={(e) =>
                            setVerifyForm((prev) => ({
                              ...prev,
                              razorpayPaymentId: e.target.value,
                            }))
                          }
                        />
                      </div>

                      <div>
                        <label className="kpi-label">Signature</label>
                        <input
                          className="ss-input"
                          style={{ width: '100%' }}
                          placeholder="sig_mock_abcdef"
                          value={verifyForm.signature}
                          onChange={(e) =>
                            setVerifyForm((prev) => ({
                              ...prev,
                              signature: e.target.value,
                            }))
                          }
                        />
                      </div>
                    </div>

                    <div style={{ marginTop: 14 }}>
                      <button
                        className="ss-btn"
                        disabled={
                          paymentLoading ||
                          !verifyForm.razorpayPaymentId.trim() ||
                          !verifyForm.signature.trim()
                        }
                        onClick={handleVerifyPayment}
                      >
                        {paymentLoading ? 'VERIFYING...' : 'Verify Payment'}
                      </button>
                    </div>
                  </div>
                )}
            </div>
          )}

          <div
            style={{
              marginTop: 22,
              display: 'flex',
              justifyContent: 'space-between',
              gap: 12,
              flexWrap: 'wrap',
            }}
          >
            <button
              className="ss-btn ss-btn-outline"
              onClick={() => navigate('/customer/dashboard')}
            >
              Go to Dashboard
            </button>

            <button
              className="ss-btn"
              onClick={() => navigate('/customer/shipments')}
            >
              Open Shipments
            </button>
          </div>
        </div>
      )}
    </div>
  );
};
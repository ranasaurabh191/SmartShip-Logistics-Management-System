import { useEffect, useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { apiClient } from '../../core/api/axios';
import { useNotificationStore } from '../../store/useNotificationStore';
import {
  validateAddressSection, validatePackageSection, hasErrors,
  type AddressErrors, type PackageErrors,
} from '../../utils/shipmentValidation';
import { calculateCosts, fmtINR, type CostBreakdown } from '../../utils/costCalculator';
import { InvoicePrintView, type InvoiceData } from './InvoicePrintView';

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

const STEPS = [
  { key: 'sender', label: 'Sender Details' },
  { key: 'receiver', label: 'Receiver Details' },
  { key: 'package', label: 'Package Info' },
  { key: 'costs', label: 'Cost Breakdown' },
  { key: 'payment', label: 'Payment & Invoice' },
  { key: 'label', label: 'Upload Label' },
];
const PAYMENTMETHOD = { COD: 0, ONLINE: 1 };
const RAZORPAY_KEY_ID = import.meta.env.VITE_RAZORPAY_KEY_ID as string;
const COUNTRIES = ['India', 'United States', 'United Kingdom', 'Canada', 'Australia', 'Germany', 'UAE', 'Singapore', 'Other'];

const inputErrorStyle: React.CSSProperties = {
  color: '#ff6b6b', fontSize: 12, marginTop: 4, fontFamily: 'Inter, sans-serif',
};
const fieldLabel = (hasError: boolean): React.CSSProperties => ({
  display: 'block', fontFamily: 'Orbitron, monospace', fontSize: 11,
  fontWeight: 600, letterSpacing: '0.14em', textTransform: 'uppercase',
  color: hasError ? '#ff6b6b' : '#e0001a', marginBottom: 6,
});

function genInvoiceNumber(trackingNumber: string) {
  const d = new Date();
  const datePart = `${d.getFullYear()}${String(d.getMonth() + 1).padStart(2, '0')}${String(d.getDate()).padStart(2, '0')}`;
  return `INV-${datePart}-${trackingNumber.slice(-6).toUpperCase()}`;
}
function fmtDate(d = new Date()) {
  return d.toLocaleDateString('en-IN', { day: '2-digit', month: 'short', year: 'numeric' });
}

function getApiError(err: any, defaultMsg: string): string {
  const data = err?.response?.data;
  if (!data) return defaultMsg;
  if (typeof data === 'string') return data;
  if (data.errors && typeof data.errors === 'object') {
    return Object.values(data.errors).flat().join(' | ');
  }
  if (data.message) return data.message;
  if (data.title) return data.title;
  return defaultMsg;
}

export const CreateShipmentWizard = () => {
  const navigate = useNavigate();
  const addNotification = useNotificationStore(s => s.addNotification);

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
  const [fragile, setFragile] = useState(false);
  const [selectedPayMode, setSelectedPayMode] = useState<'COD' | 'ONLINE' | null>(null);
  const [costs, setCosts] = useState<CostBreakdown | null>(null);

  const [createdShipment, setCreatedShipment] = useState<ShipmentResponse | null>(null);
  const [paymentResponse, setPaymentResponse] = useState<PaymentResponse | null>(null);
  const [paymentLoading, setPaymentLoading] = useState(false);
  const [paymentError, setPaymentError] = useState('');
  const [paymentSuccess, setPaymentSuccess] = useState('');


  const [showInvoice, setShowInvoice] = useState(false);
  const [invoiceFile, setInvoiceFile] = useState<File | null>(null);
  const [invoiceData, setInvoiceData] = useState<InvoiceData | null>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [labelFile, setLabelFile] = useState<File | null>(null);
  const [uploadLoading, setUploadLoading] = useState(false);
  const [uploadError, setUploadError] = useState('');
  const [uploadSuccess, setUploadSuccess] = useState('');

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
        setRateError(getApiError(err, 'Unable to calculate rate'));
      } finally { setRateLoading(false); }
    }, 400);
    return () => clearTimeout(timer);
  }, [formData.package.weightKg, formData.shipmentType]);


  useEffect(() => {
    if (estimatedRate !== null) {
      setCosts(calculateCosts({
        baseRate: estimatedRate,
        shipmentType: formData.shipmentType,
        paymentMode: selectedPayMode,
        fragile,
      }));
    } else {
      setCosts(null);
    }
  }, [estimatedRate, formData.shipmentType, selectedPayMode, fragile]);

  /* ── Field helpers ── */
  const handleChange = (section: 'sender' | 'receiver' | 'package', field: string, value: string) => {
    setFormData(prev => ({ ...prev, [section]: { ...prev[section], [field]: value } }));
    if (section === 'sender') setSenderErrors(p => ({ ...p, [field]: '' }));
    if (section === 'receiver') setReceiverErrors(p => ({ ...p, [field]: '' }));
    if (section === 'package') setPackageErrors(p => ({ ...p, [field]: '' }));
  };

  /* ── Wizard navigation ── */
  const handleNext = async () => {
    if (activeStep === 0) {
      const e = validateAddressSection(formData.sender, 'Sender');
      setSenderErrors(e); if (hasErrors(e)) return;
    }
    if (activeStep === 1) {
      const e = validateAddressSection(formData.receiver, 'Receiver');
      setReceiverErrors(e); if (hasErrors(e)) return;
    }
    if (activeStep === 2) {
      const e = validatePackageSection(formData.package);
      setPackageErrors(e); if (hasErrors(e)) return;
      if (rateLoading) { addNotification('Waiting for rate calculation…', 'info'); return; }
      if (rateError) { addNotification('Fix rate error before continuing.', 'error'); return; }
    }
    if (activeStep === 3) {
      await submitShipment();
      return;
    }
    if (activeStep === 4) {
      if (!paymentResponse) { addNotification('Complete payment first.', 'warning'); return; }
      setActiveStep(5); return;
    }
    setActiveStep(p => p + 1);
  };

  const handleBack = () => setActiveStep(p => p - 1);

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
        notes: formData.notes || 'Created via SmartShip.',
      };
      const res = await apiClient.post('/shipments', payload);
      setCreatedShipment(res.data);
      setActiveStep(4); // jump to payment step
      setPaymentError(''); setPaymentSuccess(''); setPaymentResponse(null);
    } catch (err: any) {
      addNotification(getApiError(err, 'Failed to create shipment.'), 'error');
    } finally { setSubmitting(false); }
  };

  /* ── Razorpay ── */
  const launchRazorpay = (payment: PaymentResponse, shipmentId: number) => {
    if (!window.Razorpay) { setPaymentError('Razorpay SDK not loaded. Refresh and retry.'); return; }
    if (!payment.razorpayOrderId) { setPaymentError('No Razorpay Order ID. Try again.'); return; }
    setPaymentLoading(false);

    const options: RazorpayOptions = {
      key: RAZORPAY_KEY_ID,
      amount: payment.amount * 100,
      currency: 'INR',
      name: 'SmartShip',
      description: `Shipment ${payment.trackingNumber}`,
      order_id: payment.razorpayOrderId,
      handler: async (response: RazorpayPaymentResponse) => {
        setPaymentLoading(true); setPaymentError(''); setPaymentSuccess('');
        try {
          const res = await apiClient.post('/payment/verify', {
            razorpayOrderId: response.razorpay_order_id,
            razorpayPaymentId: response.razorpay_payment_id,
            signature: response.razorpay_signature,
            shipmentId, paymentMethod: 'Online',
          });
          const pRes = res.data as PaymentResponse;
          setPaymentResponse(pRes);
          setPaymentSuccess('✅ Payment verified! Invoice ready below.');
          buildAndShowInvoice(pRes, shipmentId);
        } catch (err: any) {
          setPaymentError(getApiError(err, 'Payment made but verification failed. Go to My Payments → Retry Verify.'));
        } finally { setPaymentLoading(false); }
      },
      theme: { color: '#e0001a' },
      modal: {
        ondismiss: () => {
          setPaymentSuccess('');
          setPaymentError('Checkout closed. Click "Pay Online" to retry.');
          setPaymentLoading(false);
        },
      },
    };
    new window.Razorpay(options).open();
  };

  /* ── Create payment order ── */
  const handleCreateOrder = async (mode: 'COD' | 'ONLINE') => {
    if (!createdShipment?.id) return;
    setSelectedPayMode(mode);
    setPaymentLoading(true); setPaymentError(''); setPaymentSuccess(''); setPaymentResponse(null);
    try {
      const res = await apiClient.post('/payment/create-order', {
        shipmentId: createdShipment.id,
        paymentMethod: mode === 'COD' ? PAYMENTMETHOD.COD : PAYMENTMETHOD.ONLINE,
      });
      const payment = res.data as PaymentResponse;
      setPaymentResponse(payment);
      if (mode === 'COD') {
        setPaymentSuccess(payment.message || '✅ COD registered.');
        setPaymentLoading(false);
        buildAndShowInvoice(payment, createdShipment.id);
        return;
      }
      launchRazorpay(payment, createdShipment.id);
    } catch (err: any) {
      setPaymentError(getApiError(err, 'Unable to create payment order.'));
      setPaymentLoading(false);
    }
  };

  /* ── Build invoice data object ── */
  const buildAndShowInvoice = (payment: PaymentResponse, shipmentId: number) => {
    if (!createdShipment || !costs) return;
    const data: InvoiceData = {
      invoiceNumber: genInvoiceNumber(createdShipment.trackingNumber),
      invoiceDate: fmtDate(),
      trackingNumber: createdShipment.trackingNumber,
      shipmentId,
      status: createdShipment.status,
      shipmentType: formData.shipmentType,
      paymentMethod: payment.paymentMethod,
      paymentStatus: payment.paymentStatus,
      sender: formData.sender,
      receiver: formData.receiver,
      package: {
        weightKg: Number(formData.package.weightKg), lengthCm: Number(formData.package.lengthCm),
        widthCm: Number(formData.package.widthCm), heightCm: Number(formData.package.heightCm),
        description: formData.package.description,
      },
      costs,
      razorpayOrderId: payment.razorpayOrderId,
      razorpayPaymentId: payment.razorpayPaymentId,
      notes: formData.notes,
    };
    setInvoiceData(data);
    setShowInvoice(true);
  };

  /* ── Label upload ── */
  const handleLabelUpload = async () => {
    const file = labelFile;
    if (!file || !createdShipment) return;
    setUploadLoading(true); setUploadError(''); setUploadSuccess('');
    try {
      const fd = new FormData();
      fd.append('shipmentId', String(createdShipment.id));
      fd.append('trackingNumber', createdShipment.trackingNumber);
      fd.append('documentType', 'ShippingLabel');
      fd.append('file', file);
      await apiClient.post('/tracking/documents/upload', fd, {
        headers: { 'Content-Type': 'multipart/form-data' },
      });
      setUploadSuccess(`Label uploaded successfully for ${createdShipment.trackingNumber}`);
      addNotification('Label uploaded!', 'success');
    } catch (err: any) {
      setUploadError(getApiError(err, 'Upload failed.'));
    } finally { setUploadLoading(false); }
  };


  const renderAddressField = (
    section: 'sender' | 'receiver', field: string, label: string,
    placeholder: string, errors: AddressErrors, wide?: boolean, isSelect?: boolean,
  ) => {
    const err = (errors as any)[field];
    return (
      <div key={field} style={wide ? { gridColumn: '1 / -1' } : {}}>
        <label style={fieldLabel(!!err)}>{label}</label>
        {isSelect ? (
          <select className="ss-input" style={{ width: '100%', borderColor: err ? '#ff6b6b' : undefined }}
            value={(formData[section] as any)[field]}
            onChange={e => handleChange(section, field, e.target.value)}>
            {COUNTRIES.map(c => <option key={c} value={c}>{c}</option>)}
          </select>
        ) : (
          <input className="ss-input" style={{ width: '100%', borderColor: err ? '#ff6b6b' : undefined }}
            placeholder={placeholder} value={(formData[section] as any)[field]}
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
        <label style={fieldLabel(!!err)}>{label}</label>
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

    /* ── Step 2: Package Info ── */
    if (activeStep === 2) {
      return (
        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 14 }}>
          <div style={{ gridColumn: '1 / -1' }}>
            <label style={fieldLabel(false)}>Shipment Type</label>
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
            <label style={fieldLabel(false)}>Notes</label>
            <textarea className="ss-input" style={{ width: '100%', resize: 'vertical' }}
              placeholder="Optional handling instructions" value={formData.notes}
              onChange={e => setFormData(p => ({ ...p, notes: e.target.value }))} />
          </div>
          <div style={{ gridColumn: '1 / -1' }}>
            <label style={fieldLabel(!!rateError)}>Estimated Base Rate</label>
            <div className="ss-input" style={{ width: '100%', fontSize: 18, display: 'flex', alignItems: 'center', minHeight: 38, color: rateError ? 'var(--color-accent)' : '#fff', fontFamily: 'Orbitron, monospace', fontWeight: 900, letterSpacing: '0.06em' }}>
              {rateLoading ? 'CALCULATING...' : rateError ? rateError.toUpperCase() : estimatedRate !== null ? `₹${estimatedRate.toLocaleString('en-IN')}` : '₹ 0'}
            </div>
          </div>
        </div>
      );
    }

    if (activeStep === 3) {
      const c = costs;
      const isDomestic = formData.shipmentType !== 'International';

      const rows = c ? [
        { label: 'Base Shipping Rate', amount: c.baseRate },
        { label: 'Fuel Surcharge (5%)', amount: c.fuelSurcharge },
        { label: `Handling Charge (${isDomestic ? 'Domestic' : 'International'})`, amount: c.handlingCharge },
        ...(c.fragileCharge > 0 ? [{ label: 'Fragile / Special Handling', amount: c.fragileCharge }] : []),
        ...(c.codFee > 0 ? [{ label: 'COD Service Fee (1.5%)', amount: c.codFee }] : []),
      ] : [];

      return (
        <div style={{ display: 'flex', flexDirection: 'column', gap: 20 }}>

          {/* Add-ons */}
          <div style={{ background: 'var(--color-surface-2)', border: '1px solid var(--color-border)', borderRadius: 4, padding: 20 }}>
            <div style={{ fontFamily: 'Orbitron, monospace', fontSize: 12, fontWeight: 700, letterSpacing: '0.14em', textTransform: 'uppercase', color: '#e0001a', marginBottom: 14 }}>
              Add-On Services
            </div>
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12 }}>
              {/* Fragile toggle */}
              <label style={{ display: 'flex', alignItems: 'center', gap: 10, cursor: 'pointer', padding: '10px 14px', border: `1px solid ${selectedPayMode === 'ONLINE' ? '#00c48c' : 'var(--color-border)'}`, borderRadius: 4, background: selectedPayMode === 'ONLINE' ? 'rgba(0,196,140,0.07)' : 'transparent', transition: 'all 0.15s' }}>
                <input type="radio" name="paymode" checked={selectedPayMode === 'ONLINE'} onChange={() => setSelectedPayMode('ONLINE')} style={{ accentColor: '#00c48c', width: 16, height: 16 }} />
                <div>
                  <div style={{ fontFamily: 'Orbitron, monospace', fontSize: 12, color: '#fff', fontWeight: 500, letterSpacing: '0.06em' }}>PAY ONLINE</div>
                  <div style={{ fontSize: 12, color: '#888', marginTop: 2 }}>UPI / Card / Net banking via Razorpay</div>
                </div>
              </label>
              <label style={{ display: 'flex', alignItems: 'center', gap: 10, cursor: 'pointer', padding: '10px 14px', border: `1px solid ${fragile ? '#e0001a' : 'var(--color-border)'}`, borderRadius: 4, background: fragile ? 'rgba(224,0,26,0.07)' : 'transparent', transition: 'all 0.15s' }}>
                <input type="checkbox" checked={fragile} onChange={e => setFragile(e.target.checked)} style={{ accentColor: '#e0001a', width: 16, height: 16 }} />
                <div>
                  <div style={{ fontFamily: 'Orbitron, monospace', fontSize: 12, color: '#fff', fontWeight: 500, letterSpacing: '0.06em' }}>FRAGILE HANDLING</div>
                  <div style={{ fontSize: 12, color: '#888', marginTop: 2 }}>Extra care + padding · +₹80</div>
                </div>
              </label>
              {/* COD toggle */}
              <label style={{ display: 'flex', alignItems: 'center', gap: 10, cursor: 'pointer', padding: '10px 14px', border: `1px solid ${selectedPayMode === 'COD' ? '#e0001a' : 'var(--color-border)'}`, borderRadius: 4, background: selectedPayMode === 'COD' ? 'rgba(224,0,26,0.07)' : 'transparent', transition: 'all 0.15s' }}>
                <input type="radio" name="paymode" checked={selectedPayMode === 'COD'} onChange={() => setSelectedPayMode('COD')} style={{ accentColor: '#e0001a', width: 16, height: 16 }} />
                <div>
                  <div style={{ fontFamily: 'Orbitron, monospace', fontSize: 12, color: '#fff', fontWeight: 500, letterSpacing: '0.06em' }}>CASH ON DELIVERY</div>
                  <div style={{ fontSize: 12, color: '#888', marginTop: 2 }}>COD fee 1.5% of base rate</div>
                </div>
              </label>
            </div>
          </div>

          {/* Itemized Table */}
          <div style={{ background: 'var(--color-surface-2)', border: '1px solid var(--color-border)', borderRadius: 4, overflow: 'hidden' }}>
            <div style={{ padding: '12px 16px', borderBottom: '1px solid var(--color-border)', fontFamily: 'Orbitron, monospace', fontSize: 11, fontWeight: 700, letterSpacing: '0.14em', textTransform: 'uppercase', color: '#fff' }}>
              Itemized Cost Breakdown
            </div>
            <table className="ss-table">
              <thead>
                <tr>
                  <th>Description</th>
                  <th style={{ textAlign: 'right' }}>Amount</th>
                </tr>
              </thead>
              <tbody>
                {c ? rows.map((row, i) => (
                  <tr key={i}>
                    <td>{row.label}</td>
                    <td style={{ textAlign: 'right', fontFamily: 'sans-serif', fontWeight: 500 }}>{fmtINR(row.amount)}</td>
                  </tr>
                )) : (
                  <tr><td colSpan={2} style={{ color: '#888', textAlign: 'center', padding: 20 }}>Enter package weight to see breakdown</td></tr>
                )}
              </tbody>
            </table>
            {c && (
              <div style={{ borderTop: '1px solid var(--color-border)' }}>
                {/* Subtotal */}
                <div style={{ display: 'flex', justifyContent: 'space-between', padding: '8px 13px', borderBottom: '1px solid rgba(255,255,255,0.04)' }}>
                  <span style={{ color: '#888', fontSize: 12 }}>Subtotal (excl. GST)</span>
                  <span style={{ fontFamily: 'sans-serif', fontWeight: 500 }}>{fmtINR(c.subtotal)}</span>
                </div>
                {/* GST rows */}
                {isDomestic ? (
                  <>
                    <div style={{ display: 'flex', justifyContent: 'space-between', padding: '6px 13px', borderBottom: '1px solid rgba(255,255,255,0.04)', color: '#888', fontSize: 12 }}>
                      <span>CGST @ 9%</span><span style={{ fontFamily: 'sans-serif' }}>{fmtINR(c.cgst)}</span>
                    </div>
                    <div style={{ display: 'flex', justifyContent: 'space-between', padding: '6px 13px', borderBottom: '1px solid rgba(255,255,255,0.04)', color: '#888', fontSize: 12 }}>
                      <span>SGST @ 9%</span><span style={{ fontFamily: 'sans-serif' }}>{fmtINR(c.sgst)}</span>
                    </div>
                  </>
                ) : (
                  <div style={{ display: 'flex', justifyContent: 'space-between', padding: '6px 13px', borderBottom: '1px solid rgba(255,255,255,0.04)', color: '#888', fontSize: 12 }}>
                    <span>IGST @ 18%</span><span style={{ fontFamily: 'sans-serif' }}>{fmtINR(c.igst)}</span>
                  </div>
                )}
                {/* Grand Total */}
                <div style={{ display: 'flex', justifyContent: 'space-between', padding: '14px 13px', background: 'rgba(224,0,26,0.1)', borderTop: '1px solid rgba(224,0,26,0.25)' }}>
                  <span style={{ fontFamily: 'Orbitron, monospace', fontSize: 13, fontWeight: 700, color: '#fff', letterSpacing: '0.08em' }}>GRAND TOTAL</span>
                  <span style={{ fontFamily: 'Orbitron, monospace', fontSize: 18, fontWeight: 600, color: '#e0001a' }}>{fmtINR(c.grandTotal)}</span>
                </div>
              </div>
            )}
          </div>

          {!selectedPayMode && (
            <div style={{ fontSize: 12, color: '#f5a623', display: 'flex', gap: 8, alignItems: 'center' }}>
              <span>⚠</span> Select a payment mode above to continue.
            </div>
          )}
        </div>
      );
    }

    /* ── Step 4: Payment & Invoice ── */
    if (activeStep === 4) {
      return (
        <div style={{ display: 'flex', flexDirection: 'column', gap: 20 }}>
          {/* Shipment summary banner */}
          {createdShipment && (
            <div style={{ display: 'flex', justifyContent: 'space-between', flexWrap: 'wrap', gap: 16, padding: '16px 20px', background: 'rgba(0,196,140,0.07)', border: '1px solid rgba(0,196,140,0.2)', borderRadius: 4 }}>
              <div>
                <div style={{ fontFamily: 'Orbitron, monospace', fontSize: 13, fontWeight: 700, color: '#00c48c' }}>
                  ✓ SHIPMENT CREATED
                </div>
                <div style={{ fontSize: 12, color: '#888', marginTop: 4 }}>
                  Tracking: <strong style={{ color: '#e0001a' }}>{createdShipment.trackingNumber}</strong>
                  &nbsp;·&nbsp; Status: {createdShipment.status}
                </div>
              </div>
              <div style={{ textAlign: 'right' }}>
                <div className="kpi-label">Grand Total</div>
                <div className="kpi-value" style={{ fontSize: 22, color: '#e0001a' }}>
                  {costs ? fmtINR(costs.grandTotal) : `₹${Number(createdShipment.shippingRate || 0).toLocaleString('en-IN')}`}
                </div>
              </div>
            </div>
          )}

          {/* Payment buttons */}
          {paymentResponse?.paymentStatus !== 'Paid' && (
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 16 }}>
              <button className="ss-btn" disabled={paymentLoading} onClick={() => handleCreateOrder('COD')}
                style={{ opacity: paymentLoading && selectedPayMode === 'COD' ? 0.7 : 1, justifyContent: 'center', padding: '12px 0', fontSize: 13 }}>
                {paymentLoading && selectedPayMode === 'COD' ? 'CREATING ORDER...' : 'Cash on Delivery'}
              </button>
              <button className="ss-btn ss-btn-outline" disabled={paymentLoading} onClick={() => handleCreateOrder('ONLINE')}
                style={{ opacity: paymentLoading && selectedPayMode === 'ONLINE' ? 0.7 : 1, justifyContent: 'center', padding: '12px 0', fontSize: 13 }}>
                {paymentLoading && selectedPayMode === 'ONLINE' ? 'OPENING RAZORPAY...' : 'Pay Online'}
              </button>
            </div>
          )}

          {paymentError && (
            <div style={{ padding: 12, border: '1px solid rgba(224,0,26,0.35)', background: 'rgba(224,0,26,0.08)', color: '#ff8d8d', borderRadius: 4 }}>
              {paymentError}
            </div>
          )}
          {paymentSuccess && (
            <div style={{ padding: 12, border: '1px solid rgba(80,180,120,0.35)', background: 'rgba(80,180,120,0.08)', color: '#99e6b3', borderRadius: 4 }}>
              {paymentSuccess}
            </div>
          )}

          {/* Payment details card */}
          {paymentResponse && (
            <div style={{ padding: 20, border: '1px solid var(--color-border)', background: 'var(--color-surface-2)', borderRadius: 4 }}>
              <div style={{ fontFamily: 'Orbitron, monospace', fontSize: 13, fontWeight: 700, letterSpacing: '0.1em', textTransform: 'uppercase', marginBottom: 14, color: '#fff' }}>Payment Details</div>
              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12 }}>
                <div><div className="kpi-label">Method</div><div>{paymentResponse.paymentMethod}</div></div>
                <div><div className="kpi-label">Status</div>
                  <span className={`ss-badge ${paymentResponse.paymentStatus === 'Paid' ? 'success' : 'warning'}`}>{paymentResponse.paymentStatus}</span>
                </div>
                <div><div className="kpi-label">Amount</div><div>₹{Number(paymentResponse.amount || 0).toLocaleString('en-IN')}</div></div>
                <div><div className="kpi-label">Tracking</div><div style={{ color: '#e0001a' }}>{paymentResponse.trackingNumber}</div></div>
                {paymentResponse.razorpayOrderId && (
                  <div style={{ gridColumn: '1/-1' }}>
                    <div className="kpi-label">Razorpay Order ID</div>
                    <div style={{ fontFamily: 'Orbitron, monospace', color: '#888', wordBreak: 'break-all', fontSize: 11 }}>{paymentResponse.razorpayOrderId}</div>
                  </div>
                )}
              </div>
            </div>
          )}

          {/* Invoice view button */}
          {invoiceData && (
            <div style={{ display: 'flex', justifyContent: 'flex-end' }}>
              <button
                className="ss-btn"
                style={{
                  width: '18%',
                  padding: '10px 10px',
                  fontSize: 13
                }}
                onClick={() => setShowInvoice(true)}
              >
                View Invoice
              </button>
            </div>
          )}
        </div>
      );
    }

    /* ── Step 5: Upload Label ── */
    if (activeStep === 5) {
      return (
        <div style={{ display: 'flex', flexDirection: 'column', gap: 20 }}>
          <div style={{ padding: 20, background: 'var(--color-surface-2)', border: '1px solid var(--color-border)', borderRadius: 4 }}>
            <div style={{ fontFamily: 'Orbitron, monospace', fontSize: 11, fontWeight: 700, letterSpacing: '0.14em', textTransform: 'uppercase', color: '#e0001a', marginBottom: 12 }}>
              Upload Shipping Label / Invoice
            </div>
            <div style={{ fontSize: 12, color: '#b3b3b3ff', marginBottom: 16 }}>
              Upload the invoice PDF you downloaded in the previous step. Accepted: PDF, PNG, JPG (max 10 MB).
            </div>

            {/* Dropzone */}
            <div
              onClick={() => fileInputRef.current?.click()}
              onDrop={e => { e.preventDefault(); const f = e.dataTransfer.files[0]; if (f) setLabelFile(f); }}
              onDragOver={e => e.preventDefault()}
              style={{
                border: `2px dashed ${labelFile ? '#00c48c' : 'rgba(224,0,26,0.3)'}`,
                borderRadius: 4, padding: '32px 20px', textAlign: 'center', cursor: 'pointer',
                background: labelFile ? 'rgba(0,196,140,0.05)' : 'rgba(224,0,26,0.03)',
                transition: 'all 0.15s',
              }}>
              <div style={{ fontSize: 32, marginBottom: 8 }}>{labelFile ? '✅' : '⬆'}</div>
              <div style={{ fontFamily: 'Orbitron, monospace', fontSize: 12, color: labelFile ? '#00c48c' : '#e0001a', fontWeight: 900 }}>
                {labelFile ? labelFile.name : 'CLICK OR DROP FILE HERE'}
              </div>
              {!labelFile && <div style={{ fontSize: 11, color: '#888', marginTop: 4 }}>PDF, PNG, JPG — max 10 MB</div>}
              <input ref={fileInputRef} type="file" accept=".pdf,.png,.jpg,.jpeg" style={{ display: 'none' }}
                onChange={e => { const f = e.target.files?.[0]; if (f) setLabelFile(f); }} />
            </div>

            {/* Pre-fill with invoice if downloaded */}
            {invoiceFile && !labelFile && (
              <div style={{ marginTop: 12, padding: '10px 14px', background: 'rgba(0,196,140,0.08)', border: '1px solid rgba(0,196,140,0.2)', borderRadius: 4, fontSize: 12, color: '#99e6b3', cursor: 'pointer' }}
                onClick={() => setLabelFile(invoiceFile)}>
                ↑ Use the invoice you downloaded: <strong>{invoiceFile.name}</strong>
              </div>
            )}
          </div>

          {uploadError && <div style={{ padding: 12, border: '1px solid rgba(224,0,26,0.35)', background: 'rgba(224,0,26,0.08)', color: '#ff8d8d', borderRadius: 4 }}>{uploadError}</div>}
          {uploadSuccess && <div style={{ padding: 12, border: '1px solid rgba(80,180,120,0.35)', background: 'rgba(80,180,120,0.08)', color: '#99e6b3', borderRadius: 4 }}>{uploadSuccess}</div>}

          <div style={{ display: 'flex', gap: 12 }}>
            <button className="ss-btn" disabled={!labelFile || uploadLoading} onClick={handleLabelUpload}
              style={{ flex: 1, padding: '11px 20px', justifyContent: 'center', fontSize: 13, opacity: !labelFile || uploadLoading ? 0.5 : 1 }}>
              {uploadLoading ? 'UPLOADING...' : '⬆ Upload Label'}
            </button>
            {uploadSuccess && (
              <button className="ss-btn ss-btn-outline" onClick={() => navigate('/customer/shipments')} style={{ flex: 1, padding: '11px 20px', justifyContent: 'center', fontSize: 13 }}>
                View Shipments →
              </button>
            )}
          </div>

          <div style={{ display: 'flex', justifyContent: 'space-between', marginTop: 4 }}>
            <button className="ss-btn ss-btn-outline" onClick={() => navigate('/customer/dashboard')}>Go to Dashboard</button>
            <button className="ss-btn ss-btn-outline" style={{ color: '#888', borderColor: '#333' }} onClick={() => navigate('/customer/shipments')}>Skip & Go to Shipments</button>
          </div>
        </div>
      );
    }
  };

  /* ══════════════════ STEP NEXT BUTTON LABEL ══════════════════ */
  const nextLabel = () => {
    if (submitting) return 'CREATING SHIPMENT...';
    if (activeStep === 3) return selectedPayMode ? 'Confirm & Create Shipment →' : 'Select Payment Mode First';
    if (activeStep === 4) return paymentResponse ? 'Continue to Label Upload →' : 'Complete Payment First';
    if (activeStep === STEPS.length - 1) return null; // handled inside step
    return 'Continue →';
  };

  const showNextButton = activeStep < STEPS.length - 1;
  const nextDisabled = submitting ||
    (activeStep === 3 && !selectedPayMode) ||
    (activeStep === 4 && !paymentResponse);

  return (
    <div style={{ maxWidth: 860 }}>
      {showInvoice && invoiceData && (
        <InvoicePrintView
          data={invoiceData}
          onClose={() => setShowInvoice(false)}
          onDownloaded={(file) => {
            setInvoiceFile(file);
            setShowInvoice(false);
            addNotification('Invoice downloaded! You can now upload it as a label.', 'success');
          }}
        />
      )}

      <div onClick={() => navigate(-1)} style={{ fontFamily: 'Orbitron, monospace', fontSize: 15, color: '#ff0000', fontWeight: 900, letterSpacing: '0.1em', cursor: 'pointer', marginBottom: 30 }}>
        ← BACK TO SHIPMENTS
      </div>
      <div className="accent-line" style={{ marginBottom: 8 }} />
      <h1 className="section-title">New Shipment</h1>
      <p className="section-sub" style={{ marginBottom: 24 }}>
        Complete each step — create shipment, get cost breakdown, choose payment, download invoice, then upload label.
      </p>

      {/* ── Step Indicators ── */}
      <div style={{ display: 'flex', marginBottom: 32, gap: 0 }}>
        {STEPS.map((s, i) => (
          <div key={s.key} style={{ flex: 1, display: 'flex', flexDirection: 'column', alignItems: 'center', position: 'relative' }}>
            {i > 0 && (
              <div style={{ position: 'absolute', left: '-50%', top: 14, width: '100%', height: 1, background: i <= activeStep ? 'var(--color-accent)' : 'var(--color-border)' }} />
            )}
            <div style={{
              width: 28, height: 28, borderRadius: 10, display: 'flex', alignItems: 'center', justifyContent: 'center',
              background: i < activeStep ? 'var(--color-accent)' : i === activeStep ? 'var(--color-accent)' : 'var(--color-surface-2)',
              border: `1px solid ${i <= activeStep ? 'var(--color-accent)' : 'var(--color-border)'}`,
              fontFamily: 'Orbitron, monospace', fontSize: 13, fontWeight: 700,
              color: i <= activeStep ? '#fff' : '#555', zIndex: 1,
              boxShadow: i === activeStep ? '0 0 10px rgba(224,0,26,0.4)' : 'none',
            }}>
              {i < activeStep ? '✓' : i + 1}
            </div>
            <div style={{
              fontFamily: 'Orbitron, monospace', fontSize: 10, fontWeight: 900, letterSpacing: '0.1em',
              textTransform: 'uppercase', textAlign: 'center',
              color: i === activeStep ? 'var(--color-accent)' : '#555', marginTop: 6,
            }}>
              {s.label}
            </div>
          </div>
        ))}
      </div>

      {/* ── Step Card ── */}
      <div className="ss-card" style={{ padding: 28, marginBottom: 20 }}>
        <div style={{ fontFamily: 'Orbitron, monospace', fontSize: 14, fontWeight: 700, letterSpacing: '0.08em', textTransform: 'uppercase', color: '#fff', marginBottom: 20, borderBottom: '1px solid var(--color-border)', paddingBottom: 12 }}>
          Step {activeStep + 1} — {STEPS[activeStep].label}
        </div>
        {renderStep()}
      </div>

      {/* ── Navigation ── */}
      {activeStep < STEPS.length - 1 && (
        <div style={{ display: 'flex', justifyContent: 'space-between' }}>
          <button className="ss-btn ss-btn-outline" disabled={activeStep === 0 || submitting}
            onClick={handleBack} style={{ opacity: activeStep === 0 || submitting ? 0.3 : 1 }}>
            Back
          </button>
          {showNextButton && (
            <button className="ss-btn" onClick={handleNext} disabled={nextDisabled}
              style={{ opacity: nextDisabled ? 0.5 : 1 }}>
              {nextLabel()}
            </button>
          )}
        </div>
      )}
    </div>
  );
};
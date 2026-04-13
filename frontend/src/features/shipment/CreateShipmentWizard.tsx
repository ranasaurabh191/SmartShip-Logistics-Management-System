import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { apiClient } from '../../core/api/axios';

const steps = [
  { key: 'sender', label: 'Sender Details' },
  { key: 'receiver', label: 'Receiver Details' },
  { key: 'package', label: 'Package Info' },
  { key: 'review', label: 'Review & Pay' },
];

export const CreateShipmentWizard = () => {
  const navigate = useNavigate();
  const [activeStep, setActiveStep] = useState(0);
  const [formData, setFormData] = useState({
    sender: { fullName: '', phone: '', street: '', city: '', state: '', postalCode: '', country: 'India' },
    receiver: { fullName: '', phone: '', street: '', city: '', state: '', postalCode: '', country: 'India' },
    package: { weightKg: '', lengthCm: '', widthCm: '', heightCm: '', description: '', declaredValue: '' },
    shipmentType: 'Domestic',
  });

  const handleNext = async () => {
    if (activeStep === steps.length - 1) {
      await submitShipment();
    } else {
      setActiveStep(prev => prev + 1);
    }
  };

  const handleBack = () => setActiveStep(prev => prev - 1);

  const handleChange = (section: string, field: string, value: string) => {
    setFormData((prev: any) => ({ ...prev, [section]: { ...prev[section], [field]: value } }));
  };

  const submitShipment = async () => {
    try {
      const payload = {
        ...formData,
        package: {
          weightKg: Number(formData.package.weightKg),
          lengthCm: Number(formData.package.lengthCm),
          widthCm: Number(formData.package.widthCm),
          heightCm: Number(formData.package.heightCm),
          description: formData.package.description,
          declaredValue: Number(formData.package.declaredValue),
        },
        pickupScheduledAt: new Date(Date.now() + 86400000).toISOString(),
        notes: 'Created via SmartShip frontend',
      };
      await apiClient.post('/shipments', payload);
      navigate('/customer/dashboard');
    } catch {
      navigate('/customer/dashboard');
    }
  };

  const addressFields = (_section: 'sender' | 'receiver') => [
    { field: 'fullName', label: 'Full Name', placeholder: 'Rahul Singh' },
    { field: 'phone', label: 'Phone Number', placeholder: '+91 98765 43210' },
    { field: 'street', label: 'Street Address', placeholder: '14B, Connaught Place', wide: true },
    { field: 'city', label: 'City', placeholder: 'Delhi' },
    { field: 'state', label: 'State', placeholder: 'Delhi' },
    { field: 'postalCode', label: 'Postal Code', placeholder: '110001' },
  ];

  const renderStep = () => {
    if (activeStep === 0 || activeStep === 1) {
      const sec = activeStep === 0 ? 'sender' : 'receiver';
      return (
        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 14 }}>
          {addressFields(sec as 'sender' | 'receiver').map(f => (
            <div key={f.field} style={f.wide ? { gridColumn: '1 / -1' } : {}}>
              <label style={{ display: 'block', fontFamily: 'Rajdhani, sans-serif', fontSize: 10, fontWeight: 600, letterSpacing: '0.14em', textTransform: 'uppercase', color: 'var(--color-text-muted)', marginBottom: 6 }}>{f.label}</label>
              <input
                className="ss-input"
                style={{ width: '100%' }}
                placeholder={f.placeholder}
                value={(formData as any)[sec][f.field]}
                onChange={e => handleChange(sec, f.field, e.target.value)}
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
        { field: 'declaredValue', label: 'Declared Value (₹)', placeholder: '5000' },
      ];
      return (
        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 14 }}>
          {pkgFields.map(f => (
            <div key={f.field}>
              <label style={{ display: 'block', fontFamily: 'Rajdhani, sans-serif', fontSize: 10, fontWeight: 600, letterSpacing: '0.14em', textTransform: 'uppercase', color: 'var(--color-text-muted)', marginBottom: 6 }}>{f.label}</label>
              <input className="ss-input" style={{ width: '100%' }} placeholder={f.placeholder} value={(formData.package as any)[f.field]} onChange={e => handleChange('package', f.field, e.target.value)} type={f.field === 'description' ? 'text' : 'number'} />
            </div>
          ))}
        </div>
      );
    }

    // Step 3: Review
    const items = [
      { label: 'From', value: formData.sender.city || '—' },
      { label: 'To', value: formData.receiver.city || '—' },
      { label: 'Receiver', value: formData.receiver.fullName || '—' },
      { label: 'Weight', value: formData.package.weightKg ? `${formData.package.weightKg} kg` : '—' },
      { label: 'Declared Value', value: formData.package.declaredValue ? `₹${formData.package.declaredValue}` : '—' },
      { label: 'Type', value: formData.shipmentType },
    ];
    return (
      <div style={{ display: 'flex', flexDirection: 'column', gap: 20 }}>
        <div style={{ background: 'var(--color-surface-2)', border: '1px solid var(--color-border)', borderRadius: 4, padding: 24 }}>
          <div style={{ fontFamily: 'Rajdhani, sans-serif', fontSize: 13, fontWeight: 700, letterSpacing: '0.08em', textTransform: 'uppercase', color: '#fff', marginBottom: 16 }}>Shipment Summary</div>
          {items.map(item => (
            <div key={item.label} style={{ display: 'flex', justifyContent: 'space-between', padding: '8px 0', borderBottom: '1px solid rgba(255,255,255,0.04)' }}>
              <span style={{ fontSize: 12, color: 'var(--color-text-muted)', fontFamily: 'Rajdhani, sans-serif', letterSpacing: '0.08em', textTransform: 'uppercase' }}>{item.label}</span>
              <span style={{ fontSize: 13, color: '#fff', fontFamily: 'Rajdhani, sans-serif', fontWeight: 600 }}>{item.value}</span>
            </div>
          ))}
          <div style={{ display: 'flex', justifyContent: 'space-between', padding: '12px 0', marginTop: 4 }}>
            <span style={{ fontFamily: 'Rajdhani, sans-serif', fontSize: 14, fontWeight: 700, letterSpacing: '0.08em', textTransform: 'uppercase', color: '#fff' }}>Estimated Cost</span>
            <span style={{ fontFamily: 'Rajdhani, sans-serif', fontSize: 18, fontWeight: 700, color: 'var(--color-accent)' }}>₹850.00</span>
          </div>
        </div>
        <div style={{ fontSize: 12, color: 'var(--color-text-muted)', display: 'flex', alignItems: 'center', gap: 8 }}>
          <span style={{ color: 'var(--color-warning)', fontSize: 14 }}>⚠</span>
          Upon submission, Razorpay payment confirmation will complete the booking and trigger the SAGA state machine.
        </div>
      </div>
    );
  };

  return (
    <div style={{ maxWidth: 760 }}>
      <div onClick={() => navigate(-1)} style={{ fontFamily: 'Rajdhani, sans-serif', fontSize: 11, color: 'var(--color-accent)', letterSpacing: '0.1em', cursor: 'pointer', marginBottom: 16 }}>← BACK</div>
      <div className="accent-line" style={{ marginBottom: 8 }} />
      <h1 className="section-title">New Shipment</h1>
      <p className="section-sub" style={{ marginBottom: 24 }}>Create a new shipment order — collected step by step</p>

      {/* Step tracker */}
      <div style={{ display: 'flex', marginBottom: 32, gap: 0 }}>
        {steps.map((s, i) => (
          <div key={s.key} style={{ flex: 1, display: 'flex', flexDirection: 'column', alignItems: 'center', position: 'relative' }}>
            {/* connecting line */}
            {i > 0 && <div style={{ position: 'absolute', left: '-50%', top: 14, width: '100%', height: 1, background: i <= activeStep ? 'var(--color-accent)' : 'var(--color-border)' }} />}
            <div style={{
              width: 28, height: 28, borderRadius: 2, display: 'flex', alignItems: 'center', justifyContent: 'center',
              background: i < activeStep ? 'var(--color-accent)' : i === activeStep ? 'var(--color-accent)' : 'var(--color-surface-2)',
              border: `1px solid ${i <= activeStep ? 'var(--color-accent)' : 'var(--color-border)'}`,
              fontFamily: 'Rajdhani, sans-serif', fontSize: 12, fontWeight: 700, color: i <= activeStep ? '#fff' : '#555',
              zIndex: 1,
              boxShadow: i === activeStep ? '0 0 10px rgba(224,0,26,0.4)' : 'none',
            }}>
              {i < activeStep ? '✓' : i + 1}
            </div>
            <div style={{ fontFamily: 'Rajdhani, sans-serif', fontSize: 9, fontWeight: 600, letterSpacing: '0.12em', textTransform: 'uppercase', color: i === activeStep ? 'var(--color-accent)' : '#555', marginTop: 6 }}>
              {s.label}
            </div>
          </div>
        ))}
      </div>

      {/* Step content */}
      <div className="ss-card" style={{ padding: 28, marginBottom: 20 }}>
        <div style={{ fontFamily: 'Rajdhani, sans-serif', fontSize: 14, fontWeight: 700, letterSpacing: '0.08em', textTransform: 'uppercase', color: '#fff', marginBottom: 20, borderBottom: '1px solid var(--color-border)', paddingBottom: 12 }}>
          Step {activeStep + 1} — {steps[activeStep].label}
        </div>
        {renderStep()}
      </div>

      {/* Navigation */}
      <div style={{ display: 'flex', justifyContent: 'space-between' }}>
        <button className="ss-btn ss-btn-outline" disabled={activeStep === 0} onClick={handleBack} style={{ opacity: activeStep === 0 ? 0.3 : 1 }}>
          ← Back
        </button>
        <button className="ss-btn" onClick={handleNext}>
          {activeStep === steps.length - 1 ? '▷ Submit & Book' : 'Continue →'}
        </button>
      </div>
    </div>
  );
};

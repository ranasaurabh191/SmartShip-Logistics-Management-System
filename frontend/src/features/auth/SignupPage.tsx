import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { apiClient } from '../../core/api/axios';

type Step = 'register' | 'otp';

export const SignupPage = () => {
  const parseApiError = (err: any): string => {
    const data = err?.response?.data;
    if (!data) return 'Network error. Is the server running?';

    if (data.errors && typeof data.errors === 'object') {
      return Object.entries(data.errors)
        .map(([field, msgs]) => {
          const messages = Array.isArray(msgs) ? msgs.join(', ') : msgs;
          return `${field}: ${messages}`;
        })
        .join('\n');
    }

    if (typeof data.message === 'string') return data.message;
    if (typeof data.title === 'string') return data.title;
    if (typeof data === 'string') return data;

    return 'Something went wrong. Please try again.';
  };
  const navigate = useNavigate();
  const [step, setStep] = useState<Step>('register');
  const [formData, setFormData] = useState({ name: '', email: '', phone: '', password: '' });
  const [otp, setOtp] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState(false);

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setFormData(prev => ({ ...prev, [e.target.name]: e.target.value }));
    setError('');
  };

  // ── Step 1: Request OTP ──
  const handleRequestOtp = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setError('');
    try {
      await apiClient.post('/auth/signup/request-otp', formData);
      setStep('otp');
    } catch (err: any) {
      setError(parseApiError(err));
    } finally {
      setLoading(false);
    }
  };

  // ── Step 2: Verify OTP ──
 const handleVerifyOtp = async (e: React.FormEvent) => {
  e.preventDefault();
  setLoading(true);
  setError('');
  try {
    await apiClient.post('/auth/signup/verify-otp', {
      email: formData.email,
      otp: otp,
      name: formData.name,
      phone: formData.phone,
      password: formData.password,
    });
    setSuccess(true);
    setTimeout(() => navigate('/auth/login'), 1500);
  } catch (err: any) {
    setError(parseApiError(err));
  } finally {
    setLoading(false);
  }
};

  const registerFields = [
    { name: 'name', label: 'Full Name', type: 'text', placeholder: 'name' },
    { name: 'email', label: 'Email Address', type: 'email', placeholder: 'email' },
    { name: 'phone', label: 'Phone Number', type: 'tel', placeholder: 'phone' },
    { name: 'password', label: 'Password', type: 'password', placeholder: 'password' },
  ] as const;

  return (
    <div>
      <div className="accent-line" />
      <h1 style={{ fontFamily: 'Rajdhani, sans-serif', fontSize: 28, fontWeight: 700, letterSpacing: '0.04em', textTransform: 'uppercase', color: '#fff', marginBottom: 4 }}>
        {step === 'register' ? 'Register' : 'Verify OTP'}
      </h1>
      <p style={{ fontSize: 13, color: 'var(--color-text-muted)', marginBottom: 28 }}>
        {step === 'register'
          ? 'Create a SmartShip logistics account'
          : `OTP sent to ${formData.email} — enter it below`}
      </p>

      {/* Step indicator */}
      <div style={{ display: 'flex', gap: 8, marginBottom: 24 }}>
        {(['register', 'otp'] as Step[]).map((s, i) => (
          <div key={s} style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
            <div style={{
              width: 22, height: 22, borderRadius: 2, display: 'flex', alignItems: 'center', justifyContent: 'center',
              fontFamily: 'Rajdhani, sans-serif', fontSize: 13, fontWeight: 700,
              background: step === s ? 'var(--color-accent)' : step === 'otp' && s === 'register' ? 'var(--color-success)' : 'var(--color-surface-2)',
              border: `1px solid ${step === s ? 'var(--color-accent)' : step === 'otp' && s === 'register' ? 'var(--color-success)' : 'var(--color-border)'}`,
              color: step === s || (step === 'otp' && s === 'register') ? '#fff' : 'var(--color-text-dim)',
            }}>{step === 'otp' && s === 'register' ? '✓' : i + 1}</div>
            <span style={{ fontFamily: 'Rajdhani, sans-serif', fontSize: 13, fontWeight: 600, letterSpacing: '0.1em', textTransform: 'uppercase', color: step === s ? 'var(--color-accent)' : 'var(--color-text-dim)' }}>
              {s === 'register' ? 'Details' : 'Verify'}
            </span>
            {i === 0 && <span style={{ color: 'var(--color-border)', margin: '0 4px' }}>—</span>}
          </div>
        ))}
      </div>

      {/* Success banner */}
      {success && (
        <div style={{ background: 'rgba(0,196,140,0.1)', border: '1px solid var(--color-success)', borderRadius: 2, padding: '10px 14px', marginBottom: 16, fontFamily: 'Rajdhani, sans-serif', fontSize: 12, color: 'var(--color-success)', letterSpacing: '0.08em', textTransform: 'uppercase' }}>
          ✓ Account created — redirecting to login...
        </div>
      )}

      {/* Error banner */}
      {error && (
        <div style={{
          background: 'rgba(224,0,26,0.08)', border: '1px solid var(--color-accent)',
          borderRadius: 2, padding: '10px 14px', marginBottom: 16,
        }}>
          {error.split('\n').map((line, i) => (
            <div key={i} style={{
              fontFamily: 'Rajdhani, sans-serif', fontSize: 14,
              color: '#ffffffbf', letterSpacing: '0.06em',
              marginBottom: i < error.split('\n').length - 1 ? 4 : 0,
            }}>
              ⚠ {line}
            </div>
          ))}
        </div>
      )}

      {/* ── Step 1: Register form ── */}
      {step === 'register' && (
        <form onSubmit={handleRequestOtp} style={{ display: 'flex', flexDirection: 'column', gap: 14 }}>
          {registerFields.map(f => (
            <div key={f.name}>
              <label style={{ display: 'block', fontFamily: 'Rajdhani, sans-serif', fontSize: 10, fontWeight: 600, letterSpacing: '0.14em', textTransform: 'uppercase', color: 'var(--color-text-muted)', marginBottom: 6 }}>
                {f.label}
              </label>
              <input
                className="ss-input"
                name={f.name}
                type={f.type}
                placeholder={f.placeholder}
                value={formData[f.name]}
                onChange={handleChange}
                required
                style={{ width: '100%' }}
              />
            </div>
          ))}
          <button
            type="submit"
            className="ss-btn"
            disabled={loading}
            style={{ width: '100%', justifyContent: 'center', marginTop: 6, padding: '12px 16px', fontSize: 13 }}
          >
            {loading ? 'SENDING OTP...' : 'SEND OTP →'}
          </button>
        </form>
      )}

      {/* ── Step 2: OTP form ── */}
      {step === 'otp' && (
        <form onSubmit={handleVerifyOtp} style={{ display: 'flex', flexDirection: 'column', gap: 14 }}>
          <div>
            <label style={{ display: 'block', fontFamily: 'Rajdhani, sans-serif', fontSize: 10, fontWeight: 600, letterSpacing: '0.14em', textTransform: 'uppercase', color: 'var(--color-text-muted)', marginBottom: 6 }}>
              One-Time Password
            </label>
            <input
              className="ss-input"
              type="text"
              placeholder="Enter OTP"
              value={otp}
              onChange={e => { setOtp(e.target.value); setError(''); }}
              required
              autoFocus
              maxLength={8}
              style={{ width: '100%', fontFamily: 'Rajdhani, sans-serif', fontSize: 20, fontWeight: 700, letterSpacing: '0.3em', textAlign: 'center' }}
            />
          </div>
          <button
            type="submit"
            className="ss-btn"
            disabled={loading || success}
            style={{ width: '100%', justifyContent: 'center', marginTop: 6, padding: '12px 16px', fontSize: 13 }}
          >
            {loading ? 'VERIFYING...' : 'VERIFY & CREATE ACCOUNT'}
          </button>
          <button
            type="button"
            className="ss-btn-outline ss-btn"
            style={{ width: '100%', justifyContent: 'center', fontSize: 11 }}
            onClick={() => { setStep('register'); setError(''); setOtp(''); }}
          >
            ← Back / Resend OTP
          </button>
        </form>
      )}

      <div style={{ marginTop: 20, textAlign: 'center' }}>
        <span style={{ fontSize: 12, color: 'var(--color-text-muted)' }}>Already registered ? </span>
        <Link to="/auth/login" style={{ fontSize: 12, color: 'var(--color-accent)', fontFamily: 'Rajdhani, sans-serif', fontWeight: 600, letterSpacing: '0.08em', textDecoration: 'none' }}>
          SIGN IN
        </Link>
      </div>
    </div>
  );
};
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
  const IDENTITY_BASE = 'http://localhost:5001';
  const GoogleIcon = () => (
    <svg width="18" height="18" viewBox="0 0 24 24" fill="none">
      <path d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z" fill="#4285F4" />
      <path d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z" fill="#34A853" />
      <path d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l3.66-2.84z" fill="#FBBC05" />
      <path d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z" fill="#EA4335" />
    </svg>
  );

  const GitHubIcon = () => (
    <svg width="19" height="19" viewBox="0 0 24 24" fill="currentColor">
      <path d="M12 2C6.477 2 2 6.484 2 12.017c0 4.425 2.865 8.18 6.839 9.504.5.092.682-.217.682-.483 0-.237-.008-.868-.013-1.703-2.782.605-3.369-1.343-3.369-1.343-.454-1.158-1.11-1.466-1.11-1.466-.908-.62.069-.608.069-.608 1.003.07 1.531 1.032 1.531 1.032.892 1.53 2.341 1.088 2.91.832.092-.647.35-1.088.636-1.338-2.22-.253-4.555-1.113-4.555-4.951 0-1.093.39-1.988 1.029-2.688-.103-.253-.446-1.272.098-2.65 0 0 .84-.27 2.75 1.026A9.564 9.564 0 0112 6.844c.85.004 1.705.115 2.504.337 1.909-1.296 2.747-1.027 2.747-1.027.546 1.379.202 2.398.1 2.651.64.7 1.028 1.595 1.028 2.688 0 3.848-2.339 4.695-4.566 4.943.359.309.678.92.678 1.855 0 1.338-.012 2.419-.012 2.747 0 .268.18.58.688.482A10.019 10.019 0 0022 12.017C22 6.484 17.522 2 12 2z" />
    </svg>
  );
  const handleGoogleLogin = () => { window.location.href = `${IDENTITY_BASE}/auth/oauth/google`; };
  const handleGitHubLogin = () => { window.location.href = `${IDENTITY_BASE}/auth/oauth/github`; };
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

      <h1 style={{ fontFamily: 'Rajdhani, sans-serif', fontSize: 28, fontWeight: 700, letterSpacing: '0.04em', textTransform: 'uppercase', color: '#fff', marginBottom: 4 }}>
        {step === 'register' ? 'Register' : 'Verify OTP'}
      </h1>
      <div className="accent-line" />
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
            <span style={{ fontFamily: 'Rajdhani, sans-serif', fontSize: 13, fontWeight: 900, letterSpacing: '0.1em', textTransform: 'uppercase', color: step === s ? 'var(--color-accent)' : 'var(--color-text-dim)' }}>
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
              <label style={{ display: 'block', fontFamily: 'Rajdhani, sans-serif', fontSize: 10, fontWeight: 900, letterSpacing: '0.14em', textTransform: 'uppercase', color: '#ffffffbf', marginBottom: 6 }}>
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

      <div style={{ marginBottom: 20, marginTop: 20, textAlign: 'center' }}>
        <span style={{ fontSize: 14, color: 'var(--color-text-muted)' }}>Already registered ? </span>
        <Link to="/auth/login" style={{ fontSize: 14, color: 'var(--color-accent)', fontFamily: 'Rajdhani, sans-serif', fontWeight: 900, letterSpacing: '0.08em', textDecoration: 'none' }}>
          SIGN IN
        </Link>

      </div>
      {/* ── OAuth Buttons ── */}
      <div style={{ display: 'flex', flexDirection: 'column', gap: 10 }}>

        {/* Google */}
        <button
          onClick={handleGoogleLogin}
          type="button"
          onMouseEnter={e => {
            (e.currentTarget as HTMLButtonElement).style.background = 'rgba(66,133,244,0.12)';
            (e.currentTarget as HTMLButtonElement).style.borderColor = 'rgba(66,133,244,0.5)';
          }}
          onMouseLeave={e => {
            (e.currentTarget as HTMLButtonElement).style.background = 'rgba(255,255,255,0.04)';
            (e.currentTarget as HTMLButtonElement).style.borderColor = 'rgba(255,255,255,0.1)';
          }}
          style={{
            display: 'flex', alignItems: 'center', justifyContent: 'center', gap: 10,
            width: '100%', padding: '11px 16px',
            background: 'rgba(255,255,255,0.04)',
            border: '1px solid rgba(255,255,255,0.1)',
            borderRadius: 2, cursor: 'pointer',
            fontFamily: 'Orbitron, sans-serif', fontSize: 10, fontWeight: 600,
            letterSpacing: '0.1em', textTransform: 'uppercase', color: '#ffffffcc',
            transition: 'background 0.2s, border-color 0.2s',
          }}
        >
          <GoogleIcon />
          SignUp with Google
        </button>

        {/* GitHub */}
        <button
          onClick={handleGitHubLogin}
          type="button"
          onMouseEnter={e => {
            (e.currentTarget as HTMLButtonElement).style.background = 'rgba(255,255,255,0.1)';
            (e.currentTarget as HTMLButtonElement).style.borderColor = 'rgba(255,255,255,0.3)';
          }}
          onMouseLeave={e => {
            (e.currentTarget as HTMLButtonElement).style.background = 'rgba(255,255,255,0.04)';
            (e.currentTarget as HTMLButtonElement).style.borderColor = 'rgba(255,255,255,0.1)';
          }}
          style={{
            display: 'flex', alignItems: 'center', justifyContent: 'center', gap: 10,
            width: '100%', padding: '11px 16px',
            background: 'rgba(255,255,255,0.04)',
            border: '1px solid rgba(255,255,255,0.1)',
            borderRadius: 2, cursor: 'pointer',
            fontFamily: 'Orbitron, sans-serif', fontSize: 10, fontWeight: 600,
            letterSpacing: '0.1em', textTransform: 'uppercase', color: '#ffffffcc',
            transition: 'background 0.2s, border-color 0.2s',
          }}
        >
          <GitHubIcon />
          SignUp with GitHub
        </button>
      </div>
    </div>
  );
};
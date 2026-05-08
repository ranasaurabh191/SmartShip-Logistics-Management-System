import { useState } from 'react';
import { Link } from 'react-router-dom';
import { apiClient } from '../../core/api/axios';

export const ForgotPasswordPage = () => {
  const [email, setEmail] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');

  const handleSubmit = async (e: React.SyntheticEvent) => {
    e.preventDefault();
    setLoading(true);
    setError('');
    setSuccess('');
    try {
      await apiClient.post('/auth/forgot-password', { email });
      setSuccess('If an account exists with that email, a password reset link has been sent.');
    } catch (err: any) {
      const msg =
        err?.response?.data?.error ||
        err?.response?.data?.message ||
        'Failed to request password reset. Please try again later.';
      setError(msg);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div>
      <h1 style={{
        fontFamily: 'Rajdhani, sans-serif', fontSize: 28, fontWeight: 700,
        letterSpacing: '0.04em', textTransform: 'uppercase', color: '#fff', marginBottom: 1,
      }}>
        Reset Password
      </h1>
      <div className="accent-line" />
      <p style={{ fontSize: 13, color: 'var(--color-text-muted)', marginBottom: 28 }}>
        Enter your email to receive a password reset link.
      </p>

      {error && (
        <div style={{
          padding: '10px 14px', background: 'rgba(224,0,26,0.08)',
          border: '1px solid rgba(224,0,26,0.3)', borderRadius: 2, marginBottom: 16,
          fontFamily: 'Rajdhani, sans-serif', fontSize: 14, fontWeight: 900,
          letterSpacing: '0.08em', color: '#e0001a',
        }}>
          {error}
        </div>
      )}

      {success && (
        <div style={{
          padding: '10px 14px', background: 'rgba(0,196,140,0.08)',
          border: '1px solid rgba(0,196,140,0.3)', borderRadius: 2, marginBottom: 16,
          fontFamily: 'Rajdhani, sans-serif', fontSize: 14, fontWeight: 600,
          letterSpacing: '0.04em', color: '#00c48c',
        }}>
          {success}
        </div>
      )}

      {!success && (
        <form onSubmit={handleSubmit} style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
          <div>
            <label style={{
              display: 'block', fontFamily: 'Rajdhani, sans-serif', fontSize: 12,
              fontWeight: 600, letterSpacing: '0.14em', textTransform: 'uppercase',
              color: '#ffffffd2', marginBottom: 6,
            }}>
              Email Address
            </label>
            <input
              className="ss-input" type="email" value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="operator@smartship.in" required style={{ width: '100%' }}
            />
          </div>

          <button type="submit" className="ss-btn" disabled={loading || !email} style={{
            width: '100%', justifyContent: 'center', marginTop: 8,
            padding: '12px 16px', fontSize: 13, opacity: loading || !email ? 0.7 : 1,
          }}>
            {loading ? 'SENDING LINK...' : 'SEND RESET LINK'}
          </button>
        </form>
      )}

      <div style={{ marginTop: 24, textAlign: 'center' }}>
        <Link to="/auth/login" style={{
          fontSize: 14, color: 'var(--color-text-muted)', fontFamily: 'Rajdhani, sans-serif',
          fontWeight: 600, letterSpacing: '0.08em', textDecoration: 'none', transition: 'color 0.2s',
        }}>
          ← BACK TO LOGIN
        </Link>
      </div>
    </div>
  );
};

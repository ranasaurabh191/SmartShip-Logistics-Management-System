import { useState, useEffect } from 'react';
import { Link, useSearchParams, useNavigate } from 'react-router-dom';
import { apiClient } from '../../core/api/axios';

const EyeIcon = () => (
  <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
    <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"></path>
    <circle cx="12" cy="12" r="3"></circle>
  </svg>
);

const EyeOffIcon = () => (
  <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
    <path d="M17.94 17.94A10.07 10.07 0 0 1 12 20c-7 0-11-8-11-8a18.45 18.45 0 0 1 5.06-5.94M9.9 4.24A9.12 9.12 0 0 1 12 4c7 0 11 8 11 8a18.5 18.5 0 0 1-2.16 3.19m-6.72-1.07a3 3 0 1 1-4.24-4.24"></path>
    <line x1="1" y1="1" x2="23" y2="23"></line>
  </svg>
);

export const ResetPasswordPage = () => {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  
  const token = searchParams.get('token');
  const email = searchParams.get('email');

  const [password, setPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');

  useEffect(() => {
    if (!token || !email) {
      setError('Invalid or missing reset token. Please request a new password reset link.');
    }
  }, [token, email]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (password !== confirmPassword) {
      setError('Passwords do not match.');
      return;
    }
    
    setLoading(true);
    setError('');
    setSuccess('');
    
    try {
      await apiClient.post('/auth/reset-password', { email, token, newPassword: password });
      setSuccess('Your password has been reset successfully.');
      setTimeout(() => {
        navigate('/auth/login');
      }, 3000);
    } catch (err: any) {
      const msg =
        err?.response?.data?.error ||
        err?.response?.data?.message ||
        'Failed to reset password. The link might be expired.';
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
        Create New Password
      </h1>
      <div className="accent-line" />
      <p style={{ fontSize: 13, color: 'var(--color-text-muted)', marginBottom: 28 }}>
        Please enter your new password below.
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
          {success} Redirecting to login...
        </div>
      )}

      {!success && token && email && (
        <form onSubmit={handleSubmit} style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
          <div>
            <label style={{
              display: 'block', fontFamily: 'Rajdhani, sans-serif', fontSize: 12,
              fontWeight: 600, letterSpacing: '0.14em', textTransform: 'uppercase',
              color: '#ffffffd2', marginBottom: 6,
            }}>
              New Password
            </label>
            <div style={{ position: 'relative' }}>
              <input
                className="ss-input" type={showPassword ? "text" : "password"} value={password}
                onChange={(e) => setPassword(e.target.value)}
                placeholder="***************" required style={{ width: '100%', paddingRight: 40 }}
                minLength={6}
              />
              <button
                type="button"
                onClick={() => setShowPassword(!showPassword)}
                title={showPassword ? "Hide password" : "Show password"}
                style={{
                  position: 'absolute', right: 10, top: '50%', transform: 'translateY(-50%)',
                  background: 'none', border: 'none', color: '#888', cursor: 'pointer',
                  display: 'flex', alignItems: 'center', justifyContent: 'center', padding: 4
                }}
              >
                {showPassword ? <EyeOffIcon /> : <EyeIcon />}
              </button>
            </div>
          </div>
          
          <div>
            <label style={{
              display: 'block', fontFamily: 'Rajdhani, sans-serif', fontSize: 12,
              fontWeight: 600, letterSpacing: '0.14em', textTransform: 'uppercase',
              color: '#ffffffd2', marginBottom: 6,
            }}>
              Confirm New Password
            </label>
            <input
              className="ss-input" type="password" value={confirmPassword}
              onChange={(e) => setConfirmPassword(e.target.value)}
              placeholder="***************" required style={{ width: '100%' }}
              minLength={6}
            />
          </div>

          <button type="submit" className="ss-btn" disabled={loading || !password || !confirmPassword} style={{
            width: '100%', justifyContent: 'center', marginTop: 8,
            padding: '12px 16px', fontSize: 13, opacity: loading || !password ? 0.7 : 1,
          }}>
            {loading ? 'RESETTING...' : 'RESET PASSWORD'}
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

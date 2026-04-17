import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useAuthStore } from '../../store/useAuthStore';
import { apiClient } from '../../core/api/axios';

export const LoginPage = () => {
  const navigate = useNavigate();
  const login = useAuthStore((state) => state.login);
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  const handleLogin = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setError('');

    try {
      const res = await apiClient.post('/auth/login', { email, password });
      const { token, role, name, id } = res.data;

      if (!token || !role) {
        setError('Invalid response from server. Please try again.');
        return;
      }

      const normalizedRole = String(role).toUpperCase() as 'ADMIN' | 'CUSTOMER';

      login(
        {
          id: id ?? 0,
          email,
          name: name ?? email,
          role: normalizedRole,
        },
        token
      );

      navigate(normalizedRole === 'ADMIN' ? '/admin/dashboard' : '/customer/dashboard');

    } catch (err: any) {
      const msg =
        err?.response?.data?.error ||
        err?.response?.data?.message ||
        (err?.code === 'ERR_NETWORK'
          ? 'Cannot reach server. Is the backend running?'
          : null) ||
        'Authentication failed. Please check your credentials.';
      setError(msg);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div>
      <div className="accent-line" />
      <h1 style={{
        fontFamily: 'Rajdhani, sans-serif', fontSize: 28, fontWeight: 700,
        letterSpacing: '0.04em', textTransform: 'uppercase', color: '#fff', marginBottom: 4,
      }}>
        Login
      </h1>
      <p style={{ fontSize: 13, color: 'var(--color-text-muted)', marginBottom: 28 }}>
        Authenticate to enter SmartShip
      </p>

      {error && (
        <div style={{
          padding: '10px 14px', background: 'rgba(224,0,26,0.08)',
          border: '1px solid rgba(224,0,26,0.3)', borderRadius: 2, marginBottom: 16,
          fontFamily: 'Rajdhani, sans-serif', fontSize: 12, fontWeight: 600,
          letterSpacing: '0.08em', color: '#e0001a',
        }}>
          {error}
        </div>
      )}

      <form onSubmit={handleLogin} style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
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

        <div>
          <label style={{
            display: 'block', fontFamily: 'Rajdhani, sans-serif', fontSize: 12,
            fontWeight: 600, letterSpacing: '0.14em', textTransform: 'uppercase',
            color: '#ffffffd2', marginBottom: 6,
          }}>
            Password
          </label>
          <input
            className="ss-input" type="password" value={password}
            onChange={(e) => setPassword(e.target.value)}
            placeholder="***************" required style={{ width: '100%' }}
          />
        </div>

        <button type="submit" className="ss-btn" disabled={loading} style={{
          width: '100%', justifyContent: 'center', marginTop: 8,
          padding: '12px 16px', fontSize: 13, opacity: loading ? 0.7 : 1,
        }}>
          {loading ? 'AUTHENTICATING...' : 'ENTER SYSTEM'}
        </button>
      </form>

      <div style={{ marginTop: 20, textAlign: 'center' }}>
        <span style={{ fontSize: 13, color: 'var(--color-text-muted)' }}>No account?{'  '}</span>
        <Link to="/auth/signup" style={{
          fontSize: 13, color: 'var(--color-accent)', fontFamily: 'Rajdhani, sans-serif',
          fontWeight: 600, letterSpacing: '0.08em', textDecoration: 'none',
        }}>
          REGISTER
        </Link>
      </div>
    </div>
  );
};
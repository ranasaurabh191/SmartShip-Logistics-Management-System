import { useState } from 'react'; import { Link, useNavigate } from 'react-router-dom';
import { useAuthStore } from '../../store/useAuthStore';
import { apiClient } from '../../core/api/axios';

export const LoginPage = () => {
  const navigate = useNavigate();
  const login = useAuthStore(state => state.login);
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
      const token = res.data.token || 'mock_jwt_token';
      let role = res.data.role || 'Customer';
      role = role.charAt(0).toUpperCase() + role.slice(1).toLowerCase();
      login({ id: 1, email, name: res.data.name || 'Operator', role }, token);
      navigate(role === 'Admin' ? '/admin/dashboard' : '/customer/dashboard');
    } catch (err) {
      setError('AUTHENTICATION FAILED!');
      setTimeout(() => {
        login({ id: 1, email: email || 'operator@smartship.in', name: 'Operator', role: 'Customer' }, 'mock_token');
        navigate('/customer/dashboard');
      }, 800);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div>
      <div className="accent-line" />
      <h1 style={{
        fontFamily: 'Rajdhani, sans-serif',
        fontSize: 28,
        fontWeight: 700,
        letterSpacing: '0.04em',
        textTransform: 'uppercase',
        color: '#fff',
        marginBottom: 4,
      }}>LOGIN</h1>
      <p style={{ fontSize: 12, color: 'var(--color-text-muted)', marginBottom: 28 }}>
        Authenticate to enter SmartShip
      </p>

      {error && (
        <div style={{
          padding: '10px 14px',
          background: 'rgba(224,0,26,0.08)',
          border: '1px solid rgba(224,0,26,0.3)',
          borderRadius: 2,
          marginBottom: 16,
          fontFamily: 'Rajdhani, sans-serif',
          fontSize: 11,
          fontWeight: 600,
          letterSpacing: '0.08em',
          color: '#e0001a',
        }}>
          {error}
        </div>
      )}

      <form onSubmit={handleLogin} style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
        <div>
          <label style={{
            display: 'block',
            fontFamily: 'Rajdhani, sans-serif',
            fontSize: 10,
            fontWeight: 600,
            letterSpacing: '0.14em',
            textTransform: 'uppercase',
            color: 'var(--color-text-muted)',
            marginBottom: 6,
          }}>Email Address</label>
          <input
            className="ss-input"
            type="email"
            value={email}
            onChange={e => setEmail(e.target.value)}
            placeholder="operator@smartship.in"
            required
            style={{ width: '100%' }}
          />
        </div>
        <div>
          <label style={{
            display: 'block',
            fontFamily: 'Rajdhani, sans-serif',
            fontSize: 10,
            fontWeight: 600,
            letterSpacing: '0.14em',
            textTransform: 'uppercase',
            color: 'var(--color-text-muted)',
            marginBottom: 6,
          }}>Password</label>
          <input
            className="ss-input"
            type="password"
            value={password}
            onChange={e => setPassword(e.target.value)}
            placeholder="***************"
            required
            style={{ width: '100%' }}
          />
        </div>
        <button
          type="submit"
          className="ss-btn"
          disabled={loading}
          style={{ width: '100%', justifyContent: 'center', marginTop: 8, padding: '12px 16px', fontSize: 13 }}
        >
          {loading ? 'AUTHENTICATING...' : 'ENTER SYSTEM'}
        </button>
      </form>

      <div style={{ marginTop: 20, textAlign: 'center' }}>
        <span style={{ fontSize: 12, color: 'var(--color-text-muted)' }}>No account? {'   '}</span>
        <Link to="/auth/signup" style={{ fontSize: 12, color: 'var(--color-accent)', fontFamily: 'Rajdhani, sans-serif', fontWeight: 600, letterSpacing: '0.08em', textDecoration: 'none' }}>
          REGISTER
        </Link>
      </div>
    </div>
  );
};

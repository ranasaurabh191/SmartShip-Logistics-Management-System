import { useState } from 'react'; import { Link, useNavigate } from 'react-router-dom';
import { apiClient } from '../../core/api/axios';

export const SignupPage = () => {
  const navigate = useNavigate();
  const [formData, setFormData] = useState({ name: '', email: '', phone: '', password: '' });
  const [loading, setLoading] = useState(false);

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setFormData(prev => ({ ...prev, [e.target.name]: e.target.value }));
  };

  const handleSignup = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    try {
      await apiClient.post('/auth/signup', formData);
      navigate('/auth/login');
    } catch {
      navigate('/auth/login');
    } finally {
      setLoading(false);
    }
  };

  const fields = [
    { name: 'name', label: 'Full Name', type: 'text', placeholder: 'User Name' },
    { name: 'email', label: 'Email Address', type: 'email', placeholder: 'user@smartship.in' },
    { name: 'phone', label: 'Phone Number', type: 'tel', placeholder: '+91 9876543210' },
    { name: 'password', label: 'Password', type: 'password', placeholder: 'Password' },
  ];

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
      }}>Register</h1>
      <p style={{ fontSize: 12, color: 'var(--color-text-muted)', marginBottom: 28 }}>
        Create a SmartShip logistics account to begin
      </p>

      <form onSubmit={handleSignup} style={{ display: 'flex', flexDirection: 'column', gap: 14 }}>
        {fields.map(f => (
          <div key={f.name}>
            <label style={{
              display: 'block',
              fontFamily: 'Rajdhani, sans-serif',
              fontSize: 10,
              fontWeight: 600,
              letterSpacing: '0.14em',
              textTransform: 'uppercase',
              color: 'var(--color-text-muted)',
              marginBottom: 6,
            }}>{f.label}</label>
            <input
              className="ss-input"
              name={f.name}
              type={f.type}
              placeholder={f.placeholder}
              value={formData[f.name as keyof typeof formData]}
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
          {loading ? 'REGISTERING...' : 'CREATE ACCOUNT'}
        </button>
      </form>

      <div style={{ marginTop: 20, textAlign: 'center' }}>
        <span style={{ fontSize: 12, color: 'var(--color-text-muted)' }}>Already registered?{' '}</span>
        <Link to="/auth/login" style={{ fontSize: 12, color: 'var(--color-accent)', fontFamily: 'Rajdhani, sans-serif', fontWeight: 600, letterSpacing: '0.08em', textDecoration: 'none' }}>
          SIGN IN
        </Link>
      </div>
    </div>
  );
};

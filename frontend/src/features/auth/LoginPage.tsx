import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useAuthStore } from '../../store/useAuthStore';
import { apiClient } from '../../core/api/axios';

const IDENTITY_BASE = 'http://localhost:5001';

const GoogleIcon = () => (
  <svg width="18" height="18" viewBox="0 0 24 24" fill="none">
    <path d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z" fill="#4285F4"/>
    <path d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z" fill="#34A853"/>
    <path d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l3.66-2.84z" fill="#FBBC05"/>
    <path d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z" fill="#EA4335"/>
  </svg>
);

const GitHubIcon = () => (
  <svg width="19" height="19" viewBox="0 0 24 24" fill="currentColor">
    <path d="M12 2C6.477 2 2 6.484 2 12.017c0 4.425 2.865 8.18 6.839 9.504.5.092.682-.217.682-.483 0-.237-.008-.868-.013-1.703-2.782.605-3.369-1.343-3.369-1.343-.454-1.158-1.11-1.466-1.11-1.466-.908-.62.069-.608.069-.608 1.003.07 1.531 1.032 1.531 1.032.892 1.53 2.341 1.088 2.91.832.092-.647.35-1.088.636-1.338-2.22-.253-4.555-1.113-4.555-4.951 0-1.093.39-1.988 1.029-2.688-.103-.253-.446-1.272.098-2.65 0 0 .84-.27 2.75 1.026A9.564 9.564 0 0112 6.844c.85.004 1.705.115 2.504.337 1.909-1.296 2.747-1.027 2.747-1.027.546 1.379.202 2.398.1 2.651.64.7 1.028 1.595 1.028 2.688 0 3.848-2.339 4.695-4.566 4.943.359.309.678.92.678 1.855 0 1.338-.012 2.419-.012 2.747 0 .268.18.58.688.482A10.019 10.019 0 0022 12.017C22 6.484 17.522 2 12 2z"/>
  </svg>
);

export const LoginPage = () => {
  const navigate = useNavigate();
  const login = useAuthStore((state) => state.login);
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  const handleGoogleLogin = () => { window.location.href = `${IDENTITY_BASE}/auth/oauth/google`; };
  const handleGitHubLogin = () => { window.location.href = `${IDENTITY_BASE}/auth/oauth/github`; };

  const handleLogin = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setError('');
    try {
      const res = await apiClient.post('/auth/login', { email, password });
      const { token, role, name, id } = res.data;
      if (!token || !role) { setError('Invalid response from server. Please try again.'); return; }
      const normalizedRole = String(role).toUpperCase() as 'ADMIN' | 'CUSTOMER';
      login({ id: id ?? 0, email, name: name ?? email, role: normalizedRole }, token);
      navigate(normalizedRole === 'ADMIN' ? '/admin/dashboard' : '/customer/dashboard');
    } catch (err: any) {
      const msg =
        err?.response?.data?.error ||
        err?.response?.data?.message ||
        (err?.code === 'ERR_NETWORK' ? 'Cannot reach server. Is the backend running?' : null) ||
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

      {/* ── Divider ── */}
      <div style={{
        display: 'flex', alignItems: 'center', gap: 12, margin: '24px 0',
      }}>
        <div style={{ flex: 1, height: 1, background: 'rgba(255,255,255,0.08)' }} />
        <span style={{
          fontFamily: 'Rajdhani, sans-serif', fontSize: 11, fontWeight: 600,
          letterSpacing: '0.16em', textTransform: 'uppercase', color: 'rgba(255,255,255,0.3)',
        }}>
          OR CONTINUE WITH
        </span>
        <div style={{ flex: 1, height: 1, background: 'rgba(255,255,255,0.08)' }} />
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
            fontFamily: 'Rajdhani, sans-serif', fontSize: 13, fontWeight: 600,
            letterSpacing: '0.1em', textTransform: 'uppercase', color: '#ffffffcc',
            transition: 'background 0.2s, border-color 0.2s',
          }}
        >
          <GoogleIcon />
          Sign in with Google
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
            fontFamily: 'Rajdhani, sans-serif', fontSize: 13, fontWeight: 600,
            letterSpacing: '0.1em', textTransform: 'uppercase', color: '#ffffffcc',
            transition: 'background 0.2s, border-color 0.2s',
          }}
        >
          <GitHubIcon />
          Sign in with GitHub
        </button>
      </div>

      <div style={{ marginTop: 24, textAlign: 'center' }}>
        <span style={{ fontSize: 14, color: 'var(--color-text-muted)' }}>No account ?{'  '}</span>
        <Link to="/auth/signup" style={{
          fontSize: 14, color: 'var(--color-accent)', fontFamily: 'Rajdhani, sans-serif',
          fontWeight: 900, letterSpacing: '0.08em', textDecoration: 'none',
        }}>
          REGISTER
        </Link>
      </div>
    </div>
  );
};
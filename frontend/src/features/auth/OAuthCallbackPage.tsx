import { useEffect } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { useAuthStore } from '../../store/useAuthStore';

export const OAuthCallbackPage = () => {
  const [params] = useSearchParams();
  const navigate = useNavigate();
  const { login } = useAuthStore();

  useEffect(() => {
    const token  = params.get('token');
    const userId = params.get('userId');
    const role   = params.get('role') as 'CUSTOMER' | 'ADMIN';
    const name   = params.get('name') ?? '';
    const email  = params.get('email') ?? '';
    const error  = params.get('error');

    if (error || !token || !userId || !role) {
      navigate('/auth/login?error=oauth_failed', { replace: true });
      return;
    }

    login(
      { id: parseInt(userId), email: decodeURIComponent(email), name: decodeURIComponent(name), role },
      token
    );

    navigate(role === 'ADMIN' ? '/admin/dashboard' : '/customer/dashboard', { replace: true });
  }, []);

  return (
    <div style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', height: '100vh' }}>
      <p>Signing you in...</p>
    </div>
  );
};
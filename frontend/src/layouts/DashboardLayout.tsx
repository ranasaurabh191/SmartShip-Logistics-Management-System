import { useEffect } from 'react';
import { Outlet, useNavigate, useLocation } from 'react-router-dom';
import { AnimatePresence, motion } from 'framer-motion';
import { useAuthStore } from '../store/useAuthStore';
import { SmartShipLogo } from '../shared/components/Logo';

interface NavItem {
  label: string;
  path: string;
  icon: string;
}

const customerNav: NavItem[] = [
  { label: 'Dashboard', path: '/customer/dashboard', icon: '⊞' },
  { label: 'Shipments', path: '/customer/shipments', icon: '◈' },
  { label: 'Payments', path: '/customer/payments', icon: '◇' },
  { label: 'Tracking', path: '/customer/tracking', icon: '◎' },
];

const adminNav: NavItem[] = [
  { label: 'Dashboard', path: '/admin/dashboard', icon: '⊞' },
  { label: 'Shipments', path: '/admin/shipments', icon: '◈' },
  { label: 'Tracking', path: '/admin/tracking', icon: '◎' },
  { label: 'Payments', path: '/admin/payments', icon: '◇' },
  { label: 'Hub Management', path: '/admin/hubs', icon: '◫' },
  { label: 'Users', path: '/admin/users', icon: '◩' },
];

interface DashboardLayoutProps {
  role: 'CUSTOMER' | 'ADMIN';
}

export const DashboardLayout: React.FC<DashboardLayoutProps> = ({ role }) => {
  const navigate = useNavigate();
  const location = useLocation();
  const { isAuthenticated, user, logout } = useAuthStore();


  useEffect(() => {
    if (!isAuthenticated) {
      navigate('/auth/login', { replace: true });
      return;
    }
    if (user && user.role !== role) {
      navigate(
        user.role === 'ADMIN' ? '/admin/dashboard' : '/customer/dashboard',
        { replace: true }
      );
    }
  }, [isAuthenticated, user, role, navigate]);

  if (!isAuthenticated || (user && user.role !== role)) return null;

  const navItems = role === 'ADMIN' ? adminNav : customerNav;

  return (
    <div style={{ display: 'flex', height: '100vh', overflow: 'hidden', background: 'var(--color-bg)' }}>
      <aside style={{
        width: 257, flexShrink: 0, background: '#111111',
        borderRight: '1px solid rgba(255,255,255,0.06)',
        display: 'flex', flexDirection: 'column', overflow: 'hidden',
      }}>
        <div style={{ padding: '20px 16px 16px', borderBottom: '1px solid rgba(255,255,255,0.06)' }}>
          <div style={{ marginBottom: 34 }}>
            <SmartShipLogo />
          </div>
          <div style={{
            marginTop: 6, fontFamily: 'Rajdhani, sans-serif', fontSize: 18,
            fontWeight: 900, letterSpacing: '0.1em', textTransform: 'uppercase',
            color: 'var(--color-text-dim)',
          }}>
            {role === 'ADMIN' ? 'Admin Command Center' : 'Logistics Dashboard'}
          </div>
        </div>

        <nav style={{ flex: 1, padding: '12px 8px', overflowY: 'auto' }}>
          <div style={{ marginBottom: 8, padding: '0 6px' }}>
            <span style={{
              fontFamily: 'Rajdhani, sans-serif', fontSize: 13, fontWeight: 600,
              letterSpacing: '0.18em', textTransform: 'uppercase', color: 'var(--color-text-dim)',
            }}>
              Navigation
            </span>
          </div>

          {navItems.map(item => {
            const isActive = location.pathname === item.path;
            return (
              <div
                key={item.path}
                className={`sidebar-item ${isActive ? 'active' : ''}`}
                onClick={() => navigate(item.path)}
              >
                <span style={{ fontSize: 18, opacity: 0.7 }}>{item.icon}</span>
                <span>{item.label}</span>
              </div>
            );
          })}
        </nav>

        <div style={{
          padding: '12px 17px', borderTop: '1px solid rgba(255,255,255,0.06)',
          display: 'flex', alignItems: 'center', justifyContent: 'space-between',
        }}>
          <div>
            <div style={{ fontSize: 14, fontWeight: 900, color: 'var(--color-text)' }}>
              {user?.name || 'User'}
            </div>
            <div style={{
              fontSize: 14, color: 'var(--color-text-muted)', textTransform: 'uppercase',
              fontFamily: 'Rajdhani, sans-serif', letterSpacing: '0.08em', fontWeight: 900,
            }}>
              {role}
            </div>
          </div>
          <button
            className="ss-btn ss-btn-outline"
            style={{ fontWeight: 600, fontSize: 14, padding: '4px 7px' }}
            onClick={() => { logout(); navigate('/auth/login'); }}
          >
            LOGOUT
          </button>
        </div>
      </aside>

      <div style={{ flex: 1, display: 'flex', flexDirection: 'column', overflow: 'hidden' }}>
        <header style={{
          height: 69, borderBottom: '1px solid rgba(255,255,255,0.06)',
          background: '#0f0f0f', display: 'flex', alignItems: 'center',
          justifyContent: 'space-between', padding: '0 24px', flexShrink: 0,
        }}>
          <div style={{
            fontFamily: 'Rajdhani, sans-serif', fontSize: 17, fontWeight: 600,
            letterSpacing: '0.14em', textTransform: 'uppercase', color: 'var(--color-text-muted)',
          }}>
            {(() => {
              const p = location.pathname;
              if (p.includes('/dashboard')) return 'Dashboard';
              if (p.includes('/shipments/create')) return 'New Shipment';
              if (p.includes('/shipments')) return 'Shipment Registry';
              if (p.includes('/payments')) return 'Payments';
              if (p.includes('/track') || p.includes('/tracking')) return 'Tracking Details';
              if (p.includes('/hubs')) return 'Hub Management';
              if (p.includes('/users')) return 'User Directory';
              return 'Overview';
            })()}
          </div>


        </header>

        <main
          style={{
            flex: 1,
            overflow: 'hidden',
            padding: 24,
            position: 'relative',
          }}
        >
          <AnimatePresence mode="wait">
            <motion.div
              key={location.pathname}
              initial={{
                opacity: 0,
                x: 28,
                scale: 0.85,
                filter: 'blur(4px)',
              }}
              animate={{
                opacity: 1,
                x: 0,
                scale: 1,
                filter: 'blur(0px)',
              }}
              exit={{
                opacity: 0,
                x: -18,
                scale: 0.99,
                filter: 'blur(3px)',
              }}
              transition={{
                duration: 0.26,
                ease: [0.2, 1, 0.36, 1],
              }}
              style={{
                height: '100%',
                overflow: 'auto',
              }}
            >
              <Outlet />
            </motion.div>
          </AnimatePresence>
        </main>
      </div>
    </div>
  );
};
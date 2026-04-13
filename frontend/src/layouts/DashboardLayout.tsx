import { Outlet, useNavigate, useLocation } from 'react-router-dom';
import { useAuthStore } from '../store/useAuthStore';
import { SmartShipLogo } from '../shared/components/Logo';

const services = [
  { name: 'Identity', status: 'online' },
  { name: 'Shipment', status: 'online' },
  { name: 'Tracking', status: 'online' },
  { name: 'Payment', status: 'online' },
  { name: 'Admin', status: 'online' },
  { name: 'Notification', status: 'offline' },
];

interface NavItem {
  label: string;
  path: string;
  icon: string;
}

const customerNav: NavItem[] = [
  { label: 'Dashboard', path: '/customer/dashboard', icon: 'âŠž' },
  { label: 'Shipments', path: '/customer/shipments', icon: 'â—ˆ' },
  { label: 'Tracking', path: '/customer/tracking', icon: 'â—Ž' },
  { label: 'Payments', path: '/customer/payments', icon: 'â—‡' },
  { label: 'Saga Viewer', path: '/customer/saga', icon: 'â—ˆ' },
];

const adminNav: NavItem[] = [
  { label: 'Dashboard', path: '/admin/dashboard', icon: 'âŠž' },
  { label: 'Shipments', path: '/admin/shipments', icon: 'â—ˆ' },
  { label: 'Tracking', path: '/admin/tracking', icon: 'â—Ž' },
  { label: 'Payments', path: '/admin/payments', icon: 'â—‡' },
  { label: 'Admin Panel', path: '/admin/panel', icon: 'â—§' },
  { label: 'Saga Viewer', path: '/admin/saga', icon: 'â—ˆ' },
];

interface DashboardLayoutProps {
  role: 'Customer' | 'Admin';
}

export const DashboardLayout: React.FC<DashboardLayoutProps> = ({ role }) => {
  const navigate = useNavigate();
  const location = useLocation();
  const { isAuthenticated, user, logout } = useAuthStore();

  if (!isAuthenticated) {
    navigate('/auth/login');
    return null;
  }

  const navItems = role === 'Admin' ? adminNav : customerNav;

  return (
    <div style={{ display: 'flex', height: '100vh', overflow: 'hidden', background: 'var(--color-bg)' }}>

      {/* â”€â”€â”€ SIDEBAR â”€â”€â”€ */}
      <aside style={{
        width: 240,
        flexShrink: 0,
        background: '#111111',
        borderRight: '1px solid rgba(255,255,255,0.06)',
        display: 'flex',
        flexDirection: 'column',
        overflow: 'hidden',
      }}>

        {/* Logo */}
        <div style={{
          padding: '20px 16px 16px',
          borderBottom: '1px solid rgba(255,255,255,0.06)',
        }}>
          <SmartShipLogo />
          <div style={{
            marginTop: 6,
            fontFamily: 'Rajdhani, sans-serif',
            fontSize: 9,
            fontWeight: 600,
            letterSpacing: '0.18em',
            textTransform: 'uppercase',
            color: 'var(--color-text-dim)',
          }}>
            LOGISTICS COMMAND CENTER
          </div>
        </div>

        {/* Nav items */}
        <nav style={{ flex: 1, padding: '12px 8px', overflowY: 'auto' }}>
          <div style={{ marginBottom: 8, padding: '0 6px' }}>
            <span style={{
              fontFamily: 'Rajdhani, sans-serif',
              fontSize: 9,
              fontWeight: 600,
              letterSpacing: '0.18em',
              textTransform: 'uppercase',
              color: 'var(--color-text-dim)',
            }}>Navigation</span>
          </div>
          {navItems.map((item) => {
            const isActive = location.pathname === item.path;
            return (
              <div
                key={item.path}
                className={`sidebar-item ${isActive ? 'active' : ''}`}
                onClick={() => navigate(item.path)}
              >
                <span style={{ fontSize: 11, opacity: 0.7 }}>{item.icon}</span>
                <span>{item.label}</span>
                {isActive && (
                  <span className="ss-badge glow" style={{ marginLeft: 'auto', fontSize: 8 }}>LIVE</span>
                )}
              </div>
            );
          })}
        </nav>

        {/* Service Health Module */}
        <div style={{
          padding: '14px 12px',
          borderTop: '1px solid rgba(255,255,255,0.06)',
          background: '#0f0f0f',
        }}>
          <div style={{
            fontFamily: 'Rajdhani, sans-serif',
            fontSize: 9,
            fontWeight: 600,
            letterSpacing: '0.18em',
            textTransform: 'uppercase',
            color: 'var(--color-text-dim)',
            marginBottom: 10,
          }}>
            Service Health
          </div>
          {services.map((svc) => (
            <div key={svc.name} style={{
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'space-between',
              padding: '5px 0',
            }}>
              <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                <div className={`health-dot ${svc.status}`} />
                <span style={{
                  fontFamily: 'var(--font-body)',
                  fontSize: 11,
                  color: 'var(--color-text-muted)',
                }}>
                  {svc.name}
                </span>
              </div>
              <span className={`ss-badge ${svc.status === 'online' ? 'success' : 'muted'}`}>
                {svc.status === 'online' ? 'ACTIVE' : 'OFFLINE'}
              </span>
            </div>
          ))}
        </div>

        {/* User panel */}
        <div style={{
          padding: '12px 14px',
          borderTop: '1px solid rgba(255,255,255,0.06)',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
        }}>
          <div>
            <div style={{ fontSize: 12, fontWeight: 600, color: 'var(--color-text)' }}>
              {user?.name || 'User'}
            </div>
            <div style={{ fontSize: 10, color: 'var(--color-text-muted)', textTransform: 'uppercase', fontFamily: 'Rajdhani, sans-serif', letterSpacing: '0.08em' }}>
              {role}
            </div>
          </div>
          <button
            className="ss-btn ss-btn-outline"
            style={{ fontSize: 10, padding: '4px 10px' }}
            onClick={() => { logout(); navigate('/auth/login'); }}
          >
            Exit
          </button>
        </div>
      </aside>

      {/* â”€â”€â”€ MAIN AREA â”€â”€â”€ */}
      <div style={{ flex: 1, display: 'flex', flexDirection: 'column', overflow: 'hidden' }}>

        {/* Top Header */}
        <header style={{
          height: 52,
          borderBottom: '1px solid rgba(255,255,255,0.06)',
          background: '#0f0f0f',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
          padding: '0 24px',
          flexShrink: 0,
        }}>
          <div style={{
            fontFamily: 'Rajdhani, sans-serif',
            fontSize: 11,
            letterSpacing: '0.14em',
            textTransform: 'uppercase',
            color: 'var(--color-text-muted)',
          }}>
            {/* Breadcrumb */}
            {location.pathname.split('/').filter(Boolean).map((seg, i, arr) => (
              <span key={i}>
                <span style={{ color: i === arr.length - 1 ? 'var(--color-text)' : undefined }}>
                  {seg.toUpperCase()}
                </span>
                {i < arr.length - 1 && <span style={{ margin: '0 6px', opacity: 0.4 }}>/</span>}
              </span>
            ))}
          </div>
          <div style={{ display: 'flex', alignItems: 'center', gap: 14 }}>
            <span style={{ fontSize: 16, cursor: 'pointer', color: 'var(--color-text-muted)' }}>ðŸ””</span>
            <div style={{
              width: 28, height: 28,
              background: 'var(--color-accent)',
              borderRadius: 2,
              display: 'flex', alignItems: 'center', justifyContent: 'center',
              fontFamily: 'Rajdhani, sans-serif',
              fontSize: 13,
              fontWeight: 700,
              color: '#fff',
            }}>
              {(user?.name || 'U').charAt(0).toUpperCase()}
            </div>
          </div>
        </header>

        {/* Content */}
        <main style={{ flex: 1, overflow: 'auto', padding: 24 }}>
          <Outlet />
        </main>
      </div>
    </div>
  );
};

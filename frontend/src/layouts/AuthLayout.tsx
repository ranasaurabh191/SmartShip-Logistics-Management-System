import { Outlet, Link, useLocation } from 'react-router-dom';
import { SmartShipLogo } from '../shared/components/Logo';
import { useEffect, useState } from 'react';
import { useAuthStore } from '../store/useAuthStore';

const STATS = [
  { value: '31,000+', label: 'Pin Codes Covered' },
  { value: '25+', label: 'Courier Partners' },
  { value: '99.9%', label: 'Delivery Uptime' },
  { value: '2M+', label: 'Shipments/Month' },
];

const FEATURES = [
  { title: 'Real-Time Tracking', desc: 'Live updates at every hub transition across India' },
  { title: 'Lowest Shipping Rates', desc: 'Compare 25+ couriers and auto-select best rate' },
  { title: 'Automated NDR', desc: 'Smart non-delivery management with retry flows' },
  { title: 'Bulk Shipments', desc: 'Upload thousands of orders in one click' },
];

export const AuthLayout = () => {
  const logout = useAuthStore(state => state.logout);
  const location = useLocation();
  const isLogin = location.pathname.includes('login');

  const [visible, setVisible] = useState(true);

  useEffect(() => {
    logout();
  }, [logout]);

  useEffect(() => {
    const timer = setTimeout(() => {
      setVisible(true);
    }, 200);

    return () => clearTimeout(timer);
  }, [location.pathname]);
  
  return (
    <>
      <div className="auth-root">
        <div className="auth-brand">
          <div className="corner-tr" />
          <div className="auth-brand-content">

            <div className="auth-logo-area">
              <SmartShipLogo />
              <div className="auth-logo-sub">India's Fastest Growing Shipping Platform</div>
            </div>

            <div className="auth-headline">
              <div className="auth-accent-line" />
              <h2>Ship Smarter.<br />Deliver Faster.<br />Grow Bigger.</h2>
              <p>
                One platform to manage all your shipments, compare courier
                rates, automate tracking, and scale your eCommerce business
                across 31,000+ pin codes in India.
              </p>
            </div>

            {/* Stats */}
            <div className="auth-stats">
              {STATS.map(s => (
                <div key={s.label} className="auth-stat">
                  <div className="auth-stat-value">{s.value}</div>
                  <div className="auth-stat-label">{s.label}</div>
                </div>
              ))}
            </div>

            {/* Features */}
            <div className="auth-features">
              {FEATURES.map(f => (
                <div key={f.title} className="auth-feature">
                  <div>
                    <div className="auth-feature-title">{f.title}</div>
                    <div className="auth-feature-desc">{f.desc}</div>
                  </div>
                </div>
              ))}
            </div>

          </div>

          <div className="auth-anim-strip">
            <div className="auth-road" />

            <div className="auth-city">
              {[22, 20, 16, 28, 10, 22, 55, 11, 55].map((h, i) => (
                <div key={i} className="auth-building" style={{ height: h }} />
              ))}
            </div>

            <svg
              className="auth-truck"
              width="140"
              height="50"
              viewBox="0 0 140 50"
              fill="none"
            >
              {/* Truck body */}
              <rect x="8" y="18" width="72" height="20" rx="3" fill="#d90429" />

              {/* Cabin */}
              <rect x="80" y="12" width="32" height="26" rx="3" fill="#ef233c" />

              {/* Window */}
              <rect x="99" y="18" width="12" height="7" rx="1.5" fill="#90e0ef" />

              {/* Back box line */}
              <rect x="16" y="24" width="36" height="3" rx="1.5" fill="rgba(255,255,255,0.2)" />

              {/* Wheels */}
              <circle cx="20" cy="40" r="6" fill="#222" />
              <circle cx="20" cy="40" r="2.5" fill="#666" />

              <circle cx="33" cy="40" r="6" fill="#222" />
              <circle cx="33" cy="40" r="2.5" fill="#666" />

              <circle cx="104" cy="40" r="6" fill="#222" />
              <circle cx="104" cy="40" r="2.5" fill="#666" />
            </svg>


            {/* Van */}
            <svg className="auth-van" width="80" height="42" viewBox="0 0 80 44" fill="none">
              <rect x="4" y="14" width="68" height="24" rx="3" fill="#e0001a" />
              <rect x="4" y="14" width="24" height="24" rx="3" fill="#c8001a" />
              <rect x="56" y="17" width="16" height="12" rx="1" fill="#90e0ef" opacity="0.8" />
              <circle cx="18" cy="40" r="5" fill="#222" stroke="#555" strokeWidth="1.5" />
              <circle cx="18" cy="40" r="2" fill="#444" />
              <circle cx="62" cy="40" r="5" fill="#222" stroke="#555" strokeWidth="1.5" />
              <circle cx="62" cy="40" r="2" fill="#444" />
              <rect x="36" y="20" width="24" height="14" rx="1" fill="rgba(255,255,255,0.12)" />
              <line x1="48" y1="20" x2="48" y2="34" stroke="rgba(255,255,255,0.2)" strokeWidth="1" />
            </svg>

            {/* Plane */}
            <svg className="auth-plane" width="64" height="22" viewBox="0 0 64 24" fill="none">
              <path d="M4 12 L40 4 L60 12 L40 20 Z" fill="#cc0017" />
              <path d="M20 12 L36 6 L44 12 L36 18 Z" fill="#e0001a" />
              <path d="M8 12 L20 8 L24 12 L20 16 Z" fill="#c8001a" />
              <rect x="30" y="16" width="14" height="4" rx="2" fill="#1a1a1a" />
              <rect x="22" y="15" width="10" height="3" rx="1" fill="#1a1a1a" />
            </svg>

          </div>
        </div>

        {/* ─── RIGHT: Form Panel ─── */}
        <div className="auth-form-panel">
          {/* Back button */}
          <div style={{
            position: 'absolute',
            top: 24,
            left: 24,
          }}>
            <Link
              to="/"
              style={{
                display: 'inline-flex',
                alignItems: 'center',
                gap: 6,
                fontFamily: 'Rajdhani, sans-serif',
                fontSize: 11,
                fontWeight: 900,
                letterSpacing: '0.15em',
                textTransform: 'uppercase',
                color: '#9a9a9aff',
                padding: '6px 10px',
                border: '1px solid rgba(169, 16, 16, 0.61)',
                borderRadius: 2,
                transition: 'color 0.18s, border-color 0.18s, background 0.18s',
              }}
              onMouseEnter={e => {
                (e.currentTarget ).style.color = '#ffffff';
                (e.currentTarget ).style.borderColor = 'rgba(224,0,26,0.3)';
                (e.currentTarget ).style.background = 'rgba(224,0,26,0.05)';
              }}
              onMouseLeave={e => {
                (e.currentTarget ).style.color = '#9a9a9aff';
                (e.currentTarget ).style.borderColor = 'rgba(169, 16, 16, 0.61)';
                (e.currentTarget ).style.background = 'transparent';
              }}
            >
              <svg width="12" height="12" viewBox="0 0 24 24" fill="none"
                stroke="currentColor" strokeWidth="3.5" strokeLinecap="round">
                <path d="M 23 12 H5 M12 5 l-7 7 l7 7" />
              </svg>
              Back to Home
            </Link>
          </div>
          <div className="auth-mobile-bar">
            <SmartShipLogo />
            <span style={{
              fontFamily: 'Rajdhani, sans-serif',
              fontSize: 10,
              fontWeight: 600,
              letterSpacing: '0.16em',
              textTransform: 'uppercase',
              color: '#c8c8c8ff',
            }}>
              31,000+ Pin Codes
            </span>
          </div>

          <div className="auth-form-inner">

            <div className="auth-tabs">
              <Link to="/auth/login" className={`auth-tab ${isLogin ? 'active' : ''}`}>Sign In</Link>
              <Link to="/auth/signup" className={`auth-tab ${!isLogin ? 'active' : ''}`}>Register</Link>
            </div>

            <div style={{
              opacity: visible ? 1 : 0,
              transform: visible ? 'translateY(0px)' : 'translateY(18px)',
              transition: 'opacity 0.18s ease, transform 0.18s ease',
            }}>
              <Outlet />
            </div>

          </div>

          <div className="auth-mobile-strip">
            <div className="auth-road" />
            <svg className="auth-truck" width="100" height="38" viewBox="0 0 120 48" fill="none">
              <rect x="70" y="12" width="44" height="28" rx="3" fill="#e0001a" />
              <rect x="8" y="10" width="66" height="30" rx="2" fill="#cc0017" />
              <circle cx="28" cy="42" r="6" fill="#222" stroke="#555" strokeWidth="1.5" />
              <circle cx="96" cy="42" r="6" fill="#222" stroke="#555" strokeWidth="1.5" />
              <circle cx="78" cy="42" r="6" fill="#222" stroke="#555" strokeWidth="1.5" />
            </svg>
            <svg className="auth-van" width="68" height="36" viewBox="0 0 80 44" fill="none">
              <rect x="4" y="14" width="68" height="24" rx="3" fill="#e0001a" />
              <circle cx="18" cy="40" r="5" fill="#222" stroke="#555" strokeWidth="1.5" />
              <circle cx="62" cy="40" r="5" fill="#222" stroke="#6e6e6eff" strokeWidth="1.5" />
            </svg>
          </div>

        </div>
      </div>
    </>
  );
};
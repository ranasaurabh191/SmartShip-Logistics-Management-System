import { Outlet, Link, useLocation } from 'react-router-dom';
import { SmartShipLogo } from '../shared/components/Logo';
import { useEffect, useState } from 'react';

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
  const location = useLocation();
  const isLogin = location.pathname.includes('login');
  const [visible, setVisible] = useState(true);
  const [displayPath, setDisplayPath] = useState(location.pathname);

  useEffect(() => {
    setVisible(false);

    const timer = setTimeout(() => {
      setDisplayPath(location.pathname);
      setVisible(true);
    }, 180);

    return () => clearTimeout(timer);
  }, [location.pathname]);
  return (
    <>
      <style>{`
        .auth-root {
          min-height: 100vh;
          background: var(--color-bg);
          display: flex;
          overflow: hidden;
          position: relative;
        }

        /* ══ LEFT BRAND PANEL ══ */
        .auth-brand {
          width: 760px;
          flex-shrink: 0;
          background: #0f0f0f;
          border-right: 1px solid rgba(224, 0, 26, 0.24);
          display: flex;
          flex-direction: column;
          padding: 36px 40px 0;
          position: relative;
          overflow: hidden;
        }
        .auth-brand::before {
          content: '';
          position: absolute;
          inset: 0;
          pointer-events: none;
          z-index: 0;
          background-image: repeating-linear-gradient(
            0deg,
            transparent, transparent 2px,
            rgba(255,255,255,0.011) 2px,
            rgba(255,255,255,0.011) 4px
          );
        }
        .auth-brand .corner-tr {
          position: absolute;
          top: 0; right: 0;
          width: 0; height: 0;
          border-top: 120px solid rgba(224, 0, 26, 0.21);
          border-left: 120px solid transparent;
          pointer-events: none;
          z-index: 1;
        }
        .auth-brand-content {
          position: relative;
          z-index: 2;
          display: flex;
          flex-direction: column;
          flex: 1;
        }

        /* Logo */
        .auth-logo-area { 
          margin-bottom: 32px;
          margin-left: -40px;
         }
        .auth-logo-sub {
          margin-top: 5px;
          font-family: 'Rajdhani', sans-serif;
          font-size: 12px;
          margin-left: 40px;
          font-weight: 600;
          letter-spacing: 0.2em;
          text-transform: uppercase;
          color: #bcbcbcff;
        }

        /* Headline */
        .auth-headline { margin-bottom: 20px; }
        .auth-accent-line {
          width: 0;
          height: 2px;
          background: var(--color-accent);
          margin-bottom: 11px;
          animation: growLine 0.8s 0.3s ease forwards;
        }
        @keyframes growLine { to { width: 40px; } }

        .auth-headline h2 {
          font-family: 'Rajdhani', sans-serif;
          font-size: 28px;
          font-weight: 700;
          letter-spacing: 0.04em;
          text-transform: uppercase;
          color: #fff;
          line-height: 1.15;
          margin-bottom: 10px;
          animation: fadeUp 0.6s 0.4s ease both;
        }
        .auth-headline p {
          font-size: 12px;
          color: #aea9a9ff;
          line-height: 1.7;
          max-width: 320px;
          animation: fadeUp 0.6s 0.5s ease both;
        }
        @keyframes fadeUp {
          from { opacity: 0; transform: translateY(16px); }
          to   { opacity: 1; transform: translateY(0); }
        }

        /* Stats */
        .auth-stats {
          display: grid;
          grid-template-columns: 1fr 1fr;
          gap: 8px;
          margin-bottom: 22px;
          animation: fadeUp 0.6s 0.6s ease both;
        }
        .auth-stat {
          background: rgba(224,0,26,0.05);
          border: 1px solid rgba(224, 0, 26, 0.35);
          border-radius: 3px;
          padding: 11px 14px;
          cursor: default;
          transition: background 0.2s, border-color 0.2s, transform 0.2s;
        }
        .auth-stat:hover {
          background: rgba(224,0,26,0.1);
          border-color: rgba(224,0,26,0.3);
          transform: translateY(-2px);
        }
        .auth-stat-value {
          font-family: 'Orbitron', monospace;
          font-size: 17px;
          font-weight: 700;
          color: #ffffffff;
          line-height: 1;
          margin-bottom: 3px;
        }
        .auth-stat-label {
          font-family: 'Rajdhani', sans-serif;
          font-size: 9px;
          font-weight: 600;
          letter-spacing: 0.15em;
          text-transform: uppercase;
          color: #aea9a9ff;
        }

        /* Features */
        .auth-features {
          display: flex;
          flex-direction: column;
          gap: 7px;
          margin-bottom: 16px;
          animation: fadeUp 0.6s 0.7s ease both;
        }
        .auth-feature {
          display: flex;
          align-items: flex-start;
          gap: 12px;
          padding: 10px 12px;
          background: rgba(255, 255, 255, 0.08);
          border: 1px solid rgba(255,255,255,0.05);
          border-radius: 3px;
          cursor: default;
          transition: background 0.2s, border-color 0.2s, transform 0.2s;
        }
        .auth-feature:hover {
          background: rgba(224,0,26,0.06);
          border-color: rgba(224,0,26,0.2);
          transform: translateX(4px);
        }
        .auth-feature-icon {
          font-size: 14px;
          flex-shrink: 0;
          margin-top: 1px;
          transition: filter 0.2s;
          filter: grayscale(0.3);
        }
        .auth-feature:hover .auth-feature-icon { filter: grayscale(0); }
        .auth-feature-title {
          font-family: 'Rajdhani', sans-serif;
          font-size: 12px;
          font-weight: 700;
          letter-spacing: 0.06em;
          text-transform: uppercase;
          color: #ddd;
          margin-bottom: 2px;
          transition: color 0.2s;
        }
        .auth-feature:hover .auth-feature-title { color: #fff; }
        .auth-feature-desc {
          font-size: 11px;
          color: var(--color-text-muted);
          line-height: 1.5;
        }

        /* ══ SHIPPING ANIMATION STRIP ══ */
        .auth-anim-strip {
          position: relative;
          height: 88px;
          margin: 0 -40px;
          background: #0a0a0a;
          border-top: 1px solid rgba(224, 0, 26, 0.32);
          overflow: hidden;
          flex-shrink: 0;
        }
        .auth-road {
          position: absolute;
          bottom: 18px;
          left: 0; right: 0;
          height: 2px;
          background: rgba(255, 255, 255, 0.25);
        }
        .auth-road::after {
          content: '';
          position: absolute;
          top: -4px;
          left: 0; right: 0;
          height: 1px;
          background: repeating-linear-gradient(
            90deg,
            rgba(224, 0, 26, 0.66) 0px, rgba(224, 0, 26, 0.37) 16px,
            transparent 16px, transparent 32px
          );
          animation: roadScroll 1s linear infinite;
        }
        @keyframes roadScroll {
          from { background-position: 0 0; }
          to   { background-position: 32px 0; }
        }
        .auth-truck {
          position: absolute;
          bottom: 20px;
          animation: driveTruck 6s linear infinite;
        }
        @keyframes driveTruck {
          0%   { left: -160px; }
          100% { left: calc(100% + 20px); }
        }
        .auth-van {
          position: absolute;
          bottom: 20px;
          animation: driveVan 9s linear infinite;
        }
        @keyframes driveVan {
          0%   { left: -100px; }
          100% { left: calc(100% + 20px); }
        }
        .auth-plane {
          position: absolute;
          top: 6px;
          animation: flyPlane 4s 1s linear infinite;
        }
        @keyframes flyPlane {
          0%   { left: -80px; opacity: 0; }
          5%   { opacity: 1; }
          95%  { opacity: 1; }
          100% { left: calc(100% + 20px); opacity: 0; }
        }
        .auth-city {
          position: absolute;
          bottom: 20px;
          right: 20px;
          display: flex;
          align-items: flex-end;
          gap: 3px;
          opacity: 0.12;
          pointer-events: none;
        }
        .auth-building {
          background: #e0001a;
          width: 51px;
          border-radius: 1px 1px 0 0;
        }

        /* ══ RIGHT FORM PANEL ══ */
        .auth-form-panel {
          flex: 1;
          display: flex;
          flex-direction: column;
          align-items: center;
          justify-content: center;
          padding: 40px 24px;
          background: var(--color-bg);
          position: relative;
        }
        .auth-form-panel::before {
          content: '';
          position: absolute;
          top: 50%; left: 50%;
          transform: translate(-50%, -50%);
          width: 420px; height: 420px;
          background: radial-gradient(circle, rgba(224, 0, 26, 0.09) 0%, transparent 70%);
          pointer-events: none;
        }
        .auth-form-inner {
          width: 100%;
          max-width: 480px;
          position: relative;
          z-index: 1;
          animation: fadeUp 0.4s 0.4s ease both;
        }

        /* Tabs */
        .auth-tabs {
          display: flex;
          margin-bottom: 58px;
          border-bottom: 1px solid var(--color-border);
            
        }
        .auth-tab {
          flex: 1;
          padding: 10px 0;
          text-align: center;
          font-family: 'Rajdhani', sans-serif;
          font-size: 18px;
          font-weight: 700;
          letter-spacing: 0.14em;
          text-transform: uppercase;
          color: var(--color-text-dim);
          text-decoration: none;
          border-bottom: 2px solid transparent;
          margin-bottom: -1px;
        }
        .auth-tab:hover { color: var(--color-text-muted); }
        .auth-tab.active {
          color: var(--color-accent);
          border-bottom-color: var(--color-accent);
        }

        /* ══ MOBILE ══ */
        .auth-mobile-bar { display: none; }
        .auth-mobile-strip { display: none; }

        @media (max-width: 767px) {
          .auth-brand { display: none; }
          .auth-mobile-bar {
            display: flex !important;
            align-items: center;
            justify-content: space-between;
            padding: 14px 20px;
            background: #111;
            border-bottom: 1px solid rgba(224,0,26,0.18);
            flex-shrink: 0;
            width: 100%;
          }
          .auth-form-panel {
            flex-direction: column;
            align-items: stretch;
            justify-content: flex-start;
            padding: 0;
          }
          .auth-form-inner {
            max-width: 100%;
            padding: 28px 24px;
          }
          .auth-mobile-strip {
            display: block !important;
            width: 100%;
            height: 58px;
            background: #0a0a0a;
            border-top: 1px solid rgba(224,0,26,0.1);
            position: relative;
            overflow: hidden;
            flex-shrink: 0;
            margin-top: auto;
          }
        }
      `}</style>

      <div className="auth-root">

        {/* ─── LEFT: Brand Panel ─── */}
        <div className="auth-brand">
          <div className="corner-tr" />

          <div className="auth-brand-content">

            {/* Logo */}
            <div className="auth-logo-area">
              <SmartShipLogo />
              <div className="auth-logo-sub">India's Fastest Growing Shipping Platform</div>
            </div>

            {/* Headline */}
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

          {/* ── Shipping Animation Strip ── */}
          <div className="auth-anim-strip">
            <div className="auth-road" />

            {/* City silhouette */}
            <div className="auth-city">
              {[22, 20, 16, 28, 10, 22, 55, 11, 55].map((h, i) => (
                <div key={i} className="auth-building" style={{ height: h }} />
              ))}
            </div>

            {/* Simple Clean Truck */}
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
                fontWeight: 600,
                letterSpacing: '0.12em',
                textTransform: 'uppercase',
                color: '#9a9a9aff',
                textDecoration: 'none',
                padding: '6px 10px',
                border: '1px solid rgba(169, 16, 16, 0.61)',
                borderRadius: 2,
                transition: 'color 0.18s, border-color 0.18s, background 0.18s',
              }}
              onMouseEnter={e => {
                (e.currentTarget as HTMLAnchorElement).style.color = '#ffffff';
                (e.currentTarget as HTMLAnchorElement).style.borderColor = 'rgba(224,0,26,0.3)';
                (e.currentTarget as HTMLAnchorElement).style.background = 'rgba(224,0,26,0.05)';
              }}
              onMouseLeave={e => {
                (e.currentTarget as HTMLAnchorElement).style.color = '#9a9a9aff';
                (e.currentTarget as HTMLAnchorElement).style.borderColor = 'rgba(255,255,255,0.07)';
                (e.currentTarget as HTMLAnchorElement).style.background = 'transparent';
              }}
            >
              <svg width="12" height="12" viewBox="0 0 24 24" fill="none"
                stroke="currentColor" strokeWidth="2.5" strokeLinecap="round">
                <path d="M19 12H5M12 5l-7 7 7 7" />
              </svg>
              Back to Home
            </Link>
          </div>
          {/* Mobile top bar */}
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

            {/* Tab switcher */}
            <div className="auth-tabs">
              <Link to="/auth/login" className={`auth-tab ${isLogin ? 'active' : ''}`}>Sign In</Link>
              <Link to="/auth/signup" className={`auth-tab ${!isLogin ? 'active' : ''}`}>Register</Link>
            </div>

            <div style={{
              opacity: visible ? 1 : 0,
              transform: visible ? 'translateY(0)' : 'translateY(8px)',
              transition: 'opacity 0.18s ease, transform 0.18s ease',
            }}>
              <Outlet />
            </div>

          </div>

          {/* Mobile animation strip */}
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
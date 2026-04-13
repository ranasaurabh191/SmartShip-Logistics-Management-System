import { Outlet } from 'react-router-dom';
import { SmartShipLogo } from '../shared/components/Logo';

export const AuthLayout = () => {
  return (
    <div style={{
      minHeight: '100vh',
      background: 'var(--color-bg)',
      display: 'flex',
    }}>
      {/* Left brand panel */}
      <div style={{
        width: 420,
        flexShrink: 0,
        background: '#111',
        borderRight: '1px solid rgba(224,0,26,0.2)',
        display: 'flex',
        flexDirection: 'column',
        justifyContent: 'space-between',
        padding: 40,
        position: 'relative',
        overflow: 'hidden',
      }}>
        {/* Geometric corner decoration */}
        <div style={{
          position: 'absolute',
          top: 0, right: 0,
          width: 0, height: 0,
          borderTop: '80px solid rgba(224,0,26,0.08)',
          borderLeft: '80px solid transparent',
        }} />
        <div style={{
          position: 'absolute',
          bottom: 0, left: 0,
          width: 0, height: 0,
          borderBottom: '60px solid rgba(224,0,26,0.05)',
          borderRight: '60px solid transparent',
        }} />

        <div>
          <SmartShipLogo />
          <div style={{
            marginTop: 6,
            fontFamily: 'Rajdhani, sans-serif',
            fontSize: 9,
            fontWeight: 600,
            letterSpacing: '0.18em',
            textTransform: 'uppercase',
            color: '#444',
          }}>
            LOGISTICS COMMAND CENTER
          </div>
        </div>

        <div>
          <div className="accent-line" />
          <h2 style={{
            fontFamily: 'Rajdhani, sans-serif',
            fontSize: 32,
            fontWeight: 700,
            letterSpacing: '0.04em',
            textTransform: 'uppercase',
            color: '#fff',
            lineHeight: 1.1,
            marginBottom: 16,
          }}>
            Enterprise<br/>Logistics<br/>Intelligence
          </h2>
          <p style={{
            fontSize: 13,
            color: 'var(--color-text-muted)',
            lineHeight: 1.6,
            maxWidth: 280,
          }}>
            Distributed microservices architecture powering end-to-end shipment lifecycle with real-time SAGA orchestration.
          </p>
        </div>

        {/* Service bullets */}
        <div style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
          {['Identity Service', 'Shipment Service', 'Payment Service', 'Tracking Service'].map(svc => (
            <div key={svc} style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
              <div className="health-dot online" />
              <span style={{ fontFamily: 'Rajdhani, sans-serif', fontSize: 11, fontWeight: 600, letterSpacing: '0.1em', textTransform: 'uppercase', color: '#555' }}>{svc}</span>
            </div>
          ))}
        </div>
      </div>

      {/* Right form panel */}
      <div style={{
        flex: 1,
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        padding: 40,
      }}>
        <div style={{ width: '100%', maxWidth: 380 }}>
          <Outlet />
        </div>
      </div>
    </div>
  );
};

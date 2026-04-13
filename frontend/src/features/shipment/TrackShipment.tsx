import { useParams, useNavigate } from 'react-router-dom';

const MOCK_EVENTS = [
  { hub: 'Delhi Main Hub', timestamp: '12 Apr 2026, 14:30 IST', status: 'IN TRANSIT', desc: 'Shipment dispatched from origin hub', done: true, active: true },
  { hub: 'Agra Transit Point', timestamp: '12 Apr 2026, 09:15 IST', status: 'IN TRANSIT', desc: 'Package scanned at transit facility', done: true, active: false },
  { hub: 'Hyderabad Hub', timestamp: '11 Apr 2026, 22:00 IST', status: 'IN TRANSIT', desc: 'Arrived at regional distribution center', done: true, active: false },
  { hub: 'Mumbai Sorting Facility', timestamp: '11 Apr 2026, 16:45 IST', status: 'BOOKED', desc: 'Package received and verified at source', done: true, active: false },
  { hub: 'Chennai Main Hub', timestamp: 'Estimated 13 Apr 2026', status: 'PENDING', desc: 'Awaiting delivery at destination hub', done: false, active: false },
];

export const TrackShipment = () => {
  const { id } = useParams();
  const navigate = useNavigate();

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 20, maxWidth: 900 }}>
      {/* Header */}
      <div>
        <div
          style={{ fontFamily: 'Rajdhani, sans-serif', fontSize: 11, color: 'var(--color-accent)', letterSpacing: '0.1em', cursor: 'pointer', marginBottom: 12 }}
          onClick={() => navigate(-1)}
        >
          ← BACK
        </div>
        <div className="accent-line" style={{ marginBottom: 8 }} />
        <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
          <div>
            <h1 className="section-title">Shipment Tracking</h1>
            <p className="section-sub">Tracking ID: <span style={{ color: 'var(--color-accent)', fontFamily: 'Rajdhani, sans-serif', fontWeight: 600 }}>{id || 'SHP-2601'}</span></p>
          </div>
          <span className="ss-badge warning" style={{ fontSize: 12, padding: '4px 12px' }}>IN TRANSIT</span>
        </div>
      </div>

      {/* Shipment summary card */}
      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr', gap: 12 }}>
        {[
          { label: 'Origin', value: 'Delhi Main Hub' },
          { label: 'Destination', value: 'Chennai Main Hub' },
          { label: 'ETA', value: '13 Apr 2026' },
          { label: 'Package Weight', value: '4.2 kg' },
          { label: 'Declared Value', value: '₹4,820' },
          { label: 'Shipment Type', value: 'Domestic Express' },
        ].map(item => (
          <div key={item.label} className="ss-card" style={{ padding: '14px 16px' }}>
            <div className="kpi-label" style={{ marginBottom: 4 }}>{item.label}</div>
            <div style={{ fontFamily: 'Rajdhani, sans-serif', fontSize: 16, fontWeight: 700, color: '#fff' }}>{item.value}</div>
          </div>
        ))}
      </div>

      {/* Timeline */}
      <div className="ss-card" style={{ padding: 28 }}>
        <h2 style={{
          fontFamily: 'Rajdhani, sans-serif',
          fontSize: 14,
          fontWeight: 700,
          letterSpacing: '0.1em',
          textTransform: 'uppercase',
          color: '#fff',
          marginBottom: 28,
        }}>
          Hub Transition Timeline
        </h2>

        <div style={{ position: 'relative', paddingLeft: 32 }}>
          {/* Vertical line */}
          <div style={{
            position: 'absolute',
            left: 5,
            top: 0,
            bottom: 0,
            width: 1,
            background: 'var(--color-border)',
          }} />

          {MOCK_EVENTS.map((evt, i) => (
            <div key={i} style={{
              position: 'relative',
              paddingBottom: i < MOCK_EVENTS.length - 1 ? 28 : 0,
              opacity: evt.done ? 1 : 0.4,
            }}>
              {/* Dot */}
              <div style={{
                position: 'absolute',
                left: -27,
                top: 2,
                width: 12,
                height: 12,
                borderRadius: '50%',
                background: evt.active ? 'var(--color-accent)' : evt.done ? 'var(--color-success)' : 'var(--color-surface-2)',
                border: `2px solid ${evt.active ? 'var(--color-accent)' : evt.done ? 'var(--color-success)' : '#333'}`,
                boxShadow: evt.active ? '0 0 10px rgba(224,0,26,0.6)' : evt.done ? '0 0 6px rgba(0,196,140,0.3)' : 'none',
                zIndex: 1,
              }} />

              <div style={{ display: 'flex', alignItems: 'flex-start', justifyContent: 'space-between', gap: 16 }}>
                <div>
                  <div style={{
                    fontFamily: 'Rajdhani, sans-serif',
                    fontSize: 15,
                    fontWeight: 700,
                    color: evt.active ? '#fff' : evt.done ? '#aaa' : '#555',
                    letterSpacing: '0.04em',
                    marginBottom: 2,
                  }}>
                    {evt.hub}
                  </div>
                  <div style={{ fontSize: 12, color: 'var(--color-text-muted)' }}>{evt.desc}</div>
                </div>
                <div style={{ textAlign: 'right', flexShrink: 0 }}>
                  <div style={{ fontSize: 11, color: 'var(--color-text-muted)', fontFamily: 'Rajdhani, sans-serif', letterSpacing: '0.08em' }}>
                    {evt.timestamp}
                  </div>
                  <span className={`ss-badge ${evt.active ? 'glow' : evt.done ? 'success' : 'muted'}`} style={{ marginTop: 4, display: 'inline-block' }}>
                    {evt.status}
                  </span>
                </div>
              </div>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
};

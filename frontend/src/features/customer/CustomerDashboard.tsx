import { useNavigate } from 'react-router-dom';

const MOCK_SHIPMENTS = [
  { id: 'SHP-2601', sender: 'Aryan Logistics', receiver: 'TechCorp Chennai', origin: 'Delhi', dest: 'Chennai', status: 'IN TRANSIT', date: '12 Apr 2026', amount: '₹4,820' },
  { id: 'SHP-2600', sender: 'MumbaiMart Pvt', receiver: 'Global Traders', origin: 'Mumbai', dest: 'Hyderabad', status: 'BOOKED', date: '12 Apr 2026', amount: '₹2,100' },
  { id: 'SHP-2599', sender: 'Pune Exports', receiver: 'Delhi Electronics', origin: 'Pune', dest: 'Delhi', status: 'DELIVERED', date: '11 Apr 2026', amount: '₹6,340' },
  { id: 'SHP-2598', sender: 'Chennai Fresh', receiver: 'Kolkata Bazaar', origin: 'Chennai', dest: 'Kolkata', status: 'IN TRANSIT', date: '11 Apr 2026', amount: '₹1,890' },
  { id: 'SHP-2597', sender: 'Hyderabad IT', receiver: 'Pune Startups', origin: 'Hyderabad', dest: 'Pune', status: 'CANCELLED', date: '10 Apr 2026', amount: '₹3,200' },
  { id: 'SHP-2596', sender: 'Kolkata Crafts', receiver: 'Mumbai Retail', origin: 'Kolkata', dest: 'Mumbai', status: 'DRAFT', date: '10 Apr 2026', amount: '₹980' },
];

const statusStyle: Record<string, string> = {
  'IN TRANSIT': 'warning',
  'BOOKED': '',
  'DELIVERED': 'success',
  'CANCELLED': 'muted',
  'DRAFT': 'muted',
};

const KPI_DATA = [
  { label: 'Total Shipments', value: '1,428', delta: '+12% this month', up: true, accent: false },
  { label: 'Active Deliveries', value: '342', delta: '23 due today', up: true, accent: true },
  { label: 'Pending Payments', value: '₹8.4L', delta: '17 invoices', up: false, accent: false },
  { label: 'Revenue Today', value: '₹2.1L', delta: '+8% vs yesterday', up: true, accent: false },
];

export const CustomerDashboard = () => {
  const navigate = useNavigate();

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 24, maxWidth: 1200 }}>

      {/* Page header */}
      <div>
        <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
          <div>
            <div className="accent-line" style={{ marginBottom: 8 }} />
            <h1 className="section-title">Operations Dashboard</h1>
            <p className="section-sub">Real-time logistics intelligence — SmartShip Command Center</p>
          </div>
          <button className="ss-btn" onClick={() => navigate('/customer/shipments/create')}>
            ▷ New Shipment
          </button>
        </div>
      </div>

      {/* KPI Strip */}
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: 12 }}>
        {KPI_DATA.map((kpi, i) => (
          <div key={i} className="kpi-card" style={kpi.accent ? {
            borderColor: 'rgba(224,0,26,0.5)',
            boxShadow: '0 0 14px rgba(224,0,26,0.1)',
          } : {}}>
            <div className="kpi-label">{kpi.label}</div>
            <div className="kpi-value" style={kpi.accent ? { color: '#e0001a' } : {}}>{kpi.value}</div>
            <div className={`kpi-delta ${kpi.up ? 'up' : 'down'}`}>{kpi.delta}</div>
          </div>
        ))}
      </div>

      {/* Charts + Mini-map row */}
      <div style={{ display: 'grid', gridTemplateColumns: '1fr 320px', gap: 16 }}>
        {/* Bar chart placeholder */}
        <div className="ss-card" style={{ padding: 24 }}>
          <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 20 }}>
            <h2 style={{
              fontFamily: 'Rajdhani, sans-serif',
              fontSize: 14,
              fontWeight: 700,
              letterSpacing: '0.1em',
              textTransform: 'uppercase',
              color: '#fff',
            }}>Shipment Volume — This Week</h2>
            <span className="ss-badge">LIVE</span>
          </div>
          {/* Bar chart */}
          <div style={{ display: 'flex', alignItems: 'flex-end', gap: 10, height: 120 }}>
            {[45, 72, 58, 89, 63, 95, 81].map((h, i) => {
              const days = ['MON', 'TUE', 'WED', 'THU', 'FRI', 'SAT', 'SUN'];
              const isToday = i === 6;
              return (
                <div key={i} style={{ flex: 1, display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 6 }}>
                  <div style={{
                    width: '100%',
                    height: `${h}%`,
                    background: isToday ? 'var(--color-accent)' : 'var(--color-surface-2)',
                    border: `1px solid ${isToday ? 'var(--color-accent)' : 'var(--color-border)'}`,
                    borderRadius: '2px 2px 0 0',
                    boxShadow: isToday ? '0 0 10px rgba(224,0,26,0.3)' : 'none',
                    transition: 'background 0.2s',
                  }} />
                  <span style={{ fontSize: 9, fontFamily: 'Rajdhani, sans-serif', fontWeight: 600, letterSpacing: '0.1em', color: isToday ? 'var(--color-accent)' : 'var(--color-text-dim)' }}>{days[i]}</span>
                </div>
              );
            })}
          </div>
        </div>

        {/* Live map panel */}
        <div className="ss-card" style={{ padding: 24 }}>
          <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 14 }}>
            <h2 style={{ fontFamily: 'Rajdhani, sans-serif', fontSize: 14, fontWeight: 700, letterSpacing: '0.1em', textTransform: 'uppercase', color: '#fff' }}>Hub Network</h2>
            <span className="ss-badge success glow-success">LIVE</span>
          </div>
          {/* SVG India map with hub dots */}
          <div style={{
            background: 'var(--color-surface-2)',
            border: '1px solid var(--color-border)',
            borderRadius: 2,
            height: 130,
            position: 'relative',
            overflow: 'hidden',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
          }}>
            <svg width="200" height="120" viewBox="0 0 200 120">
              {/* Simplified India outline mesh */}
              <path d="M80,10 L100,5 L130,15 L150,30 L155,60 L140,90 L120,105 L90,110 L70,100 L55,80 L50,55 L60,30 Z" fill="none" stroke="rgba(255,255,255,0.06)" strokeWidth="1"/>
              {/* Hub dots */}
              {[
                { x: 85, y: 25, name: 'Delhi' },
                { x: 70, y: 65, name: 'Mumbai' },
                { x: 110, y: 75, name: 'Chennai' },
                { x: 105, y: 60, name: 'Hyderabad' },
                { x: 80, y: 62, name: 'Pune' },
                { x: 130, y: 40, name: 'Kolkata' },
              ].map(hub => (
                <g key={hub.name}>
                  <circle cx={hub.x} cy={hub.y} r="4" fill="#e0001a" opacity="0.8"/>
                  <circle cx={hub.x} cy={hub.y} r="8" fill="rgba(224,0,26,0.15)" />
                  <text x={hub.x + 10} y={hub.y + 4} fontSize="7" fill="#888" fontFamily="Rajdhani, sans-serif" fontWeight="600">{hub.name}</text>
                </g>
              ))}
              {/* Route lines */}
              <line x1="85" y1="25" x2="70" y2="65" stroke="rgba(224,0,26,0.25)" strokeWidth="1" strokeDasharray="3,3"/>
              <line x1="70" y1="65" x2="110" y2="75" stroke="rgba(224,0,26,0.15)" strokeWidth="1" strokeDasharray="3,3"/>
              <line x1="85" y1="25" x2="130" y2="40" stroke="rgba(224,0,26,0.2)" strokeWidth="1" strokeDasharray="3,3"/>
            </svg>
          </div>
          <div style={{ marginTop: 12, display: 'flex', flexDirection: 'column', gap: 6 }}>
            {[
              { hub: 'Delhi Hub', count: 89 },
              { hub: 'Mumbai Hub', count: 67 },
              { hub: 'Chennai Hub', count: 54 },
            ].map(h => (
              <div key={h.hub} style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                <span style={{ fontSize: 11, color: 'var(--color-text-muted)' }}>{h.hub}</span>
                <span style={{ fontFamily: 'Rajdhani, sans-serif', fontSize: 12, fontWeight: 700, color: 'var(--color-text)' }}>{h.count} active</span>
              </div>
            ))}
          </div>
        </div>
      </div>

      {/* Recent orders table */}
      <div className="ss-card" style={{ padding: 0, overflow: 'hidden' }}>
        <div style={{ padding: '16px 20px', borderBottom: '1px solid rgba(255,255,255,0.06)', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
          <h2 style={{ fontFamily: 'Rajdhani, sans-serif', fontSize: 14, fontWeight: 700, letterSpacing: '0.1em', textTransform: 'uppercase', color: '#fff' }}>
            Recent Orders
          </h2>
          <button className="ss-btn ss-btn-outline" style={{ fontSize: 10, padding: '5px 12px' }} onClick={() => navigate('/customer/shipments')}>
            View All
          </button>
        </div>
        <table className="ss-table">
          <thead>
            <tr>
              <th>Shipment ID</th>
              <th>Sender</th>
              <th>Receiver</th>
              <th>Route</th>
              <th>Status</th>
              <th>Date</th>
              <th>Amount</th>
            </tr>
          </thead>
          <tbody>
            {MOCK_SHIPMENTS.map(s => (
              <tr key={s.id} style={{ cursor: 'pointer' }} onClick={() => navigate(`/customer/track/${s.id}`)}>
                <td style={{ fontFamily: 'Rajdhani, sans-serif', fontWeight: 600, fontSize: 13, color: 'var(--color-accent)' }}>{s.id}</td>
                <td style={{ color: 'var(--color-text)' }}>{s.sender}</td>
                <td style={{ color: 'var(--color-text-muted)' }}>{s.receiver}</td>
                <td style={{ fontSize: 12, color: 'var(--color-text-muted)' }}>{s.origin} → {s.dest}</td>
                <td><span className={`ss-badge ${statusStyle[s.status] || ''}`}>{s.status}</span></td>
                <td style={{ fontSize: 12, color: 'var(--color-text-muted)' }}>{s.date}</td>
                <td style={{ fontFamily: 'Rajdhani, sans-serif', fontWeight: 600 }}>{s.amount}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

    </div>
  );
};

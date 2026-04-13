import { useState } from 'react';
const HUBS = [
  { id: 1, name: 'Delhi Main Hub', city: 'Delhi', state: 'Delhi', active: true, shipments: 89, capacity: 120, phone: '9800000001' },
  { id: 2, name: 'Mumbai Central Hub', city: 'Mumbai', state: 'Maharashtra', active: true, shipments: 67, capacity: 100, phone: '9800000002' },
  { id: 3, name: 'Chennai Hub', city: 'Chennai', state: 'Tamil Nadu', active: true, shipments: 54, capacity: 80, phone: '9800000003' },
  { id: 4, name: 'Hyderabad Hub', city: 'Hyderabad', state: 'Telangana', active: true, shipments: 41, capacity: 70, phone: '9800000004' },
  { id: 5, name: 'Pune Hub', city: 'Pune', state: 'Maharashtra', active: false, shipments: 0, capacity: 60, phone: '9800000005' },
  { id: 6, name: 'Kolkata Hub', city: 'Kolkata', state: 'West Bengal', active: true, shipments: 33, capacity: 70, phone: '9800000006' },
];

const SYS_KPI = [
  { label: 'Total Users', value: '3,241', delta: '+18 today' },
  { label: 'Total Hubs', value: '6', delta: '5 online' },
  { label: 'SLA Compliance', value: '94.2%', delta: '+1.3% vs last week' },
  { label: 'Avg Delivery Time', value: '2.4 Days', delta: '-0.2 days improved' },
  { label: 'Exception Rate', value: '1.8%', delta: 'Within threshold' },
  { label: 'System Uptime', value: '99.97%', delta: 'Last 30 days' },
];

export const AdminPanel = () => {
  const [tab, setTab] = useState<'hubs' | 'kpi'>('hubs');

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 20, maxWidth: 1100 }}>
      {/* Header */}
      <div>
        <div className="accent-line" style={{ marginBottom: 8 }} />
        <h1 className="section-title">Admin Control Panel</h1>
        <p className="section-sub">Manage logistics infrastructure and system performance</p>
      </div>

      {/* Tabs */}
      <div style={{
        display: 'flex',
        borderBottom: '1px solid var(--color-border)',
        gap: 0,
      }}>
        {[
          { key: 'hubs', label: 'Logistics Hubs' },
          { key: 'kpi', label: 'System KPIs' },
        ].map(t => (
          <div
            key={t.key}
            onClick={() => setTab(t.key as 'hubs' | 'kpi')}
            style={{
              fontFamily: 'Rajdhani, sans-serif',
              fontSize: 12,
              fontWeight: 700,
              letterSpacing: '0.1em',
              textTransform: 'uppercase',
              padding: '10px 20px',
              cursor: 'pointer',
              color: tab === t.key ? '#fff' : 'var(--color-text-muted)',
              borderBottom: `2px solid ${tab === t.key ? 'var(--color-accent)' : 'transparent'}`,
              transition: 'all 0.15s',
              marginBottom: -1,
            }}
          >
            {t.label}
          </div>
        ))}
      </div>

      {/* Hubs tab */}
      {tab === 'hubs' && (
        <div>
          <div style={{ display: 'flex', justifyContent: 'flex-end', marginBottom: 16 }}>
            <button className="ss-btn">â–· Add Hub</button>
          </div>
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: 14 }}>
            {HUBS.map(hub => {
              const utilPct = Math.round((hub.shipments / hub.capacity) * 100);
              return (
                <div key={hub.id} className="ss-card" style={{ padding: 20 }}>
                  <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: 12 }}>
                    <div>
                      <div style={{
                        fontFamily: 'Rajdhani, sans-serif',
                        fontSize: 15,
                        fontWeight: 700,
                        letterSpacing: '0.04em',
                        color: '#fff',
                        marginBottom: 2,
                      }}>{hub.name}</div>
                      <div style={{ fontSize: 11, color: 'var(--color-text-muted)' }}>{hub.city}, {hub.state}</div>
                    </div>
                    <span className={`ss-badge ${hub.active ? 'success glow-success' : 'muted'}`}>
                      {hub.active ? 'ACTIVE' : 'OFFLINE'}
                    </span>
                  </div>

                  {/* Utilization bar */}
                  <div style={{ marginBottom: 14 }}>
                    <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 5 }}>
                      <span style={{ fontFamily: 'Rajdhani, sans-serif', fontSize: 10, fontWeight: 600, letterSpacing: '0.12em', textTransform: 'uppercase', color: 'var(--color-text-muted)' }}>Utilization</span>
                      <span style={{ fontFamily: 'Rajdhani, sans-serif', fontSize: 12, fontWeight: 700, color: utilPct > 80 ? 'var(--color-warning)' : '#fff' }}>{hub.shipments}/{hub.capacity}</span>
                    </div>
                    <div style={{ height: 4, background: 'var(--color-surface-2)', borderRadius: 2, overflow: 'hidden' }}>
                      <div style={{
                        height: '100%',
                        width: `${utilPct}%`,
                        background: utilPct > 80 ? 'var(--color-warning)' : 'var(--color-accent)',
                        borderRadius: 2,
                        transition: 'width 0.4s ease',
                      }} />
                    </div>
                  </div>

                  <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                    <span style={{ fontSize: 11, color: 'var(--color-text-muted)' }}>{hub.phone}</span>
                    <div style={{ display: 'flex', gap: 8 }}>
                      <button className="ss-btn ss-btn-outline" style={{ fontSize: 10, padding: '4px 10px' }}>Edit</button>
                      <button className="ss-btn" style={{ fontSize: 10, padding: '4px 10px', background: hub.active ? 'rgba(224,0,26,0.3)' : 'var(--color-accent)' }}>
                        {hub.active ? 'Deactivate' : 'Activate'}
                      </button>
                    </div>
                  </div>
                </div>
              );
            })}
          </div>
        </div>
      )}

      {/* KPIs tab */}
      {tab === 'kpi' && (
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: 14 }}>
          {SYS_KPI.map(kpi => (
            <div key={kpi.label} className="kpi-card" style={{ padding: '24px 22px' }}>
              <div className="kpi-label">{kpi.label}</div>
              <div className="kpi-value" style={{ fontSize: 32, marginTop: 8 }}>{kpi.value}</div>
              <div className="kpi-delta up" style={{ marginTop: 6 }}>{kpi.delta}</div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
};

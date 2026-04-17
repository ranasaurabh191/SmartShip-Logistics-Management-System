import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { apiClient } from '../../core/api/axios';

interface Shipment {
  id: number;
  trackingNumber: string;
  shipmentType: string;
  status: string;
  shippingRate: number;
  createdAt: string;
  customerId?: number;
  customerName?: string;
}

interface SystemKpi {
  label: string;
  value: string | number;
  delta: string;
  up?: boolean;
}

interface Hub {
  id: number;
  name: string;
  city: string;
  state: string;
  isActive: boolean;
}

export const AdminDashboard = () => {
  const navigate = useNavigate();

  const [shipments, setShipments]     = useState<Shipment[]>([]);
  const [kpis, setKpis]               = useState<SystemKpi[]>([]);
  const [hubs, setHubs]               = useState<Hub[]>([]);
  const [loading, setLoading]         = useState(true);
  const [kpiError, setKpiError]       = useState(false);

  useEffect(() => {
    const fetchAll = async () => {
      setLoading(true);

      try {
        const res = await apiClient.get('/admin/shipments', {
          params: { page: 1, pageSize: 10 },
        });
        const items = Array.isArray(res.data)
          ? res.data
          : res.data?.data ?? res.data?.items ?? res.data?.Items ?? [];
        setShipments(items);

        if (kpis.length === 0) {
          setKpis([
            { label: 'Total Shipments', value: items.length,
              delta: 'Last 10 records', up: true },
            { label: 'In Transit',
              value: items.filter((s: Shipment) =>
                ['InTransit','Booked','PickedUp','OutForDelivery'].includes(s.status)).length,
              delta: 'Active', up: true },
            { label: 'Delivered',
              value: items.filter((s: Shipment) => s.status === 'Delivered').length,
              delta: 'Completed', up: true },
            { label: 'Cancelled',
              value: items.filter((s: Shipment) => s.status === 'Cancelled').length,
              delta: 'Failed', up: false },
          ]);
        }
      } catch (err: any) {
        console.error('Shipments fetch failed:', err?.response?.status, err?.response?.data);
        setShipments([]);
      }

      // --- Hubs summary ---
      try {
        const res = await apiClient.get('/admin/hubs');
        const raw = Array.isArray(res.data) ? res.data
          : res.data?.data ?? res.data?.items ?? res.data?.Items ?? [];
        setHubs(raw);
      } catch {
        setHubs([]);
      }

      setLoading(false);
    };

    fetchAll();
  }, []);

  if (loading) {
    return (
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3,1fr)', gap: 14 }}>
        {[...Array(6)].map((_, i) => (
          <div key={i} className="kpi-card" style={{ height: 90, background: 'var(--color-surface-2)' }} />
        ))}
      </div>
    );
  }

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 24 }}>

      {/* Header */}
      <div>
        <div className="accent-line" style={{ marginBottom: 8 }} />
        <h1 className="section-title">Admin Dashboard</h1>
        <p className="section-sub">System-wide overview — shipments, hubs, and platform health</p>
      </div>

      {/* KPI Grid */}
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: 14 }}>
        {kpis.length > 0 ? kpis.map(kpi => (
          <div key={kpi.label} className="kpi-card" style={{ padding: '20px 18px' }}>
            <div className="kpi-label">{kpi.label}</div>
            <div className="kpi-value" style={{ fontSize: 28, marginTop: 6 }}>{kpi.value}</div>
            <div className={`kpi-delta ${kpi.up !== false ? 'up' : 'down'}`} style={{ marginTop: 4,fontSize:14 }}>
              {kpi.delta}
            </div>
          </div>
        )) : (
          <div className="ss-card" style={{
            gridColumn: '1 / -1', padding: 32, textAlign: 'center',
            color: 'var(--color-text-muted)', fontFamily: 'Rajdhani, sans-serif',
            fontSize: 12, textTransform: 'uppercase', letterSpacing: '0.1em',
          }}>
            {kpiError
              ? '— /admin/dashboard endpoint not found. Add it to your AdminService. —'
              : '— No KPI data —'}
          </div>
        )}
      </div>

      {/* Quick Actions */}
      <div className="ss-card" style={{
        padding: 20, display: 'flex',
        justifyContent: 'space-between', alignItems: 'center', gap: 16,
      }}>
        <div>
          <div className="section-title" style={{ fontSize: 14 }}>Quick Actions</div>
          <div className="section-sub">Manage hubs, users, and shipments</div>
        </div>
        <div style={{ display: 'flex', gap: 10, flexWrap: 'wrap' }}>
          <button className="ss-btn" onClick={() => navigate('/admin/hubs')}>
            Manage Hubs
          </button>
          <button className="ss-btn ss-btn-outline" onClick={() => navigate('/admin/users')}>
            Manage Users
          </button>
          <button className="ss-btn ss-btn-outline" onClick={() => navigate('/admin/shipments')}>
            All Shipments
          </button>
        </div>
      </div>

      {/* Hubs Summary */}
      <div className="ss-card" style={{ padding: 20 }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 14 }}>
          <div>
            <div className="section-title" style={{ fontSize: 14 }}>Logistics Hubs</div>
            <div className="section-sub">
              {hubs.length} registered — {hubs.filter(h => h.isActive).length} active
            </div>
          </div>
          <button className="ss-btn ss-btn-outline" style={{ fontSize: 11 }}
            onClick={() => navigate('/admin/hubs')}>
            Full Hub Manager
          </button>
        </div>

        {hubs.length === 0 ? (
          <div style={{ color: 'var(--color-text-muted)', fontSize: 13 }}>No hubs found.</div>
        ) : (
          <div style={{ display: 'flex', flexWrap: 'wrap', gap: 10 }}>
            {hubs.map(hub => (
              <div key={hub.id} style={{
                padding: '8px 14px',
                background: 'var(--color-surface-2)',
                border: '1px solid var(--color-border)',
                borderRadius: 2,
                display: 'flex', alignItems: 'center', gap: 10,
              }}>
                <div style={{
                  width: 7, height: 7, borderRadius: '50%',
                  background: hub.isActive ? 'var(--color-success)' : '#555',
                  flexShrink: 0,
                }} />
                <div>
                  <div style={{ fontSize: 13, fontWeight: 600, color: 'var(--color-text)' }}>
                    {hub.name}
                  </div>
                  <div style={{ fontSize: 12, color: 'var(--color-text-muted)' }}>
                    {hub.city}, {hub.state}
                  </div>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>

      {/* Recent Shipments */}
      <div className="ss-card" style={{ padding: 20 }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 15 }}>
          <div>
            <div className="section-title" style={{ fontSize: 14 }}>Recent Shipments</div>
            <div className="section-sub">Latest platform-wide shipment activity</div>
          </div>
          <button className="ss-btn" onClick={() => navigate('/admin/shipments')}>
            Open Registry
          </button>
        </div>

        {shipments.length === 0 ? (
          <div style={{ color: 'var(--color-text-muted)', fontSize: 13 }}>
            No shipments found. Make sure <code style={{ color: 'var(--color-accent)' }}>GET /shipments</code> returns all shipments for Admin role.
          </div>
        ) : (
          <table className="ss-table">
            <thead>
              <tr>
                <th>Tracking</th>
                <th>Customer</th>
                <th>Type</th>
                <th>Status</th>
                <th>Rate</th>
                <th>Created</th>
              </tr>
            </thead>
            <tbody>
              {shipments.map(s => (
                <tr
                  key={s.id}
                  style={{ cursor: 'pointer' }}
                  onClick={() => navigate(`/admin/track/${s.id}`)}
                >
                  <td>{s.trackingNumber}</td>
                  <td>{s.customerName}</td>
                  <td>{s.shipmentType}</td>
                  <td>
                    <span className={`ss-badge ${
                      s.status === 'Delivered' ? 'success' :
                      s.status === 'Cancelled' ? '' : 'glow'
                    }`}>
                      {s.status}
                    </span>
                  </td>
                  <td>₹{Number(s.shippingRate || 0).toLocaleString('en-IN')}</td>
                  <td>{new Date(s.createdAt).toLocaleDateString('en-IN')}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

    </div>
  );
};
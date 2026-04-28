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
  deliveredAt?: string | null;
}

interface DashboardStats {
  totalShipments: number;
  inTransit: number;
  delivered: number;
  cancelled: number;
  drafts: number;
  totalSpend: number;
}

export const CustomerDashboard = () => {
  const navigate = useNavigate();
  const [stats, setStats] = useState<DashboardStats | null>(null);
  const [shipments, setShipments] = useState<Shipment[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const fetchAll = async () => {
      try {
        const recentRes = await apiClient.get('/shipments/my', {
          params: { page: 1, pageSize: 10 },
        });

        const items = Array.isArray(recentRes.data)
          ? recentRes.data
          : recentRes.data?.data ?? recentRes.data?.items ?? [];

        setShipments(items);

        const statsData: DashboardStats = {
          totalShipments: items.length,
          inTransit: items.filter((s: Shipment) =>
            ['InTransit', 'In Transit', 'Booked', 'PickedUp', 'OutForDelivery'].includes(
              s.status
            )
          ).length,
          delivered: items.filter((s: Shipment) => s.status === 'Delivered').length,
          cancelled: items.filter((s: Shipment) => s.status === 'Cancelled').length,
          drafts: items.filter((s: Shipment) => s.status === 'Draft').length,
          totalSpend: items.reduce(
            (sum: number, s: Shipment) => sum + Number(s.shippingRate || 0),
            0
          ),
        };

        setStats(statsData);
      } catch (error) {
        console.error('Dashboard fetch failed:', error);
        setStats({
          totalShipments: 0,
          inTransit: 0,
          delivered: 0,
          cancelled: 0,
          drafts: 0,
          totalSpend: 0,
        });
        setShipments([]);
      } finally {
        setLoading(false);
      }
    };

    fetchAll();
  }, []);

  if (loading) {
    return <div>Loading dashboard...</div>;
  }

  return (
    <div style={{ display: 'flex',marginRight: 20, flexDirection: 'column', gap: 10 }}>
      <div>
        <div className="accent-line" style={{ marginBottom: 8 }} />
        <h1 className="section-title">Customer Dashboard</h1>
        <p className="section-sub">
          Create shipments, complete payment, then schedule pickup from your shipment list.
        </p>
      </div>

      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: 12 }}>
        <div className="kpi-card">
          <div className="kpi-label">Total Shipments</div>
          <div className="kpi-value">{stats?.totalShipments ?? 0}</div>
        </div>

        <div className="kpi-card">
          <div className="kpi-label">Draft</div>
          <div className="kpi-value">{stats?.drafts ?? 0}</div>
        </div>

        <div className="kpi-card">
          <div className="kpi-label">In Transit</div>
          <div className="kpi-value">{stats?.inTransit ?? 0}</div>
        </div>

        <div className="kpi-card">
          <div className="kpi-label">Delivered</div>
          <div className="kpi-value">{stats?.delivered ?? 0}</div>
        </div>

        <div className="kpi-card">
          <div className="kpi-label">Cancelled</div>
          <div className="kpi-value">{stats?.cancelled ?? 0}</div>
        </div>

        <div className="kpi-card">
          <div className="kpi-label">Total Spend</div>
          <div className="kpi-value">
            ₹{(stats?.totalSpend ?? 0).toLocaleString('en-IN')}
          </div>
        </div>
      </div>

      <div
        className="ss-card"
        style={{ padding: 20, display: 'flex', justifyContent: 'space-between', gap: 16 }}
      >
        <div>
          <div className="section-title" style={{ fontSize: 12 }}>
            Quick Actions
          </div>
          <div className="section-sub">
            Start with shipment creation. Payment and pickup happen after draft creation.
          </div>
        </div>

        <div style={{ display: 'flex', gap: 10, flexWrap: 'wrap' }}>
          <button className="ss-btn" style={{  marginTop: '10px' }} onClick={() => navigate('/customer/shipments/create')}>
            + New Shipment
          </button>
          <button
            className="ss-btn ss-btn-outline"
            onClick={() => navigate('/customer/shipments')}
            style={{ marginTop: '10px' }}
          >
            View Shipments
          </button>
        </div>
      </div>

      <div className="ss-card" style={{ padding: 20 }}>
        <div
          style={{
            display: 'flex',
            justifyContent: 'space-between',
            alignItems: 'center',
            marginBottom: 16,
          }}
        >
          <div>
            <div className="section-title" style={{ fontSize: 14 }}>
              Recent Shipments
            </div>
            <div className="section-sub">Latest shipment records</div>
          </div>

          <button className="ss-btn" onClick={() => navigate('/customer/shipments')}>
            Open Registry
          </button>
        </div>

        {shipments.length === 0 ? (
          <div style={{ color: 'var(--color-text-muted)', fontSize: 13 }}>
            No shipments found.
          </div>
        ) : (
          <table className="ss-table">
            <thead>
              <tr>
                <th>Tracking</th>
                <th>Type</th>
                <th>Status</th>
                <th>Rate</th>
                <th>Created</th>
              </tr>
            </thead>
            <tbody>
              {shipments.map((shipment) => (
                <tr
                  key={shipment.id}
                  style={{ cursor: 'pointer' }}
                  onClick={() => navigate(`/customer/track/${shipment.id}`)}
                >
                  <td>{shipment.trackingNumber}</td>
                  <td>{shipment.shipmentType}</td>
                  <td >{shipment.status}</td>
                  <td>₹{Number(shipment.shippingRate || 0).toLocaleString('en-IN')}</td>
                  <td>{new Date(shipment.createdAt).toLocaleDateString('en-IN')}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
    </div>
  );
};
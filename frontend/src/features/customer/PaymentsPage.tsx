import { useState } from 'react';
const PAYMENTS = [
  { id: 'PAY-8801', shipment: 'SHP-2601', amount: 'â‚¹4,820', gateway: 'Razorpay', status: 'COMPLETED', date: '12 Apr 2026' },
  { id: 'PAY-8800', shipment: 'SHP-2600', amount: 'â‚¹2,100', gateway: 'Razorpay', status: 'PENDING', date: '12 Apr 2026' },
  { id: 'PAY-8799', shipment: 'SHP-2599', amount: 'â‚¹6,340', gateway: 'Razorpay', status: 'COMPLETED', date: '11 Apr 2026' },
  { id: 'PAY-8798', shipment: 'SHP-2598', amount: 'â‚¹1,890', gateway: 'Razorpay', status: 'COMPLETED', date: '11 Apr 2026' },
  { id: 'PAY-8797', shipment: 'SHP-2597', amount: 'â‚¹3,200', gateway: 'Razorpay', status: 'FAILED', date: '10 Apr 2026' },
  { id: 'PAY-8796', shipment: 'SHP-2595', amount: 'â‚¹5,100', gateway: 'Razorpay', status: 'PENDING', date: '09 Apr 2026' },
  { id: 'PAY-8795', shipment: 'SHP-2594', amount: 'â‚¹2,800', gateway: 'Razorpay', status: 'COMPLETED', date: '09 Apr 2026' },
];

const statusStyle: Record<string, string> = {
  'COMPLETED': 'success',
  'PENDING': 'warning',
  'FAILED': '',
};

export const PaymentsPage = () => {
  const [filter, setFilter] = useState('ALL');

  const totalRevenue = PAYMENTS.filter(p => p.status === 'COMPLETED').reduce((acc, p) => acc + parseInt(p.amount.replace(/[â‚¹,]/g, '')), 0);
  const pending = PAYMENTS.filter(p => p.status === 'PENDING').length;
  const failed = PAYMENTS.filter(p => p.status === 'FAILED').length;

  const filtered = filter === 'ALL' ? PAYMENTS : PAYMENTS.filter(p => p.status === filter);

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 20, maxWidth: 1100 }}>
      <div>
        <div className="accent-line" style={{ marginBottom: 8 }} />
        <h1 className="section-title">Payment Ledger</h1>
        <p className="section-sub">Full transaction history â€” Powered by Razorpay Gateway</p>
      </div>

      {/* KPIs */}
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: 12 }}>
        <div className="kpi-card">
          <div className="kpi-label">Total Revenue</div>
          <div className="kpi-value">â‚¹{totalRevenue.toLocaleString('en-IN')}</div>
          <div className="kpi-delta up">Settled transactions</div>
        </div>
        <div className="kpi-card" style={{ borderColor: 'rgba(245,166,35,0.3)' }}>
          <div className="kpi-label">Pending</div>
          <div className="kpi-value" style={{ color: 'var(--color-warning)' }}>{pending}</div>
          <div className="kpi-delta" style={{ color: 'var(--color-warning)' }}>Awaiting confirmation</div>
        </div>
        <div className="kpi-card" style={{ borderColor: 'rgba(224,0,26,0.3)' }}>
          <div className="kpi-label">Failed</div>
          <div className="kpi-value" style={{ color: 'var(--color-accent)' }}>{failed}</div>
          <div className="kpi-delta down">Requires manual review</div>
        </div>
      </div>

      {/* Filter */}
      <div className="ss-card" style={{ padding: '12px 16px', display: 'flex', gap: 10 }}>
        {['ALL', 'COMPLETED', 'PENDING', 'FAILED'].map(s => (
          <button
            key={s}
            onClick={() => setFilter(s)}
            style={{
              fontFamily: 'Rajdhani, sans-serif',
              fontSize: 10,
              fontWeight: 600,
              letterSpacing: '0.1em',
              textTransform: 'uppercase',
              padding: '5px 14px',
              border: '1px solid',
              borderRadius: 2,
              cursor: 'pointer',
              background: 'transparent',
              borderColor: filter === s ? 'var(--color-accent)' : 'var(--color-border)',
              color: filter === s ? 'var(--color-accent)' : 'var(--color-text-muted)',
            }}
          >
            {s}
          </button>
        ))}
      </div>

      {/* Table */}
      <div className="ss-card" style={{ padding: 0, overflow: 'hidden' }}>
        <table className="ss-table">
          <thead>
            <tr>
              <th>Payment ID</th>
              <th>Shipment Ref</th>
              <th>Amount</th>
              <th>Gateway</th>
              <th>Status</th>
              <th>Date</th>
            </tr>
          </thead>
          <tbody>
            {filtered.map(p => (
              <tr key={p.id}>
                <td style={{ fontFamily: 'Rajdhani, sans-serif', fontWeight: 700, color: 'var(--color-accent)', fontSize: 13 }}>{p.id}</td>
                <td style={{ color: 'var(--color-text-muted)', fontSize: 12 }}>{p.shipment}</td>
                <td style={{ fontFamily: 'Rajdhani, sans-serif', fontWeight: 600, fontSize: 14, color: '#fff' }}>{p.amount}</td>
                <td>
                  <div style={{
                    display: 'inline-flex',
                    alignItems: 'center',
                    gap: 6,
                    padding: '3px 8px',
                    background: 'rgba(0, 119, 181, 0.1)',
                    border: '1px solid rgba(0,119,181,0.3)',
                    borderRadius: 2,
                  }}>
                    <div style={{ width: 8, height: 8, borderRadius: 1, background: '#0077b5' }} />
                    <span style={{ fontFamily: 'Rajdhani, sans-serif', fontSize: 10, fontWeight: 700, letterSpacing: '0.1em', color: '#4db8ff' }}>
                      {p.gateway.toUpperCase()}
                    </span>
                  </div>
                </td>
                <td><span className={`ss-badge ${statusStyle[p.status]}`}>{p.status}</span></td>
                <td style={{ fontSize: 12, color: 'var(--color-text-muted)' }}>{p.date}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
};

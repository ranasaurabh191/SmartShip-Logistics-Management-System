import { useState } from 'react';import { useNavigate } from 'react-router-dom';

const ALL_SHIPMENTS = [
  { id: 'SHP-2601', sender: 'Aryan Logistics', receiver: 'TechCorp Chennai', origin: 'Delhi', dest: 'Chennai', status: 'IN TRANSIT', date: '12 Apr 2026', amount: 'â‚¹4,820' },
  { id: 'SHP-2600', sender: 'MumbaiMart Pvt', receiver: 'Global Traders', origin: 'Mumbai', dest: 'Hyderabad', status: 'BOOKED', date: '12 Apr 2026', amount: 'â‚¹2,100' },
  { id: 'SHP-2599', sender: 'Pune Exports', receiver: 'Delhi Electronics', origin: 'Pune', dest: 'Delhi', status: 'DELIVERED', date: '11 Apr 2026', amount: 'â‚¹6,340' },
  { id: 'SHP-2598', sender: 'Chennai Fresh', receiver: 'Kolkata Bazaar', origin: 'Chennai', dest: 'Kolkata', status: 'IN TRANSIT', date: '11 Apr 2026', amount: 'â‚¹1,890' },
  { id: 'SHP-2597', sender: 'Hyderabad IT', receiver: 'Pune Startups', origin: 'Hyderabad', dest: 'Pune', status: 'CANCELLED', date: '10 Apr 2026', amount: 'â‚¹3,200' },
  { id: 'SHP-2596', sender: 'Kolkata Crafts', receiver: 'Mumbai Retail', origin: 'Kolkata', dest: 'Mumbai', status: 'DRAFT', date: '10 Apr 2026', amount: 'â‚¹980' },
  { id: 'SHP-2595', sender: 'Delhi Foods', receiver: 'Bangalore Stores', origin: 'Delhi', dest: 'Bangalore', status: 'BOOKED', date: '09 Apr 2026', amount: 'â‚¹5,100' },
  { id: 'SHP-2594', sender: 'Chennai Textiles', receiver: 'Pune Boutique', origin: 'Chennai', dest: 'Pune', status: 'DELIVERED', date: '09 Apr 2026', amount: 'â‚¹2,800' },
];

const statusStyle: Record<string, string> = {
  'IN TRANSIT': 'warning',
  'BOOKED': '',
  'DELIVERED': 'success',
  'CANCELLED': 'muted',
  'DRAFT': 'muted',
};

export const ShipmentsPage = () => {
  const navigate = useNavigate();
  const [filter, setFilter] = useState('ALL');
  const [search, setSearch] = useState('');

  const statuses = ['ALL', 'BOOKED', 'IN TRANSIT', 'DELIVERED', 'CANCELLED', 'DRAFT'];

  const filtered = ALL_SHIPMENTS.filter(s => {
    const matchStatus = filter === 'ALL' || s.status === filter;
    const matchSearch = s.id.toLowerCase().includes(search.toLowerCase()) ||
      s.sender.toLowerCase().includes(search.toLowerCase()) ||
      s.receiver.toLowerCase().includes(search.toLowerCase());
    return matchStatus && matchSearch;
  });

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 20, maxWidth: 1200 }}>
      {/* Header */}
      <div style={{ display: 'flex', alignItems: 'flex-end', justifyContent: 'space-between' }}>
        <div>
          <div className="accent-line" style={{ marginBottom: 8 }} />
          <h1 className="section-title">Shipments Registry</h1>
          <p className="section-sub">{ALL_SHIPMENTS.length} total records â€” filtered: {filtered.length}</p>
        </div>
        <button className="ss-btn" onClick={() => navigate('/customer/shipments/create')}>
          â–· New Shipment
        </button>
      </div>

      {/* Filter bar */}
      <div className="ss-card" style={{
        padding: '14px 16px',
        display: 'flex',
        alignItems: 'center',
        gap: 12,
        flexWrap: 'wrap',
      }}>
        <input
          className="ss-input"
          placeholder="Search by ID, sender, receiver..."
          value={search}
          onChange={e => setSearch(e.target.value)}
          style={{ width: 260 }}
        />
        <div style={{ width: '1px', height: 28, background: 'var(--color-border)', margin: '0 4px' }} />
        {statuses.map(s => (
          <button
            key={s}
            onClick={() => setFilter(s)}
            style={{
              fontFamily: 'Rajdhani, sans-serif',
              fontSize: 10,
              fontWeight: 600,
              letterSpacing: '0.1em',
              textTransform: 'uppercase',
              padding: '5px 12px',
              border: '1px solid',
              borderRadius: 2,
              cursor: 'pointer',
              background: 'transparent',
              borderColor: filter === s ? 'var(--color-accent)' : 'var(--color-border)',
              color: filter === s ? 'var(--color-accent)' : 'var(--color-text-muted)',
              transition: 'all 0.15s',
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
              <th>Shipment ID</th>
              <th>Sender</th>
              <th>Receiver</th>
              <th>Origin Hub</th>
              <th>Destination Hub</th>
              <th>Status</th>
              <th>Created</th>
              <th>Amount</th>
              <th>Action</th>
            </tr>
          </thead>
          <tbody>
            {filtered.length === 0 ? (
              <tr>
                <td colSpan={9} style={{ textAlign: 'center', padding: 40, color: 'var(--color-text-muted)', fontFamily: 'Rajdhani, sans-serif', letterSpacing: '0.1em', textTransform: 'uppercase', fontSize: 12 }}>
                  â€” No records match filter â€”
                </td>
              </tr>
            ) : filtered.map(s => (
              <tr key={s.id} style={{ cursor: 'pointer' }} onClick={() => navigate(`/customer/track/${s.id}`)}>
                <td style={{ fontFamily: 'Rajdhani, sans-serif', fontWeight: 700, fontSize: 13, color: 'var(--color-accent)' }}>{s.id}</td>
                <td>{s.sender}</td>
                <td style={{ color: 'var(--color-text-muted)' }}>{s.receiver}</td>
                <td style={{ color: 'var(--color-text-muted)', fontSize: 12 }}>{s.origin}</td>
                <td style={{ color: 'var(--color-text-muted)', fontSize: 12 }}>{s.dest}</td>
                <td><span className={`ss-badge ${statusStyle[s.status] || ''}`}>{s.status}</span></td>
                <td style={{ fontSize: 12, color: 'var(--color-text-muted)' }}>{s.date}</td>
                <td style={{ fontFamily: 'Rajdhani, sans-serif', fontWeight: 600 }}>{s.amount}</td>
                <td>
                  <button
                    className="ss-btn ss-btn-outline"
                    style={{ fontSize: 10, padding: '4px 10px' }}
                    onClick={e => { e.stopPropagation(); navigate(`/customer/track/${s.id}`); }}
                  >
                    Track
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
};

import { useEffect, useState } from 'react';
import { apiClient } from '../../core/api/axios';

interface Hub {
  id: number;
  name: string;
  city: string;
  state: string;
  country: string;
  contactPhone: string;
  isActive: boolean;
}

const MOCK_HUBS: Hub[] = [
  { id: 1, name: "Delhi Main Hub", city: "Delhi", state: "Delhi", country: "India", contactPhone: "9800000001", isActive: true },
  { id: 2, name: "Mumbai Hub", city: "Mumbai", state: "Maharashtra", country: "India", contactPhone: "9800000002", isActive: false },
  { id: 3, name: "Gurgaon Hub", city: "Gurgaon", state: "Haryana", country: "India", contactPhone: "9800000003", isActive: true },
  { id: 4, name: "Chennai Hub", city: "Chennai", state: "Tamil Nadu", country: "India", contactPhone: "9800000004", isActive: true },
];

export const HubManagement = () => {
  const [hubs, setHubs] = useState<Hub[]>([]);


  useEffect(() => {
    const fetchHubs = async () => {
      try {
        const res = await apiClient.get('/admin/hubs');
        if (Array.isArray(res.data)) setHubs(res.data);
        else if (res.data?.items) setHubs(res.data.items);
        else throw new Error("not array");
      } catch {
        setHubs(MOCK_HUBS);
      }
    };
    fetchHubs();
  }, []);

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 20, maxWidth: 1100 }}>
      <div style={{ display: 'flex', alignItems: 'flex-end', justifyContent: 'space-between' }}>
        <div>
          <div className="accent-line" style={{ marginBottom: 8 }} />
          <h1 className="section-title">Hub Management</h1>
          <p className="section-sub">Manage logistics centers across the country</p>
        </div>
        <button className="ss-btn">▷ Add Hub</button>
      </div>
      <div className="ss-card" style={{ padding: 0, overflow: 'hidden' }}>
        <table className="ss-table">
          <thead>
            <tr>
              <th>Hub Name</th>
              <th>Location</th>
              <th>Contact</th>
              <th>Status</th>
              <th align="right">Actions</th>
            </tr>
          </thead>
          <tbody>
            {hubs.map(hub => (
              <tr key={hub.id}>
                <td style={{ fontFamily: 'Rajdhani, sans-serif', fontWeight: 700, color: '#fff' }}>{hub.name}</td>
                <td style={{ color: 'var(--color-text-muted)', fontSize: 12 }}>{hub.city}, {hub.state}</td>
                <td style={{ fontSize: 12, color: 'var(--color-text-muted)' }}>{hub.contactPhone}</td>
                <td><span className={`ss-badge ${hub.isActive ? 'success glow-success' : 'muted'}`}>{hub.isActive ? 'ACTIVE' : 'OFFLINE'}</span></td>
                <td style={{ textAlign: 'right' }}>
                  <button className="ss-btn ss-btn-outline" style={{ fontSize: 10, padding: '3px 10px', marginRight: 8 }}>Edit</button>
                  <button className="ss-btn" style={{ fontSize: 10, padding: '3px 10px', background: 'rgba(224,0,26,0.3)' }}>Delete</button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
};

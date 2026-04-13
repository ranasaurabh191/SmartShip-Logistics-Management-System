import { useEffect, useState } from 'react';
import { apiClient } from '../../core/api/axios';

interface User {
  id: number;
  name: string;
  email: string;
  phone: string;
  isActive: boolean;
  role: string;
}

const MOCK_USERS: User[] = [
  { id: 1, name: "Administrator", email: "admin@smartship.com", phone: "9999900099", isActive: true, role: "Admin" },
  { id: 2, name: "Rahul Singh", email: "rahul@gmail.com", phone: "2345679442", isActive: true, role: "Customer" },
  { id: 3, name: "John Doe", email: "john@example.com", phone: "1234567890", isActive: false, role: "Customer" },
  { id: 4, name: "Priya Verma", email: "priya@smartship.com", phone: "9876543210", isActive: true, role: "Customer" },
];

export const UserManagement = () => {
  const [users, setUsers] = useState<User[]>([]);

  useEffect(() => {
    const fetchUsers = async () => {
      try {
        const res = await apiClient.get('/admin/users');
        if (Array.isArray(res.data)) setUsers(res.data);
        else if (res.data?.items) setUsers(res.data.items);
        else throw new Error("not array");
      } catch {
        setUsers(MOCK_USERS);
      }
    };
    fetchUsers();
  }, []);

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 20, maxWidth: 1100 }}>
      <div>
        <div className="accent-line" style={{ marginBottom: 8 }} />
        <h1 className="section-title">User Directory</h1>
        <p className="section-sub">Manage system access and operator roles</p>
      </div>

      <div className="ss-card" style={{ padding: 0, overflow: 'hidden' }}>
        <table className="ss-table">
          <thead>
            <tr>
              <th>Name</th>
              <th>Email</th>
              <th>Phone</th>
              <th>Role</th>
              <th>Status</th>
              <th align="right">Options</th>
            </tr>
          </thead>
          <tbody>
            {users.map(user => (
              <tr key={user.id}>
                <td style={{ fontFamily: 'Rajdhani, sans-serif', fontWeight: 700, color: '#fff', fontSize: 13 }}>{user.name}</td>
                <td style={{ fontSize: 12, color: 'var(--color-text-muted)' }}>{user.email}</td>
                <td style={{ fontSize: 12, color: 'var(--color-text-muted)' }}>{user.phone}</td>
                <td><span className={`ss-badge ${user.role === 'Admin' ? 'glow' : 'muted'}`}>{user.role.toUpperCase()}</span></td>
                <td><span className={`ss-badge ${user.isActive ? 'success' : 'muted'}`}>{user.isActive ? 'ACTIVE' : 'INACTIVE'}</span></td>
                <td style={{ textAlign: 'right' }}>
                  <button className="ss-btn ss-btn-outline" style={{ fontSize: 10, padding: '3px 10px', marginRight: 8 }}>Edit</button>
                  <button className="ss-btn" style={{ fontSize: 10, padding: '3px 10px', background: 'rgba(224,0,26,0.3)' }}>Remove</button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
};

import { useEffect, useState } from 'react';
import { apiClient } from '../../core/api/axios';
import { useNotificationStore } from '../../store/useNotificationStore';

interface User {
  id: number;
  name: string;
  email: string;
  phone: string;
  isActive: boolean;
  role: string;
}

// Normalise whatever shape the API returns into our interface
const normaliseUser = (u: any): User => ({
  id: u.id,
  name: u.name ?? u.fullName ?? '—',
  email: u.email ?? '—',
  phone: u.phone ?? u.phoneNumber ?? u.phoneNo ?? '—',
  isActive: u.isActive ?? u.active ?? false,
  role: Array.isArray(u.roles) ? u.roles[0] : (u.role ?? u.userRole ?? '—'),
});

export const UserManagement = () => {
  const [users, setUsers] = useState<User[]>([]);
  const addNotification = useNotificationStore((state) => state.addNotification);
  const [loading, setLoading] = useState(true);
  const [total, setTotal] = useState(0);
  const [userToDelete, setUserToDelete] = useState<number | null>(null);

  const confirmDelete = async () => {
    if (!userToDelete) return;
    try {
      await apiClient.delete(`/admin/users/${userToDelete}`);
      setUsers(prev => prev.filter(u => u.id !== userToDelete));
      addNotification('User deleted successfully.', 'success');
    } catch {
      addNotification('Failed to remove user.', 'error');
    } finally {
      setUserToDelete(null);
    }
  };
  const fetchUsers = async () => {
    setLoading(true);
    try {
      const res = await apiClient.get('/admin/users');
      const raw = Array.isArray(res.data)
        ? res.data
        : res.data?.Data ?? res.data?.data ?? res.data?.items ?? [];
      setTotal(res.data?.TotalCount ?? res.data?.totalCount ?? raw.length);
      setUsers(raw.map(normaliseUser));
    } catch (err: any) {
      console.error('Failed to fetch users:', err?.response?.data ?? err);
      setUsers([]);
    } finally {
      setLoading(false);
    }
  };

  const toggleActive = async (id: number, current: boolean) => {
    try {
      const res = await apiClient.get(`/admin/users/${id}`);
      const u = res.data;
      await apiClient.put(`/admin/users/${id}`, {
        name: u.Name ?? u.name,
        phone: u.Phone ?? u.phone ?? u.phoneNumber,
        isActive: !current,
        role: u.Role ?? u.role,
      });
      fetchUsers();
    } catch (err: any) {
      const msg =
        err?.response?.data?.message ||
        err?.response?.data?.title ||
        'Failed to update user.';
      addNotification(msg, 'error');
    }
  };

  useEffect(() => { fetchUsers(); }, []);

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 20, maxWidth: 1100 }}>
      <div>
        <div className="accent-line" style={{ marginBottom: 8 }} />
        <h1 className="section-title">User Directory</h1>
        <p className="section-sub">
          {loading ? 'Loading...' : `${total} users · ${users.filter(u => u.isActive).length} active`}
        </p>
      </div>

      <div className="ss-card" style={{ padding: 0, overflow: 'hidden' }}>
        <table className="ss-table">
          <thead>
            <tr>
              <th>#</th><th>Name</th><th>Email</th>
              <th>Phone</th><th>Role</th><th>Status</th><th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {loading ? (
              <tr><td colSpan={7} style={{ textAlign: 'center', padding: 40, color: 'var(--color-text-muted)', fontFamily: 'Rajdhani', textTransform: 'uppercase', fontSize: 12 }}>
                LOADING...
              </td></tr>
            ) : users.length === 0 ? (
              <tr><td colSpan={7} style={{ textAlign: 'center', padding: 40, color: 'var(--color-text-muted)', fontFamily: 'Rajdhani', textTransform: 'uppercase', fontSize: 12 }}>
                — No users found —
              </td></tr>
            ) : users.map(u => (
              <tr key={u.id}>
                <td style={{ color: 'var(--color-text-muted)', fontSize: 12 }}>{u.id}</td>
                <td style={{ fontFamily: 'Rajdhani, sans-serif', fontWeight: 700, color: '#fff' }}>{u.name}</td>
                <td style={{ color: 'var(--color-text-muted)', fontSize: 12 }}>{u.email}</td>
                <td style={{ color: 'var(--color-text-muted)', fontSize: 12 }}>{u.phone}</td>
                <td><span className="ss-badge">{u.role}</span></td>
                <td><span className={`ss-badge ${u.isActive ? 'success glow-success' : 'muted'}`}>
                  {u.isActive ? 'ACTIVE' : 'INACTIVE'}
                </span></td>
                <td style={{ display: 'flex', gap: 6 }}>
                  <button className="ss-btn ss-btn-outline" style={{ fontSize: 10, padding: '3px 10px' }}
                    onClick={() => toggleActive(u.id, u.isActive)}>
                    {u.isActive ? 'Deactivate' : 'Activate'}
                  </button>
                  <button className="ss-btn" style={{ fontSize: 10, padding: '3px 10px', background: 'transparent', border: '1px solid #555', color: '#888' }}
                    onClick={() => setUserToDelete(u.id)}>
                    Delete
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      
      {/* Delete Confirmation Modal */}
      {userToDelete && (
        <div onClick={() => setUserToDelete(null)} style={{ position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.8)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 9999, backdropFilter: 'blur(2px)' }}>
          <div onClick={e => e.stopPropagation()} className="ss-card" style={{ width: 400, padding: 24, boxShadow: '0 0 40px rgba(0,0,0,0.45)' }}>
            <h2 style={{ fontFamily: 'Orbitron, monospace', marginBottom: 16, color: '#fff', fontSize: 20, fontWeight: 700 }}>Confirm Deletion</h2>
            <p style={{ color: 'var(--color-text-muted)', marginBottom: 18, fontSize: 14 }}>
              Are you sure you want to delete this user?
            </p>
            <div style={{ display: 'flex', justifyContent: 'flex-end', gap: 10 }}>
              <button className="ss-btn ss-btn-outline" onClick={() => setUserToDelete(null)}>Cancel</button>
              <button className="ss-btn" style={{ background: '#e0001a', color: '#fff', border: '1px solid #e0001a' }} onClick={confirmDelete}>Yes, Delete</button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
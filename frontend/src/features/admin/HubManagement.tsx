import { useEffect, useState } from 'react';
import { apiClient } from '../../core/api/axios';
import { useNotificationStore } from '../../store/useNotificationStore';

interface Hub {
  id: number;
  name: string;
  city: string;
  state: string;
  country: string;
  contactPhone: string;
  isActive: boolean;
}

type FormState = { name: string; city: string; state: string; country: string; contactPhone: string };
const EMPTY_FORM: FormState = { name: '', city: '', state: '', country: 'India', contactPhone: '' };

const FIELDS = [
  { key: 'name' as const, label: 'Hub Name', placeholder: 'Enter Hub Name' },
  { key: 'city' as const, label: 'City', placeholder: 'Enter City' },
  { key: 'state' as const, label: 'State', placeholder: 'Enter State' },
  { key: 'country' as const, label: 'Country', placeholder: 'Enter Country' },
  { key: 'contactPhone' as const, label: 'Contact Phone', placeholder: 'Enter Contact Phone' },
];

export const HubManagement = () => {
  const [hubs, setHubs] = useState<Hub[]>([]);
  const [loading, setLoading] = useState(true);
  const [showForm, setShowForm] = useState(false);
  const [editingHub, setEditingHub] = useState<Hub | null>(null);
  const [form, setForm] = useState<FormState>(EMPTY_FORM);
  const [saving, setSaving] = useState(false);
  const addNotification = useNotificationStore(state => state.addNotification);
  const [hubToDelete, setHubToDelete] = useState<Hub | null>(null);

  // ── Fetch ──────────────────────────────────────────────────────────────────
  const fetchHubs = async () => {
    setLoading(true);
    try {
      const res = await apiClient.get('/admin/hubs');
      const raw =
        Array.isArray(res.data) ? res.data :
          Array.isArray(res.data?.Data) ? res.data.Data :
            Array.isArray(res.data?.data) ? res.data.data :
              Array.isArray(res.data?.items) ? res.data.items :
                Array.isArray(res.data?.Items) ? res.data.Items :
                  [];
      setHubs(raw);
    } catch (err: any) {
      console.error('Hubs fetch error:', err?.response?.data ?? err);
      setHubs([]);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { fetchHubs(); }, []);

  // ── Form helpers ───────────────────────────────────────────────────────────
  const openCreate = () => {
    setEditingHub(null);
    setForm(EMPTY_FORM);
    setShowForm(true);
  };

  const openEdit = (hub: Hub) => {
    setEditingHub(hub);
    setForm({ name: hub.name, city: hub.city, state: hub.state, country: hub.country, contactPhone: hub.contactPhone });
    setShowForm(true);
  };

  const closeForm = () => {
    setShowForm(false);
    setEditingHub(null);
    setForm(EMPTY_FORM);
  };

  const parseError = (err: any) =>
    err?.response?.data?.message ||
    err?.response?.data?.title ||
    (err?.response?.status === 403 ? 'Forbidden — Admin role required.' :
      err?.response?.status === 400 ? 'Validation error — check all fields.' :
        'Operation failed.');

  // ── CRUD ───────────────────────────────────────────────────────────────────
  const submitForm = async (e: React.FormEvent) => {
    e.preventDefault();
    setSaving(true);
    try {
      if (editingHub) {
        await apiClient.put(`/admin/hubs/${editingHub.id}`, {
          ...form,
          isActive: editingHub.isActive,   // PUT requires isActive
        });
        addNotification(`"${form.name}" Updated successfully`, 'success');
      } else {
        await apiClient.post('/admin/hubs', form);
        addNotification(`"${form.name}" Created successfully`, 'success');
      }
      closeForm();
      await fetchHubs();
    } catch (err: any) {
      addNotification(parseError(err), 'error');
    } finally {
      setSaving(false);
    }
  };

  const toggleHub = async (hub: Hub) => {
    try {
      // No PATCH endpoint — must PUT full object with isActive flipped
      await apiClient.put(`/admin/hubs/${hub.id}`, {
        name: hub.name,
        city: hub.city,
        state: hub.state,
        country: hub.country,
        contactPhone: hub.contactPhone,
        isActive: !hub.isActive,
      });
      await fetchHubs();
    } catch (err: any) {
      addNotification(parseError(err), 'error');
    }
  };

  const confirmDelete = async () => {
    if (!hubToDelete) return;
    try {
      await apiClient.delete(`/admin/hubs/${hubToDelete.id}`);
      addNotification(`Deleted "${hubToDelete.name}" successfully`, 'success');
      await fetchHubs();
    } catch (err: any) {
      addNotification(parseError(err), 'error');
    } finally {
      setHubToDelete(null);
    }
  };

  // ── Render ─────────────────────────────────────────────────────────────────
  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 20, maxWidth: 1100 }}>

      {/* Header */}
      <div style={{ display: 'flex', alignItems: 'flex-end', justifyContent: 'space-between' }}>
        <div>
          <div className="accent-line" style={{ marginBottom: 8 }} />
          <h1 className="section-title">Hub Management</h1>
          <p className="section-sub">
            {loading ? 'Loading...' : `${hubs.length} hubs · ${hubs.filter(h => h.isActive).length} active`}
          </p>
        </div>
        <button className="ss-btn" onClick={showForm ? closeForm : openCreate}>
          {showForm ? '✕ Close' : '▷ Add Hub'}
        </button>
      </div>

      {/* Form */}
      {showForm && (
        <div className="ss-card" style={{ padding: 24 }}>
          <h2 style={{ fontFamily: 'Rajdhani, sans-serif', fontSize: 13, fontWeight: 700, letterSpacing: '0.1em', textTransform: 'uppercase', color: '#fff', marginBottom: 16 }}>
            {editingHub ? `Edit — ${editingHub.name}` : 'New Hub'}
          </h2>
          <form onSubmit={submitForm} style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 14 }}>
            {FIELDS.map(f => (
              <div key={f.key}>
                <label style={{ display: 'block', fontFamily: 'Rajdhani, sans-serif', fontSize: 10, fontWeight: 600, letterSpacing: '0.14em', textTransform: 'uppercase', color: 'var(--color-text-muted)', marginBottom: 6 }}>
                  {f.label}
                </label>
                <input
                  className="ss-input" style={{ width: '100%' }}
                  placeholder={f.placeholder}
                  value={form[f.key]}
                  onChange={e => setForm(prev => ({ ...prev, [f.key]: e.target.value }))}
                  required
                />
              </div>
            ))}
            <div style={{ gridColumn: '1 / -1', display: 'flex', gap: 10, justifyContent: 'flex-end' }}>
              <button type="button" className="ss-btn ss-btn-outline" onClick={closeForm}>Cancel</button>
              <button type="submit" className="ss-btn" disabled={saving}>
                {saving ? 'Saving...' : editingHub ? '▷ Update Hub' : '▷ Create Hub'}
              </button>
            </div>
          </form>
        </div>
      )}

      {/* Grid */}
      {loading ? (
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: 14 }}>
          {[...Array(3)].map((_, i) => (
            <div key={i} className="ss-card" style={{ height: 160, background: 'var(--color-surface-2)', animation: 'borderGlow 2s ease infinite' }} />
          ))}
        </div>
      ) : hubs.length === 0 ? (
        <div className="ss-card" style={{ padding: 60, textAlign: 'center', color: 'var(--color-text-muted)', fontFamily: 'Rajdhani', textTransform: 'uppercase', fontSize: 12 }}>
          — No hubs found in database —
        </div>
      ) : (
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: 14 }}>
          {hubs.map(hub => (
            <div key={hub.id} className="ss-card" style={{ padding: 20 }}>

              {/* Hub header */}
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: 12 }}>
                <div>
                  <div style={{ fontFamily: 'Rajdhani, sans-serif', fontSize: 18, fontWeight: 700, letterSpacing: '0.04em', color: '#fff', marginBottom: 2 }}>
                    {hub.name}
                  </div>
                  <div style={{ fontSize: 12, color: 'var(--color-text-muted)' }}>
                    {hub.city}, {hub.state}
                  </div>
                </div>
                <span className={`ss-badge ${hub.isActive ? 'success glow-success' : 'muted'}`}>
                  {hub.isActive ? 'ACTIVE' : 'OFFLINE'}
                </span>
              </div>

              {/* Phone */}
              <div style={{ fontSize: 12, color: 'var(--color-text-muted)', marginBottom: 16 }}>
                {hub.contactPhone}
              </div>

              {/* Actions */}
              <div style={{ display: 'flex', gap: 6, marginTop: 4 }}>

                {/* Status indicator button — green if active, red if inactive */}
                <button
                  onClick={() => toggleHub(hub)}
                  style={{
                    flex: 1,
                    fontSize: 13,
                    padding: '4px 0',
                    fontFamily: 'Rajdhani, sans-serif',
                    fontWeight: 700,
                    letterSpacing: '0.1em',
                    textTransform: 'uppercase',
                    border: `1px solid ${hub.isActive ? '#00c48c' : '#e0001a'}`,
                    borderRadius: 7,
                    background: hub.isActive
                      ? 'rgba(0, 196, 140, 0.12)'
                      : 'rgba(224, 0, 26, 0.12)',
                    color: hub.isActive ? '#00c48c' : '#e0001a',
                    cursor: 'pointer',
                    transition: 'all 0.5s ease',
                  }}
                  onMouseEnter={e => {
                    const btn = e.currentTarget as HTMLButtonElement;
                    btn.style.background = hub.isActive
                      ? 'rgba(224, 0, 26, 0.15)'
                      : 'rgba(0, 196, 140, 0.15)';
                    btn.style.borderColor = hub.isActive ? '#e0001a' : '#00c48c';
                    btn.style.color = hub.isActive ? '#e0001a' : '#00c48c';
                    btn.textContent = hub.isActive ? 'Click to Deactivate' : 'Click to Activate';
                  }}
                  onMouseLeave={e => {
                    const btn = e.currentTarget as HTMLButtonElement;
                    btn.style.background = hub.isActive
                      ? 'rgba(0, 196, 140, 0.12)'
                      : 'rgba(224, 0, 26, 0.12)';
                    btn.style.borderColor = hub.isActive ? '#00c48c' : '#e0001a';
                    btn.style.color = hub.isActive ? '#00c48c' : '#e0001a';
                    btn.textContent = hub.isActive ? '● Active' : '● Offline';
                  }}
                >
                  {hub.isActive ? '● Active' : '● Offline'}
                </button>

                {/* Edit */}
                <button
                  className="ss-btn ss-btn-outline"
                  style={{ fontSize: 11, padding: '4px 12px' }}
                  onClick={() => openEdit(hub)}
                >
                  Edit
                </button>

                {/* Delete */}
                <button
                  style={{
                    fontSize: 11,
                    padding: '4px 10px',
                    background: 'transparent',
                    border: '1px solid #333',
                    borderRadius: 4,
                    color: '#555',
                    cursor: 'pointer',
                    transition: 'all 0.2s ease',
                  }}
                  onMouseEnter={e => {
                    (e.currentTarget as HTMLButtonElement).style.borderColor = '#e0001a';
                    (e.currentTarget as HTMLButtonElement).style.color = '#e0001a';
                  }}
                  onMouseLeave={e => {
                    (e.currentTarget as HTMLButtonElement).style.borderColor = '#333';
                    (e.currentTarget as HTMLButtonElement).style.color = '#555';
                  }}
                  onClick={() => setHubToDelete(hub)}
                >
                  ✕
                </button>

              </div>
            </div>
          ))}
        </div>
      )}

      {/* Delete Confirmation Modal */}
      {hubToDelete && (
        <div onClick={() => setHubToDelete(null)} style={{ position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.8)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 9999, backdropFilter: 'blur(2px)' }}>
          <div onClick={e => e.stopPropagation()} className="ss-card" style={{ width: 400, padding: 24, boxShadow: '0 0 40px rgba(0,0,0,0.45)' }}>
            <h2 style={{ fontFamily: 'Orbitron, monospace', marginBottom: 16, color: '#fff', fontSize: 20, fontWeight: 700 }}>Confirm Deletion</h2>
            <p style={{ color: 'var(--color-text-muted)', marginBottom: 18, fontSize: 14 }}>
              Permanently delete "{hubToDelete.name}"? This cannot be undone.
            </p>
            <div style={{ display: 'flex', justifyContent: 'flex-end', gap: 10 }}>
              <button className="ss-btn ss-btn-outline" onClick={() => setHubToDelete(null)}>Cancel</button>
              <button className="ss-btn" style={{ background: '#e0001a', color: '#fff', border: '1px solid #e0001a' }} onClick={confirmDelete}>Yes, Delete</button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
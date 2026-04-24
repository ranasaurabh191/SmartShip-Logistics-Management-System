import { useRef, useState } from 'react';
import { apiClient } from '../../core/api/axios';

interface Props {
  shipmentId: number;
  trackingNumber: string;
  receiverName: string;
  onClose: () => void;
  onSuccess: () => void;
}

const modalBackdrop: React.CSSProperties = {
  position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.8)',
  display: 'flex', alignItems: 'center', justifyContent: 'center',
  zIndex: 9999, backdropFilter: 'blur(2px)',
};

const labelStyle: React.CSSProperties = {
  fontFamily: 'Orbitron, monospace', fontSize: 11, fontWeight: 600,
  letterSpacing: '0.14em', textTransform: 'uppercase', color: '#888',
  marginBottom: 6, display: 'block',
};

const errorStyle: React.CSSProperties = {
  color: '#ff6b6b', fontSize: 12, marginTop: 4, fontFamily: 'Inter, sans-serif',
};

export const DeliveryConfirmModal = ({
  shipmentId, trackingNumber, receiverName, onClose, onSuccess,
}: Props) => {
  const [form, setForm] = useState({
    receiverName: receiverName || '',
    deliveredBy: '',
    notes: '',
  });
  const [errors, setErrors] = useState<Partial<typeof form>>({});
  const [signatureFile, setSignatureFile] = useState<File | null>(null);
  const [photoFile, setPhotoFile] = useState<File | null>(null);
  const [signatureError, setSignatureError] = useState('');
  const [photoError, setPhotoError] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [apiError, setApiError] = useState('');
  const sigRef = useRef<HTMLInputElement>(null);
  const photoRef = useRef<HTMLInputElement>(null);

  const ALLOWED_IMG = ['.jpg', '.jpeg', '.png'];
  const MAX_IMG_MB = 5;

  const validateImageFile = (file: File): string => {
    const ext = '.' + file.name.split('.').pop()?.toLowerCase();
    if (!ALLOWED_IMG.includes(ext)) return 'Only JPG, PNG allowed';
    if (file.size > MAX_IMG_MB * 1024 * 1024) return `Image must be under ${MAX_IMG_MB} MB`;
    return '';
  };

  const validate = (): boolean => {
    const errs: Partial<typeof form> = {};
    if (!form.receiverName.trim() || form.receiverName.trim().length < 2)
      errs.receiverName = 'Receiver name is required (min 2 characters)';
    if (!form.deliveredBy.trim() || form.deliveredBy.trim().length < 2)
      errs.deliveredBy = 'Delivery agent name is required';
    if (!form.notes.trim())
      errs.notes = 'Notes are required';
    setErrors(errs);
    return Object.keys(errs).length === 0;
  };

  const handleSubmit = async () => {
    if (!validate()) return;
    setSubmitting(true);
    setApiError('');
    try {
      try {
        const formData = new FormData();
        formData.append('shipmentId', String(shipmentId));
        formData.append('trackingNumber', trackingNumber);
        formData.append('receiverName', form.receiverName);
        formData.append('notes', form.notes);
        formData.append('deliveredBy', form.deliveredBy);
        if (signatureFile) formData.append('signature', signatureFile);
        if (photoFile) formData.append('photo', photoFile);

        await apiClient.post('/tracking/delivery-proof', formData, {
          headers: { 'Content-Type': 'multipart/form-data' },
        });
      } catch (proofErr: any) {
        const msg: string = proofErr?.response?.data?.message ?? '';
        if (!msg.toLowerCase().includes('already exists')) {
          throw proofErr;
        }
      }

      await apiClient.put(`/admin/shipments/status/${shipmentId}`, {
        status: 'Delivered',
        location: form.receiverName,
        resolution: '',
      });

      onSuccess();
    } catch (err: any) {
      setApiError(err?.response?.data?.message || 'Failed to confirm delivery.');
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div style={modalBackdrop} onClick={onClose}>
      <div
        onClick={e => e.stopPropagation()}
        className="ss-card"
        style={{
          width: 520, maxWidth: '95vw', maxHeight: '90vh', overflowY: 'auto',
          padding: 28, borderRadius: 8, boxShadow: '0 0 48px rgba(224,0,26,0.18)',
        }}
      >
        {/* Header */}
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: 24 }}>
          <div>
            <div className="accent-line" style={{ marginBottom: 6 }} />
            <h2 style={{ fontFamily: 'Orbitron, monospace', fontSize: 16, fontWeight: 700, letterSpacing: '0.08em', textTransform: 'uppercase', color: '#fff', marginBottom: 4 }}>
              Confirm Delivery
            </h2>
            <p style={{ fontSize: 12, color: '#888' }}>
              Tracking: <span style={{ color: 'var(--color-accent)', fontWeight: 600 }}>{trackingNumber}</span>
            </p>
          </div>
          <button onClick={onClose} style={{ background: 'transparent', border: 'none', color: '#666', fontSize: 20, cursor: 'pointer', lineHeight: 1, padding: '0 4px' }}>
            ✕
          </button>
        </div>

        <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>

          {/* Receiver Name */}
          <div>
            <label style={labelStyle}>Receiver Name *</label>
            <input
              className="ss-input"
              style={{ width: '100%', borderColor: errors.receiverName ? '#ff6b6b' : undefined }}
              placeholder="Name of person who received the package"
              value={form.receiverName}
              onChange={e => { setForm(p => ({ ...p, receiverName: e.target.value })); setErrors(p => ({ ...p, receiverName: '' })); }}
            />
            {errors.receiverName && <div style={errorStyle}>⚠ {errors.receiverName}</div>}
          </div>

          {/* Delivered By */}
          <div>
            <label style={labelStyle}>Delivered By (Agent Name) *</label>
            <input
              className="ss-input"
              style={{ width: '100%', borderColor: errors.deliveredBy ? '#ff6b6b' : undefined }}
              placeholder="Delivery agent's name"
              value={form.deliveredBy}
              onChange={e => { setForm(p => ({ ...p, deliveredBy: e.target.value })); setErrors(p => ({ ...p, deliveredBy: '' })); }}
            />
            {errors.deliveredBy && <div style={errorStyle}>⚠ {errors.deliveredBy}</div>}
          </div>

          {/* Notes */}
          <div>
            <label style={labelStyle}>Delivery Notes <span style={{ color: '#555' }}>(optional)</span></label>
            <textarea
              className="ss-input"
              style={{ width: '100%', minHeight: 80, resize: 'vertical' }}
              placeholder='"Package left at reception", "Collected by security guard"'
              value={form.notes}
              onChange={e => setForm(p => ({ ...p, notes: e.target.value }))}
            />
          </div>

          {/* File Uploads */}
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 14 }}>
            {/* Signature */}
            <div>
              <label style={labelStyle}>Signature Image <span style={{ color: '#555' }}>(optional)</span></label>
              <div
                onClick={() => sigRef.current?.click()}
                style={{
                  border: `1px dashed ${signatureError ? '#ff6b6b' : 'rgba(255,255,255,0.15)'}`,
                  borderRadius: 4, padding: '14px 10px', textAlign: 'center',
                  cursor: 'pointer', background: 'var(--color-surface-2)', transition: 'border-color 0.15s',
                }}
              >
                <input ref={sigRef} type="file" accept=".jpg,.jpeg,.png" style={{ display: 'none' }}
                  onChange={e => {
                    const f = e.target.files?.[0];
                    if (!f) return;
                    const err = validateImageFile(f);
                    setSignatureError(err);
                    setSignatureFile(err ? null : f);
                  }}
                />
                {signatureFile ? (
                  <div style={{ fontSize: 12, color: '#fff' }}>✓ {signatureFile.name}</div>
                ) : (
                  <>
                    <div style={{ fontSize: 22, marginBottom: 4 }}>✍️</div>
                    <div style={{ fontSize: 11, color: '#666', fontFamily: 'Orbitron, monospace', textTransform: 'uppercase', letterSpacing: '0.06em' }}>Upload Signature</div>
                  </>
                )}
              </div>
              {signatureError && <div style={errorStyle}>⚠ {signatureError}</div>}
            </div>

            {/* Proof Photo */}
            <div>
              <label style={labelStyle}>Proof Photo <span style={{ color: '#555' }}>(optional)</span></label>
              <div
                onClick={() => photoRef.current?.click()}
                style={{
                  border: `1px dashed ${photoError ? '#ff6b6b' : 'rgba(255,255,255,0.15)'}`,
                  borderRadius: 4, padding: '14px 10px', textAlign: 'center',
                  cursor: 'pointer', background: 'var(--color-surface-2)', transition: 'border-color 0.15s',
                }}
              >
                <input ref={photoRef} type="file" accept=".jpg,.jpeg,.png" style={{ display: 'none' }}
                  onChange={e => {
                    const f = e.target.files?.[0];
                    if (!f) return;
                    const err = validateImageFile(f);
                    setPhotoError(err);
                    setPhotoFile(err ? null : f);
                  }}
                />
                {photoFile ? (
                  <div style={{ fontSize: 12, color: '#fff' }}>✓ {photoFile.name}</div>
                ) : (
                  <>
                    <div style={{ fontSize: 22, marginBottom: 4 }}>📸</div>
                    <div style={{ fontSize: 11, color: '#666', fontFamily: 'Orbitron, monospace', textTransform: 'uppercase', letterSpacing: '0.06em' }}>Upload Photo</div>
                  </>
                )}
              </div>
              {photoError && <div style={errorStyle}>⚠ {photoError}</div>}
            </div>
          </div>

          {/* Warning */}
          <div style={{ padding: '10px 14px', background: 'rgba(245,166,35,0.07)', border: '1px solid rgba(245,166,35,0.25)', borderRadius: 4 }}>
            <div style={{ fontSize: 12, color: '#f5a623', display: 'flex', alignItems: 'flex-start', gap: 8 }}>
              <span style={{ flexShrink: 0 }}>⚠</span>
              <span>This action will mark the shipment as <strong>Delivered</strong> and cannot be undone. Ensure the package has been physically handed over.</span>
            </div>
          </div>

          {/* API Error */}
          {apiError && (
            <div style={{ padding: '10px 14px', border: '1px solid rgba(224,0,26,0.35)', background: 'rgba(224,0,26,0.07)', color: '#ff8d8d', borderRadius: 4, fontSize: 13 }}>
              {apiError}
            </div>
          )}

          {/* Buttons */}
          <div style={{ display: 'flex', justifyContent: 'flex-end', gap: 10, marginTop: 4 }}>
            <button className="ss-btn ss-btn-outline" onClick={onClose} disabled={submitting}>Cancel</button>
            <button className="ss-btn" onClick={handleSubmit} disabled={submitting} style={{ opacity: submitting ? 0.7 : 1, minWidth: 160 }}>
              {submitting ? 'CONFIRMING...' : '✓ CONFIRM DELIVERY'}
            </button>
          </div>
        </div>
      </div>
    </div>
  );
};
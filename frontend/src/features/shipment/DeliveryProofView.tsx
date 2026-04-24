import { useEffect, useState } from 'react';
import { apiClient } from '../../core/api/axios';

interface DeliveryProofDto {
  shipmentId: number;
  trackingNumber: string;
  receiverName: string;
  signatureImagePath: string | null;
  photoPath: string | null;
  notes: string;
  deliveredAt: string;
  deliveredBy: string;
}

interface Props { shipmentId: number; }

const useBlobUrl = (endpoint: string, enabled: boolean) => {
  const [url, setUrl] = useState<string | null>(null);

  useEffect(() => {
    if (!enabled) return;
    let objectUrl: string;
    apiClient.get(endpoint, { responseType: 'blob' })
      .then(res => {
        objectUrl = URL.createObjectURL(res.data);
        setUrl(objectUrl);
      })
      .catch(() => setUrl(null));

    return () => { if (objectUrl) URL.revokeObjectURL(objectUrl); };
  }, [endpoint, enabled]);

  return url;
};

export const DeliveryProofView = ({ shipmentId }: Props) => {
  const [proof, setProof] = useState<DeliveryProofDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    const fetchProof = async () => {
      setLoading(true);
      try {
        const res = await apiClient.get(`/tracking/delivery-proof/${shipmentId}`);
        setProof(res.data);
      } catch (err: any) {
        if (err?.response?.status !== 404)
          setError('Unable to load delivery proof.');
      } finally {
        setLoading(false);
      }
    };
    fetchProof();
  }, [shipmentId]);

  // Fetch images as blobs — auth-protected, no direct disk path used
  const sigUrl = useBlobUrl(
    `/tracking/delivery-proof/${shipmentId}/signature`,
    !!proof?.signatureImagePath
  );
  const photoUrl = useBlobUrl(
    `/tracking/delivery-proof/${shipmentId}/photo`,
    !!proof?.photoPath
  );

  const labelStyle: React.CSSProperties = {
    fontFamily: 'Orbitron, monospace', fontSize: 10, fontWeight: 600,
    letterSpacing: '0.14em', textTransform: 'uppercase', color: '#888', marginBottom: 4,
  };

  if (loading) return (
    <div style={{ color: '#555', fontSize: 12, fontFamily: 'Orbitron, monospace',
      textTransform: 'uppercase', letterSpacing: '0.1em', padding: '16px 0' }}>
      LOADING DELIVERY PROOF...
    </div>
  );

  if (error) return (
    <div style={{ color: '#ff6b6b', fontSize: 12, padding: '16px 0' }}>{error}</div>
  );

  if (!proof) return null;

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 20 }}>
      {/* Header */}
      <div style={{ borderBottom: '1px solid rgba(255,255,255,0.08)', paddingBottom: 14 }}>
        <div className="accent-line" style={{ marginBottom: 6 }} />
        <h2 style={{ fontFamily: 'Orbitron, monospace', fontSize: 14, fontWeight: 700,
          letterSpacing: '0.1em', textTransform: 'uppercase', color: '#fff', marginBottom: 8 }}>
          Delivery Proof
        </h2>
        <div style={{ display: 'inline-flex', alignItems: 'center', gap: 6,
          padding: '3px 10px', background: 'rgba(0,196,140,0.1)',
          border: '1px solid rgba(0,196,140,0.3)', borderRadius: 4 }}>
          <span style={{ width: 7, height: 7, borderRadius: '50%',
            background: 'var(--color-success)', display: 'inline-block' }} />
          <span style={{ fontSize: 11, color: 'var(--color-success)',
            fontFamily: 'Orbitron, monospace', letterSpacing: '0.1em',
            textTransform: 'uppercase' }}>Delivered</span>
        </div>
      </div>

      {/* Info Grid */}
      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 14 }}>
        {[
          { label: 'Received By', value: proof.receiverName },
          { label: 'Delivered By', value: proof.deliveredBy },
          { label: 'Delivered At', value: proof.deliveredAt },
          { label: 'Tracking No.', value: proof.trackingNumber },
        ].map(item => (
          <div key={item.label} className="kpi-card" style={{ padding: '14px 16px' }}>
            <div style={labelStyle}>{item.label}</div>
            <div style={{ fontSize: 14, fontWeight: 700, color: '#fff',
              fontFamily: 'Orbitron, monospace' }}>{item.value}</div>
          </div>
        ))}

        {proof.notes && (
          <div className="kpi-card" style={{ padding: '14px 16px', gridColumn: '1 / -1' }}>
            <div style={labelStyle}>Notes</div>
            <div style={{ fontSize: 13, color: 'var(--color-text)', lineHeight: 1.6 }}>
              {proof.notes}
            </div>
          </div>
        )}
      </div>

      {/* Proof Images — loaded as blobs through authenticated API */}
      {(proof.signatureImagePath || proof.photoPath) && (
        <div>
          <div style={{ ...labelStyle, marginBottom: 12 }}>Proof Images</div>
          <div style={{ display: 'grid',
            gridTemplateColumns: proof.signatureImagePath && proof.photoPath ? '1fr 1fr' : '1fr',
            gap: 14 }}>

            {proof.signatureImagePath && (
              <div style={{ background: 'var(--color-surface-2)',
                border: '1px solid rgba(255,255,255,0.08)', borderRadius: 4, overflow: 'hidden' }}>
                <div style={{ padding: '8px 12px',
                  borderBottom: '1px solid rgba(255,255,255,0.06)' }}>
                  <span style={{ ...labelStyle, marginBottom: 0 }}>Signature</span>
                </div>
                <div style={{ padding: 12, background: '#fff', minHeight: 120,
                  display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
                  {sigUrl
                    ? <img src={sigUrl} alt="Delivery signature"
                        style={{ maxWidth: '100%', maxHeight: 160, objectFit: 'contain' }} />
                    : <span style={{ color: '#aaa', fontSize: 12 }}>Loading...</span>
                  }
                </div>
              </div>
            )}

            {proof.photoPath && (
              <div style={{ background: 'var(--color-surface-2)',
                border: '1px solid rgba(255,255,255,0.08)', borderRadius: 4, overflow: 'hidden' }}>
                <div style={{ padding: '8px 12px',
                  borderBottom: '1px solid rgba(255,255,255,0.06)' }}>
                  <span style={{ ...labelStyle, marginBottom: 0 }}>Proof Photo</span>
                </div>
                <div style={{ padding: 12, minHeight: 120,
                  display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
                  {photoUrl
                    ? <img src={photoUrl} alt="Proof of delivery"
                        style={{ maxWidth: '100%', maxHeight: 180,
                          objectFit: 'contain', borderRadius: 2 }} />
                    : <span style={{ color: '#aaa', fontSize: 12 }}>Loading...</span>
                  }
                </div>
              </div>
            )}
          </div>
        </div>
      )}

      {/* Verified Badge */}
      <div style={{ display: 'flex', alignItems: 'center', gap: 8, padding: '8px 14px',
        background: 'rgba(67,122,34,0.1)', border: '1px solid rgba(67,122,34,0.3)',
        borderRadius: 4 }}>
        <span style={{ color: 'var(--color-success)', fontSize: 16 }}>✓</span>
        <span style={{ fontFamily: 'Orbitron, monospace', fontSize: 11, fontWeight: 600,
          letterSpacing: '0.1em', textTransform: 'uppercase',
          color: 'var(--color-success)' }}>
          Delivery Verified
        </span>
      </div>
    </div>
  );
};
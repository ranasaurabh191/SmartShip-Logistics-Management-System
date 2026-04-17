import { useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import { apiClient } from '../../core/api/axios';

interface TrackingEvent {
  hubName: string;
  timestamp: string;
  status: string;
  description: string;
}

interface ShipmentDetail {
  id: string;
  trackingNumber: string;
  originCity: string;
  destinationCity: string;
  weightKg: number;
  shipmentType: string;
  status: string;
  trackingEvents: TrackingEvent[];
}

export const TrackShipment = () => {
  const { id } = useParams();
  const [shipment, setShipment] = useState<ShipmentDetail | null>(null);
  const [loading, setLoading] = useState(true);
  const [trackInput, setTrackInput] = useState('');
  const [error, setError] = useState('');

  const buildShipmentViewModel = (
    shipmentData: any,
    trackingItems: any[]
  ): ShipmentDetail => {
    return {
      id: String(shipmentData?.id ?? ''),
      trackingNumber: shipmentData?.trackingNumber ?? '',
      originCity: shipmentData?.senderAddress?.city ?? '—',
      destinationCity: shipmentData?.receiverAddress?.city ?? '—',
      weightKg: Number(shipmentData?.package?.weightKg ?? 0),
      shipmentType: shipmentData?.shipmentType ?? '—',
      status:
        trackingItems[trackingItems.length - 1]?.status ??
        shipmentData?.status ??
        '—',
      trackingEvents: trackingItems.map((e: any) => ({
        hubName: e.location ?? '—',
        timestamp: e.eventTime ?? '',
        status: e.status ?? '—',
        description: e.description ?? '—',
      })),
    };
  };

  const normalizeTrackingItems = (data: any): any[] => {
    if (Array.isArray(data)) return data;

    return (
      data?.items ??
      data?.data ??
      data?.events ??
      []
    );
  };

  const fetchTrackingTimeline = async (trackingNumber: string) => {
    try {
      const res = await apiClient.get(`/tracking/${trackingNumber}`);
      return normalizeTrackingItems(res.data);
    } catch {
      return [];
    }
  };

  const fetchShipmentDetails = async (
    shipmentIdOrTracking: string
  ) => {
    try {
      // numeric shipment id
      if (/^\d+$/.test(shipmentIdOrTracking)) {
        try {
          const res = await apiClient.get(
            `/shipments/${shipmentIdOrTracking}`
          );
          return res.data;
        } catch {
          const res = await apiClient.get(
            `/admin/shipments/${shipmentIdOrTracking}`
          );
          return res.data;
        }
      }

      // tracking number
      const res = await apiClient.get(
        `/shipments/by-tracking/${shipmentIdOrTracking}`
      );
      return res.data;
    } catch {
      return null;
    }
  };

  const loadTracking = async (value: string) => {
    if (!value?.trim()) {
      setLoading(false);
      return;
    }

    setLoading(true);
    setError('');

    try {
      const shipmentData = await fetchShipmentDetails(value.trim());

      if (!shipmentData) {
        setShipment(null);
        setError('Shipment details not found.');
        return;
      }

      const trackingNumber = shipmentData?.trackingNumber ?? value.trim();

      const trackingItems = await fetchTrackingTimeline(trackingNumber);

      setShipment(
        buildShipmentViewModel(shipmentData, trackingItems)
      );
    } catch {
      setShipment(null);
      setError('Unable to load shipment tracking details.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (!id) {
      setLoading(false);
      return;
    }

    loadTracking(id);
  }, [id]);

  const events = shipment?.trackingEvents ?? [];
  const lastIdx = events.length - 1;

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 20, maxWidth: 1100 }}>
      <div>
        <div className="accent-line" style={{ marginBottom: 8 }} />
        <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
          <div>
            <h1 className="section-title">Shipment Tracking</h1>
            <p className="section-sub">Live hub-by-hub transit status</p>
          </div>
        </div>
      </div>

      {!id && (
        <div className="ss-card" style={{ padding: '16px 20px', display: 'flex', gap: 12 }}>
          <input
            className="ss-input"
            placeholder="Enter Tracking Number"
            value={trackInput}
            onChange={e => setTrackInput(e.target.value)}
            onKeyDown={e => e.key === 'Enter' && loadTracking(trackInput)}
            style={{ flex: 1 }}
          />
          <button className="ss-btn" onClick={() => loadTracking(trackInput)}>
            ▷ Track
          </button>
        </div>
      )}

      {loading && (
        <div
          style={{
            padding: 60,
            textAlign: 'center',
            color: 'var(--color-text-muted)',
            fontFamily: 'Rajdhani, sans-serif',
            textTransform: 'uppercase',
            fontSize: 12,
          }}
        >
          LOADING...
        </div>
      )}

      {!loading && error && (
        <div
          style={{
            padding: 60,
            textAlign: 'center',
            color: '#ff6b6b',
            fontFamily: 'Rajdhani, sans-serif',
            textTransform: 'uppercase',
            fontSize: 12,
          }}
        >
          {error}
        </div>
      )}

      {shipment && (
        <>
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: 12 }}>
            {[
              { label: 'Origin', value: shipment.originCity },
              { label: 'Destination', value: shipment.destinationCity },
              { label: 'Package Weight', value: `${shipment.weightKg} kg` },
              { label: 'Shipment Type', value: shipment.shipmentType },
            ].map(item => (
              <div key={item.label} className="ss-card" style={{ padding: '14px 16px' }}>
                <div className="kpi-label" style={{ marginBottom: 4 }}>
                  {item.label}
                </div>
                <div
                  style={{
                    fontFamily: 'Rajdhani, sans-serif',
                    fontSize: 18,
                    fontWeight: 700,
                    color: '#fff',
                  }}
                >
                  {item.value || '—'}
                </div>
              </div>
            ))}
          </div>

          <div className="ss-card" style={{ padding: 28 }}>
            <div
              style={{
                display: 'flex',
                justifyContent: 'space-between',
                alignItems: 'center',
                marginBottom: 28,
              }}
            >
              <h2
                style={{
                  fontFamily: 'Rajdhani, sans-serif',
                  fontSize: 14,
                  fontWeight: 700,
                  letterSpacing: '0.1em',
                  textTransform: 'uppercase',
                  color: '#fff',
                }}
              >
                Hub Transition Timeline
              </h2>

            </div>

            {events.length === 0 ? (
              <div
                style={{
                  color: 'var(--color-text-muted)',
                  fontFamily: 'Rajdhani',
                  textTransform: 'uppercase',
                  fontSize: 11,
                }}
              >
                No tracking events yet.
              </div>
            ) : (
              <div style={{ position: 'relative', paddingLeft: 32 }}>
                <div
                  style={{
                    position: 'absolute',
                    left: 5,
                    top: 0,
                    bottom: 0,
                    width: 1,
                    background: 'var(--color-border)',
                  }}
                />

                {events.map((evt, i) => {
                  const isActive = i === lastIdx;
                  const isDone = i < lastIdx;

                  return (
                    <div
                      key={i}
                      style={{
                        position: 'relative',
                        paddingBottom: i < events.length - 1 ? 26 : 0,
                        opacity: 1,
                      }}
                    >
                      <div
                        style={{
                          position: 'absolute',
                          left: -32,
                          top: 2,
                          width: 12,
                          height: 12,
                          borderRadius: '50%',
                          background: isActive
                            ? 'var(--color-accent)'
                            : isDone
                              ? 'var(--color-success)'
                              : 'var(--color-surface-2)',
                          border: `2px solid ${isActive
                            ? 'var(--color-accent)'
                            : isDone
                              ? 'var(--color-success)'
                              : '#333'
                            }`,
                          zIndex: 1,
                        }}
                      />

                      <div
                        style={{
                          display: 'flex',
                          alignItems: 'flex-start',
                          justifyContent: 'space-between',
                          gap: 16,
                        }}
                      >
                        <div>
                          <div
                            style={{
                              fontFamily: 'Rajdhani, sans-serif',
                              fontSize: 16,
                              fontWeight: 700,
                              color: '#fff',
                              letterSpacing: '0.04em',
                              marginBottom: 2,
                            }}
                          >
                            {evt.hubName}
                          </div>

                          <div
                            style={{
                              fontSize: 13,
                              color: 'var(--color-text-muted)',
                            }}
                          >
                            {evt.description}
                          </div>
                        </div>

                        <div style={{ textAlign: 'right', flexShrink: 0 }}>
                          <div
                            style={{
                              fontSize: 13,
                              color: '#d6d6d6',
                              fontFamily: 'Rajdhani, sans-serif',
                              letterSpacing: '0.08em',
                            }}
                          >
                            {evt.timestamp
                              ? new Date(evt.timestamp).toLocaleString('en-IN')
                              : '—'}
                          </div>

                          <span
                            className={`ss-badge ${isActive ? 'glow' : isDone ? 'success' : 'muted'
                              }`}
                            style={{
                              marginTop: 4,
                              display: 'inline-block',
                            }}
                          >
                            {evt.status?.toUpperCase()}
                          </span>
                        </div>
                      </div>
                    </div>
                  );
                })}
              </div>
            )}
          </div>
        </>
      )}
    </div>
  );
};
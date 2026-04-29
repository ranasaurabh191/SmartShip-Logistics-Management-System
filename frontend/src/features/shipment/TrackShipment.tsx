import { useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import { apiClient } from '../../core/api/axios';
import { DocumentUpload } from './DocumentUpload';
import { DeliveryProofView } from './DeliveryProofView';
import { DocumentsReadOnly } from './DocumentsReadOnly';
import { useAuthStore } from '../../store/useAuthStore';
import { useChatStore } from '../../store/useChatStore';
import { ShipmentRouteMap } from '../../components/ShipmentRouteMap';

interface TrackingEvent {
  hubName: string;
  timestamp: string;
  status: string;
  description: string;
}

interface RouteStopData {
  id: number; hubId: number; hubName: string; hubCity: string;
  latitude: number; longitude: number; sequenceOrder: number;
  isCompleted: boolean; reachedAt?: string | null;
}

interface ShipmentDetail {
  id: string;
  numericId: number;
  trackingNumber: string;
  originCity: string;
  destinationCity: string;
  weightKg: number;
  shipmentType: string;
  status: string;
  trackingEvents: TrackingEvent[];
  senderName?: string;
  receiverName?: string;
  senderPhone?: string;
  receiverPhone?: string;
  senderAddress?: string;
  receiverAddress?: string;
  senderAddressObj?: any;
  receiverAddressObj?: any;
}

export const TrackShipment = () => {
  const { id } = useParams();
  const [shipment, setShipment] = useState<ShipmentDetail | null>(null);
  const [loading, setLoading] = useState(true);
  const [trackInput, setTrackInput] = useState('');
  const [error, setError] = useState('');
  const [activeTab, setActiveTab] = useState<'timeline' | 'map' | 'documents' | 'delivery'>('timeline');
  const [routeData, setRouteData] = useState<RouteStopData[]>([]);
  const user = useAuthStore(state => state.user);
  const isAdmin = user?.role === 'ADMIN';
  const buildShipmentViewModel = (shipmentData: any, trackingItems: any[]): ShipmentDetail => ({
    id: String(shipmentData?.id ?? ''),
    numericId: Number(shipmentData?.id ?? 0),
    trackingNumber: shipmentData?.trackingNumber ?? '',
    originCity: shipmentData?.senderAddress?.city ?? '',
    destinationCity: shipmentData?.receiverAddress?.city ?? '',
    weightKg: Number(shipmentData?.package?.weightKg ?? 0),
    shipmentType: shipmentData?.shipmentType ?? '',
    status: shipmentData?.status ?? trackingItems[trackingItems.length - 1]?.status ?? '',
    senderName: shipmentData?.senderAddress?.fullName ?? '',
    receiverName: shipmentData?.receiverAddress?.fullName ?? '',
    senderPhone: shipmentData?.senderAddress?.phone ?? '',
    receiverPhone: shipmentData?.receiverAddress?.phone ?? '',
    senderAddress: [
      shipmentData?.senderAddress?.street,
      shipmentData?.senderAddress?.city,
      shipmentData?.senderAddress?.state,
      shipmentData?.senderAddress?.postalCode,
    ].filter(Boolean).join(', '),
    receiverAddress: [
      shipmentData?.receiverAddress?.street,
      shipmentData?.receiverAddress?.city,
      shipmentData?.receiverAddress?.state,
      shipmentData?.receiverAddress?.postalCode,
    ].filter(Boolean).join(', '),
    senderAddressObj: shipmentData?.senderAddress,
    receiverAddressObj: shipmentData?.receiverAddress,
    trackingEvents: trackingItems.map((e: any) => ({
      hubName: e.location ?? '',
      timestamp: e.eventTime ?? '',
      status: e.status ?? '',
      description: e.description ?? '',
    })),
  });
  
  const setShipmentId = useChatStore(state => state.setShipmentId);
  useEffect(() => {
    if (shipment?.numericId) setShipmentId(shipment.numericId);
    return () => setShipmentId(undefined); 
  }, [shipment?.numericId]);

  const normalizeTrackingItems = (data: any): any[] => {
    if (Array.isArray(data)) return data;
    return data?.items ?? data?.data ?? data?.events ?? [];
  };

  const fetchTrackingTimeline = async (trackingNumber: string) => {
    try {
      const res = await apiClient.get(`/tracking/${trackingNumber}`);
      return normalizeTrackingItems(res.data);
    } catch { return []; }
  };

  const fetchShipmentDetails = async (shipmentIdOrTracking: string) => {
    try {
      if (/^\d+$/.test(shipmentIdOrTracking)) {
        try { const res = await apiClient.get(`/shipments/${shipmentIdOrTracking}`); return res.data; } catch { }
        try { const res = await apiClient.get(`/admin/shipments/${shipmentIdOrTracking}`); return res.data; } catch { }
      }
      const res = await apiClient.get(`/shipments/by-tracking/${shipmentIdOrTracking}`);
      return res.data;
    } catch { return null; }
  };

  const loadTracking = async (value: string) => {
    if (!value?.trim()) { setLoading(false); return; }
    setLoading(true);
    setError('');
    try {
      const shipmentData = await fetchShipmentDetails(value.trim());
      if (!shipmentData) { setShipment(null); setError('Shipment details not found.'); return; }
      const trackingNumber = shipmentData?.trackingNumber ?? value.trim();
      const trackingItems = await fetchTrackingTimeline(trackingNumber);
      setShipment(buildShipmentViewModel(shipmentData, trackingItems));
    } catch {
      setShipment(null);
      setError('Unable to load shipment tracking details.');
    } finally {
      setLoading(false);
    }
  };

  // Fetch route data whenever shipment is loaded
  useEffect(() => {
    if (!shipment?.numericId) return;
    const fetchRoute = async () => {
      try {
        const res = await apiClient.get(`/shipments/route/${shipment.numericId}`);
        const stops: RouteStopData[] = Array.isArray(res.data) ? res.data : [];
        setRouteData(stops);
      } catch { setRouteData([]); }
    };
    fetchRoute();
  }, [shipment?.numericId]);

  useEffect(() => {
    if (!id) { setLoading(false); return; }
    loadTracking(id);
  }, [id]);

  const events = shipment?.trackingEvents ?? [];
  const lastIdx = events.length - 1;
  const isDelivered = shipment?.status === 'Delivered';
  useEffect(() => {
    if (isDelivered) setActiveTab('delivery');
  }, [isDelivered]);
  const tabStyle = (tab: typeof activeTab): React.CSSProperties => ({
    fontFamily: 'Orbitron, monospace', fontSize: 11, fontWeight: 600,
    letterSpacing: '0.12em', textTransform: 'uppercase',
    padding: '8px 16px', cursor: 'pointer', background: 'transparent', border: 'none',
    borderBottom: `2px solid ${activeTab === tab ? 'var(--color-accent)' : 'transparent'}`,
    color: activeTab === tab ? 'var(--color-accent)' : '#666',
    transition: 'all 0.15s ease',
  });

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 20, maxWidth: 1100 }}>
      {/* Page Header */}
      <div>
        <div className="accent-line" style={{ marginBottom: 8 }} />
        <h1 className="section-title">Shipment Tracking</h1>
        <p className="section-sub">Live hub-by-hub transit status</p>
      </div>

      {/* Manual Search (only when no id in route) */}
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
          <button className="ss-btn" onClick={() => loadTracking(trackInput)}>Track</button>
        </div>
      )}

      {loading && (
        <div style={{ padding: 60, textAlign: 'center', color: 'var(--color-text-muted)', fontFamily: 'Orbitron, monospace', textTransform: 'uppercase', fontSize: 12 }}>
          LOADING...
        </div>
      )}

      {!loading && error && (
        <div style={{ padding: 60, textAlign: 'center', color: '#ff6b6b', fontFamily: 'Orbitron, monospace', textTransform: 'uppercase', fontSize: 12 }}>
          {error}
        </div>
      )}

      {shipment && (
        <>
          {/* KPI Cards */}
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: 12 }}>
            {[
              { label: 'Origin', value: shipment.originCity },
              { label: 'Destination', value: shipment.destinationCity },
              { label: 'Package Weight', value: `${shipment.weightKg} kg` },
              { label: 'Shipment Type', value: shipment.shipmentType },
            ].map(item => (
              <div key={item.label} className="ss-card" style={{ padding: '14px 16px' }}>
                <div className="kpi-label" style={{ marginBottom: 4 }}>{item.label}</div>
                <div style={{ fontFamily: 'Roboto, sans-serif', fontSize: 18, fontWeight: 700, color: '#fff' }}>{item.value}</div>
              </div>
            ))}
          </div>

          {/* Sender / Receiver Cards */}
          {(shipment.senderName || shipment.receiverName) && (
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12 }}>
              {shipment.senderName && (
                <div className="ss-card" style={{ padding: '16px 20px' }}>
                  <div className="kpi-label" style={{ marginBottom: 8 }}>Sender</div>
                  <div style={{ fontFamily: 'Roboto, sans-serif', fontSize: 14, fontWeight: 700, color: '#fff', marginBottom: 4 }}>{shipment.senderName}</div>
                  {shipment.senderPhone && <div style={{ fontSize: 12, color: '#888' }}>📞 {shipment.senderPhone}</div>}
                  {shipment.senderAddress && <div style={{ fontSize: 12, color: '#666', marginTop: 4 }}>{shipment.senderAddress}</div>}
                </div>
              )}
              {shipment.receiverName && (
                <div className="ss-card" style={{ padding: '16px 20px' }}>
                  <div className="kpi-label" style={{ marginBottom: 8 }}>Receiver</div>
                  <div style={{ fontFamily: 'Roboto, sans-serif', fontSize: 14, fontWeight: 700, color: '#fff', marginBottom: 4 }}>{shipment.receiverName}</div>
                  {shipment.receiverPhone && <div style={{ fontSize: 12, color: '#888' }}>📞 {shipment.receiverPhone}</div>}
                  {shipment.receiverAddress && <div style={{ fontSize: 12, color: '#666', marginTop: 4 }}>{shipment.receiverAddress}</div>}
                </div>
              )}
            </div>
          )}

          {/* Tabs */}
          <div className="ss-card" style={{ padding: 0, overflow: 'hidden' }}>
            {/* Tab Bar */}
            <div style={{ display: 'flex', borderBottom: '1px solid rgba(255,255,255,0.08)', padding: '0 20px' }}>
              <button style={tabStyle('timeline')} onClick={() => setActiveTab('timeline')}> Timeline</button>
              <button style={tabStyle('map')} onClick={() => setActiveTab('map')}> Route Map</button>
              <button style={tabStyle('documents')} onClick={() => setActiveTab('documents')}> Documents</button>
              {isDelivered && (
                <button style={tabStyle('delivery')} onClick={() => setActiveTab('delivery')}> Delivery Proof</button>
              )}
            </div>

            {/* Tab Content */}
            <div style={{ padding: 28 }}>

              {/* TIMELINE TAB */}
              {activeTab === 'timeline' && (
                <>
                  {isDelivered && (
                    <div
                      onClick={() => setActiveTab('delivery')}
                      style={{
                        display: 'flex', alignItems: 'center', gap: 12,
                        padding: '12px 16px', marginBottom: 24,
                        background: 'rgba(67,122,34,0.1)',
                        border: '1px solid rgba(67,122,34,0.3)',
                        borderRadius: 4, cursor: 'pointer',
                      }}
                    >
                      <div>
                        <div style={{
                          fontFamily: 'Roboto, sans-serif', fontSize: 11,
                          fontWeight: 700, letterSpacing: '0.1em', textTransform: 'uppercase',
                          color: 'var(--color-success)'
                        }}>
                          Shipment Delivered
                        </div>
                        <div style={{ fontSize: 11, color: '#888', marginTop: 2 }}>
                          Click to view delivery proof →
                        </div>
                      </div>
                    </div>
                  )}
                  <div>
                    <h2 style={{ fontFamily: 'Roboto, sans-serif', fontSize: 12, fontWeight: 700, letterSpacing: '0.1em', textTransform: 'uppercase', color: '#fff', marginBottom: 24 }}>
                      Hub Transition Timeline
                    </h2>
                    {events.length === 0 ? (
                      <div style={{ color: 'var(--color-text-muted)', fontFamily: 'Roboto, sans-serif', textTransform: 'uppercase', fontSize: 11, padding: '20px 0', textAlign: 'center' }}>
                        No tracking events yet.
                      </div>
                    ) : (
                      <div style={{ position: 'relative', paddingLeft: 32 }}>
                        <div style={{ position: 'absolute', left: 5, top: 0, bottom: 0, width: 1, background: 'var(--color-border)' }} />
                        {events.map((evt, i) => {
                          const isActive = i === lastIdx;
                          const isDone = i < lastIdx;
                          return (
                            <div key={i} style={{ position: 'relative', paddingBottom: i < events.length - 1 ? 26 : 0 }}>
                              <div style={{
                                position: 'absolute', left: -32, top: 2, width: 12, height: 12, borderRadius: '50%',
                                background: isActive ? 'var(--color-accent)' : isDone ? 'var(--color-success)' : 'var(--color-surface-2)',
                                border: `2px solid ${isActive ? 'var(--color-accent)' : isDone ? 'var(--color-success)' : '#333'}`,
                                zIndex: 1,
                              }} />
                              <div style={{ display: 'flex', alignItems: 'flex-start', justifyContent: 'space-between', gap: 16 }}>
                                <div>
                                  <div style={{ fontFamily: 'Roboto, sans-serif', fontSize: 14, fontWeight: 700, color: '#fff', letterSpacing: '0.04em', marginBottom: 2 }}>
                                    {evt.hubName}
                                  </div>
                                  <div style={{ fontSize: 12, color: 'var(--color-text-muted)' }}>{evt.description}</div>
                                </div>
                                <div style={{ textAlign: 'right', flexShrink: 0 }}>
                                  <div style={{ fontSize: 12, color: '#d6d6d6', fontFamily: 'Roboto, sans-serif', letterSpacing: '0.08em' }}>
                                    {evt.timestamp ? new Date(evt.timestamp).toLocaleString('en-IN') : ''}
                                  </div>
                                  <span className={`ss-badge ${isActive ? 'glow' : isDone ? 'success' : 'muted'}`} style={{ marginTop: 4, display: 'inline-block' }}>
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

              {/* MAP TAB */}
              {activeTab === 'map' && shipment && (
                <ShipmentRouteMap
                  originCity={shipment.originCity}
                  destinationCity={shipment.destinationCity}
                  originCoords={shipment.senderAddressObj ? { lat: shipment.senderAddressObj.latitude, lng: shipment.senderAddressObj.longitude } : undefined}
                  destCoords={shipment.receiverAddressObj ? { lat: shipment.receiverAddressObj.latitude, lng: shipment.receiverAddressObj.longitude } : undefined}
                  shipmentStatus={shipment.status}
                  stops={shipment.trackingEvents.map((evt, i) => ({
                    label: evt.hubName,
                    timestamp: evt.timestamp,
                    status: evt.status,
                    isActive: i === shipment.trackingEvents.length - 1,
                    isDone: i < shipment.trackingEvents.length - 1,
                  }))}
                  routeData={routeData.map(r => ({
                    hubName: r.hubName,
                    hubCity: r.hubCity,
                    latitude: r.latitude,
                    longitude: r.longitude,
                    isCompleted: r.isCompleted,
                    sequenceOrder: r.sequenceOrder,
                  }))}
                />
              )}

              {/* DOCUMENTS TAB */}
              {activeTab === 'documents' && (
                isAdmin
                  ? <DocumentsReadOnly shipmentId={shipment.numericId} />
                  : <DocumentUpload shipmentId={shipment.numericId} trackingNumber={shipment.trackingNumber} />
              )}

              {/* DELIVERY PROOF TAB */}
              {activeTab === 'delivery' && isDelivered && (
                <DeliveryProofView shipmentId={shipment.numericId} />
              )}
            </div>
          </div>
        </>
      )}
      {/* <ChatWidget shipmentId={shipment?.numericId} /> */}
    </div>
  );
};
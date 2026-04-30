/**
 * ShipmentRouteMap — Leaflet + OpenStreetMap (100% free, no API key)
 * Now supports pre-computed route coordinates from the backend.
 * Falls back to Nominatim geocoding for origin/destination cities if no route data.
 */
import { useEffect, useRef, useState } from 'react';
import { MapContainer, TileLayer, Marker, Polyline, Popup } from 'react-leaflet';
import L from 'leaflet';


/* ── Fix default icon broken by bundlers ── */
// @ts-expect-error internal method
delete L.Icon.Default.prototype._getIconUrl;
L.Icon.Default.mergeOptions({
  iconRetinaUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon-2x.png',
  iconUrl:       'https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon.png',
  shadowUrl:     'https://unpkg.com/leaflet@1.9.4/dist/images/marker-shadow.png',
});

/* ── Icon factory ── */
function makeIcon(color: string, letter: string) {
  return L.divIcon({
    className: '',
    html: `<svg xmlns="http://www.w3.org/2000/svg" width="32" height="42" viewBox="0 0 32 42">
      <ellipse cx="16" cy="39" rx="6" ry="2.5" fill="rgba(0,0,0,0.35)"/>
      <path d="M16 0C9.4 0 4 5.4 4 12c0 9 12 27 12 27S28 21 28 12C28 5.4 22.6 0 16 0z" fill="${color}" stroke="${color}bb" stroke-width="1"/>
      <circle cx="16" cy="12" r="8" fill="white"/>
      <text x="16" y="17" text-anchor="middle" font-size="10" font-weight="bold" fill="${color}" font-family="Arial,sans-serif">${letter}</text>
    </svg>`,
    iconSize:    [32, 42],
    iconAnchor:  [16, 42],
    popupAnchor: [0, -44],
  });
}

const ORIGIN_ICON  = makeIcon('#00c48c', 'O');
const DEST_ICON    = makeIcon('#e0001a', 'D');
const ACTIVE_ICON  = makeIcon('#f5a623', '▶');
const DONE_ICON    = makeIcon('#555555', '✓');
const PLANNED_ICON = makeIcon('#3a3a5c', '•');

export interface RouteStop {
  label: string;
  timestamp?: string;
  status?: string;
  isActive?: boolean;
  isDone?: boolean;
}

export interface RouteHubData {
  hubName: string;
  hubCity: string;
  latitude: number;
  longitude: number;
  isCompleted: boolean;
  sequenceOrder: number;
}

interface Props {
  originCity: string;
  destinationCity: string;
  originCoords?: { lat: number; lng: number };
  destCoords?: { lat: number; lng: number };
  stops?: RouteStop[];
  routeData?: RouteHubData[];
  shipmentStatus?: string;
}

interface GeoPoint {
  lat: number; lng: number;
  label: string; status?: string;
  isActive?: boolean; isDone?: boolean;
  isPlanned?: boolean;
  timestamp?: string;
  isOrigin?: boolean; isDest?: boolean;
}



async function geocodeCity(city: string): Promise<{ lat: number; lng: number } | null> {
  const q = city.includes(',') ? city : city + ', India';
  
  // Try Provider 1: Photon
  try {
    const res = await fetch(`https://photon.komoot.io/api/?q=${encodeURIComponent(q)}&limit=1`);
    if (res.ok) {
      const data = await res.json();
      if (data.features && data.features.length > 0) {
        const c = data.features[0].geometry.coordinates;
        return { lat: c[1], lng: c[0] };
      }
    }
  } catch (e) {
    console.warn("Photon geocode failed, trying Nominatim...");
  }

  // Try Provider 2: Nominatim
  try {
    const res = await fetch(`https://nominatim.openstreetmap.org/search?q=${encodeURIComponent(q)}&format=json&limit=1`, {
      headers: { 'User-Agent': 'SmartShip-Logistics-App' }
    });
    if (res.ok) {
      const data = await res.json();
      if (data && data[0]) {
        return { lat: parseFloat(data[0].lat), lng: parseFloat(data[0].lon) };
      }
    }
  } catch (e) {
    console.error("All geocoders failed for city:", city);
  }

  return null;
}

function pickIcon(pt: GeoPoint): L.DivIcon {
  if (pt.isOrigin) return ORIGIN_ICON;
  if (pt.isDest)   return pt.isActive ? ACTIVE_ICON : DEST_ICON; // Highlight destination if active (delivered)
  if (pt.isActive) return ACTIVE_ICON;
  if (pt.isPlanned) return PLANNED_ICON;
  return DONE_ICON;
}

export const ShipmentRouteMap = ({ originCity, destinationCity, originCoords, destCoords, stops = [], routeData = [], shipmentStatus }: Props) => {
  const mapRef   = useRef<L.Map | null>(null);
  const [points, setPoints]   = useState<GeoPoint[]>([]);
  const [loading, setLoading] = useState(true);
  const [error,   setError]   = useState('');
  const [donePath, setDonePath] = useState<[number, number][]>([]);
  const [todoPath, setTodoPath] = useState<[number, number][]>([]);

  useEffect(() => {
    if (!originCity || !destinationCity) { setLoading(false); return; }
    setLoading(true);
    setError('');

    // If we have route data from the backend, use real coordinates
    if (routeData.length > 0) {
      buildFromRouteData();
    } else {
      buildFromNominatim();
    }

    async function buildFromRouteData() {
      // Use direct coordinates if provided, otherwise fallback to geocoding
      const originGeo = originCoords || await geocodeCity(originCity);
      const destGeo   = destCoords || await geocodeCity(destinationCity);

      const pts: GeoPoint[] = [];
      const isDelivered = shipmentStatus === 'Delivered';

      // Origin
      if (originGeo) {
        pts.push({ ...originGeo, label: originCity, isOrigin: true, isDone: true });
      }

      // Hub stops from route data (using real coordinates)
      const lastCompletedIdx = routeData.reduce((acc, r, i) => r.isCompleted ? i : acc, -1);

      routeData.forEach((hub, idx) => {
        if (hub.latitude === 0 && hub.longitude === 0) return; // skip invalid coords
        const trackingStop = stops.find(s => s.label === hub.hubName);
        
        // If delivered, all hubs are done
        const isDone = isDelivered || hub.isCompleted;
        
        // Active if it's the next uncompleted hub AND we are actually in transit
        const inTransitPhase = !!shipmentStatus && !['Draft', 'Pending', 'PickupScheduled', 'Created', 'Booked'].includes(shipmentStatus);
        const isActive = inTransitPhase && !isDelivered && idx === lastCompletedIdx + 1 && !hub.isCompleted;

        pts.push({
          lat: hub.latitude,
          lng: hub.longitude,
          label: hub.hubName,
          status: trackingStop?.status || (isDone ? 'Completed' : isActive ? 'Current' : 'Planned'),
          timestamp: trackingStop?.timestamp,
          isActive,
          isDone,
          isPlanned: !isDone && !isActive,
        });
      });

      // Destination
      if (destGeo) {
        pts.push({ 
          ...destGeo, 
          label: destinationCity, 
          isDest: true, 
          isActive: isDelivered,
          isDone: isDelivered 
        });
      }

      setPoints(pts);
      fitMap(pts);
      setLoading(false);
    }

    async function buildFromNominatim() {
      // Legacy: geocode all hub names via Nominatim
      const hubNames  = stops.map(s => s.label).filter(Boolean);
      const allNames  = [originCity, ...hubNames, destinationCity];
      const uniqNames = Array.from(new Set(allNames));

      const promises = uniqNames.map((name, i) =>
        new Promise<{ name: string; geo: { lat: number; lng: number } | null }>(resolve =>
          setTimeout(async () => {
            const geo = await geocodeCity(name);
            resolve({ name, geo });
          }, i * 350)
        )
      );

      Promise.all(promises).then(results => {
        const geoMap = new Map(results.map(r => [r.name, r.geo]));
        const pts: GeoPoint[] = [];

        uniqNames.forEach((name, idx) => {
          const geo = geoMap.get(name);
          if (!geo) return;

          const isOrigin = idx === 0;
          const isDest   = idx === uniqNames.length - 1 && !isOrigin;
          const stop     = stops.find(s => s.label === name);

          pts.push({
            ...geo,
            label:     name,
            status:    stop?.status,
            timestamp: stop?.timestamp,
            isActive:  stop?.isActive,
            isDone:    stop?.isDone,
            isOrigin,
            isDest,
          });
        });

        setPoints(pts);
        fitMap(pts);
        setLoading(false);
      });
    }

    function fitMap(pts: GeoPoint[]) {
      if (pts.length > 1 && mapRef.current) {
        const bounds = L.latLngBounds(pts.map(p => [p.lat, p.lng]));
        mapRef.current.fitBounds(bounds, { padding: [40, 40] });
      }
    }
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [originCity, destinationCity, stops.length, routeData.length]);

  useEffect(() => {
    if (points.length < 2) { setDonePath([]); setTodoPath([]); return; }
    
    async function updateRoads() {
       const pivotIdx = points.findIndex(p => p.isActive);
       const splitAt = pivotIdx !== -1 ? pivotIdx : points.reduce((acc, p, i) => (p.isDone || p.isOrigin) ? i : acc, 0);
       
       const traveled = points.slice(0, splitAt + 1);
       const upcoming = points.slice(splitAt);
       
       if (traveled.length >= 2) setDonePath(await getOSRMPath(traveled));
       else setDonePath([]);
       
       if (upcoming.length >= 2) setTodoPath(await getOSRMPath(upcoming));
       else setTodoPath([]);
    }
    updateRoads();
  }, [points]);

  async function getOSRMPath(coords: { lat: number; lng: number }[]): Promise<[number, number][]> {
    try {
      const q = coords.map(c => `${c.lng},${c.lat}`).join(';');
      const res = await fetch(`https://router.project-osrm.org/route/v1/driving/${q}?overview=full&geometries=geojson`);
      const data = await res.json();
      if (data.code === 'Ok') return data.routes[0].geometry.coordinates.map((c: any) => [c[1], c[0]]);
    } catch {}
    return coords.map(c => [c.lat, c.lng]);
  }

  if (loading) {
    return (
      <div style={{
        height: 420, display: 'flex', flexDirection: 'column',
        alignItems: 'center', justifyContent: 'center',
        background: '#0d0d1a', borderRadius: 8,
        border: '1px solid rgba(224,0,26,0.2)', gap: 14,
      }}>
        <div style={{
          width: 40, height: 40, borderRadius: '50%',
          border: '3px solid rgba(224,0,26,0.15)',
          borderTopColor: '#e0001a',
          animation: 'spin 0.8s linear infinite',
        }} />
        <span style={{
          fontFamily: 'Orbitron, monospace', fontSize: 11,
          color: '#555', letterSpacing: '0.14em',
        }}>
          PLOTTING ROUTE…
        </span>
      </div>
    );
  }

  if (error) {
    return (
      <div style={{ padding: 16, color: '#ff6b6b', fontSize: 12, border: '1px solid rgba(224,0,26,0.3)', borderRadius: 8 }}>
        ⚠ {error}
      </div>
    );
  }

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
      {/* Header */}
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', flexWrap: 'wrap', gap: 8 }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
          <span style={{
            fontFamily: 'Orbitron, monospace', fontSize: 12, fontWeight: 700,
            letterSpacing: '0.12em', textTransform: 'uppercase', color: '#fff',
          }}>
            Live Shipment Route
          </span>
          {routeData.length > 0 && (
            <span style={{ fontSize: 9, padding: '2px 6px', borderRadius: 3, background: 'rgba(0,196,140,0.15)', color: '#00c48c', fontFamily: 'Orbitron, monospace', letterSpacing: '0.1em' }}>
              AUTO-ROUTED
            </span>
          )}
        </div>
        <div style={{ display: 'flex', gap: 16, fontSize: 11, color: '#888', fontFamily: 'Inter, sans-serif' }}>
          <span><span style={{ color: '#00c48c' }}>●</span> Origin</span>
          <span><span style={{ color: '#555' }}>●</span> Done</span>
          <span><span style={{ color: '#f5a623' }}>●</span> Current Hub</span>
          <span><span style={{ color: '#3a3a5c' }}>●</span> Planned</span>
          <span><span style={{ color: '#e0001a' }}>●</span> Destination</span>
        </div>
      </div>

      {/* Map Container */}
      <div style={{
        height: 500, borderRadius: 12, overflow: 'hidden',
        border: '1px solid rgba(224,0,26,0.2)',
        boxShadow: '0 8px 32px rgba(0,0,0,0.4)',
        position: 'relative'
      }}>
        {loading && (
          <div style={{
            position: 'absolute', inset: 0, zIndex: 1000,
            background: 'rgba(10,10,10,0.7)', backdropFilter: 'blur(4px)',
            display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', gap: 12
          }}>
            <div className="spinner" style={{ width: 40, height: 40, border: '3px solid rgba(224,0,26,0.1)', borderTopColor: '#e0001a', borderRadius: '50%', animation: 'spin 1s linear infinite' }} />
            <div style={{ fontFamily: 'Orbitron, monospace', fontSize: 12, color: '#e0001a', letterSpacing: '0.1em' }}>Calculating Road Network...</div>
          </div>
        )}

        <MapContainer
          center={points.length > 0 ? [points[0].lat, points[0].lng] : [20.5937, 78.9629]}
          zoom={5}
          style={{ height: '100%', width: '100%' }}
          ref={mapRef}
          scrollWheelZoom
        >
          <TileLayer
            url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
            attribution='&copy; <a href="https://openstreetmap.org/copyright">OpenStreetMap</a>'
            className="map-tiles"
          />

          {/* Traveled and Upcoming Path - Only show when in transit */}
          {shipmentStatus && !['Draft', 'Pending', 'PickupScheduled', 'Created'].includes(shipmentStatus) && (
            <>
              {/* Traveled Path (Grey) */}
              {donePath.length > 1 && (
                <Polyline
                  positions={donePath}
                  pathOptions={{
                    color: '#6b7280', // Cool Grey
                    weight: 4,
                    opacity: 0.6,
                    lineJoin: 'round'
                  }}
                />
              )}

              {/* Upcoming Path (Professional Solid Blue) */}
              {todoPath.length > 1 && (
                <Polyline
                  positions={todoPath}
                  pathOptions={{
                    color: '#3b82f6', // Premium Blue
                    weight: 5,
                    opacity: 0.9,
                    lineJoin: 'round'
                  }}
                />
              )}
            </>
          )}

          {/* Hub markers */}
          {points.map((pt, idx) => (
            <Marker key={idx} position={[pt.lat, pt.lng]} icon={pickIcon(pt)}>
              <Popup>
                <div style={{ fontFamily: 'Inter, sans-serif', minWidth: 160 }}>
                  <div style={{ fontWeight: 700, fontSize: 13, marginBottom: 4, color: '#111' }}>
                    {pt.label}
                  </div>
                  {pt.isOrigin && <div style={{ fontSize: 11, color: '#00a070' }}>Origin</div>}
                  {pt.isDest   && <div style={{ fontSize: 11, color: '#c00' }}>Destination</div>}
                  {pt.isPlanned && <div style={{ fontSize: 11, color: '#888' }}>Upcoming Hub</div>}
                  {pt.status   && (
                    <div style={{
                      marginTop: 5, display: 'inline-block',
                      padding: '2px 8px', borderRadius: 4, fontSize: 10, fontWeight: 700,
                      background: pt.isActive ? '#f5a623' : pt.isDone ? '#555' : pt.isPlanned ? '#3a3a5c' : '#ddd',
                      color: '#fff',
                    }}>
                      {pt.status.toUpperCase()}
                    </div>
                  )}
                  {pt.timestamp && (
                    <div style={{ marginTop: 4, fontSize: 10, color: '#666' }}>
                      {new Date(pt.timestamp).toLocaleString('en-IN')}
                    </div>
                  )}
                </div>
              </Popup>
            </Marker>
          ))}
        </MapContainer>
      </div>

      {/* Route breadcrumb */}
      {points.length >= 2 && (
        <div style={{
          display: 'flex', alignItems: 'center', gap: 6, flexWrap: 'wrap',
          padding: '10px 16px',
          background: 'rgba(224,0,26,0.05)',
          border: '1px solid rgba(224,0,26,0.12)',
          borderRadius: 6, fontSize: 15,
          fontFamily: 'Inter, sans-serif', color: '#aaa',
        }}>
          {points.map((p, idx) => (
            <span key={idx} style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
              {idx > 0 && <span style={{ color: '#ffffffff', fontSize: 17 }}>→</span>}
              <span style={{
                color: p.isOrigin ? '#00c48c' : p.isDest ? '#e0001a' : p.isActive ? '#f5a623' : p.isDone ? '#555' : '#3a3a5c',
                fontWeight: p.isOrigin || p.isDest ? 700 : 400,
              }}>
                {p.label}
              </span>
            </span>
          ))}
        </div>
      )}

      {points.length === 0 && !loading && (
        <div style={{ fontSize: 12, color: '#555', textAlign: 'center', padding: 12, fontFamily: 'Inter, sans-serif' }}>
          Route map unavailable — city names could not be resolved.
        </div>
      )}
    </div>
  );
};

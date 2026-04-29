/**
 * ShipmentRouteMap — Leaflet + OpenStreetMap (100% free, no API key)
 * Geocoding : Nominatim reverse geocode for city → lat/lng
 * Tiles     : OpenStreetMap  (dark-mode via CSS filter)
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

export interface RouteStop {
  label: string;
  timestamp?: string;
  status?: string;
  isActive?: boolean;
  isDone?: boolean;
}

interface Props {
  originCity: string;
  destinationCity: string;
  stops?: RouteStop[];
}

interface GeoPoint {
  lat: number; lng: number;
  label: string; status?: string;
  isActive?: boolean; isDone?: boolean;
  timestamp?: string;
  isOrigin?: boolean; isDest?: boolean;
}

const NOM_HEADERS = { 'Accept-Language': 'en', 'User-Agent': 'SmartShip-App/1.0' };

async function geocodeCity(city: string): Promise<{ lat: number; lng: number } | null> {
  try {
    const url = `https://nominatim.openstreetmap.org/search?q=${encodeURIComponent(city + ', India')}&format=json&limit=1`;
    const res  = await fetch(url, { headers: NOM_HEADERS });
    const data = await res.json();
    if (!data?.[0]) return null;
    return { lat: parseFloat(data[0].lat), lng: parseFloat(data[0].lon) };
  } catch { return null; }
}

function pickIcon(pt: GeoPoint): L.DivIcon {
  if (pt.isOrigin) return ORIGIN_ICON;
  if (pt.isDest)   return DEST_ICON;
  if (pt.isActive) return ACTIVE_ICON;
  return DONE_ICON;
}

export const ShipmentRouteMap = ({ originCity, destinationCity, stops = [] }: Props) => {
  const mapRef   = useRef<L.Map | null>(null);
  const [points, setPoints]   = useState<GeoPoint[]>([]);
  const [loading, setLoading] = useState(true);
  const [error,   setError]   = useState('');

  useEffect(() => {
    if (!originCity || !destinationCity) { setLoading(false); return; }
    setLoading(true);
    setError('');

    // Collect unique city names to geocode
    const hubNames  = stops.map(s => s.label).filter(Boolean);
    const allNames  = [originCity, ...hubNames, destinationCity];
    const uniqNames = Array.from(new Set(allNames));

    // Stagger Nominatim requests (1 req/s policy)
    const promises = uniqNames.map((name, i) =>
      new Promise<{ name: string; geo: { lat: number; lng: number } | null }>(resolve =>
        setTimeout(async () => {
          const geo = await geocodeCity(name);
          resolve({ name, geo });
        }, i * 350)   // 350ms between each = safe for Nominatim
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

      if (pts.length > 1 && mapRef.current) {
        const bounds = L.latLngBounds(pts.map(p => [p.lat, p.lng]));
        mapRef.current.fitBounds(bounds, { padding: [40, 40] });
      }
      setLoading(false);
    });
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [originCity, destinationCity, stops.length]);

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

  const polyline = points.map(p => [p.lat, p.lng] as [number, number]);

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
        </div>
        <div style={{ display: 'flex', gap: 16, fontSize: 11, color: '#888', fontFamily: 'Inter, sans-serif' }}>
          <span><span style={{ color: '#00c48c' }}>●</span> Origin</span>
          <span><span style={{ color: '#f5a623' }}>●</span> Current Hub</span>
          <span><span style={{ color: '#555' }}>●</span> Done</span>
          <span><span style={{ color: '#e0001a' }}>●</span> Destination</span>
        </div>
      </div>

      {/* Map */}
      <div style={{
        height: 420, borderRadius: 8, overflow: 'hidden',
        border: '1px solid rgba(224,0,26,0.2)',
        filter: 'brightness(0.82) saturate(0.65) hue-rotate(180deg)',
      }}>
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
            maxZoom={18}
          />

          {/* Route polyline */}
          {polyline.length > 1 && (
            <Polyline
              positions={polyline}
              pathOptions={{
                color: '#e0001a',
                weight: 3,
                opacity: 0.75,
                dashArray: '10, 8',
              }}
            />
          )}

          {/* Hub markers */}
          {points.map((pt, idx) => (
            <Marker key={idx} position={[pt.lat, pt.lng]} icon={pickIcon(pt)}>
              <Popup>
                <div style={{ fontFamily: 'Inter, sans-serif', minWidth: 160 }}>
                  <div style={{ fontWeight: 700, fontSize: 13, marginBottom: 4, color: '#111' }}>
                    {pt.label}
                  </div>
                  {pt.isOrigin && <div style={{ fontSize: 11, color: '#00a070' }}>📦 Origin</div>}
                  {pt.isDest   && <div style={{ fontSize: 11, color: '#c00' }}>🏁 Destination</div>}
                  {pt.status   && (
                    <div style={{
                      marginTop: 5, display: 'inline-block',
                      padding: '2px 8px', borderRadius: 4, fontSize: 10, fontWeight: 700,
                      background: pt.isActive ? '#f5a623' : pt.isDone ? '#555' : '#ddd',
                      color: '#fff',
                    }}>
                      {pt.status.toUpperCase()}
                    </div>
                  )}
                  {pt.timestamp && (
                    <div style={{ marginTop: 4, fontSize: 10, color: '#666' }}>
                      🕐 {new Date(pt.timestamp).toLocaleString('en-IN')}
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
                color: p.isOrigin ? '#00c48c' : p.isDest ? '#e0001a' : p.isActive ? '#f5a623' : '#555',
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

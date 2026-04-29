
import { useCallback, useEffect, useRef, useState } from 'react';
import { MapContainer, TileLayer, Marker, useMapEvents } from 'react-leaflet';
import L from 'leaflet';


/* ── Fix Leaflet default marker icons broken by bundlers ── */
// @ts-expect-error – _getIconUrl is an internal Leaflet method
delete L.Icon.Default.prototype._getIconUrl;
L.Icon.Default.mergeOptions({
  iconRetinaUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon-2x.png',
  iconUrl:       'https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon.png',
  shadowUrl:     'https://unpkg.com/leaflet@1.9.4/dist/images/marker-shadow.png',
});

/* ── Custom red pin icon ── */
const redPin = L.divIcon({
  className: '',
  html: `<svg xmlns="http://www.w3.org/2000/svg" width="28" height="38" viewBox="0 0 28 38">
    <ellipse cx="14" cy="35" rx="5" ry="2.5" fill="rgba(0,0,0,0.3)"/>
    <path d="M14 0C7.4 0 2 5.4 2 12c0 9 12 26 12 26S26 21 26 12C26 5.4 20.6 0 14 0z" fill="#e0001a" stroke="#a00012" stroke-width="1"/>
    <circle cx="14" cy="12" r="5.5" fill="white"/>
    <circle cx="14" cy="12" r="3" fill="#e0001a"/>
  </svg>`,
  iconSize:   [28, 38],
  iconAnchor: [14, 38],
  popupAnchor:[0, -38],
});

/* ── Types ── */
export interface PickedLocation {
  lat: number; lng: number;
  street: string; city: string; state: string;
  postalCode: string; country: string; formattedAddress: string;
}

interface NominatimResult {
  lat: string; lon: string; display_name: string;
  address: {
    road?: string; house_number?: string;
    suburb?: string; village?: string; town?: string; city?: string;
    county?: string; state?: string; postcode?: string; country?: string;
  };
}

interface Props {
  label: string;
  onPick: (loc: PickedLocation) => void;
  defaultCenter?: [number, number];
}

/* ── Nominatim helpers ── */
const NOM_HEADERS = { 'Accept-Language': 'en', 'User-Agent': 'SmartShip-App/1.0' };

async function reverseGeocode(lat: number, lng: number): Promise<PickedLocation> {
  const url = `https://nominatim.openstreetmap.org/reverse?lat=${lat}&lon=${lng}&format=json&addressdetails=1`;
  const res = await fetch(url, { headers: NOM_HEADERS });
  const data: NominatimResult = await res.json();
  const a = data.address;
  const streetNum = a.house_number ?? '';
  const road      = a.road ?? '';
  const city      = a.city ?? a.town ?? a.village ?? a.suburb ?? a.county ?? '';
  return {
    lat, lng,
    street:           [streetNum, road].filter(Boolean).join(' ') || data.display_name.split(',')[0],
    city,
    state:            a.state ?? '',
    postalCode:       a.postcode ?? '',
    country:          a.country ?? '',
    formattedAddress: data.display_name,
  };
}

async function searchAddress(query: string): Promise<NominatimResult[]> {
  const url = `https://nominatim.openstreetmap.org/search?q=${encodeURIComponent(query)}&format=json&addressdetails=1&limit=5&countrycodes=in`;
  const res = await fetch(url, { headers: NOM_HEADERS });
  return res.json();
}

/* ── Inner click handler (must be inside MapContainer) ── */
function ClickHandler({ onMapClick }: { onMapClick: (lat: number, lng: number) => void }) {
  useMapEvents({
    click(e) { onMapClick(e.latlng.lat, e.latlng.lng); },
  });
  return null;
}

/* ── Main component ── */
export const MapLocationPicker = ({ label, onPick, defaultCenter }: Props) => {
  const center: [number, number] = defaultCenter ?? [20.5937, 78.9629];
  const [markerPos,  setMarkerPos]  = useState<[number, number] | null>(null);
  const [pickedAddr, setPickedAddr] = useState('');
  const [resolving,  setResolving]  = useState(false);
  const [searchQ,    setSearchQ]    = useState('');
  const [results,    setResults]    = useState<NominatimResult[]>([]);
  const [searching,  setSearching]  = useState(false);
  const [showDrop,   setShowDrop]   = useState(false);
  const mapRef = useRef<L.Map | null>(null);
  const searchTimer = useRef<ReturnType<typeof setTimeout> | undefined>(undefined);

  /* debounce search */
  useEffect(() => {
    clearTimeout(searchTimer.current);
    if (searchQ.trim().length < 3) { setResults([]); setShowDrop(false); return; }
    searchTimer.current = setTimeout(async () => {
      setSearching(true);
      try {
        const res = await searchAddress(searchQ);
        setResults(res);
        setShowDrop(res.length > 0);
      } finally { setSearching(false); }
    }, 500);
  }, [searchQ]);

  const handlePin = useCallback(async (lat: number, lng: number) => {
    setMarkerPos([lat, lng]);
    setResolving(true);
    try {
      const geo = await reverseGeocode(lat, lng);
      setPickedAddr(geo.formattedAddress);
      onPick(geo);
    } catch { setPickedAddr(`${lat.toFixed(5)}, ${lng.toFixed(5)}`); }
    finally   { setResolving(false); }
  }, [onPick]);

  const handleResultClick = async (r: NominatimResult) => {
    const lat = parseFloat(r.lat);
    const lng = parseFloat(r.lon);
    setShowDrop(false);
    setSearchQ(r.display_name.split(',').slice(0, 2).join(','));
    if (mapRef.current) { mapRef.current.setView([lat, lng], 15); }
    await handlePin(lat, lng);
  };

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 10 }}>
      {/* Title */}
      <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
        <span style={{ fontSize: 15 }}>📍</span>
        <span style={{
          fontFamily: 'Orbitron, monospace', fontSize: 11, fontWeight: 700,
          letterSpacing: '0.14em', textTransform: 'uppercase', color: '#e0001a',
        }}>
          {label} — click map or search
        </span>
      </div>

      {/* Search box */}
      <div style={{ position: 'relative' }}>
        <input
          type="text"
          value={searchQ}
          onChange={e => setSearchQ(e.target.value)}
          onFocus={() => results.length > 0 && setShowDrop(true)}
          placeholder="  Search city, landmark or address…"
          className="ss-input"
          style={{ width: '100%', fontSize: 13, padding: '10px 14px', boxSizing: 'border-box' }}
        />
        {searching && (
          <span style={{ position: 'absolute', right: 12, top: '50%', transform: 'translateY(-50%)', fontSize: 12, color: '#888' }}>
            ⏳
          </span>
        )}

        {/* Dropdown */}
        {showDrop && (
          <div style={{
            position: 'absolute', top: '100%', left: 0, right: 0, zIndex: 9999,
            background: '#1a1a1a', border: '1px solid rgba(224,0,26,0.3)',
            borderRadius: 6, marginTop: 2, maxHeight: 220, overflowY: 'auto',
            boxShadow: '0 8px 24px rgba(0,0,0,0.6)',
          }}>
            {results.map((r, i) => (
              <div
                key={i}
                onClick={() => handleResultClick(r)}
                style={{
                  padding: '9px 14px', cursor: 'pointer', fontSize: 12,
                  fontFamily: 'Inter, sans-serif', color: '#ccc',
                  borderBottom: i < results.length - 1 ? '1px solid rgba(255,255,255,0.06)' : 'none',
                  transition: 'background 0.12s',
                }}
                onMouseEnter={e => (e.currentTarget.style.background = 'rgba(224,0,26,0.12)')}
                onMouseLeave={e => (e.currentTarget.style.background = 'transparent')}
              >
                <span style={{ color: '#e0001a', marginRight: 6 }}>📍</span>
                {r.display_name.length > 80 ? r.display_name.slice(0, 80) + '…' : r.display_name}
              </div>
            ))}
          </div>
        )}
      </div>

      {/* Map */}
      <div style={{
        height: 340, borderRadius: 8, overflow: 'hidden',
        border: '1px solid rgba(224,0,26,0.25)',
        /* Dark-mode map via CSS filter */
        filter: 'brightness(0.85) saturate(0.7) hue-rotate(180deg)',
      }}>
        <MapContainer
          center={markerPos ?? center}
          zoom={markerPos ? 15 : 5}
          style={{ height: '100%', width: '100%' }}
          ref={mapRef}
          scrollWheelZoom
        >
          <TileLayer
            url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
            attribution='&copy; <a href="https://openstreetmap.org/copyright">OpenStreetMap</a>'
            maxZoom={19}
          />
          <ClickHandler onMapClick={handlePin} />
          {markerPos && (
            <Marker
              position={markerPos}
              icon={redPin}
              draggable
              eventHandlers={{
                dragend(e) {
                  const pos = (e.target as L.Marker).getLatLng();
                  handlePin(pos.lat, pos.lng);
                },
              }}
            />
          )}
        </MapContainer>
      </div>

      {/* Picked address pill */}
      {(resolving || pickedAddr) && (
        <div style={{
          display: 'flex', alignItems: 'center', gap: 8,
          padding: '8px 14px',
          background: 'rgba(0,196,140,0.08)',
          border: '1px solid rgba(0,196,140,0.25)',
          borderRadius: 6, fontSize: 12, color: '#99e6b3',
          fontFamily: 'Inter, sans-serif',
        }}>
          {resolving
            ? <><span style={{ display: 'inline-block', animation: 'spin 1s linear infinite' }}>⏳</span> Resolving address…</>
            : <><span>✅</span><strong style={{ wordBreak: 'break-word' }}>{pickedAddr}</strong></>
          }
        </div>
      )}

      {!markerPos && !pickedAddr && (
        <div style={{ fontSize: 11, color: '#555', textAlign: 'center', fontFamily: 'Inter, sans-serif' }}>
          Click anywhere on the map or search above to pin your location
        </div>
      )}
    </div>
  );
};

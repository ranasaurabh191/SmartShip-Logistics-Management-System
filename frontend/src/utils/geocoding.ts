
export interface GeocodeResult {
  lat: number;
  lng: number;
  displayName: string;
}

export async function geocodeAddress(city: string, state: string, country: string): Promise<GeocodeResult | null> {
  const query = [city, state, country].filter(Boolean).join(', ');
  if (!query) return null;

  try {
    // Try Photon (Komoot)
    const photonRes = await fetch(`https://photon.komoot.io/api/?q=${encodeURIComponent(query)}&limit=1`);
    if (photonRes.ok) {
      const data = await photonRes.json();
      if (data.features && data.features.length > 0) {
        const [lng, lat] = data.features[0].geometry.coordinates;
        return { lat, lng, displayName: data.features[0].properties.name };
      }
    }
  } catch (e) {
    console.warn("Photon geocoding failed", e);
  }

  try {
    // Try Nominatim
    const nominatimRes = await fetch(`https://nominatim.openstreetmap.org/search?format=json&q=${encodeURIComponent(query)}&limit=1`, {
      headers: { 'User-Agent': 'SmartShip-Logistics-Management-System' }
    });
    if (nominatimRes.ok) {
      const data = await nominatimRes.json();
      if (data && data.length > 0) {
        return { 
          lat: parseFloat(data[0].lat), 
          lng: parseFloat(data[0].lon), 
          displayName: data[0].display_name 
        };
      }
    }
  } catch (e) {
    console.warn("Nominatim geocoding failed", e);
  }

  return null;
}

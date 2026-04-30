
export interface CostBreakdown {
  baseRate: number;
  fuelSurcharge: number;
  handlingCharge: number;
  fragileCharge: number;
  distSurcharge: number;
  codFee: number;
  subtotal: number;
  cgst: number;
  sgst: number;
  igst: number;
  totalGst: number;
  grandTotal: number;
  isInternational: boolean;
}

export interface CostInput {
  baseRate: number;
  shipmentType: string;
  paymentMode: 'COD' | 'ONLINE' | null;
  fragile: boolean;
  distanceKm: number;
}

export function calculateCosts(input: CostInput): CostBreakdown {
  const { baseRate, shipmentType, paymentMode, fragile, distanceKm } = input;
  const isInternational = shipmentType === 'International';

  const fuelSurcharge   = parseFloat((baseRate * 0.05).toFixed(2));   
  const handlingCharge  = isInternational ? 120 : 50;                
  const fragileCharge   = fragile ? 80 : 0;
  const distSurcharge   = distanceKm > 500 ? parseFloat(((distanceKm - 500) * 2).toFixed(2)) : 0;
  const codFee          = paymentMode === 'COD'
    ? parseFloat((baseRate * 0.015).toFixed(2))
    : 0;                                                               

  const subtotal = baseRate + fuelSurcharge + handlingCharge + fragileCharge + distSurcharge + codFee;

  const gstBase = subtotal;
  const cgst  = isInternational ? 0 : parseFloat((gstBase * 0.09).toFixed(2));
  const sgst  = isInternational ? 0 : parseFloat((gstBase * 0.09).toFixed(2));
  const igst  = isInternational ? parseFloat((gstBase * 0.18).toFixed(2)) : 0;
  const totalGst = cgst + sgst + igst;

  const grandTotal = parseFloat((subtotal + totalGst).toFixed(2));

  return {
    baseRate, fuelSurcharge, handlingCharge, fragileCharge, distSurcharge,
    codFee, subtotal, cgst, sgst, igst, totalGst, grandTotal, isInternational,
  };
}

export function fmtINR(n: number) {
  return `₹${n.toLocaleString('en-IN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
}
// src/features/shipment/InvoicePrintView.tsx
import React, { useRef } from 'react';
import { useReactToPrint } from 'react-to-print';
import jsPDF from 'jspdf';
import html2canvas from 'html2canvas';
import { SmartShipLogo } from '../../shared/components/Logo'; 
import type { CostBreakdown } from '../../utils/costCalculator';
import { fmtINR } from '../../utils/costCalculator';

interface AddressBlock {
  fullName: string; phone: string; street: string;
  city: string; state: string; postalCode: string; country: string;
}

export interface InvoiceData {
  invoiceNumber: string;
  invoiceDate: string;
  trackingNumber: string;
  shipmentId: number;
  status: string;
  shipmentType: string;
  paymentMethod: string;
  paymentStatus: string;
  sender: AddressBlock;
  receiver: AddressBlock;
  package: {
    weightKg: number; lengthCm: number;
    widthCm: number; heightCm: number; description: string;
  };
  costs: CostBreakdown;
  razorpayOrderId?: string | null;
  razorpayPaymentId?: string | null;
  notes?: string;
}

interface Props {
  data: InvoiceData;
  onClose: () => void;
  onDownloaded: (file: File) => void;
  onFileReady?: (file: File) => void;
}

/* ─── Inline styles so the printed/PDF output is self-contained ─── */
const S = {
  page: {
    width: 794,
    minHeight: 1123,
    background: '#fff',
    color: '#111',
    fontFamily: "'Inter', 'Roboto', sans-serif",
    fontSize: 12,
    padding: '40px 48px',
    position: 'relative' as const,
  },
  // header band
  headerBand: {
    display: 'flex',
    backgroundColor: '#412828ff',
    justifyContent: 'space-between',
    alignItems: 'flex-start',
    borderBottom: '3px solid #e0001a',
    paddingBottom: 20,
    marginBottom: 24,
    padding: "10px 10px",
    margin: 0,
  },
  invoiceTitle: {
    fontFamily: "'Orbitron', monospace",
    fontSize: 22,
    fontWeight: 900,
    color: '#e0001a',
    letterSpacing: '0.06em',
    lineHeight: 1,
  },
  invoiceSub: { fontSize: 11, color: '#ffffffff', marginTop: 4, letterSpacing: '0.08em', textTransform: 'uppercase' as const },
  metaBlock: { textAlign: 'right' as const, fontSize: 12 },
  metaRow: { color: '#ffffff', marginBottom: 3 },
  metaVal: { fontWeight: 700, color: '#b7b7b7ff' },
  // addresses
  addressGrid: {
    display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 24,
    marginBottom: 28, background: '#fafafa',
    border: '1px solid #eee', borderRadius: 4, padding: 20,
  },
  addrTitle: {
    fontFamily: "'Orbitron', monospace", fontSize: 10, fontWeight: 700,
    letterSpacing: '0.14em', textTransform: 'uppercase' as const,
    color: '#e0001a', marginBottom: 8, borderBottom: '1px solid #e0001a',
    paddingBottom: 4,
  },
  addrName: { fontWeight: 700, fontSize: 13, marginBottom: 2 },
  addrLine: { color: '#444', lineHeight: 1.6, fontSize: 11 },
  // table
  table: { width: '100%', borderCollapse: 'collapse' as const, marginBottom: 0 },
  th: {
    fontFamily: "'Orbitron', monospace", fontSize: 10, fontWeight: 700,
    letterSpacing: '0.12em', textTransform: 'uppercase' as const,
    color: '#fff', background: '#1a1a1a', padding: '10px 12px',
    textAlign: 'left' as const, borderBottom: '2px solid #e0001a',
  },
  td: {
    padding: '9px 12px', fontSize: 12, color: '#222',
    borderBottom: '1px solid #eee', verticalAlign: 'middle' as const,
  },
  tdRight: { textAlign: 'right' as const },
  // cost summary
  costBox: {
    display: 'grid', gridTemplateColumns: '1fr 280px', gap: 24,
    marginTop: 0,
  },
  pkgInfo: {
    background: '#fafafa', border: '1px solid #eee',
    borderRadius: 4, padding: 16,
  },
  pkgTitle: {
    fontFamily: "'Orbitron', monospace", fontSize: 10, fontWeight: 700,
    letterSpacing: '0.14em', textTransform: 'uppercase' as const,
    color: '#e0001a', marginBottom: 10,
  },
  pkgRow: { display: 'flex', justifyContent: 'space-between', marginBottom: 5, fontSize: 11 },
  pkgKey: { color: '#888', textTransform: 'uppercase' as const, letterSpacing: '0.06em' },
  pkgVal: { fontWeight: 600, color: '#222' },
  totals: {
    border: '1px solid #eee', borderRadius: 4, overflow: 'hidden',
  },
  totalRow: {
    display: 'flex', justifyContent: 'space-between',
    padding: '7px 14px', fontSize: 12, borderBottom: '1px solid #f0f0f0',
  },
  totalKey: { color: '#555' },
  totalVal: { fontWeight: 600, color: '#222' },
  grandRow: {
    display: 'flex', justifyContent: 'space-between',
    padding: '12px 14px', background: '#e0001a',
  },
  grandKey: { fontFamily: "'Orbitron', monospace", fontWeight: 800, fontSize: 13, color: '#fff', letterSpacing: '0.06em' },
  grandVal: { fontFamily: "'Orbitron', monospace", fontWeight: 900, fontSize: 16, color: '#fff' },
  // status band
  statusBand: {
    display: 'flex', gap: 24, padding: '14px 20px',
    background: '#f8f8f8', border: '1px solid #eee',
    borderRadius: 4, marginTop: 24, flexWrap: 'wrap' as const,
  },
  statusItem: { display: 'flex', flexDirection: 'column' as const, gap: 2 },
  statusLabel: { fontSize: 10, color: '#888', textTransform: 'uppercase' as const, letterSpacing: '0.1em' },
  statusVal: { fontWeight: 700, color: '#111', fontSize: 12 },
  // footer
  footer: {
    marginTop: 32, borderTop: '1px solid #ddd', paddingTop: 14,
    display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start',
    fontSize: 10, color: '#aaa',
  },
  footerLeft: { maxWidth: 380, lineHeight: 1.7 },
  footerRight: { textAlign: 'right' as const, lineHeight: 1.7 },
  watermark: {
    position: 'absolute' as const, top: '50%', left: '50%',
    transform: 'translate(-50%,-50%) rotate(-30deg)',
    fontFamily: "'Orbitron', monospace", fontSize: 72, fontWeight: 900,
    color: 'rgba(224,0,26,0.04)', letterSpacing: '0.1em',
    pointerEvents: 'none' as const, userSelect: 'none' as const, whiteSpace: 'nowrap' as const,
  },
};

export const InvoiceDocument = React.forwardRef<HTMLDivElement, { data: InvoiceData }>(
  ({ data }, ref) => {
    const { costs } = data;
    const isDomestic = !costs.isInternational;

    const lineItems = [
      { desc: 'Base Shipping Rate', hsn: '996812', qty: 1, unit: fmtINR(costs.baseRate), amount: costs.baseRate },
      { desc: 'Fuel Surcharge (5%)', hsn: '996812', qty: 1, unit: fmtINR(costs.fuelSurcharge), amount: costs.fuelSurcharge },
      { desc: `Handling Charges (${costs.isInternational ? 'International' : 'Domestic'})`, hsn: '996812', qty: 1, unit: fmtINR(costs.handlingCharge), amount: costs.handlingCharge },
      ...(costs.fragileCharge > 0 ? [{ desc: 'Fragile / Special Handling', hsn: '996812', qty: 1, unit: fmtINR(costs.fragileCharge), amount: costs.fragileCharge }] : []),
      ...(costs.codFee > 0 ? [{ desc: 'COD Service Fee (1.5%)', hsn: '996812', qty: 1, unit: fmtINR(costs.codFee), amount: costs.codFee }] : []),
    ];

    return (
      <div ref={ref} style={S.page}>
        <div style={S.watermark}>SMARTSHIP</div>

        {/* ── Header ── */}
        <div style={S.headerBand}>
          <div>
            <SmartShipLogo/>
            <div style={S.invoiceSub}>Tax Invoice / Shipping Receipt</div>
            <div style={{ fontSize: 11, color: '#ffffffff', marginTop: 8 }}>
              SmartShip Logistics Pvt. Ltd.<br />
              GSTIN: 07AABCS1234A1Z5 &nbsp;|&nbsp; CIN: U63090DL2020PTC123456<br />
              support@smartship.in &nbsp;|&nbsp; +91-98765-43210
            </div>
          </div>
          <div style={S.metaBlock}>
            <div style={S.invoiceTitle}>{data.invoiceNumber}</div>
            <div style={{ ...S.metaRow, marginTop: 10 }}>
              <span>Invoice Date: </span><span style={S.metaVal}>{data.invoiceDate}</span>
            </div>
            <div style={S.metaRow}>
              <span>Shipment ID: </span><span style={S.metaVal}>#{data.shipmentId}</span>
            </div>
            <div style={S.metaRow}>
              <span>Tracking No: </span>
              <span style={{ ...S.metaVal, color: '#e0001a' }}>{data.trackingNumber}</span>
            </div>
            <div style={{ marginTop: 10, display: 'inline-block', padding: '3px 10px', background: data.paymentStatus === 'Paid' ? '#00c48c' : '#f5a623', borderRadius: 3, color: '#fff', fontSize: 11, fontWeight: 700, letterSpacing: '0.08em', textTransform: 'uppercase' }}>
              {data.paymentStatus}
            </div>
          </div>
        </div>

        {/* ── Addresses ── */}
        <div style={S.addressGrid}>
          {(['sender', 'receiver'] as const).map((role) => {
            const addr = role === 'sender' ? data.sender : data.receiver;
            return (
              <div key={role}>
                <div style={S.addrTitle}>{role === 'sender' ? '▸ Shipper / From' : '▸ Consignee / To'}</div>
                <div style={S.addrName}>{addr.fullName}</div>
                <div style={S.addrLine}>
                  {addr.street}<br />
                  {addr.city}, {addr.state} – {addr.postalCode}<br />
                  {addr.country}<br />
                  📞 {addr.phone}
                </div>
              </div>
            );
          })}
        </div>

        {/* ── Line Items Table ── */}
        <table style={S.table}>
          <thead>
            <tr>
              {['#', 'Description of Service', 'HSN/SAC', 'Qty', 'Unit Price', 'Amount'].map(h => (
                <th key={h} style={{ ...S.th, ...(h === 'Amount' || h === 'Unit Price' ? S.tdRight : {}) }}>{h}</th>
              ))}
            </tr>
          </thead>
          <tbody>
            {lineItems.map((item, i) => (
              <tr key={i} style={{ background: i % 2 === 0 ? '#fafafa' : '#fff' }}>
                <td style={S.td}>{i + 1}</td>
                <td style={S.td}>{item.desc}</td>
                <td style={S.td}>996812</td>
                <td style={S.td}>1</td>
                <td style={{ ...S.td, ...S.tdRight }}>{fmtINR(item.unit as unknown as number)}</td>
                <td style={{ ...S.td, ...S.tdRight, fontWeight: 600 }}>{fmtINR(item.amount)}</td>
              </tr>
            ))}
          </tbody>
        </table>

        {/* ── Cost Summary + Package Info ── */}
        <div style={S.costBox}>
          <div style={S.pkgInfo}>
            <div style={S.pkgTitle}>Package Details</div>
            {[
              ['Type', data.shipmentType], ['Description', data.package.description],
              ['Weight', `${data.package.weightKg} kg`],
              ['Dimensions', `${data.package.lengthCm} × ${data.package.widthCm} × ${data.package.heightCm} cm`],
              ['Payment Mode', data.paymentMethod],
              ...(data.razorpayOrderId ? [['Razorpay Order', data.razorpayOrderId]] : []),
              ...(data.razorpayPaymentId ? [['Razorpay Txn', data.razorpayPaymentId]] : []),
              ...(data.notes ? [['Notes', data.notes]] : []),
            ].map(([k, v]) => (
              <div key={k} style={S.pkgRow}>
                <span style={S.pkgKey}>{k}</span>
                <span style={{ ...S.pkgVal, maxWidth: 160, textAlign: 'right', wordBreak: 'break-all', fontSize: 10 }}>{v}</span>
              </div>
            ))}
          </div>

          <div style={S.totals}>
            <div style={{ padding: '8px 14px', background: '#f5f5f5', borderBottom: '1px solid #eee', fontSize: 11, fontWeight: 700, color: '#444', letterSpacing: '0.06em', textTransform: 'uppercase' }}>Cost Summary</div>
            {[
              ['Subtotal (excl. GST)', fmtINR(costs.subtotal)],
              ...(isDomestic
                ? [['CGST @ 9%', fmtINR(costs.cgst)], ['SGST @ 9%', fmtINR(costs.sgst)]]
                : [['IGST @ 18%', fmtINR(costs.igst)]]),
              ['Total GST', fmtINR(costs.totalGst)],
            ].map(([k, v]) => (
              <div key={k} style={S.totalRow}>
                <span style={S.totalKey}>{k}</span>
                <span style={S.totalVal}>{v}</span>
              </div>
            ))}
            <div style={S.grandRow}>
              <span style={S.grandKey}>Grand Total</span>
              <span style={S.grandVal}>{fmtINR(costs.grandTotal)}</span>
            </div>
          </div>
        </div>

        {/* ── Payment / Tracking Status Band ── */}
        <div style={S.statusBand}>
          {[
            ['Tracking Number', data.trackingNumber],
            ['Shipment Status', data.status],
            ['Payment Method', data.paymentMethod],
            ['Payment Status', data.paymentStatus],
            ['Shipment Type', data.shipmentType],
          ].map(([l, v]) => (
            <div key={l} style={S.statusItem}>
              <span style={S.statusLabel}>{l}</span>
              <span style={{ ...S.statusVal, color: l === 'Tracking Number' ? '#e0001a' : '#111' }}>{v}</span>
            </div>
          ))}
        </div>

        {/* ── Footer ── */}
        <div style={S.footer}>
          <div style={S.footerLeft}>
            <strong style={{ color: '#333', fontFamily: "'Orbitron', monospace", fontSize: 10 }}>Terms & Conditions</strong><br />
            1. This is a computer-generated invoice and does not require a signature.<br />
            2. Rates are inclusive of applicable surcharges. GST as per applicable slab.<br />
            3. Claims for damage must be raised within 48 hours of delivery.<br />
            4. SmartShip is not liable for delays due to force majeure events.<br />
            5. For disputes, contact support@smartship.in within 7 days of invoice date.
          </div>
          <div style={S.footerRight}>
            <strong style={{ color: '#e0001a', fontFamily: "'Orbitron', monospace", fontSize: 11 }}>SmartShip</strong><br />
            Powered by SmartShip Logistics<br />
            Invoice generated: {data.invoiceDate}<br />
            <span style={{ color: '#bbb' }}>This invoice is valid without stamp or signature</span>
          </div>
        </div>
      </div>
    );
  }
);
InvoiceDocument.displayName = 'InvoiceDocument';


/* ─── Modal Wrapper ─── */
export const InvoicePrintView: React.FC<Props> = ({ data, onClose, onDownloaded, onFileReady }) => {
  const ref = useRef<HTMLDivElement>(null);
  const [isGenerating, setIsGenerating] = React.useState(false);

  const generatePDFBlob = async (): Promise<File | null> => {
    if (!ref.current) return null;
    try {
      const canvas = await html2canvas(ref.current, { scale: 2, useCORS: true, backgroundColor: '#fff' });
      const imgData = canvas.toDataURL('image/jpeg', 0.75);
      const pdf = new jsPDF({ orientation: 'portrait', unit: 'px', format: [794, 1123], compress: true });
      pdf.addImage(imgData, 'JPEG', 0, 0, 794, 1123);
      const pdfBlob = pdf.output('blob');
      const fileName = `SmartShip-Invoice-${data.trackingNumber}.pdf`;
      return new File([pdfBlob], fileName, { type: 'application/pdf' });
    } catch (err) {
      console.error('Failed to generate PDF', err);
      return null;
    }
  };

  React.useEffect(() => {
    const timer = setTimeout(async () => {
      const file = await generatePDFBlob();
      if (file && onFileReady) {
        onFileReady(file);
      }
    }, 1000); // Give a second for fonts/images to stabilize
    return () => clearTimeout(timer);
  }, []);

  const handlePrint = useReactToPrint({
    contentRef: ref,
    documentTitle: `SmartShip-Invoice-${data.trackingNumber}`,
  });

  const handleDownloadPDF = async () => {
    setIsGenerating(true);
    const file = await generatePDFBlob();
    if (file) {
      const url = URL.createObjectURL(file);
      const link = document.createElement('a');
      link.href = url;
      link.download = file.name;
      link.click();
      URL.revokeObjectURL(url);
      onDownloaded(file);
    }
    setIsGenerating(false);
  };

  return (
    <div style={{
      position: 'fixed', inset: 0, zIndex: 9999,
      background: 'rgba(0,0,0,0.85)',
      display: 'flex', flexDirection: 'column', alignItems: 'center',
      paddingTop: 32, paddingBottom: 32, overflowY: 'auto',
    }}>
      {/* Controls */}
      <div style={{
        display: 'flex', gap: 12, marginBottom: 20, alignItems: 'center',
        position: 'sticky', top: 0, zIndex: 1, background: 'rgba(0,0,0,0.85)', padding: '12px 0',
      }}>
        <span style={{ fontFamily: 'Orbitron, monospace', color: '#fff', fontSize: 14, fontWeight: 700, letterSpacing: '0.1em' }}>
          INVOICE PREVIEW
        </span>
        <button className="ss-btn" onClick={handleDownloadPDF} disabled={isGenerating}>
          {isGenerating ? '⌛ Generating...' : '⬇ Download PDF'}
        </button>
        <button className="ss-btn ss-btn-outline" onClick={() => handlePrint()}>🖨 Print</button>
        <button className="ss-btn ss-btn-outline" onClick={onClose} style={{ borderColor: '#555', color: '#aaa' }}>✕ Close</button>
      </div>

      {/* Invoice on white background */}
      <div style={{ boxShadow: '0 8px 64px rgba(0,0,0,0.7)', borderRadius: 4 }}>
        <InvoiceDocument ref={ref} data={data} />
      </div>
    </div>
  );
};
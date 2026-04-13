import { useState } from 'react';
interface SagaState {
  id: string;
  label: string;
  desc: string;
  x: number;
  y: number;
  type: 'normal' | 'active' | 'success' | 'error' | 'ghost';
}



const STATES: SagaState[] = [
  { id: 'draft', label: 'DRAFT', desc: 'Shipment created, awaiting payment', x: 60, y: 200, type: 'active' },
  { id: 'paymentPending', label: 'PAYMENT PENDING', desc: 'Saga started, waiting for gateway', x: 340, y: 200, type: 'normal' },
  { id: 'confirmed', label: 'CONFIRMED / BOOKED', desc: 'Payment verified, Saga complete', x: 620, y: 200, type: 'success' },
  { id: 'cancelled', label: 'CANCELLED', desc: 'Payment failed or manually voided', x: 340, y: 370, type: 'error' },
];




const EVENTS = [
  { event: 'ShipmentCreatedEvent', from: 'ShipmentService', to: 'TrackingService', action: 'Auto-create first tracking event' },
  { event: 'PaymentCompletedEvent', from: 'PaymentService', to: 'ShipmentService (Saga)', action: 'Set Status â†’ Booked' },
  { event: 'PaymentFailedEvent', from: 'PaymentService', to: 'ShipmentService (Saga)', action: 'Set Status â†’ Cancelled' },
  { event: 'ShipmentStatusUpdated', from: 'ShipmentService', to: 'TrackingService', action: 'Append new tracking event' },
  { event: 'ShipmentDelivered', from: 'ShipmentService', to: 'AdminService', action: 'Update dashboard metrics' },
  { event: 'DeliveryConfirmed', from: 'TrackingService', to: 'ShipmentService', action: 'Set DeliveredAt timestamp' },
  { event: 'UserDeleted', from: 'IdentityService', to: 'ShipmentService', action: 'Remove user shipment records' },
  { event: 'HubDeactivated', from: 'AdminService', to: 'ShipmentService', action: 'Flag affected shipments' },
];

const nodeColors = {
  normal:  { bg: '#1a1a1a', border: 'rgba(255,255,255,0.1)', text: '#888', glow: 'none' },
  active:  { bg: '#1f0005', border: '#e0001a', text: '#fff', glow: '0 0 20px rgba(224,0,26,0.5)' },
  success: { bg: '#001a12', border: '#00c48c', text: '#00c48c', glow: '0 0 16px rgba(0,196,140,0.4)' },
  error:   { bg: '#1c1c1c', border: '#444', text: '#555', glow: 'none' },
  ghost:   { bg: '#141414', border: '#333', text: '#555', glow: 'none' },
};

export const SagaViewer = () => {
  const [activeState, setActiveState] = useState<string>('draft');

  const simulate = (stateId: string) => setActiveState(stateId);

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 24, maxWidth: 1100 }}>
      {/* Header */}
      <div>
        <div className="accent-line" style={{ marginBottom: 8 }} />
        <div style={{ display: 'flex', alignItems: 'flex-end', justifyContent: 'space-between' }}>
          <div>
            <h1 className="section-title">Saga State Machine Viewer</h1>
            <p className="section-sub">Visual representation of SmartShip's MassTransit SAGA orchestration flow</p>
          </div>
          <span className="ss-badge glow" style={{ fontSize: 11, padding: '5px 14px' }}>MASSTRANSIT ENGINE</span>
        </div>
      </div>

      {/* State machine diagram */}
      <div className="ss-card" style={{ padding: 28 }}>
        <div style={{ marginBottom: 16, display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
          <h2 style={{ fontFamily: 'Rajdhani, sans-serif', fontSize: 14, fontWeight: 700, letterSpacing: '0.1em', textTransform: 'uppercase', color: '#fff' }}>
            Shipment Order State Machine
          </h2>
          <div style={{ display: 'flex', gap: 8 }}>
            {['draft', 'paymentPending', 'confirmed', 'cancelled'].map(s => (
              <button
                key={s}
                className="ss-btn ss-btn-outline"
                style={{ fontSize: 9, padding: '4px 10px', borderColor: activeState === s ? 'var(--color-accent)' : undefined }}
                onClick={() => simulate(s)}
              >
                {s.toUpperCase()}
              </button>
            ))}
          </div>
        </div>

        {/* SVG Diagram */}
        <div style={{ overflowX: 'auto' }}>
          <svg width="820" height="460" viewBox="0 0 820 460" style={{ display: 'block' }}>
            {/* Background grid */}
            <defs>
              <pattern id="grid" width="40" height="40" patternUnits="userSpaceOnUse">
                <path d="M 40 0 L 0 0 0 40" fill="none" stroke="rgba(255,255,255,0.025)" strokeWidth="1"/>
              </pattern>
            </defs>
            <rect width="820" height="460" fill="url(#grid)" />

            {/* Transition arrows */}
            {/* Draft â†’ PaymentPending */}
            <g>
              <line x1="210" y1="210" x2="310" y2="210" stroke="rgba(255,255,255,0.2)" strokeWidth="1" markerEnd="url(#arrow-normal)" />
              <text x="260" y="200" textAnchor="middle" fontSize="9" fill="#555" fontFamily="Rajdhani, sans-serif" letterSpacing="0.06em">ShipmentCreatedEvent</text>
            </g>
            {/* PaymentPending â†’ Confirmed */}
            <g>
              <line x1="490" y1="200" x2="590" y2="200" stroke="rgba(0,196,140,0.4)" strokeWidth="1" markerEnd="url(#arrow-success)" />
              <text x="540" y="192" textAnchor="middle" fontSize="9" fill="#00c48c" fontFamily="Rajdhani, sans-serif" letterSpacing="0.06em">PaymentCompletedEvent</text>
            </g>
            {/* PaymentPending â†’ Cancelled */}
            <g>
              <line x1="400" y1="255" x2="400" y2="330" stroke="rgba(224,0,26,0.4)" strokeWidth="1" markerEnd="url(#arrow-error)" />
              <text x="412" y="296" fontSize="9" fill="#e0001a" fontFamily="Rajdhani, sans-serif" letterSpacing="0.06em">PaymentFailedEvent</text>
            </g>
            {/* Draft â†’ Cancelled (diagonal) */}
            <g>
              <line x1="140" y1="250" x2="340" y2="340" stroke="rgba(100,100,100,0.3)" strokeWidth="1" strokeDasharray="4,4" markerEnd="url(#arrow-dim)" />
              <text x="220" y="315" fontSize="9" fill="#444" fontFamily="Rajdhani, sans-serif">CustomerCancelledEvent</text>
            </g>

            {/* Arrowhead markers */}
            <defs>
              <marker id="arrow-normal" markerWidth="8" markerHeight="8" refX="6" refY="3" orient="auto">
                <path d="M0,0 L0,6 L9,3 z" fill="rgba(255,255,255,0.2)" />
              </marker>
              <marker id="arrow-success" markerWidth="8" markerHeight="8" refX="6" refY="3" orient="auto">
                <path d="M0,0 L0,6 L9,3 z" fill="#00c48c" />
              </marker>
              <marker id="arrow-error" markerWidth="8" markerHeight="8" refX="6" refY="3" orient="auto">
                <path d="M0,0 L0,6 L9,3 z" fill="#e0001a" />
              </marker>
              <marker id="arrow-dim" markerWidth="8" markerHeight="8" refX="6" refY="3" orient="auto">
                <path d="M0,0 L0,6 L9,3 z" fill="#444" />
              </marker>
            </defs>

            {/* State Nodes */}
            {STATES.map(node => {
              const isActive = node.id === activeState;
              const col = isActive
                ? (node.type === 'error' ? nodeColors.error : node.type === 'success' ? nodeColors.success : nodeColors.active)
                : nodeColors[node.type];

              return (
                <g key={node.id} onClick={() => setActiveState(node.id)} style={{ cursor: 'pointer' }}>
                  {/* Glow effect */}
                  {(isActive || node.type === 'success') && (
                    <rect
                      x={node.x - 6} y={node.y - 6}
                      width={162} height={80}
                      rx="4" ry="4"
                      fill="none"
                      stroke={node.type === 'success' ? 'rgba(0,196,140,0.3)' : 'rgba(224,0,26,0.3)'}
                      strokeWidth="10"
                      style={{ filter: 'blur(6px)' }}
                    />
                  )}
                  <rect
                    x={node.x} y={node.y}
                    width={150} height={68}
                    rx="3" ry="3"
                    fill={col.bg}
                    stroke={col.border}
                    strokeWidth="1"
                  />
                  <text
                    x={node.x + 75} y={node.y + 24}
                    textAnchor="middle"
                    fontSize="11"
                    fontWeight="700"
                    fontFamily="Rajdhani, sans-serif"
                    fill={col.text}
                    letterSpacing="0.08em"
                  >
                    {node.label}
                  </text>
                  <text
                    x={node.x + 75} y={node.y + 44}
                    textAnchor="middle"
                    fontSize="8"
                    fontFamily="Inter, sans-serif"
                    fill="#555"
                  >
                    {node.desc}
                  </text>
                  {/* Active badge */}
                  {isActive && (
                    <rect x={node.x + 100} y={node.y - 8} width={50} height={16} rx="2" fill="#e0001a" />
                  )}
                  {isActive && (
                    <text x={node.x + 125} y={node.y + 4} textAnchor="middle" fontSize="8" fill="#fff" fontFamily="Rajdhani, sans-serif" fontWeight="700" letterSpacing="0.1em">
                      CURRENT
                    </text>
                  )}
                </g>
              );
            })}
          </svg>
        </div>

        {/* Legend */}
        <div style={{ display: 'flex', gap: 20, marginTop: 16, flexWrap: 'wrap' }}>
          {[
            { color: 'var(--color-accent)', label: 'Active State' },
            { color: 'var(--color-success)', label: 'Terminal Success' },
            { color: '#444', label: 'Terminal Failure' },
            { color: 'rgba(255,255,255,0.2)', label: 'Normal Transition' },
          ].map(l => (
            <div key={l.label} style={{ display: 'flex', alignItems: 'center', gap: 7 }}>
              <div style={{ width: 20, height: 2, background: l.color }} />
              <span style={{ fontSize: 11, color: 'var(--color-text-muted)', fontFamily: 'Rajdhani, sans-serif', letterSpacing: '0.08em' }}>{l.label}</span>
            </div>
          ))}
        </div>
      </div>

      {/* Event Bus table */}
      <div className="ss-card" style={{ padding: 0, overflow: 'hidden' }}>
        <div style={{ padding: '16px 20px', borderBottom: '1px solid rgba(255,255,255,0.06)' }}>
          <h2 style={{ fontFamily: 'Rajdhani, sans-serif', fontSize: 14, fontWeight: 700, letterSpacing: '0.1em', textTransform: 'uppercase', color: '#fff' }}>
            RabbitMQ Event Bus â€” Published Events
          </h2>
          <p style={{ fontSize: 11, color: 'var(--color-text-muted)', marginTop: 2 }}>All domain events flowing across microservice boundaries</p>
        </div>
        <table className="ss-table">
          <thead>
            <tr>
              <th>Event Name</th>
              <th>Published By</th>
              <th>Consumed By</th>
              <th>Action Taken</th>
            </tr>
          </thead>
          <tbody>
            {EVENTS.map((e, i) => (
              <tr key={i}>
                <td style={{ fontFamily: 'Rajdhani, sans-serif', fontWeight: 700, color: 'var(--color-accent)', fontSize: 12, letterSpacing: '0.06em' }}>{e.event}</td>
                <td style={{ fontSize: 12, color: 'var(--color-text-muted)' }}>{e.from}</td>
                <td style={{ fontSize: 12, color: 'var(--color-text-muted)' }}>{e.to}</td>
                <td style={{ fontSize: 12 }}>{e.action}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
};

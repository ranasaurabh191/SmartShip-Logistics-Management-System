// features/shipment/ShipmentsPage.tsx
import { useEffect, useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { apiClient } from '../../core/api/axios';
import { useAuthStore } from '../../store/useAuthStore';
import { useNotificationStore } from '../../store/useNotificationStore';
import { DeliveryConfirmModal } from './DeliveryConfirmModal';

type ShipmentApi = {
  id: number; trackingNumber: string; customerId: number;
  shipmentType: string; status: string; shippingRate: number;
  createdAt: string; pickupScheduledAt?: string | null; deliveredAt?: string | null;
  notes?: string | null;
  senderAddress?: { fullName: string; city: string };
  receiverAddress?: { fullName: string; city: string };
  package?: { declaredValue?: number; weightKg?: number };
};

type PaymentInfo = {
  id: number; shipmentId?: number | null; trackingNumber: string;
  amount: number; paymentMethod: string; paymentStatus: string;
  razorpayOrderId?: string | null; razorpayPaymentId?: string | null;
  createdAt: string; paidAt?: string | null; message?: string | null;
};

type ShipmentRow = {
  id: number; trackingNumber: string; senderFullName: string;
  receiverFullName: string; originCity: string; destinationCity: string;
  status: string; createdAt: string; pickupScheduledAt?: string | null;
  deliveredAt?: string | null; notes?: string | null;
  shippingRate: number; shipmentType: string;
};

const statusStyle: Record<string, string> = {
  InTransit: 'warning', Booked: '', Delivered: 'success',
  Cancelled: 'muted', Draft: 'muted', PickedUp: 'warning', OutForDelivery: 'warning',
};

const PAYMENTMETHOD = { COD: 0, ONLINE: 1 };
const RAZORPAY_KEY_ID = import.meta.env.VITE_RAZORPAY_KEY_ID as string;

export const ShipmentsPage = () => {
  const navigate = useNavigate();
  const addNotification = useNotificationStore(state => state.addNotification);
  const user = useAuthStore(state => state.user);
  const isAdmin = user?.role === 'ADMIN';
  const basePath = isAdmin ? '/admin' : '/customer';
  const shipmentsEndpoint = isAdmin ? '/admin/shipments' : '/shipments/my';

  const ADMIN_NEXT_STATUS: Record<string, string> = {
    Draft: 'Booked', Booked: 'PickedUp', PickedUp: 'InTransit',
    InTransit: 'OutForDelivery',
  };

  const [shipments, setShipments] = useState<ShipmentRow[]>([]);
  const [paymentsByShipment, setPaymentsByShipment] = useState<Record<number, PaymentInfo | null>>({});
  const [filter, setFilter] = useState('ALL');
  const [search, setSearch] = useState('');
  const [loading, setLoading] = useState(true);
  const [busyShipmentId, setBusyShipmentId] = useState<number | null>(null);

  const [selectedShipment, setSelectedShipment] = useState<ShipmentRow | null>(null);
  const [cancelModalOpen, setCancelModalOpen] = useState(false);
  const [shipmentToCancel, setShipmentToCancel] = useState<number | null>(null);
  const [statusModalOpen, setStatusModalOpen] = useState(false);
  const [resolveModalOpen, setResolveModalOpen] = useState(false);
  const [pickupModalOpen, setPickupModalOpen] = useState(false);
  const [verifyModalOpen, setVerifyModalOpen] = useState(false);
  const [deliveryModalOpen, setDeliveryModalOpen] = useState(false);
  const [adminLocation, setAdminLocation] = useState('');
  const [resolutionText, setResolutionText] = useState('');
  const [pickupDateTime, setPickupDateTime] = useState('');
  const [verifyPayment, setVerifyPayment] = useState<PaymentInfo | null>(null);
  const [verifyForm, setVerifyForm] = useState({ razorpayPaymentId: '', signature: '' });

  const fetchShipments = async () => {
    setLoading(true);
    try {
      const res = await apiClient.get(shipmentsEndpoint, { params: { page: 1, pageSize: 25 } });
      const responseData = res.data;
      const items: ShipmentApi[] = Array.isArray(responseData)
        ? responseData : responseData?.data ?? responseData?.items ?? [];
      const mapped: ShipmentRow[] = items.map(s => ({
        id: s.id, trackingNumber: s.trackingNumber,
        senderFullName: s.senderAddress?.fullName ?? '',
        receiverFullName: s.receiverAddress?.fullName ?? '',
        originCity: s.senderAddress?.city ?? '',
        destinationCity: s.receiverAddress?.city ?? '',
        status: s.status, createdAt: s.createdAt,
        pickupScheduledAt: s.pickupScheduledAt, deliveredAt: s.deliveredAt,
        notes: s.notes, shippingRate: Number(s.shippingRate) || 0,
        shipmentType: s.shipmentType,
      }));
      setShipments(mapped);
      const paymentEntries = await Promise.all(
        mapped.map(async shipment => {
          try {
            const paymentRes = await apiClient.get(`/payment/shipment/${shipment.id}`);
            return [shipment.id, paymentRes.data] as const;
          } catch { return [shipment.id, null] as const; }
        })
      );
      setPaymentsByShipment(Object.fromEntries(paymentEntries));
    } catch {
      setShipments([]); setPaymentsByShipment({});
    } finally { setLoading(false); }
  };

  useEffect(() => { fetchShipments(); }, [shipmentsEndpoint]);

  const statuses = ['ALL', 'Draft', 'Booked', 'PickedUp', 'InTransit', 'OutForDelivery', 'Delivered', 'Cancelled'];

  const filtered = useMemo(() =>
    shipments.filter(s => {
      const matchesFilter = filter === 'ALL' ? true : s.status === filter;
      const q = search.toLowerCase();
      const matchesSearch =
        s.trackingNumber.toLowerCase().includes(q) ||
        s.senderFullName.toLowerCase().includes(q) ||
        s.receiverFullName.toLowerCase().includes(q) ||
        String(s.id).includes(q);
      return matchesFilter && matchesSearch;
    }),
    [shipments, filter, search]
  );

  const openCancelModal = (e: React.MouseEvent, id: number) => {
    e.stopPropagation();
    setShipmentToCancel(id);
    setCancelModalOpen(true);
  };

  const confirmCancel = async () => {
    if (!shipmentToCancel) return;
    try {
      setBusyShipmentId(shipmentToCancel);
      await apiClient.patch(`/shipments/${shipmentToCancel}/cancel`, {
        reason: isAdmin ? 'Cancelled by admin' : 'Cancelled by customer',
      });
      setCancelModalOpen(false);
      setShipmentToCancel(null);
      addNotification('Shipment cancelled successfully.', 'success');
      await fetchShipments();
    } catch (err: any) {
      addNotification(err?.response?.data?.message || err?.response?.data || 'Failed to cancel shipment.', 'error');
    } finally { setBusyShipmentId(null); }
  };

  const openPickupModal = (e: React.MouseEvent, shipment: ShipmentRow) => {
    e.stopPropagation(); setSelectedShipment(shipment); setPickupDateTime(''); setPickupModalOpen(true);
  };

  const confirmSchedulePickup = async () => {
    if (!selectedShipment) return;
    if (!pickupDateTime) { addNotification('Please select pickup date and time.', 'warning'); return; }
    try {
      setBusyShipmentId(selectedShipment.id);
      await apiClient.post(`/shipments/${selectedShipment.id}/schedule-pickup`, { pickupTime: pickupDateTime });
      setPickupModalOpen(false); setSelectedShipment(null); setPickupDateTime('');
      addNotification('Pickup scheduled successfully.', 'success');
      await fetchShipments();
    } catch (err: any) {
      addNotification(err?.response?.data?.message || err?.response?.data || 'Unable to schedule pickup.', 'error');
    } finally { setBusyShipmentId(null); }
  };

  const openStatusModal = (e: React.MouseEvent, shipment: ShipmentRow) => {
    e.stopPropagation(); setSelectedShipment(shipment); setAdminLocation(''); setStatusModalOpen(true);
  };

  const confirmAdminStatusUpdate = async () => {
    if (!selectedShipment) return;
    const nextStatus = ADMIN_NEXT_STATUS[selectedShipment.status];
    if (!nextStatus) { addNotification('No further status transition available.', 'warning'); return; }
    if (!adminLocation.trim()) { addNotification('Please enter hub location.', 'warning'); return; }
    try {
      setBusyShipmentId(selectedShipment.id);
      await apiClient.put(`/admin/shipments/status/${selectedShipment.id}`, {
        status: nextStatus, location: adminLocation, resolution: '',
      });
      setStatusModalOpen(false); setSelectedShipment(null); setAdminLocation('');
      await fetchShipments();
    } catch (err: any) {
      addNotification(err?.response?.data?.message || 'Failed to update shipment status.', 'error');
    } finally { setBusyShipmentId(null); }
  };

  const openResolveModal = (e: React.MouseEvent, shipment: ShipmentRow) => {
    e.stopPropagation(); setSelectedShipment(shipment); setAdminLocation('');
    setResolutionText(''); setResolveModalOpen(true);
  };

  const confirmResolve = async () => {
    if (!selectedShipment) return;
    if (!resolutionText.trim()) { addNotification('Resolution text is required.', 'warning'); return; }
    if (!adminLocation.trim()) { addNotification('Please enter resolution hub.', 'warning'); return; }
    try {
      setBusyShipmentId(selectedShipment.id);
      await apiClient.put(`/admin/shipments/resolve/${selectedShipment.id}`, {
        status: selectedShipment.status, location: adminLocation, resolution: resolutionText,
      });
      setResolveModalOpen(false); setSelectedShipment(null);
      setResolutionText(''); setAdminLocation('');
      await fetchShipments();
    } catch (err: any) {
      addNotification(err?.response?.data?.message || 'Failed to resolve shipment.', 'error');
    } finally { setBusyShipmentId(null); }
  };

  // ── Real Razorpay checkout ─────────────────────────────────────────────────
  const launchRazorpay = (payment: PaymentInfo, shipmentId: number) => {
    if (!window.Razorpay) {
      addNotification('Razorpay SDK not loaded. Please refresh the page.', 'error');
      return;
    }
    if (!payment.razorpayOrderId) {
      addNotification('No Razorpay Order ID found. Please re-initiate payment.', 'error');
      return;
    }

    const options: RazorpayOptions = {
      key: RAZORPAY_KEY_ID,
      amount: payment.amount * 100,
      currency: 'INR',
      name: 'SmartShip',
      description: `Shipment #${shipmentId} — ${payment.trackingNumber}`,
      order_id: payment.razorpayOrderId,

      handler: async (response: RazorpayPaymentResponse) => {
        try {
          setBusyShipmentId(shipmentId);
          await apiClient.post('/payment/verify', {
            razorpayOrderId: response.razorpay_order_id,
            razorpayPaymentId: response.razorpay_payment_id,
            signature: response.razorpay_signature,
            shipmentId,
            paymentMethod: 'Online',
          });
          addNotification('Payment verified successfully!', 'success');
          await fetchShipments();
        } catch (err: any) {
          addNotification(
            err?.response?.data?.message ||
            'Payment made but verification failed. Use "Retry Verify" on the Payments page.',
            'error'
          );
          await fetchShipments();
        } finally {
          setBusyShipmentId(null);
        }
      },

      prefill: {
        name: user?.name ?? '',
        email: user?.email ?? '',
      },

      theme: { color: '#e0001a' },

      modal: {
        ondismiss: () => {
          setBusyShipmentId(null);
        },
      },
    };

    const rzp = new window.Razorpay(options);
    rzp.open();
  };

  const handleCreatePayment = async (e: React.MouseEvent, shipment: ShipmentRow, mode: 'COD' | 'ONLINE') => {
    e.stopPropagation();
    try {
      setBusyShipmentId(shipment.id);
      const res = await apiClient.post('/payment/create-order', {
        shipmentId: shipment.id,
        paymentMethod: mode === 'COD' ? PAYMENTMETHOD.COD : PAYMENTMETHOD.ONLINE,
      });
      const payment = res.data as PaymentInfo;

      if (mode === 'COD') {
        addNotification(payment.message || 'COD registered successfully. Schedule pickup when ready.', 'success');
        await fetchShipments();
        setBusyShipmentId(null);
        return;
      }

      // Online — immediately launch Razorpay checkout with the new order
      setBusyShipmentId(null); // release before opening modal (handler sets it again)
      await fetchShipments();  // refresh so payment shows in table
      launchRazorpay(payment, shipment.id);
    } catch (err: any) {
      addNotification(err?.response?.data?.message || err?.response?.data || 'Payment action failed.', 'error');
      setBusyShipmentId(null);
    }
  };
  // For already-created pending orders — re-open Razorpay checkout
  const handlePayExisting = (e: React.MouseEvent, shipment: ShipmentRow) => {
    e.stopPropagation();
    const payment = paymentsByShipment[shipment.id];
    if (!payment) return;
    launchRazorpay(payment, shipment.id);
  };

  const confirmRetryVerify = async () => {
    if (!verifyPayment?.razorpayOrderId || !selectedShipment) return;
    try {
      setBusyShipmentId(selectedShipment.id);
      await apiClient.post('/payment/verify', {
        razorpayOrderId: verifyPayment.razorpayOrderId,
        razorpayPaymentId: verifyForm.razorpayPaymentId.trim(),
        signature: verifyForm.signature.trim(),
        shipmentId: selectedShipment.id, paymentMethod: 'Online',
      });
      setVerifyModalOpen(false); setVerifyPayment(null); setSelectedShipment(null);
      await fetchShipments();
    } catch (err: any) {
      addNotification(err?.response?.data?.message || 'Payment verification failed.', 'error');
    } finally { setBusyShipmentId(null); }
  };

  const openDeliveryModal = (e: React.MouseEvent, shipment: ShipmentRow) => {
    e.stopPropagation();
    setSelectedShipment(shipment);
    setDeliveryModalOpen(true);
  };

  const getPaymentLabel = (payment: PaymentInfo | null | undefined): string => {
    if (!payment) return 'Unpaid';
    if (payment.paymentMethod === 'COD' && payment.paymentStatus === 'Pending') return 'COD Registered';
    if (payment.paymentMethod === 'Online' && payment.paymentStatus === 'Pending') return 'Online Pending';
    if (payment.paymentStatus === 'Paid') return 'Paid';
    if (payment.paymentStatus === 'Failed') return 'Failed';
    return payment.paymentStatus || 'Unknown';
  };

  const canSchedulePickup = (shipment: ShipmentRow, payment: PaymentInfo | null | undefined): boolean => {
    if (shipment.status !== 'Draft') return false;
    if (!payment) return false;
    if (payment.paymentMethod === 'COD') return true;
    return payment.paymentMethod === 'Online' && payment.paymentStatus === 'Paid';
  };

  const modalBackdrop: React.CSSProperties = {
    position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.8)',
    display: 'flex', alignItems: 'center', justifyContent: 'center',
    zIndex: 9999, backdropFilter: 'blur(2px)',
  };
  const modalCard: React.CSSProperties = {
    width: 480, padding: 24, border: '1px solid var(--color-border)',
    background: 'var(--color-surface)', borderRadius: 8, boxShadow: '0 0 40px rgba(0,0,0,0.6)',
  };

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 20, maxWidth: 1380 }}>
      <div style={{ display: 'flex', alignItems: 'flex-end', justifyContent: 'space-between' }}>
        <div>
          <div className="accent-line" style={{ marginBottom: 8 }} />
          <h1 className="section-title">Shipments Registry</h1>
          <p className="section-sub">{filtered.length} records displayed</p>
        </div>
        {!isAdmin && (
          <button className="ss-btn" onClick={() => navigate(`${basePath}/shipments/create`)}>
            + New Shipment
          </button>
        )}
      </div>

      <div className="ss-card" style={{ padding: '14px 16px', display: 'flex', alignItems: 'center', gap: 12, flexWrap: 'wrap' }}>
        <input className="ss-input" placeholder="Search by tracking, sender, receiver..." value={search}
          onChange={e => setSearch(e.target.value)} style={{ width: 280 }} />
        {statuses.map(s => (
          <button key={s} onClick={() => setFilter(s)} style={{
            fontFamily: 'Orbitron, monospace', fontSize: 11, fontWeight: 600,
            letterSpacing: '0.1em', textTransform: 'uppercase', padding: '5px 12px',
            border: '1px solid', borderRadius: 2, cursor: 'pointer', background: 'transparent',
            borderColor: filter === s ? 'var(--color-accent)' : 'var(--color-border)',
            color: filter === s ? 'var(--color-accent)' : 'var(--color-text-muted)',
          }}>{s}</button>
        ))}
      </div>

      <div className="ss-card" style={{ padding: 10, overflowX: 'auto' }}>
        <table className="ss-table">
          <thead>
            <tr>
              <th>Tracking No.</th><th>Sender</th><th>Receiver</th>
              <th>Origin</th><th>Destination</th><th>Status</th>
              <th>Payment</th><th>Rate</th><th>Created At</th>
              <th>Pickup Time</th><th>Delivered At</th><th>Notes</th><th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {loading && <tr><td colSpan={13} style={{ textAlign: 'center', padding: 40 }}>LOADING...</td></tr>}
            {!loading && filtered.length === 0 && <tr><td colSpan={13} style={{ textAlign: 'center', padding: 40 }}>No records match filter</td></tr>}
            {!loading && filtered.map(s => {
              const payment = paymentsByShipment[s.id];
              const scheduleAllowed = canSchedulePickup(s, payment);
              const hasUnpaidOnline = payment?.paymentMethod === 'Online' &&
                payment?.paymentStatus !== 'Paid' && !!payment?.razorpayOrderId;

              return (
                <tr key={s.id} style={{ cursor: 'pointer' }} onClick={() => navigate(`${basePath}/track/${s.id}`)}>
                  <td>{s.trackingNumber}</td>
                  <td>{s.senderFullName}</td>
                  <td>{s.receiverFullName}</td>
                  <td>{s.originCity}</td>
                  <td>{s.destinationCity}</td>
                  <td><span className={`ss-badge ${statusStyle[s.status] ?? ''}`} style={{ padding: '3px 5px' }}>{s.status}</span></td>
                  <td>
                    <span className={`ss-badge ${payment?.paymentStatus === 'Paid' ? 'success' : payment?.paymentStatus === 'Failed' ? 'muted' : ''}`} style={{ padding: '3px 5px' }}>
                      {getPaymentLabel(payment)}
                    </span>
                  </td>
                  <td>{s.shippingRate.toLocaleString('en-IN')}</td>
                  <td>{new Date(s.createdAt).toLocaleString('en-IN', { day: 'numeric', month: 'numeric', year: 'numeric', hour: 'numeric', minute: '2-digit', hour12: true })}</td>
                  <td>{s.pickupScheduledAt ? new Date(s.pickupScheduledAt).toLocaleString('en-IN') : '—'}</td>
                  <td>{s.deliveredAt ? new Date(s.deliveredAt).toLocaleString('en-IN', { day: 'numeric', month: 'numeric', year: 'numeric', hour: 'numeric', minute: '2-digit', hour12: true }) : '—'}</td>
                  <td title={s.notes ?? ''} style={{ maxWidth: 180, whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}>{s.notes ?? ''}</td>
                  <td style={{ minWidth: 230 }}>
                    <div style={{ display: 'grid', gridTemplateColumns: 'repeat(2, minmax(10px, 1fr))', gap: 8, width: '100%', alignItems: 'center' }}>

                      <button className="ss-btn ss-btn-outline"
                        onClick={e => { e.stopPropagation(); navigate(`${basePath}/track/${s.id}`); }}>
                        Track
                      </button>

                      {isAdmin && (
                        <button className="ss-btn ss-btn-outline"
                          disabled={busyShipmentId === s.id}
                          onClick={e => openResolveModal(e, s)}>
                          Resolve
                        </button>
                      )}

                      {isAdmin && s.status !== 'Delivered' && s.status !== 'Cancelled' && s.status !== 'OutForDelivery' && (
                        <button className="ss-btn"
                          style={{ gridColumn: '1 / -1', padding: '6px 28px', textAlign: 'center' }}
                          disabled={busyShipmentId === s.id}
                          onClick={e => openStatusModal(e, s)}>
                          Update Status
                        </button>
                      )}

                      {isAdmin && s.status === 'OutForDelivery' && (
                        <button className="ss-btn"
                          style={{ gridColumn: '1 / -1', padding: '6px 7px', textAlign: 'center', background: 'var(--color-success)', border: '1px solid var(--color-success)' }}
                          disabled={busyShipmentId === s.id}
                          onClick={e => openDeliveryModal(e, s)}>
                          ✓ Confirm Delivery
                        </button>
                      )}

                      {/* No payment yet → show COD + Pay Online */}
                      {!payment && s.status === 'Draft' && (
                        <>
                          <button className="ss-btn"
                            disabled={busyShipmentId === s.id}
                            onClick={e => handleCreatePayment(e, s, 'COD')}>
                            COD
                          </button>
                          <button className="ss-btn ss-btn-outline"
                            disabled={busyShipmentId === s.id}
                            onClick={e => handleCreatePayment(e, s, 'ONLINE')}>
                            Pay Online
                          </button>
                        </>
                      )}

                      {/* Existing unpaid online order → Pay Now (re-open checkout) + Retry Verify */}
                      {!isAdmin && hasUnpaidOnline && s.status === 'Draft' && (
                        <>
                          <button className="ss-btn "
                            style={{ padding: '5px 6px', textAlign: 'center' }}
                            disabled={busyShipmentId === s.id}
                            onClick={e => handlePayExisting(e, s)}>
                            Pay Now
                          </button>
                        </>
                      )}

                      {!(s.status === 'Draft' || s.status === 'Booked') && !isAdmin && (
                        <button className="ss-btn ss-btn-outline"
                          disabled={busyShipmentId === s.id}
                          onClick={e => openCancelModal(e, s.id)}>
                          Cancel
                        </button>
                      )}

                      {scheduleAllowed && !isAdmin && (
                        <button className="ss-btn"
                          style={{ gridColumn: '1 / -1', padding: '6px 20px', textAlign: 'center' }}
                          disabled={busyShipmentId === s.id}
                          onClick={e => openPickupModal(e, s)}>
                          Schedule Pickup
                        </button>
                      )}
                    </div>
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>

      {/* Retry Verify Modal */}
      {verifyModalOpen && verifyPayment && (
        <div onClick={() => setVerifyModalOpen(false)} style={modalBackdrop}>
          <div onClick={e => e.stopPropagation()} className="ss-card"
            style={{ ...modalCard, boxShadow: '0 0 40px rgba(224,0,26,0.15)' }}>
            <div style={{ marginBottom: 20 }}>
              <div className="accent-line" style={{ marginBottom: 6 }} />
              <h2 className="section-title">Retry Payment Verification</h2>
              <p className="section-sub" style={{ marginTop: 4 }}>
                Order: <span style={{ color: 'var(--color-accent)' }}>{verifyPayment.razorpayOrderId}</span>
              </p>
            </div>
            <input className="ss-input" placeholder="Razorpay Payment ID (pay_...)"
              value={verifyForm.razorpayPaymentId}
              onChange={e => setVerifyForm(p => ({ ...p, razorpayPaymentId: e.target.value }))}
              style={{ width: '100%', marginBottom: 12 }} />
            <input className="ss-input" placeholder="Signature"
              value={verifyForm.signature}
              onChange={e => setVerifyForm(p => ({ ...p, signature: e.target.value }))}
              style={{ width: '100%', marginBottom: 20 }} />
            <div style={{ display: 'flex', justifyContent: 'flex-end', gap: 10 }}>
              <button className="ss-btn ss-btn-outline" onClick={() => setVerifyModalOpen(false)}>Cancel</button>
              <button className="ss-btn" onClick={confirmRetryVerify}
                disabled={busyShipmentId === selectedShipment?.id || !verifyForm.razorpayPaymentId.trim() || !verifyForm.signature.trim()}>
                Verify Payment
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Update Status Modal */}
      {statusModalOpen && isAdmin && (
        <div onClick={() => setStatusModalOpen(false)} style={modalBackdrop}>
          <div onClick={e => e.stopPropagation()} className="ss-card" style={modalCard}>
            <h2 className="section-title">Update Shipment Status</h2>
            <p className="section-sub" style={{ marginBottom: 12 }}>
              {selectedShipment?.trackingNumber} · <strong>{selectedShipment?.status}</strong>
              {selectedShipment?.status && ADMIN_NEXT_STATUS[selectedShipment.status] && (
                <> → <strong style={{ color: 'var(--color-accent)' }}>{ADMIN_NEXT_STATUS[selectedShipment.status]}</strong></>
              )}
            </p>
            <input className="ss-input" placeholder="Enter current hub location" value={adminLocation}
              onChange={e => setAdminLocation(e.target.value)} style={{ width: '100%', marginTop: 16 }} />
            <div style={{ display: 'flex', justifyContent: 'flex-end', gap: 10, marginTop: 20 }}>
              <button className="ss-btn ss-btn-outline" onClick={() => setStatusModalOpen(false)}>Cancel</button>
              <button className="ss-btn" onClick={confirmAdminStatusUpdate}>Confirm Update</button>
            </div>
          </div>
        </div>
      )}

      {/* Resolve Modal */}
      {resolveModalOpen && isAdmin && (
        <div onClick={() => setResolveModalOpen(false)} style={modalBackdrop}>
          <div onClick={e => e.stopPropagation()} className="ss-card" style={modalCard}>
            <h2 className="section-title">Resolve Shipment</h2>
            <input className="ss-input" placeholder="Resolution hub location" value={adminLocation}
              onChange={e => setAdminLocation(e.target.value)} style={{ width: '100%', marginBottom: 14 }} />
            <textarea className="ss-input" placeholder="Enter resolution details" value={resolutionText}
              onChange={e => setResolutionText(e.target.value)} style={{ width: '100%', minHeight: 120, resize: 'vertical' }} />
            <div style={{ display: 'flex', justifyContent: 'flex-end', gap: 10, marginTop: 20 }}>
              <button className="ss-btn ss-btn-outline" onClick={() => setResolveModalOpen(false)}>Cancel</button>
              <button className="ss-btn" onClick={confirmResolve}>Confirm Resolve</button>
            </div>
          </div>
        </div>
      )}

      {/* Pickup Modal */}
      {pickupModalOpen && (
        <div onClick={() => setPickupModalOpen(false)} style={modalBackdrop}>
          <div onClick={e => e.stopPropagation()} className="ss-card"
            style={{ ...modalCard, boxShadow: '0px 0px 50px rgba(195, 189, 189, 0.45)' }}>
            <h2 style={{ fontFamily: 'Orbitron, monospace', marginBottom: 16, color: '#fff', fontSize: 22, fontWeight: 700 }}>Schedule Pickup</h2>
            <p style={{ color: 'var(--color-text-muted)', marginBottom: 18, fontSize: 14 }}>
              Select pickup date and time for <strong>{selectedShipment?.trackingNumber}</strong>
            </p>
            <input type="datetime-local" className="ss-input" value={pickupDateTime}
              onChange={e => setPickupDateTime(e.target.value)} style={{ width: '100%', marginBottom: 20}} />
            <div style={{ display: 'flex', justifyContent: 'flex-end', gap: 10 }}>
              <button className="ss-btn ss-btn-outline" onClick={() => setPickupModalOpen(false)}>Cancel</button>
              <button className="ss-btn" disabled={busyShipmentId === selectedShipment?.id}
                onClick={confirmSchedulePickup}>Confirm Pickup</button>
            </div>
          </div>
        </div>
      )}

      {/* Delivery Confirmation Modal */}
      {deliveryModalOpen && selectedShipment && (
        <DeliveryConfirmModal
          shipmentId={selectedShipment.id}
          trackingNumber={selectedShipment.trackingNumber}
          receiverName={selectedShipment.receiverFullName}
          onClose={() => { setDeliveryModalOpen(false); setSelectedShipment(null); }}
          onSuccess={async () => {
            setDeliveryModalOpen(false);
            setShipments(prev =>
              prev.map(s =>
                s.id === selectedShipment!.id ? { ...s, status: 'Delivered' } : s
              )
            );
            setSelectedShipment(null);
            await fetchShipments();
          }}
        />
      )}

      {/* Cancel Confirmation Modal */}
      {cancelModalOpen && shipmentToCancel && (
        <div onClick={() => { setCancelModalOpen(false); setShipmentToCancel(null); }} style={modalBackdrop}>
          <div onClick={e => e.stopPropagation()} className="ss-card"
            style={{ ...modalCard, boxShadow: '0 0 40px rgba(0,0,0,0.45)' }}>
            <h2 style={{ fontFamily: 'Orbitron, monospace', marginBottom: 16, color: '#fff', fontSize: 22, fontWeight: 700 }}>Confirm Cancellation</h2>
            <p style={{ color: 'var(--color-text-muted)', marginBottom: 18, fontSize: 14 }}>
              Are you sure you want to cancel shipment <strong>#{shipmentToCancel}</strong>?
            </p>
            <div style={{ display: 'flex', justifyContent: 'flex-end', gap: 10 }}>
              <button className="ss-btn ss-btn-outline" onClick={() => { setCancelModalOpen(false); setShipmentToCancel(null); }}>No, keep it</button>
              <button className="ss-btn" style={{ background: '#e0001a', color: '#fff', border: '1px solid #e0001a' }}
                disabled={busyShipmentId === shipmentToCancel}
                onClick={confirmCancel}>Yes, Cancel</button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
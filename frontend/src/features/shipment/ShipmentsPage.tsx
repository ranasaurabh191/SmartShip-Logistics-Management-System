import { useEffect, useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { apiClient } from '../../core/api/axios';
import { useAuthStore } from '../../store/useAuthStore';

type ShipmentApi = {
  id: number;
  trackingNumber: string;
  customerId: number;
  shipmentType: string;
  status: string;
  shippingRate: number;
  createdAt: string;
  pickupScheduledAt?: string | null;
  deliveredAt?: string | null;
  notes?: string | null;
  senderAddress?: {
    fullName: string;
    city: string;
  };
  receiverAddress?: {
    fullName: string;
    city: string;
  };
  package?: {
    declaredValue?: number;
    weightKg?: number;
  };
};

type PaymentInfo = {
  id: number;
  shipmentId?: number | null;
  trackingNumber: string;
  amount: number;
  paymentMethod: string;
  paymentStatus: string;
  razorpayOrderId?: string | null;
  razorpayPaymentId?: string | null;
  createdAt: string;
  paidAt?: string | null;
  message?: string | null;
};

type ShipmentRow = {
  id: number;
  trackingNumber: string;
  senderFullName: string;
  receiverFullName: string;
  originCity: string;
  destinationCity: string;
  status: string;
  createdAt: string;
  pickupScheduledAt?: string | null;
  deliveredAt?: string | null;
  notes?: string | null;
  shippingRate: number;
  shipmentType: string;
};

const statusStyle: Record<string, string> = {
  InTransit: 'warning',
  Booked: '',
  Delivered: 'success',
  Cancelled: 'muted',
  Draft: 'muted',
  PickedUp: 'warning',
  OutForDelivery: 'warning',
};

const PAYMENT_METHOD = {
  COD: 0,
  ONLINE: 1,
};

export const ShipmentsPage = () => {
  const navigate = useNavigate();
  const user = useAuthStore((state) => state.user);

  const isAdmin = user?.role === 'ADMIN';
  const basePath = isAdmin ? '/admin' : '/customer';
  const shipmentsEndpoint = isAdmin ? '/admin/shipments' : '/shipments/my';
  const ADMIN_NEXT_STATUS: Record<string, string> = {
    Draft: 'Booked',
    Booked: 'PickedUp',
    PickedUp: 'InTransit',
    InTransit: 'OutForDelivery',
    OutForDelivery: 'Delivered',
  };
  const [statusModalOpen, setStatusModalOpen] = useState(false);
  const [resolveModalOpen, setResolveModalOpen] = useState(false);
  const [adminLocation, setAdminLocation] = useState('');
  const [resolutionText, setResolutionText] = useState('');
  const [shipments, setShipments] = useState<ShipmentRow[]>([]);
  const [paymentsByShipment, setPaymentsByShipment] = useState<Record<number, PaymentInfo | null>>({});
  const [filter, setFilter] = useState('ALL');
  const [search, setSearch] = useState('');
  const [loading, setLoading] = useState(true);
  const [busyShipmentId, setBusyShipmentId] = useState<number | null>(null);
  const [pickupModalOpen, setPickupModalOpen] = useState(false);
  const [selectedShipment, setSelectedShipment] = useState<ShipmentRow | null>(null);
  const [pickupDateTime, setPickupDateTime] = useState('');
  const [verifyModalOpen, setVerifyModalOpen] = useState(false);
  const [verifyPayment, setVerifyPayment] = useState<PaymentInfo | null>(null);
  const [verifyForm, setVerifyForm] = useState({
    razorpayPaymentId: '',
    signature: '',
  });
  const openRetryVerifyModal = async (
    e: React.MouseEvent,
    shipment: ShipmentRow
  ) => {
    e.stopPropagation();

    const payment = paymentsByShipment[shipment.id];

    if (!payment?.razorpayOrderId) {
      alert('No pending online payment found.');
      return;
    }

    setVerifyPayment(payment);
    setSelectedShipment(shipment);
    setVerifyForm({
      razorpayPaymentId: '',
      signature: '',
    });
    setVerifyModalOpen(true);
  };
  const confirmRetryVerify = async () => {
    if (!verifyPayment?.razorpayOrderId || !selectedShipment) return;

    try {
      setBusyShipmentId(selectedShipment.id);

      await apiClient.post('/payment/verify', {
        razorpayOrderId: verifyPayment.razorpayOrderId,
        razorpayPaymentId: verifyForm.razorpayPaymentId.trim(),
        signature: verifyForm.signature.trim(),
        shipmentId: selectedShipment.id,
        paymentMethod: 'Online',
      });

      setVerifyModalOpen(false);
      setVerifyPayment(null);
      setSelectedShipment(null);

      await fetchShipments();
    } catch (err: any) {
      alert(
        err?.response?.data?.message ||
        'Payment verification failed.'
      );
    } finally {
      setBusyShipmentId(null);
    }
  };
  const fetchShipments = async () => {
    setLoading(true);
    try {
      const res = await apiClient.get(shipmentsEndpoint, {
        params: { page: 1, pageSize: 25 },
      });

      const responseData = res.data;
      const items: ShipmentApi[] = Array.isArray(responseData)
        ? responseData
        : responseData?.data ?? responseData?.items ?? [];

      const mapped = items.map((s) => ({
        id: s.id,
        trackingNumber: s.trackingNumber,
        senderFullName: s.senderAddress?.fullName || '—',
        receiverFullName: s.receiverAddress?.fullName || '—',
        originCity: s.senderAddress?.city || '—',
        destinationCity: s.receiverAddress?.city || '—',
        status: s.status,
        createdAt: s.createdAt,
        pickupScheduledAt: s.pickupScheduledAt,
        deliveredAt: s.deliveredAt,
        notes: s.notes,
        shippingRate: Number(s.shippingRate || 0),
        shipmentType: s.shipmentType,
      }));

      setShipments(mapped);

      const paymentEntries = await Promise.all(
        mapped.map(async (shipment) => {
          try {
            const paymentRes = await apiClient.get(`/payment/shipment/${shipment.id}`);
            return [shipment.id, paymentRes.data] as const;
          } catch {
            return [shipment.id, null] as const;
          }
        })
      );

      setPaymentsByShipment(Object.fromEntries(paymentEntries));
    } catch {
      setShipments([]);
      setPaymentsByShipment({});
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchShipments();
  }, [shipmentsEndpoint]);

  const statuses = ['ALL', 'Draft', 'Booked', 'PickedUp', 'InTransit', 'Delivered', 'Cancelled'];

  const filtered = useMemo(() => {
    return shipments.filter((s) => {
      const matchesFilter = filter === 'ALL' ? true : s.status === filter;
      const q = search.toLowerCase();
      const matchesSearch =
        s.trackingNumber.toLowerCase().includes(q) ||
        s.senderFullName.toLowerCase().includes(q) ||
        s.receiverFullName.toLowerCase().includes(q) ||
        String(s.id).includes(q);

      return matchesFilter && matchesSearch;
    });
  }, [shipments, filter, search]);

  const handleCancel = async (e: React.MouseEvent, id: number) => {
    e.stopPropagation();
    if (!window.confirm(`Cancel shipment ${id}?`)) return;

    try {
      setBusyShipmentId(id);
      await apiClient.patch(`/shipments/${id}/cancel`, {
        reason: isAdmin ? 'Cancelled by admin' : 'Cancelled by customer',
      });
      await fetchShipments();
    } catch (err: any) {
      alert(err?.response?.data?.message || err?.response?.data || 'Failed to cancel shipment.');
    } finally {
      setBusyShipmentId(null);
    }
  };
  const openPickupModal = (
    e: React.MouseEvent,
    shipment: ShipmentRow
  ) => {
    e.stopPropagation();
    setSelectedShipment(shipment);
    setPickupDateTime('');
    setPickupModalOpen(true);
  };

  const confirmSchedulePickup = async () => {
    if (!selectedShipment) return;

    if (!pickupDateTime) {
      alert('Please select pickup date and time.');
      return;
    }

    try {
      setBusyShipmentId(selectedShipment.id);

      await apiClient.post(
        `/shipments/${selectedShipment.id}/schedule-pickup`,
        {
          pickupTime: pickupDateTime,
        }
      );

      setPickupModalOpen(false);
      setSelectedShipment(null);
      setPickupDateTime('');

      alert('Pickup scheduled successfully.');
      await fetchShipments();
    } catch (err: any) {
      alert(
        err?.response?.data?.message ||
        err?.response?.data ||
        'Unable to schedule pickup.'
      );
    } finally {
      setBusyShipmentId(null);
    }
  };
  const openStatusModal = (
    e: React.MouseEvent,
    shipment: ShipmentRow
  ) => {
    e.stopPropagation();
    setSelectedShipment(shipment);
    setAdminLocation('');
    setStatusModalOpen(true);
  };

  const confirmAdminStatusUpdate = async () => {
    if (!selectedShipment) return;

    const nextStatus =
      ADMIN_NEXT_STATUS[selectedShipment.status];

    if (!nextStatus) {
      alert('No further status transition available.');
      return;
    }

    if (!adminLocation.trim()) {
      alert('Please enter hub location.');
      return;
    }

    try {
      setBusyShipmentId(selectedShipment.id);

      await apiClient.put(
        `/admin/shipments/status/${selectedShipment.id}`,
        {
          status: nextStatus,
          location: adminLocation,
          resolution: '',
        }
      );

      setStatusModalOpen(false);
      setSelectedShipment(null);
      setAdminLocation('');

      await fetchShipments();
    } catch (err: any) {
      alert(
        err?.response?.data?.message ||
        'Failed to update shipment status.'
      );
    } finally {
      setBusyShipmentId(null);
    }
  };

  const openResolveModal = (
    e: React.MouseEvent,
    shipment: ShipmentRow
  ) => {
    e.stopPropagation();
    setSelectedShipment(shipment);
    setAdminLocation('');
    setResolutionText('');
    setResolveModalOpen(true);
  };

  const confirmResolve = async () => {
    if (!selectedShipment) return;

    if (!resolutionText.trim()) {
      alert('Resolution text is required.');
      return;
    }

    if (!adminLocation.trim()) {
      alert('Please enter resolution hub.');
      return;
    }

    try {
      setBusyShipmentId(selectedShipment.id);

      await apiClient.put(
        `/admin/shipments/resolve/${selectedShipment.id}`,
        {
          status: selectedShipment.status,
          location: adminLocation,
          resolution: resolutionText,
        }
      );

      setResolveModalOpen(false);
      setSelectedShipment(null);
      setResolutionText('');
      setAdminLocation('');

      await fetchShipments();
    } catch (err: any) {
      alert(
        err?.response?.data?.message ||
        'Failed to resolve shipment.'
      );
    } finally {
      setBusyShipmentId(null);
    }
  };
  const handleCreatePayment = async (
    e: React.MouseEvent,
    shipment: ShipmentRow,
    mode: 'COD' | 'ONLINE'
  ) => {
    e.stopPropagation();

    try {
      setBusyShipmentId(shipment.id);
      const res = await apiClient.post('/payment/create-order', {
        shipmentId: shipment.id,
        paymentMethod: mode === 'COD' ? PAYMENT_METHOD.COD : PAYMENT_METHOD.ONLINE,
      });

      const payment = res.data as PaymentInfo;

      if (mode === 'COD') {
        alert(payment.message || 'COD registered successfully.');
        await fetchShipments();
        return;
      }

      const paymentId = window.prompt(
        `Mock Razorpay Order Created:\n${payment.razorpayOrderId}\n\nEnter dummy Razorpay Payment ID`
      );
      if (!paymentId) return;

      const signature = window.prompt('Enter dummy payment signature');
      if (!signature) return;

      await apiClient.post('/payment/verify', {
        razorpayOrderId: payment.razorpayOrderId,
        razorpayPaymentId: paymentId,
        signature,
        shipmentId: shipment.id,
        paymentMethod: 'Online',
      });

      alert('Online payment verified successfully.');
      await fetchShipments();
    } catch (err: any) {
      alert(err?.response?.data?.message || err?.response?.data || 'Payment action failed.');
    } finally {
      setBusyShipmentId(null);
    }
  };

  const getPaymentLabel = (payment: PaymentInfo | null | undefined) => {
    if (!payment) return 'Unpaid';
    if (payment.paymentMethod === 'COD' && payment.paymentStatus === 'Pending') return 'COD Registered';
    if (payment.paymentMethod === 'Online' && payment.paymentStatus === 'Pending') return 'Online Pending';
    if (payment.paymentStatus === 'Paid') return 'Paid';
    return payment.paymentStatus || 'Unknown';
  };

  const canSchedulePickup = (shipment: ShipmentRow, payment: PaymentInfo | null | undefined) => {
    if (shipment.status !== 'Draft') return false;
    if (!payment) return false;
    if (payment.paymentMethod === 'COD') return true;
    return payment.paymentMethod === 'Online' && payment.paymentStatus === 'Paid';
  };
  const modalBackdrop = {
    position: 'fixed' as const,
    inset: 0,
    background: 'rgba(0,0,0,0.7)',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    zIndex: 9999,
  };

  const modalCard = {
    width: 480,
    padding: 24,
    border: '1px solid var(--color-border)',
    background: 'var(--color-surface)',
    borderRadius: 8,
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
            ▷ New Shipment
          </button>
        )}
      </div>

      <div className="ss-card" style={{ padding: '14px 16px', display: 'flex', alignItems: 'center', gap: 12, flexWrap: 'wrap' }}>
        <input
          className="ss-input"
          placeholder="Search by tracking, sender, receiver..."
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          style={{ width: 280 }}
        />

        {statuses.map((s) => (
          <button
            key={s}
            onClick={() => setFilter(s)}
            style={{
              fontFamily: 'Rajdhani, sans-serif',
              fontSize: 13,
              fontWeight: 600,
              letterSpacing: '0.1em',
              textTransform: 'uppercase',
              padding: '5px 12px',
              border: '1px solid',
              borderRadius: 2,
              cursor: 'pointer',
              background: 'transparent',
              borderColor: filter === s ? 'var(--color-accent)' : 'var(--color-border)',
              color: filter === s ? 'var(--color-accent)' : 'var(--color-text-muted)',
            }}
          >
            {s}
          </button>
        ))}
      </div>

      <div
        className="ss-card"
        style={{
          padding: 10,
          overflowX: 'auto',
        }}
      >        <table className="ss-table">
          <thead>
            <tr>
              <th>Tracking No.</th>
              <th>Sender</th>
              <th>Receiver</th>
              <th>Origin</th>
              <th>Destination</th>
              <th>Status</th>
              <th>Payment</th>
              <th>Rate</th>
              <th>Created At</th>
              <th>Pickup Time</th>
              <th>Delivered At</th>

              <th>Notes</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {loading ? (
              <tr><td colSpan={10} style={{ textAlign: 'center', padding: 40 }}>LOADING...</td></tr>
            ) : filtered.length === 0 ? (
              <tr><td colSpan={10} style={{ textAlign: 'center', padding: 40 }}>— No records match filter —</td></tr>
            ) : (
              filtered.map((s) => {
                const payment = paymentsByShipment[s.id];
                const scheduleAllowed = canSchedulePickup(s, payment);

                return (
                  <tr key={s.id} style={{ cursor: 'pointer' }} onClick={() => navigate(`${basePath}/track/${s.id}`)}>
                    <td>{s.trackingNumber}</td>
                    <td>{s.senderFullName}</td>
                    <td>{s.receiverFullName}</td>
                    <td>{s.originCity}</td>
                    <td>{s.destinationCity}</td>
                    <td><span className={`ss-badge ${statusStyle[s.status] ?? ''}`} style={{ padding: '3px 5px' }}>{s.status}</span></td>
                    <td><span className="ss-badge" style={{ padding: '3px 5px' }}>{getPaymentLabel(payment)}</span></td>
                    <td>₹{s.shippingRate.toLocaleString('en-IN')}</td>
                    <td>
                      {new Date(s.createdAt).toLocaleString('en-IN', { day: 'numeric', month: 'numeric', year: 'numeric', hour: 'numeric', minute: '2-digit', hour12: true })}
                    </td>
                    <td>
                      {s.pickupScheduledAt ? new Date(s.pickupScheduledAt).toLocaleString('en-IN') : '—'}
                    </td>
                    <td>
                      {s.deliveredAt
                        ? new Date(s.deliveredAt).toLocaleString('en-IN', { day: 'numeric', month: 'numeric', year: 'numeric', hour: 'numeric', minute: '2-digit', hour12: true }) : '—'}
                    </td>
                    <td
                      title={s.notes ?? '—'}
                      style={{ maxWidth: 180, whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis', }}>
                      {s.notes ?? '—'}
                    </td>
                    <td style={{ minWidth: 230 }}>
                      <div
                        style={{ display: 'grid', gridTemplateColumns: 'repeat(2, minmax(10px, 1fr))', gap: 8, width: '100%', alignItems: 'stretch', }}>
                        <button
                          className="ss-btn ss-btn-outline"
                          onClick={(e) => {
                            e.stopPropagation();
                            navigate(`${basePath}/track/${s.id}`);
                          }}
                        >
                          Track
                        </button>

                        {isAdmin ? (
                          <>
                            <button
                              className="ss-btn ss-btn-outline"
                              disabled={busyShipmentId === s.id}
                              onClick={(e) => openResolveModal(e, s)}
                            >
                              Resolve
                            </button>

                            <button
                              className="ss-btn"
                              style={{
                                gridColumn: '1 / -1',
                                padding: '6px 28px',
                                textAlign: 'center',
                              }}
                              disabled={busyShipmentId === s.id}
                              onClick={(e) => openStatusModal(e, s)}
                            >
                              Update Status
                            </button>
                          </>
                        ) : (
                          <>
                            {!payment && s.status === 'Draft' && (
                              <>
                                <button
                                  className="ss-btn"
                                  disabled={busyShipmentId === s.id}
                                  onClick={(e) => handleCreatePayment(e, s, 'COD')}
                                >
                                  COD
                                </button>

                                <button
                                  className="ss-btn ss-btn-outline"
                                  disabled={busyShipmentId === s.id}
                                  onClick={(e) => handleCreatePayment(e, s, 'ONLINE')}
                                >
                                  Pay Online
                                </button>
                              </>
                            )}
                            {(s.status === 'Draft' || s.status === 'Booked') && (
                              <button
                                className="ss-btn ss-btn-outline"
                                disabled={busyShipmentId === s.id}
                                onClick={(e) => handleCancel(e, s.id)}
                              >
                                Cancel
                              </button>
                            )}
                            {payment?.paymentMethod === 'Online' &&
                              payment?.paymentStatus !== 'Paid' &&
                              s.status === 'Draft' && (
                                <button
                                  className="ss-btn"
                                  style={{
                                    gridColumn: '1 / -1',
                                    padding: '6px 70px',
                                    textAlign: 'center',
                                  }}
                                  onClick={(e) => openRetryVerifyModal(e, s)}
                                >
                                  Retry
                                </button>
                              )}



                            {scheduleAllowed && (
                              <button
                                className="ss-btn"
                                style={{
                                  gridColumn: '1 / -1',
                                  padding: '6px 20px',
                                  textAlign: 'center',
                                }}
                                disabled={busyShipmentId === s.id}
                                onClick={(e) => openPickupModal(e, s)}
                              >
                                Schedule Pickup
                              </button>
                            )}
                          </>
                        )}
                      </div>
                    </td>

                  </tr>
                );
              })
            )}
          </tbody>

        </table>
        {verifyModalOpen && verifyPayment && (
          <div
            onClick={() => setVerifyModalOpen(false)}
            style={modalBackdrop}
          >
            <div
              onClick={(e) => e.stopPropagation()}
              className="ss-card"
              style={modalCard}
            >
              <h2 className="section-title">Retry Payment Verification</h2>

              <p className="section-sub" style={{ marginBottom: 16 }}>
                Order: {verifyPayment.razorpayOrderId}
              </p>

              <input
                className="ss-input"
                placeholder="Razorpay Payment ID"
                value={verifyForm.razorpayPaymentId}
                onChange={(e) =>
                  setVerifyForm((prev) => ({
                    ...prev,
                    razorpayPaymentId: e.target.value,
                  }))
                }
                style={{ width: '100%', marginBottom: 12 }}
              />

              <input
                className="ss-input"
                placeholder="Signature"
                value={verifyForm.signature}
                onChange={(e) =>
                  setVerifyForm((prev) => ({
                    ...prev,
                    signature: e.target.value,
                  }))
                }
                style={{ width: '100%', marginBottom: 20 }}
              />

              <div style={{ display: 'flex', justifyContent: 'flex-end', gap: 10 }}>
                <button
                  className="ss-btn ss-btn-outline"
                  onClick={() => setVerifyModalOpen(false)}
                >
                  Cancel
                </button>

                <button
                  className="ss-btn"
                  onClick={confirmRetryVerify}
                >
                  Verify Payment
                </button>
              </div>
            </div>
          </div>
        )}
        {statusModalOpen && isAdmin && (
          <div
            onClick={() => setStatusModalOpen(false)}
            style={modalBackdrop}
          >
            <div
              onClick={(e) => e.stopPropagation()}
              className="ss-card"
              style={modalCard}
            >
              <h2 className="section-title">Update Shipment Status</h2>

              <input
                className="ss-input"
                placeholder="Enter current hub location"
                value={adminLocation}
                onChange={(e) => setAdminLocation(e.target.value)}
                style={{ width: '100%', marginTop: 16 }}
              />

              <div
                style={{
                  display: 'flex',
                  justifyContent: 'flex-end',
                  gap: 10,
                  marginTop: 20,
                }}
              >
                <button
                  className="ss-btn ss-btn-outline"
                  onClick={() => setStatusModalOpen(false)}
                >
                  Cancel
                </button>

                <button
                  className="ss-btn"
                  onClick={confirmAdminStatusUpdate}
                >
                  Confirm Update
                </button>
              </div>
            </div>
          </div>
        )}
        {resolveModalOpen && isAdmin && (
          <div
            onClick={() => setResolveModalOpen(false)}
            style={modalBackdrop}
          >
            <div
              onClick={(e) => e.stopPropagation()}
              className="ss-card"
              style={modalCard}
            >
              <h2 className="section-title">Resolve Shipment</h2>

              <input
                className="ss-input"
                placeholder="Resolution hub location"
                value={adminLocation}
                onChange={(e) => setAdminLocation(e.target.value)}
                style={{ width: '100%', marginBottom: 14 }}
              />

              <textarea
                className="ss-input"
                placeholder="Enter resolution details"
                value={resolutionText}
                onChange={(e) =>
                  setResolutionText(e.target.value)
                }
                style={{
                  width: '100%',
                  minHeight: 120,
                  resize: 'vertical',
                }}
              />

              <div
                style={{
                  display: 'flex',
                  justifyContent: 'flex-end',
                  gap: 10,
                  marginTop: 20,
                }}
              >
                <button
                  className="ss-btn ss-btn-outline"
                  onClick={() => setResolveModalOpen(false)}
                >
                  Cancel
                </button>

                <button
                  className="ss-btn"
                  onClick={confirmResolve}
                >
                  Confirm Resolve
                </button>
              </div>
            </div>
          </div>
        )}
      </div>
      {pickupModalOpen && (
        <div
          onClick={() => setPickupModalOpen(false)}
          style={{
            position: 'fixed',
            inset: 0,
            background: 'rgba(0, 0, 0, 0.7)',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            zIndex: 9999,
          }}
        >
          <div
            onClick={(e) => e.stopPropagation()}
            className="ss-card"
            style={{
              width: 460,
              padding: 24,
              border: '1px solid var(--color-border)',
              background: 'var(--color-surface)',
              borderRadius: 8,
              boxShadow: '0 0 40px rgba(0,0,0,0.45)',
            }}
          >
            <h2
              style={{
                fontFamily: 'Rajdhani, sans-serif',
                marginBottom: 16,
                color: '#fff',
                fontSize: 22,
                fontWeight: 700,
              }}
            >
              Schedule Pickup
            </h2>

            <p
              style={{
                color: 'var(--color-text-muted)',
                marginBottom: 18,
                fontSize: 14,
              }}
            >
              Select pickup date and time for shipment{' '}
              <strong>{selectedShipment?.trackingNumber}</strong>
            </p>

            <input
              type="datetime-local"
              className="ss-input"
              value={pickupDateTime}
              onChange={(e) => setPickupDateTime(e.target.value)}
              style={{
                width: '100%',
                marginBottom: 20,
              }}
            />

            <div style={{ display: 'flex', justifyContent: 'flex-end', gap: 10 }}>
              <button
                className="ss-btn ss-btn-outline"
                onClick={() => setPickupModalOpen(false)}
              >
                Cancel
              </button>

              <button
                className="ss-btn"
                disabled={busyShipmentId === selectedShipment?.id}
                onClick={confirmSchedulePickup}
              >
                Confirm Pickup
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
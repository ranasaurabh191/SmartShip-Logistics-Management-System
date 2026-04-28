import { useEffect, useState } from 'react';
import { apiClient } from '../../core/api/axios';
import { useAuthStore } from '../../store/useAuthStore';
import { useNotificationStore } from '../../store/useNotificationStore';

interface Payment {
  id: number;
  shipmentId: number | null;
  trackingNumber: string;
  amount: number;
  paymentMethod: string;
  paymentStatus: string;
  razorpayOrderId?: string;
  createdAt: string;
  paidAt?: string;
}

const RAZORPAY_KEY_ID = import.meta.env.VITE_RAZORPAY_KEY_ID as string;

export const PaymentsPage = () => {
  const user = useAuthStore((state) => state.user);
  const addNotification = useNotificationStore((state) => state.addNotification);
  const isAdmin = user?.role?.toLowerCase() === 'admin';

  const [payments, setPayments] = useState<Payment[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  const [verifyModalOpen, setVerifyModalOpen] = useState(false);
  const [selectedPayment, setSelectedPayment] = useState<Payment | null>(null);
  const [verifyLoading, setVerifyLoading] = useState(false);
  const [verifyForm, setVerifyForm] = useState({ razorpayPaymentId: '', signature: '' });

  const fetchPayments = async (signal?: AbortSignal) => {
    try {
      setLoading(true);
      const endpoint = isAdmin ? '/payment/all' : '/payment/my';
      const res = await apiClient.get(endpoint, { signal });
      const data = Array.isArray(res.data)
        ? res.data
        : res.data?.data ?? res.data?.items ?? [];
      setPayments(data);
      setError('');
    } catch (err: any) {
      if (err?.code === 'ERR_CANCELED') return;
      setError(
        err?.response?.data?.message ||
          err?.response?.data?.error ||
          'Failed to load payments.'
      );
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    const controller = new AbortController();
    fetchPayments(controller.signal);
    return () => controller.abort();
  }, [isAdmin]);

  const handlePayNow = (payment: Payment) => {
    if (!payment.razorpayOrderId) {
      addNotification('No Razorpay order found. Please try creating the payment again.', 'error');
      return;
    }

    if (!window.Razorpay) {
      addNotification('Razorpay SDK not loaded. Please refresh the page.', 'error');
      return;
    }

    const options: RazorpayOptions = {
      key: RAZORPAY_KEY_ID,
      amount: payment.amount * 100, // paise
      currency: 'INR',
      name: 'SmartShip',
      description: `Payment for Shipment #${payment.shipmentId}`,
      order_id: payment.razorpayOrderId,

      handler: async (response: RazorpayPaymentResponse) => {
        try {
          await apiClient.post('/payment/verify', {
            razorpayOrderId: response.razorpay_order_id,
            razorpayPaymentId: response.razorpay_payment_id,
            signature: response.razorpay_signature,
            shipmentId: payment.shipmentId,
            paymentMethod: 'Online',
          });
          addNotification('Payment verified successfully!', 'success');
          await fetchPayments();
        } catch (err: any) {
          addNotification(
            err?.response?.data?.message ||
              'Payment was made but verification failed. Please use Retry Verify.',
            'error'
          );
          await fetchPayments();
        }
      },

      prefill: {
        name: user?.name ?? '',
        email: user?.email ?? '',
      },

      theme: { color: '#0057ff' },

      modal: {
        ondismiss: () => {
          console.log('Razorpay modal closed by user.');
        },
      },
    };

    const rzp = new window.Razorpay(options);
    rzp.open();
  };


  const closeVerifyModal = () => {
    setVerifyModalOpen(false);
    setSelectedPayment(null);
    setVerifyForm({ razorpayPaymentId: '', signature: '' });
  };

  const handleRetryVerify = async () => {
    if (!selectedPayment?.razorpayOrderId) return;
    try {
      setVerifyLoading(true);
      await apiClient.post('/payment/verify', {
        razorpayOrderId: selectedPayment.razorpayOrderId,
        razorpayPaymentId: verifyForm.razorpayPaymentId.trim(),
        signature: verifyForm.signature.trim(),
        shipmentId: selectedPayment.shipmentId,
        paymentMethod: 'Online',
      });
      closeVerifyModal();
      await fetchPayments();
    } catch (err: any) {
      addNotification(err?.response?.data?.message || 'Payment verification failed.', 'error');
    } finally {
      setVerifyLoading(false);
    }
  };

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 20, maxWidth: 1100 }}>
      <div>
        <div className="accent-line" style={{ marginBottom: 8 }} />
        <h1 className="section-title">
          {isAdmin ? 'All Customer Payments' : 'My Payments'}
        </h1>
        <p className="section-sub">
          {isAdmin
            ? 'Monitor all customer payment activity'
            : 'Your shipment payment history'}
        </p>
      </div>

      <div className="ss-card" style={{ padding: '20px 24px', overflowX: 'auto' }}>
        {loading && <div>LOADING...</div>}
        {!loading && error && <div style={{ color: '#e0001a', fontSize: 13 }}>{error}</div>}
        {!loading && !error && payments.length === 0 && <div>No payments found.</div>}

        {!loading && !error && payments.length > 0 && (
          <table className="ss-table">
            <thead>
              <tr>
                <th>Tracking #</th>
                <th>Shipment ID</th>
                <th>Amount</th>
                <th>Method</th>
                <th>Status</th>
                <th>Date</th>
                {!isAdmin && <th>Action</th>}
              </tr>
            </thead>

            <tbody>
              {payments.map((p) => (
                <tr key={p.id}>
                  <td>{p.trackingNumber || '—'}</td>
                  <td>{p.shipmentId ?? '—'}</td>
                  <td>₹{Number(p.amount).toLocaleString('en-IN')}</td>
                  <td>{p.paymentMethod}</td>
                  <td>
                    <span className={`ss-badge ${p.paymentStatus === 'Paid' ? 'success' : ''}`}>
                      {p.paymentStatus}
                    </span>
                  </td>
                  <td>{new Date(p.createdAt).toLocaleDateString('en-IN')}</td>

                  {!isAdmin && (
                    <td>
                      {p.paymentMethod?.toLowerCase() === 'online' &&
                      p.paymentStatus?.toLowerCase() !== 'paid' &&
                      p.razorpayOrderId ? (
                        <div style={{ display: 'flex', gap: 8 }}>
                          {/* Primary — opens Razorpay checkout */}
                          <button
                            className="ss-btn"
                            onClick={() => handlePayNow(p)}
                          >
                            Pay Now
                          </button>

                        </div>
                      ) : (
                        '—'
                      )}
                    </td>
                  )}
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      {/* Manual Retry Verify Modal */}
      {verifyModalOpen && selectedPayment && (
        <div
          onClick={closeVerifyModal}
          style={{
            position: 'fixed', inset: 0,
            background: 'rgba(0,0,0,0.7)',
            display: 'flex', alignItems: 'center', justifyContent: 'center',
            zIndex: 9999,
          }}
        >
          <div
            onClick={(e) => e.stopPropagation()}
            className="ss-card"
            style={{ width: 500, padding: 24 }}
          >
            <h2 className="section-title">Retry Payment Verification</h2>

            <input
              className="ss-input"
              placeholder="Razorpay Payment ID (pay_...)"
              value={verifyForm.razorpayPaymentId}
              onChange={(e) => setVerifyForm((prev) => ({ ...prev, razorpayPaymentId: e.target.value }))}
              style={{ width: '100%', marginBottom: 12 }}
            />

            <input
              className="ss-input"
              placeholder="Signature"
              value={verifyForm.signature}
              onChange={(e) => setVerifyForm((prev) => ({ ...prev, signature: e.target.value }))}
              style={{ width: '100%', marginBottom: 20 }}
            />

            <button
              className="ss-btn"
              onClick={handleRetryVerify}
              disabled={
                verifyLoading ||
                !verifyForm.razorpayPaymentId.trim() ||
                !verifyForm.signature.trim()
              }
            >
              {verifyLoading ? 'VERIFYING...' : 'Verify Payment'}
            </button>
          </div>
        </div>
      )}
    </div>
  );
};
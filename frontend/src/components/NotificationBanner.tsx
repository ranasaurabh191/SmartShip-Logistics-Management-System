import { useNotificationStore } from '../store/useNotificationStore';

export const NotificationBanner = () => {
  const { notifications, removeNotification } = useNotificationStore();

  if (notifications.length === 0) return null;

  return (
    <div style={{
      position: 'fixed',
      top: 20,
      left: '50%',
      transform: 'translateX(-50%)',
      zIndex: 10000,
      display: 'flex',
      flexDirection: 'column',
      gap: 10,
      width: 'max-content',
      maxWidth: '90%',
    }}>
      {notifications.map((notif) => {
        let bgColor = '#333';
        let color = '#fff';
        let borderColor = '#555';

        if (notif.type === 'error') {
          bgColor = '#ffebee';
          color = '#c62828';
          borderColor = '#ef9a9a';
        } else if (notif.type === 'success') {
          bgColor = '#e8f5e9';
          color = '#2e7d32';
          borderColor = '#a5d6a7';
        } else if (notif.type === 'info') {
          bgColor = '#e3f2fd';
          color = '#1565c0';
          borderColor = '#90caf9';
        } else if (notif.type === 'warning') {
          bgColor = '#fff3e0';
          color = '#ef6c00';
          borderColor = '#ffcc80';
        }

        return (
          <div
            key={notif.id}
            style={{
              padding: '12px 24px',
              borderRadius: '8px',
              background: bgColor,
              color: color,
              border: `1px solid ${borderColor}`,
              boxShadow: '0 4px 12px rgba(0,0,0,0.15)',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'space-between',
              gap: '16px',
              fontFamily: 'Orbitron, Rajdhani, sans-serif',
              fontWeight: 600,
              fontSize: '14px',
              letterSpacing: '0.05em',
              animation: 'slideDown 0.3s ease-out forwards',
            }}
          >
            <span>{notif.text}</span>
            <button
              onClick={() => removeNotification(notif.id)}
              style={{
                background: 'transparent',
                border: 'none',
                color: color,
                cursor: 'pointer',
                fontSize: '16px',
                fontWeight: 'bold',
                padding: '0 4px',
                opacity: 0.7,
              }}
              onMouseOver={(e) => ((e.target as HTMLElement).style.opacity = '1')}
              onMouseOut={(e) => ((e.target as HTMLElement).style.opacity = '0.7')}
            >
              ×
            </button>
          </div>
        );
      })}
      <style>
        {`
          @keyframes slideDown {
            from {
              opacity: 0;
              transform: translateY(-20px);
            }
            to {
              opacity: 1;
              transform: translateY(0);
            }
          }
        `}
      </style>
    </div>
  );
};

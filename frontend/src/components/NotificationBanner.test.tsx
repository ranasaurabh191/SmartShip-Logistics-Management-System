import { render, screen, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import { NotificationBanner } from './NotificationBanner';
import { useNotificationStore } from '../store/useNotificationStore';

vi.mock('../store/useNotificationStore', () => ({
  useNotificationStore: vi.fn(),
}));

describe('Component Test: NotificationBanner', () => {
  it('should render absolutely nothing when notification queue is empty', () => {
    (useNotificationStore as any).mockReturnValue({
      notifications: [],
      removeNotification: vi.fn(),
    });

    const { container } = render(<NotificationBanner />);
    
    expect(container.firstChild).toBeNull();
  });

  it('should display current notifications accurately', () => {
    const mockQueue = [
      { id: '1', text: 'Shipment updated', type: 'success' },
      { id: '2', text: 'Critical gateway failure', type: 'error' },
    ];

    (useNotificationStore as any).mockReturnValue({
      notifications: mockQueue,
      removeNotification: vi.fn(),
    });

    render(<NotificationBanner />);

    expect(screen.getByText('Shipment updated')).toBeInTheDocument();
    expect(screen.getByText('Critical gateway failure')).toBeInTheDocument();
  });

  it('should execute state removal dispatcher upon clicking close button', () => {
    const removeSpy = vi.fn();
    const mockQueue = [{ id: 'id-to-delete', text: 'Alert text', type: 'warning' }];

    (useNotificationStore as any).mockReturnValue({
      notifications: mockQueue,
      removeNotification: removeSpy,
    });

    render(<NotificationBanner />);

    const closeButton = screen.getByText('×');
    fireEvent.click(closeButton);

    expect(removeSpy).toHaveBeenCalledTimes(1);
    expect(removeSpy).toHaveBeenCalledWith('id-to-delete');
  });
});

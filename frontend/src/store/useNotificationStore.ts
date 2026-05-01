import { create } from 'zustand';

export type NotificationType = 'success' | 'error' | 'info' | 'warning';

interface NotificationMessage {
  id: string;
  text: string;
  type: NotificationType;
}

interface NotificationStore {
  notifications: NotificationMessage[];
  addNotification: (text: string, type: NotificationType) => void;
  removeNotification: (id: string) => void;
}

export const useNotificationStore = create<NotificationStore>((set) => ({
  notifications: [],
  addNotification: (text, type) => {
    const id = Math.random().toString(36).substring(7);
    set((state) => ({
      notifications: [...state.notifications, { id, text, type }],
    }));
    setTimeout(() => {
      set((state) => ({
        notifications: state.notifications.filter((n) => n.id !== id),
      }));
    }, 4000);
  },
  removeNotification: (id) =>
    set((state) => ({
      notifications: state.notifications.filter((n) => n.id !== id),
    })),
}));

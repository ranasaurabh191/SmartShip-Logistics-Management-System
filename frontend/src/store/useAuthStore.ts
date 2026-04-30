import { create } from 'zustand';

interface User {
  id: number;
  email: string;
  name: string;
  role: 'CUSTOMER' | 'ADMIN';
}

interface AuthState {
  user: User | null;
  token: string | null;
  isAuthenticated: boolean;
  hydrate: () => void;
  login: (user: User, token: string) => void;
  logout: () => void;
}

const getStoredUser = (): User | null => {
  const raw = sessionStorage.getItem('user');
  if (!raw) return null;

  try {
    return JSON.parse(raw);
  } catch {
    return null;
  }
};

export const useAuthStore = create<AuthState>((set) => ({
  user: getStoredUser(),
  token: sessionStorage.getItem('token'),
  isAuthenticated: !!sessionStorage.getItem('token'),

  hydrate: () => {
    set({
      user: getStoredUser(),
      token: sessionStorage.getItem('token'),
      isAuthenticated: !!sessionStorage.getItem('token'),
    });
  },

  login: (user, token) => {
    sessionStorage.setItem('token', token);
    sessionStorage.setItem('user', JSON.stringify(user));

    set({
      user,
      token,
      isAuthenticated: true,
    });
  },

  logout: () => {
    sessionStorage.removeItem('token');
    sessionStorage.removeItem('user');

    set({
      user: null,
      token: null,
      isAuthenticated: false,
    });
  },
}));
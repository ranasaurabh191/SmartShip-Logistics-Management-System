import { render } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { DashboardLayout } from './DashboardLayout';
import { useAuthStore } from '../store/useAuthStore';

const mockNavigate = vi.fn();
vi.mock('react-router-dom', () => ({
  useNavigate: () => mockNavigate,
  useLocation: () => ({ pathname: '/any-path' }),
  Outlet: () => <div data-testid="secured-outlet">Protected Content Rendered</div>,
}));

vi.mock('../store/useAuthStore', () => ({
  useAuthStore: vi.fn(),
}));

vi.mock('framer-motion', () => ({
  motion: {
    div: ({ children, ...props }: any) => <div {...props}>{children}</div>,
  },
  AnimatePresence: ({ children }: any) => <>{children}</>,
}));

describe('Route Guard Integration: DashboardLayout', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('should execute emergency redirect to login gateway when session is absent', () => {
    (useAuthStore as any).mockReturnValue({
      isAuthenticated: false,
      user: null,
      logout: vi.fn(),
    });

    render(<DashboardLayout role="CUSTOMER" />);

    expect(mockNavigate).toHaveBeenCalledWith('/auth/login', { replace: true });
  });

  it('should resolve protected node successfully when identities and constraints align', () => {
    (useAuthStore as any).mockReturnValue({
      isAuthenticated: true,
      user: { id: 42, name: 'Arthur Dent', role: 'CUSTOMER' },
      logout: vi.fn(),
    });

    const { queryByTestId } = render(<DashboardLayout role="CUSTOMER" />);

    expect(mockNavigate).not.toHaveBeenCalled();
    
    expect(queryByTestId('secured-outlet')).toBeInTheDocument();
  });

  it('should actively prohibit cross-role entry and redirect CUSTOMER to standard panel', () => {
    (useAuthStore as any).mockReturnValue({
      isAuthenticated: true,
      user: { id: 1, name: 'Standard User', role: 'CUSTOMER' },
      logout: vi.fn(),
    });

    render(<DashboardLayout role="ADMIN" />);

    expect(mockNavigate).toHaveBeenCalledWith('/customer/dashboard', { replace: true });
  });

  it('should aggressively restrict ADMIN elevation to customer-view and redirect into management grid', () => {
    (useAuthStore as any).mockReturnValue({
      isAuthenticated: true,
      user: { id: 99, name: 'Super Admin', role: 'ADMIN' },
      logout: vi.fn(),
    });

    render(<DashboardLayout role="CUSTOMER" />);

    expect(mockNavigate).toHaveBeenCalledWith('/admin/dashboard', { replace: true });
  });
});

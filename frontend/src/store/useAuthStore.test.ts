import { renderHook, act } from '@testing-library/react';
import { describe, it, expect, beforeEach } from 'vitest';
import { useAuthStore } from './useAuthStore';

describe('Service Store Test: useAuthStore', () => {
  beforeEach(() => {
    sessionStorage.clear();
    
    act(() => {
      useAuthStore.getState().logout();
    });
  });

  it('should initialize state as unauthenticated', () => {
    const { result } = renderHook(() => useAuthStore());

    expect(result.current.isAuthenticated).toBe(false);
    expect(result.current.user).toBeNull();
    expect(result.current.token).toBeNull();
  });

  it('should successfully persist data upon login invocation', () => {
    const { result } = renderHook(() => useAuthStore());
    
    const userPayload = {
      id: 101,
      email: 'integration@tester.com',
      name: 'Tester Q.A.',
      role: 'ADMIN' as const
    };
    const jwtToken = 'secure-jwt-signature-token';

    act(() => {
      result.current.login(userPayload, jwtToken);
    });

    expect(result.current.isAuthenticated).toBe(true);
    expect(result.current.user).toEqual(userPayload);
    expect(result.current.token).toBe(jwtToken);

    expect(sessionStorage.getItem('token')).toBe(jwtToken);
    expect(sessionStorage.getItem('user')).toContain('Tester Q.A.');
  });

  it('should clear memory and browser storage clean on logout invocation', () => {
    const { result } = renderHook(() => useAuthStore());
    
    act(() => {
      result.current.login(
        { id: 2, email: 'temp@user.com', name: 'Tmp User', role: 'CUSTOMER' },
        'existing-token'
      );
    });

    expect(result.current.isAuthenticated).toBe(true);

    act(() => {
      result.current.logout();
    });

    expect(result.current.isAuthenticated).toBe(false);
    expect(result.current.user).toBeNull();
    expect(sessionStorage.getItem('token')).toBeNull();
    expect(sessionStorage.getItem('user')).toBeNull();
  });
});

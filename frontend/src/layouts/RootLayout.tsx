import { Outlet } from 'react-router-dom';

export const RootLayout = () => (
  <div style={{ minHeight: '100vh', background: 'var(--color-bg)' }}>
    <Outlet />
  </div>
);

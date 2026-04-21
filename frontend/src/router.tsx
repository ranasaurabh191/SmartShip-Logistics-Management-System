import { createBrowserRouter, Navigate } from 'react-router-dom';
import { OAuthCallbackPage } from './features/auth/OAuthCallbackPage';

// Layouts
import { RootLayout } from './layouts/RootLayout';
import { AuthLayout } from './layouts/AuthLayout';
import { DashboardLayout } from './layouts/DashboardLayout';

// Auth Pages
import { LoginPage } from './features/auth/LoginPage';
import { SignupPage } from './features/auth/SignupPage';

// Public / Landing
import { LandingPage } from './features/public/LandingPage';

// Customer Pages
import { CustomerDashboard } from './features/customer/CustomerDashboard';
import { PaymentsPage } from './features/customer/PaymentsPage';

// Shipment Pages
import { CreateShipmentWizard } from './features/shipment/CreateShipmentWizard';
import { TrackShipment } from './features/shipment/TrackShipment';
import { ShipmentsPage } from './features/shipment/ShipmentsPage';

// Admin Pages
import { AdminDashboard } from './features/admin/AdminDashboard';
import { HubManagement } from './features/admin/HubManagement';
import { UserManagement } from './features/admin/UserManagement';

export const router = createBrowserRouter([
  {
    path: '/',
    element: <RootLayout />,
    children: [
      { index: true, element: <LandingPage /> },
      {
        path: 'auth',
        element: <AuthLayout />,
        children: [
          { path: 'login', element: <LoginPage /> },
          { path: 'signup', element: <SignupPage /> },
          { path: 'callback', element: <OAuthCallbackPage /> },

        ],
      },
      {
        path: 'customer',
        element: <DashboardLayout role="CUSTOMER" />,
        children: [
          { path: 'dashboard', element: <CustomerDashboard /> },
          { path: 'shipments', element: <ShipmentsPage /> },
          { path: 'shipments/create', element: <CreateShipmentWizard /> },
          { path: 'tracking', element: <TrackShipment /> },
          { path: 'track/:id', element: <TrackShipment /> },
          { path: 'payments', element: <PaymentsPage /> },
        ],
      },
      {
        path: 'admin',
        element: <DashboardLayout role="ADMIN" />,
        children: [
          { path: 'dashboard', element: <AdminDashboard /> },
          { path: 'shipments', element: <ShipmentsPage /> },
          { path: 'tracking', element: <TrackShipment /> },
          { path: 'track/:id', element: <TrackShipment /> },
          { path: 'payments', element: <PaymentsPage /> },
          { path: 'hubs', element: <HubManagement /> },
          { path: 'users', element: <UserManagement /> },
          { path: 'panel', element: <Navigate to="/admin/dashboard" replace /> },
        ],
      },
    ],
  },
  { path: '*', element: <Navigate to="/" replace /> },
]);
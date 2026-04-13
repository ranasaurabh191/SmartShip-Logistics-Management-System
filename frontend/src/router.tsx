import { createBrowserRouter, Navigate } from 'react-router-dom';

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
import { SagaViewer } from './features/customer/SagaViewer';

// Shipment Pages
import { CreateShipmentWizard } from './features/shipment/CreateShipmentWizard';
import { TrackShipment } from './features/shipment/TrackShipment';
import { ShipmentsPage } from './features/shipment/ShipmentsPage';

// Admin Pages
import { AdminPanel } from './features/admin/AdminDashboard';
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
        ],
      },
      {
        path: 'customer',
        element: <DashboardLayout role="Customer" />,
        children: [
          { path: 'dashboard', element: <CustomerDashboard /> },
          { path: 'shipments', element: <ShipmentsPage /> },
          { path: 'shipments/create', element: <CreateShipmentWizard /> },
          { path: 'tracking', element: <TrackShipment /> },
          { path: 'track/:id', element: <TrackShipment /> },
          { path: 'payments', element: <PaymentsPage /> },
          { path: 'saga', element: <SagaViewer /> },
        ],
      },
      {
        path: 'admin',
        element: <DashboardLayout role="Admin" />,
        children: [
          { path: 'dashboard', element: <CustomerDashboard /> },
          { path: 'shipments', element: <ShipmentsPage /> },
          { path: 'tracking', element: <TrackShipment /> },
          { path: 'track/:id', element: <TrackShipment /> },
          { path: 'payments', element: <PaymentsPage /> },
          { path: 'panel', element: <AdminPanel /> },
          { path: 'hubs', element: <HubManagement /> },
          { path: 'users', element: <UserManagement /> },
          { path: 'saga', element: <SagaViewer /> },
        ],
      },
    ],
  },
  { path: '*', element: <Navigate to="/" replace /> },
]);

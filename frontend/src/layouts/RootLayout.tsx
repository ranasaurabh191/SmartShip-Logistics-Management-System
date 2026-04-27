import { Outlet } from 'react-router-dom';
import { ChatWidget } from '../features/public/ChatWidget';
import { useChatStore } from '../store/useChatStore';

export const RootLayout = () => (
    <div style={{ minHeight: '100vh', background: 'var(--color-bg)' }}>
        <Outlet />
        <ChatWidget shipmentId={useChatStore(state => state.shipmentId)}/>
    </div>
);

import { useCallback, useState } from 'react';
import { apiClient } from '../../core/api/axios';


export interface ShipmentChip {
    shipmentId: number;
    trackingNumber: string;
    label: string;
    status: string;
}


interface Message {
    role: 'user' | 'bot';
    text: string;
    timestamp: Date;
    shipmentChips?: ShipmentChip[];
}


export const useChat = (shipmentId?: number) => {
    const [messages, setMessages] = useState<Message[]>([
        {
            role: 'bot',
            text: '👋 Hi! I\'m your SmartShip assistant. Ask me about tracking, documents, or delivery. Type **help** for options.',
            timestamp: new Date()
        }
    ]);

    const [loading, setLoading] = useState(false);
    const [activeShipmentId, setActiveShipmentId] = useState<number | undefined>(shipmentId);

    const sendMessage = async (
        text: string,
        overrideShipmentId?: number,
        selectedShipmentId?: number,
        reset?: boolean
    ) => {
        if (!text.trim()) return;

        if (selectedShipmentId) setActiveShipmentId(selectedShipmentId);
        if (reset) setActiveShipmentId(shipmentId);

        const effectiveShipmentId = selectedShipmentId ?? overrideShipmentId ?? activeShipmentId ?? shipmentId;

        const userMsg: Message = { role: 'user', text, timestamp: new Date() };
        setMessages(prev => [...prev, userMsg]);
        setLoading(true);

        try {
            const res = await apiClient.post('/chat', {
                message: text,
                shipmentId: effectiveShipmentId ?? null,
                selectedShipmentId: selectedShipmentId ?? null,
                history: messages.slice(-6).map(m => ({ role: m.role, text: m.text })),
            });

            const botMsg: Message = {
                role: 'bot',
                text: res.data.reply,
                timestamp: new Date(),
                shipmentChips: res.data.shipmentChips ?? undefined,
            };
            setMessages(prev => [...prev, botMsg]);
        } catch {
            setMessages(prev => [...prev, {
                role: 'bot',
                text: '⚠️ Something went wrong. Please try again.',
                timestamp: new Date(),
            }]);
        } finally {
            setLoading(false);
        }
    };

    const clearChat = useCallback(() => {
        setMessages([{
            role: 'bot',
            text: '👋 Hi! I\'m your SmartShip assistant. Ask me about tracking, documents, or delivery. Type **help** for options.',
            timestamp: new Date()
        }]);
        setActiveShipmentId(undefined);
    }, []);

    return { messages, loading, sendMessage, clearChat, activeShipmentId };
};
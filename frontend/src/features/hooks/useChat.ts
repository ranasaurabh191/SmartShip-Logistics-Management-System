import { useState } from 'react';
import { apiClient } from '../../core/api/axios';


export interface ShipmentChip {
    shipmentId: number;
    trackingNumber: string;
    label: string;
    status: string;
}


export interface Message {
    role: 'user' | 'bot';
    text: string;
    timestamp: Date;
    shipmentChips?: ShipmentChip[];
}


export const useChat = (shipmentId?: number) => {
    const [messages, setMessages] = useState<Message[]>([{
        role: 'bot',
        text: '👋 Hi! I\'m your SmartShip assistant. Ask me about tracking, documents, or delivery. Type **help** for options.',
        timestamp: new Date(),
    }]);
    const [loading, setLoading] = useState(false);


    const sendMessage = async (
        text: string,
        activeShipmentId?: number,
        selectedShipmentId?: number
    ) => {
        if (!text.trim()) return;

        const userMsg: Message = { role: 'user', text, timestamp: new Date() };
        setMessages(prev => [...prev, userMsg]);
        setLoading(true);

        try {
            const res = await apiClient.post('/chat', {
                message: text,
                shipmentId: activeShipmentId ?? shipmentId ?? null,
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


    const clearChat = () => setMessages([{
        role: 'bot',
        text: '👋 Chat cleared! How can I help you?',
        timestamp: new Date(),
    }]);


    return { messages, loading, sendMessage, clearChat };
};
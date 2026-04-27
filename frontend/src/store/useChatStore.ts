import { create } from 'zustand';

interface ChatStore {
  shipmentId?: number;
  setShipmentId: (id?: number) => void;
}

export const useChatStore = create<ChatStore>(set => ({
  shipmentId: undefined,
  setShipmentId: id => set({ shipmentId: id }),
}));
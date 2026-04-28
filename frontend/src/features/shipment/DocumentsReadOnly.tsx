import { useEffect, useState } from 'react';
import { apiClient } from '../../core/api/axios';
import { useNotificationStore } from '../../store/useNotificationStore';

interface DocumentDto {
    id: number;
    fileName: string;
    documentType: string;
    fileSizeBytes: number;
    uploadedAt: string;
    filePath?: string;
}

interface Props {
    shipmentId: number;
}

function formatBytes(bytes: number): string {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(2)} MB`;
}

export const DocumentsReadOnly = ({ shipmentId }: Props) => {
    const [docs, setDocs] = useState<DocumentDto[]>([]);
    const [loading, setLoading] = useState(true);
    const addNotification = useNotificationStore(state => state.addNotification);

    useEffect(() => {
        const fetch = async () => {
            setLoading(true);
            try {
                const res = await apiClient.get(`/tracking/documents/${shipmentId}`);
                const raw = res.data;
                const items: DocumentDto[] = Array.isArray(raw) ? raw : raw?.data ?? raw?.items ?? [];
                setDocs(items);
            } catch {
                setDocs([]);
            } finally {
                setLoading(false);
            }
        };
        fetch();
    }, [shipmentId]);

    const sectionLabel: React.CSSProperties = {
        fontFamily: 'Orbitron, monospace', fontSize: 11, fontWeight: 600,
        letterSpacing: '0.14em', textTransform: 'uppercase', color: '#888', marginBottom: 8,
    };

    const getFileIcon = (fileName: string) => {
        const ext = fileName.split('.').pop()?.toLowerCase();
        if (ext === 'pdf') return '📄';
        if (['jpg', 'jpeg', 'png'].includes(ext ?? '')) return '🖼️';
        return '📎';
    };

    const handleOpen = async (doc: DocumentDto) => {
        try {
            const res = await apiClient.get(`/tracking/document/file/${doc.id}`, {
                responseType: 'blob', 
            });

            const blob = new Blob([res.data], { type: res.headers['content-type'] });
            const url = URL.createObjectURL(blob);

            const a = window.open(url, '_blank', 'noopener,noreferrer');

            setTimeout(() => URL.revokeObjectURL(url), 60_000);

            if (!a) {
                const link = document.createElement('a');
                link.href = url;
                link.download = doc.fileName;
                link.click();
            }
        } catch (err: any) {
            addNotification(err?.response?.data?.message || 'Failed to open document.', 'error');
        }
    };
    return (
        <div style={{ display: 'flex', flexDirection: 'column', gap: 20 }}>
            {/* Header */}
            <div style={{ borderBottom: '1px solid rgba(255,255,255,0.08)', paddingBottom: 14 }}>
                <div className="accent-line" style={{ marginBottom: 6 }} />
                <h2 style={{ fontFamily: 'Orbitron, monospace', fontSize: 14, fontWeight: 700, letterSpacing: '0.1em', textTransform: 'uppercase', color: '#fff', marginBottom: 4 }}>
                    Shipment Documents
                </h2>
                <p style={{ fontSize: 12, color: '#888' }}>View and open invoices, labels, and other documents uploaded by the customer.</p>
            </div>

            {loading && (
                <div style={{ color: '#555', fontSize: 12, fontFamily: 'Orbitron, monospace', textTransform: 'uppercase', letterSpacing: '0.1em', padding: '16px 0' }}>
                    LOADING...
                </div>
            )}

            {!loading && docs.length === 0 && (
                <div style={{ padding: '28px 0', textAlign: 'center', color: '#555' }}>
                    <div style={{ fontSize: 24, marginBottom: 8 }}>📂</div>
                    <div style={{ fontFamily: 'Orbitron, monospace', fontSize: 11, letterSpacing: '0.1em', textTransform: 'uppercase' }}>
                        No documents uploaded for this shipment
                    </div>
                </div>
            )}

            {!loading && docs.length > 0 && (
                <div style={{ display: 'flex', flexDirection: 'column', gap: 10 }}>
                    <div style={sectionLabel}>{docs.length} document{docs.length !== 1 ? 's' : ''} found</div>
                    {docs.map(doc => (
                        <div
                            key={doc.id}
                            style={{
                                display: 'flex', justifyContent: 'space-between', alignItems: 'center',
                                padding: '12px 16px',
                                background: 'var(--color-surface-2)',
                                border: '1px solid rgba(255,255,255,0.07)',
                                borderRadius: 4,
                                transition: 'border-color 0.15s ease',
                            }}
                        >
                            {/* Left — file info */}
                            <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
                                <span style={{ fontSize: 22, flexShrink: 0 }}>{getFileIcon(doc.fileName)}</span>
                                <div>
                                    <div style={{ fontSize: 13, color: '#fff', fontWeight: 600, marginBottom: 3 }}>
                                        {doc.fileName}
                                    </div>
                                    <div style={{ fontSize: 11, color: '#666', display: 'flex', gap: 10 }}>
                                        <span>{doc.documentType}</span>
                                        <span>·</span>
                                        <span>{formatBytes(doc.fileSizeBytes)}</span>
                                        <span>·</span>
                                        <span>{doc.uploadedAt}</span>
                                    </div>
                                </div>
                            </div>

                            {/* Right — badge + open button */}
                            <div style={{ display: 'flex', alignItems: 'center', gap: 10, flexShrink: 0 }}>
                                <span className={`ss-badge ${doc.documentType === 'Invoice' ? 'warning' : doc.documentType === 'Label' ? 'success' : ''}`}>
                                    {doc.documentType}
                                </span>
                                <button
                                    className="ss-btn ss-btn-outline"
                                    style={{ padding: '4px 14px', fontSize: 11 }}
                                    onClick={() => handleOpen(doc)}
                                >
                                    Open ↗
                                </button>
                            </div>
                        </div>
                    ))}
                </div>
            )}
        </div>
    );
};
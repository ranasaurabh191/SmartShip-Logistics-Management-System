import { useEffect, useRef, useState } from 'react';
import { apiClient } from '../../core/api/axios';

interface DocumentDto {
  id: number;
  fileName: string;
  documentType: string;
  fileSizeBytes: number;
  uploadedAt: string;
}

interface Props {
  shipmentId: number;
  trackingNumber: string;
}

const DOC_TYPES = ['Invoice', 'Label', 'Other'];
const ALLOWED_EXTS = ['.pdf', '.jpg', '.jpeg', '.png'];
const MAX_SIZE_MB = 10;

function formatBytes(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(2)} MB`;
}

export const DocumentUpload = ({ shipmentId, trackingNumber }: Props) => {
  const [docs, setDocs] = useState<DocumentDto[]>([]);
  const [docType, setDocType] = useState('Invoice');
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [fileError, setFileError] = useState('');
  const [uploading, setUploading] = useState(false);
  const [uploadSuccess, setUploadSuccess] = useState('');
  const [uploadError, setUploadError] = useState('');
  const [dragging, setDragging] = useState(false);
  const [loadingDocs, setLoadingDocs] = useState(true);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const fetchDocs = async () => {
    setLoadingDocs(true);
    try {
      const res = await apiClient.get(`/tracking/documents/${shipmentId}`);
      const raw = res.data;
      const items: DocumentDto[] = Array.isArray(raw) ? raw : raw?.data ?? raw?.items ?? [];
      setDocs(items);
    } catch {
      setDocs([]);
    } finally {
      setLoadingDocs(false);
    }
  };

  useEffect(() => { fetchDocs(); }, [shipmentId]);

  const validateFile = (file: File): string => {
    const ext = '.' + file.name.split('.').pop()?.toLowerCase();
    if (!ALLOWED_EXTS.includes(ext))
      return `File type not allowed. Use: ${ALLOWED_EXTS.join(', ')}`;
    if (file.size > MAX_SIZE_MB * 1024 * 1024)
      return `File must be under ${MAX_SIZE_MB} MB (current: ${formatBytes(file.size)})`;
    return '';
  };

  const handleFileSelect = (file: File) => {
    const err = validateFile(file);
    setFileError(err);
    setUploadSuccess('');
    setUploadError('');
    setSelectedFile(err ? null : file);
  };

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault();
    setDragging(false);
    const file = e.dataTransfer.files[0];
    if (file) handleFileSelect(file);
  };

  const handleUpload = async () => {
    if (!selectedFile) { setFileError('Please select a file first.'); return; }
    const err = validateFile(selectedFile);
    if (err) { setFileError(err); return; }
    setUploading(true);
    setUploadError('');
    setUploadSuccess('');
    try {
      const form = new FormData();
      form.append('shipmentId', String(shipmentId));
      form.append('trackingNumber', trackingNumber);
      form.append('documentType', docType);
      form.append('file', selectedFile);
      await apiClient.post('/tracking/documents/upload', form, {
        headers: { 'Content-Type': 'multipart/form-data' },
      });
      setUploadSuccess(`"${selectedFile.name}" uploaded successfully.`);
      setSelectedFile(null);
      if (fileInputRef.current) fileInputRef.current.value = '';
      await fetchDocs();
    } catch (err: any) {
      setUploadError(
        err?.response?.data?.message || err?.response?.data || 'Upload failed. Please try again.'
      );
    } finally {
      setUploading(false);
    }
  };

  const sectionLabel: React.CSSProperties = {
    fontFamily: 'Orbitron, monospace',
    fontSize: 11,
    fontWeight: 600,
    letterSpacing: '0.14em',
    textTransform: 'uppercase',
    color: '#888',
    marginBottom: 8,
  };

  const errorStyle: React.CSSProperties = {
    color: '#ff6b6b',
    fontSize: 12,
    marginTop: 5,
    fontFamily: 'Inter, sans-serif',
  };

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 20 }}>
      {/* Header */}
      <div style={{ borderBottom: '1px solid rgba(255,255,255,0.08)', paddingBottom: 14 }}>
        <div className="accent-line" style={{ marginBottom: 6 }} />
        <h2 style={{ fontFamily: 'Orbitron, monospace', fontSize: 14, fontWeight: 700, letterSpacing: '0.1em', textTransform: 'uppercase', color: '#fff', marginBottom: 4 }}>
          Documents
        </h2>
        <p style={{ fontSize: 12, color: '#888' }}>Upload invoices, shipping labels, or other shipment documents.</p>
      </div>

      {/* Document Type Selector */}
      <div>
        <div style={sectionLabel}>Document Type</div>
        <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap' }}>
          {DOC_TYPES.map(t => (
            <button
              key={t}
              onClick={() => setDocType(t)}
              style={{
                fontFamily: 'Orbitron, monospace', fontSize: 11, fontWeight: 600,
                letterSpacing: '0.1em', textTransform: 'uppercase',
                padding: '5px 14px', border: '1px solid', borderRadius: 4,
                cursor: 'pointer', background: 'transparent',
                borderColor: docType === t ? 'var(--color-accent)' : 'rgba(255,255,255,0.12)',
                color: docType === t ? 'var(--color-accent)' : '#888',
                transition: 'all 0.15s ease',
              }}
            >
              {t}
            </button>
          ))}
        </div>
      </div>

      {/* Drag & Drop Zone */}
      <div>
        <div
          onDragOver={e => { e.preventDefault(); setDragging(true); }}
          onDragLeave={() => setDragging(false)}
          onDrop={handleDrop}
          onClick={() => fileInputRef.current?.click()}
          style={{
            border: `2px dashed ${dragging ? 'var(--color-accent)' : fileError ? '#ff6b6b' : 'rgba(255,255,255,0.13)'}`,
            borderRadius: 6, padding: '32px 20px', textAlign: 'center',
            cursor: 'pointer',
            background: dragging ? 'rgba(224,0,26,0.05)' : 'var(--color-surface-2)',
            transition: 'all 0.2s ease', marginBottom: 4,
          }}
        >
          <input
            ref={fileInputRef}
            type="file"
            accept=".pdf,.jpg,.jpeg,.png"
            style={{ display: 'none' }}
            onChange={e => { if (e.target.files?.[0]) handleFileSelect(e.target.files[0]); }}
          />
          {selectedFile ? (
            <div>
              <div style={{ fontSize: 13, color: '#fff', fontWeight: 600, marginBottom: 4 }}>
                📄 {selectedFile.name}
              </div>
              <div style={{ fontSize: 12, color: '#888' }}>{formatBytes(selectedFile.size)}</div>
              <div style={{ fontSize: 11, color: 'var(--color-accent)', marginTop: 6, textTransform: 'uppercase', letterSpacing: '0.08em' }}>
                Click to change file
              </div>
            </div>
          ) : (
            <div>
              <div style={{ fontSize: 28, marginBottom: 8 }}>📁</div>
              <div style={{ fontFamily: 'Orbitron, monospace', fontSize: 12, fontWeight: 600, letterSpacing: '0.08em', color: '#fff', marginBottom: 6 }}>
                Drop file here or click to browse
              </div>
              <div style={{ fontSize: 11, color: '#666' }}>PDF, JPG, PNG · Max {MAX_SIZE_MB} MB</div>
            </div>
          )}
        </div>

        {fileError && <div style={errorStyle}>⚠ {fileError}</div>}

        {uploadError && (
          <div style={{ marginTop: 10, padding: '10px 14px', border: '1px solid rgba(224,0,26,0.35)', background: 'rgba(224,0,26,0.07)', color: '#ff8d8d', borderRadius: 4, fontSize: 13 }}>
            {uploadError}
          </div>
        )}
        {uploadSuccess && (
          <div style={{ marginTop: 10, padding: '10px 14px', border: '1px solid rgba(0,196,140,0.35)', background: 'rgba(0,196,140,0.07)', color: '#99e6b3', borderRadius: 4, fontSize: 13 }}>
            ✓ {uploadSuccess}
          </div>
        )}

        <button
          className="ss-btn"
          onClick={handleUpload}
          disabled={uploading || !selectedFile}
          style={{ marginTop: 14, opacity: uploading || !selectedFile ? 0.6 : 1, width: '100%', justifyContent: 'center', padding: '9px 0' }}
        >
          {uploading ? 'UPLOADING...' : `UPLOAD ${docType.toUpperCase()}`}
        </button>
      </div>

      {/* Uploaded Documents List */}
      <div>
        <div style={sectionLabel}>Uploaded Documents</div>
        {loadingDocs ? (
          <div style={{ color: '#555', fontSize: 12, fontFamily: 'Roboto, sans-serif', textTransform: 'uppercase', letterSpacing: '0.1em', padding: '16px 0' }}>
            LOADING...
          </div>
        ) : docs.length === 0 ? (
          <div style={{ padding: '20px 0', textAlign: 'center', color: '#555' }}>
            <div style={{ fontSize: 20, marginBottom: 6 }}>📂</div>
            <div style={{ fontFamily: 'Roboto, sans-serif', fontSize: 11, letterSpacing: '0.1em', textTransform: 'uppercase' }}>
              No documents uploaded yet
            </div>
          </div>
        ) : (
          <div style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
            {docs.map(doc => (
              <div
                key={doc.id}
                style={{
                  display: 'flex', justifyContent: 'space-between', alignItems: 'center',
                  padding: '10px 14px', background: 'var(--color-surface-2)',
                  border: '1px solid rgba(255,255,255,0.07)', borderRadius: 4,
                }}
              >
                <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
                  <span style={{ fontSize: 18 }}>
                    {doc.fileName.endsWith('.pdf') ? '📄' : '🖼️'}
                  </span>
                  <div>
                    <div style={{ fontSize: 13, color: '#fff', fontWeight: 600 }}>{doc.fileName}</div>
                    <div style={{ fontSize: 11, color: '#666', marginTop: 2 }}>
                      {doc.documentType} · {formatBytes(doc.fileSizeBytes)}
                    </div>
                  </div>
                </div>
                <div style={{ textAlign: 'right' }}>
                  <span className="ss-badge success">{doc.documentType}</span>
                  <div style={{ fontSize: 11, color: '#555', marginTop: 4 }}>{doc.uploadedAt}</div>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
};
import { useRef, useEffect, useState } from 'react';
import { useChat, type ShipmentChip } from '../hooks/useChat';
import { useAuthStore } from '../../store/useAuthStore';
import { useNotificationStore } from '../../store/useNotificationStore';
import { useSpeechRecognition } from '../hooks/useSpeechRecognition';


interface Props { shipmentId?: number; }


const AI_LOGO = (
    <svg width="36" height="36" viewBox="0 0 76 76" fill="none">
        {/* Body */}
        <rect x="0" y="0" width="76" height="76" rx="16" fill="#CC2222" />
        <rect x="10" y="10" width="56" height="56" rx="10" fill="#AA1A1A" />
        {/* Side arms */}
        <rect x="8" y="30" width="4" height="16" rx="2" fill="#ffffffff" />
        <rect x="64" y="30" width="4" height="16" rx="2" fill="#ffffffff" />
        {/* Antennae */}
        <rect x="28" y="5" width="6" height="8" rx="3" fill="#d78686ff" />
        <rect x="42" y="5" width="6" height="8" rx="3" fill="#d78686ff" />
        {/* Eyes (whites) */}
        <rect x="18" y="20" width="15" height="15" rx="7" fill="white" />
        <rect x="44" y="20" width="14" height="14" rx="7" fill="white" />
        {/* Pupils */}
        <circle cx="25" cy="27" r="5" fill="#CC2222" />
        <circle cx="51" cy="27" r="5" fill="#CC2222" />
        {/* Eye shine */}
        <circle cx="25" cy="27" r="2" fill="white" />
        <circle cx="51" cy="27" r="2" fill="white" />
        {/* Mouth panel */}
        <rect x="20" y="42" width="36" height="10" rx="5" fill="#CC2222" />
        {/* Mouth teeth */}
        <rect x="24" y="45" width="5" height="4" rx="1" fill="white" />
        <rect x="31" y="45" width="5" height="4" rx="1" fill="white" />
        <rect x="38" y="45" width="5" height="4" rx="1" fill="white" />
        <rect x="45" y="45" width="5" height="4" rx="1" fill="white" />
    </svg>
);


const FAB_ICON = (
    <svg width="30" height="30" viewBox="0 0 52 52" fill="none">
        {/* Body */}
        <rect x="0" y="0" width="50" height="50" rx="12" fill="#CC2222" />
        <rect x="7" y="7" width="38" height="38" rx="8" fill="#AA1A1A" />
        {/* Side arms */}
        <rect x="4" y="18" width="3" height="12" rx="1.5" fill="#ffffffff" />
        <rect x="45" y="18" width="3" height="12" rx="1.5" fill="#ffffffff" />
        {/* Antennae */}
        <rect x="16" y="3" width="4" height="6" rx="2" fill="#d2c6c6ff" />
        <rect x="32" y="3" width="4" height="6" rx="2" fill="#d2c6c6ff" />
        {/* Eyes (whites) */}
        <rect x="11" y="13" width="12" height="12" rx="5" fill="white" />
        <rect x="31" y="13" width="12" height="12" rx="5" fill="white" />
        {/* Pupils */}
        <circle cx="16" cy="18" r="3.5" fill="#CC2222" />
        <circle cx="36" cy="18" r="3.5" fill="#CC2222" />
        {/* Eye shine */}
        <circle cx="16" cy="18" r="1.5" fill="white" />
        <circle cx="36" cy="18" r="1.5" fill="white" />
        {/* Mouth panel */}
        <rect x="13" y="29" width="26" height="8" rx="4" fill="#CC2222" />
        {/* Mouth teeth */}
        <rect x="16" y="32" width="4" height="3" rx="0.5" fill="white" />
        <rect x="22" y="32" width="4" height="3" rx="0.5" fill="white" />
        <rect x="28" y="32" width="4" height="3" rx="0.5" fill="white" />
        <rect x="34" y="32" width="4" height="3" rx="0.5" fill="white" />
    </svg>
);


const SEND_ICON = (
    <svg width="14" height="14" viewBox="0 0 14 14" fill="none">
        <path d="M13 7L1 1l3 6-3 6 12-6z" fill="#683434ff" />
    </svg>
);


const CLOSE_ICON = (
    <svg width="14" height="14" viewBox="0 0 14 14" fill="none">
        <path d="M1 1l12 12M13 1L1 13" stroke="rgba(255,255,255,0.9)" strokeWidth="2" strokeLinecap="round" />
    </svg>
);


export const ChatWidget = ({ shipmentId }: Props) => {
    const [open, setOpen] = useState(false);
    const [input, setInput] = useState('');
    const bottomRef = useRef<HTMLDivElement>(null);
    const inputRef = useRef<HTMLInputElement>(null);
    const windowRef = useRef<HTMLDivElement>(null);
    const fabRef = useRef<HTMLButtonElement>(null);
    const user = useAuthStore(state => state.user);
    const addNotification = useNotificationStore(state => state.addNotification);
    const { messages, loading, sendMessage, clearChat, activeShipmentId } = useChat(shipmentId);
    const prevUserIdRef = useRef<number | null>(null);
    const {
        isSupported: voiceSupported,
        speechState,
        interimText,
        toggle: toggleVoice,
        isListening,
        stop: stopVoice,
    } = useSpeechRecognition({
        lang: 'en-IN',
        onResult: (transcript, isFinal) => {
            setInput(transcript);
            if (isFinal && transcript.trim()) {
                setTimeout(() => {
                    handleSend(transcript.trim());
                    setInput('');
                }, 300);
            }
        },
        onError: (err) => {
            addNotification(err, 'error');
        },
        onEnd: () => {
        },
    });

    useEffect(() => {
        if (!open) stopVoice();
    }, [open]);
    useEffect(() => {
        if (prevUserIdRef.current !== null && prevUserIdRef.current !== user?.id) {
            clearChat();
            setOpen(false);
        }
        prevUserIdRef.current = user?.id ?? null;
    }, [user?.id]);
    useEffect(() => {
        bottomRef.current?.scrollIntoView({ behavior: 'smooth' });
    }, [messages]);

    useEffect(() => {
        if (open && !loading) inputRef.current?.focus();
    }, [open, loading]);

    useEffect(() => {
        const handleClickOutside = (event: MouseEvent) => {
            if (
                open &&
                windowRef.current &&
                !windowRef.current.contains(event.target as Node) &&
                fabRef.current &&
                !fabRef.current.contains(event.target as Node)
            ) {
                setOpen(false);
            }
        };
        document.addEventListener('mousedown', handleClickOutside);
        return () => {
            document.removeEventListener('mousedown', handleClickOutside);
        };
    }, [open]);

    if (!user) return null;

    const handleSend = (overrideMsg?: string) => {
        const msg = overrideMsg ?? input.trim();
        if (!msg || loading) return;
        sendMessage(msg);
        if (!overrideMsg) setInput('');
    };

    const handleChipClick = (chip: ShipmentChip) => {
        sendMessage(
            `Tell me about shipment ${chip.trackingNumber}`,
            undefined,
            chip.shipmentId
        );
    };

    const quickActions = [
        { label: '📍 Track shipment', msg: '📍 Track my shipment' },
        { label: '📎 Documents', msg: '📎 Show documents' },
        { label: '✅ Delivery proof', msg: '✅ Delivery proof' },
        { label: '❓ Help', msg: '❓ Help' },
    ];


    return (
        <>
            <style>{`
                @keyframes ss-pulse {
                    0%   { transform: scale(1);   opacity: 0.55; }
                    100% { transform: scale(1.55); opacity: 0; }
                }
                @keyframes ss-slideUp {
                    from { opacity: 0; transform: translateY(14px) scale(0.97); }
                    to   { opacity: 1; transform: translateY(0)   scale(1); }
                }
                @keyframes ss-fadeMsg {
                    from { opacity: 0; transform: translateY(5px); }
                    to   { opacity: 1; transform: translateY(0); }
                }
                @keyframes ss-blink {
                    0%, 100% { opacity: 1; } 50% { opacity: 0.2; }
                }
                .ss-window  { animation: ss-slideUp 0.28s cubic-bezier(0.34,1.4,0.64,1) both; }
                .ss-msg-anim{ animation: ss-fadeMsg 0.22s ease both; }
                .ss-fab-btn { transition: transform 0.22s cubic-bezier(0.34,1.4,0.64,1), background 0.2s ease; }
                .ss-fab-btn:hover  { transform: scale(1.1); }
                .ss-fab-btn:active { transform: scale(0.93); }
                .ss-quick   { transition: background 0.15s ease, color 0.15s ease, border-color 0.15s ease, transform 0.15s ease; }
                .ss-quick:hover { background: rgba(226,75,74,0.08) !important; color: #E24B4A !important; border-color: rgba(226,75,74,0.4) !important; transform: translateY(-1px); }
                .ss-send    { transition: opacity 0.15s ease, transform 0.12s ease; }
                .ss-send:hover:not(:disabled)  { transform: scale(1.08); }
                .ss-send:active:not(:disabled) { transform: scale(0.93); }
                .ss-input:focus { border-color: rgba(226,75,74,0.5) !important; box-shadow: 0 0 0 3px rgba(226,75,74,0.1) !important; }
                .ss-dot { width: 5px; height: 5px; border-radius: 50%; display: inline-block; }
                .ss-chip { transition: background 0.15s ease, color 0.15s ease, border-color 0.15s ease, transform 0.15s ease; }
                .ss-chip:hover { background: rgba(226,75,74,0.08) !important; color: #E24B4A !important; border-color: rgba(226,75,74,0.5) !important; transform: translateY(-1px); }
                .ss-chip:active { transform: scale(0.96); }
                
                @keyframes ss-voice-pulse {
                0%, 100% { transform: scale(1);   box-shadow: 0 0 0 0 rgba(226,75,74,0.6); }
                50%       { transform: scale(1.1); box-shadow: 0 0 0 7px rgba(226,75,74,0); }
                }
                @keyframes ss-wave {
                0%, 100% { height: 4px; }
                25%       { height: 12px; }
                50%       { height: 7px; }
                75%       { height: 16px; }
                }
                .ss-mic-btn { transition: background 0.18s ease, transform 0.18s ease, box-shadow 0.18s ease; }
                .ss-mic-btn:hover:not(:disabled) { transform: scale(1.08); }
                .ss-mic-btn:active:not(:disabled) { transform: scale(0.93); }
                .ss-mic-listening { animation: ss-voice-pulse 1.1s ease infinite; }
                .ss-voice-bar { display: inline-block; width: 3px; border-radius: 2px; background: #E24B4A; margin: 0 1px; animation: ss-wave 0.7s ease-in-out infinite; }
                .ss-voice-bar:nth-child(2) { animation-delay: 0.1s; }
                .ss-voice-bar:nth-child(3) { animation-delay: 0.22s; }
                .ss-voice-bar:nth-child(4) { animation-delay: 0.12s; }

            `}</style>


            {/* FAB */}
            <div style={{ position: 'fixed', bottom: 28, right: 28, zIndex: 10000 }}>
                {/* Pulse ring */}
                {!open && (
                    <div style={{
                        position: 'absolute', top: -4, left: -4,
                        width: 56, height: 56, borderRadius: '30%',
                        border: '1.5px solid rgba(246, 246, 246, 0.85)',
                        animation: 'ss-pulse 2.2s ease-out infinite',
                        pointerEvents: 'none',
                    }} />
                )}
                <button
                    ref={fabRef}
                    className="ss-fab-btn"
                    onClick={() => setOpen(o => !o)}
                    aria-label="Toggle chat"
                    style={{
                        width: 48, height: 48, borderRadius: '30%',
                        background: open ? '#222' : '#E24B4A',
                        border: 'none', cursor: 'pointer',
                        display: 'flex', alignItems: 'center', justifyContent: 'center',
                        boxShadow: '0 4px 18px rgba(226,75,74,0.35)',
                    }}
                >
                    {open ? CLOSE_ICON : FAB_ICON}
                </button>
            </div>


            {/* Chat window */}
            {open && (
                <div
                    ref={windowRef}
                    className="ss-window"
                    style={{
                        position: 'fixed', bottom: 92, right: 28, zIndex: 9999,
                        width: 448, height: 620,
                        background: 'var(--color-surface, #fff)',
                        border: '0.5px solid rgba(199, 34, 34, 0.57)',
                        borderRadius: 18,
                        display: 'flex', flexDirection: 'column',
                        overflow: 'hidden',
                        boxShadow: '0 8px 40px rgba(0,0,0,0.14)',
                        transformOrigin: 'bottom right',
                    }}
                >
                    {/* Header */}
                    <div style={{
                        padding: '13px 15px',
                        background: 'var(--color-surface-2, #f7f7f7)',
                        borderBottom: '0.5px solid rgba(0,0,0,0.08)',
                        display: 'flex', justifyContent: 'space-between', alignItems: 'center',
                    }}>
                        <div style={{ display: 'flex', alignItems: 'center', gap: 11 }}>
                            {/* Animated logo */}
                            <div style={{ position: 'relative', width: 36, height: 36, flexShrink: 0 }}>
                                {AI_LOGO}
                                <div style={{
                                    position: 'absolute', top: -3, left: -3,
                                    width: 42, height: 42, borderRadius: '50%',
                                    border: '1.5px solid rgba(226,75,74,0.45)',
                                    animation: 'ss-pulse 2.4s ease-out infinite',
                                    pointerEvents: 'none',
                                }} />
                            </div>
                            <div>
                                <div style={{ fontSize: 13, fontWeight: 500, color: 'var(--color-text, #111)', letterSpacing: '0.01em' }}>
                                    SmartShip AI
                                    {/* Show active shipment context badge */}
                                    {activeShipmentId && activeShipmentId !== shipmentId && (
                                        <span style={{
                                            marginLeft: 7, fontSize: 10,
                                            background: 'rgba(226,75,74,0.1)',
                                            color: '#E24B4A', borderRadius: 10,
                                            padding: '2px 7px', fontWeight: 600,
                                        }}>
                                            #{activeShipmentId}
                                        </span>
                                    )}
                                </div>
                                <div style={{ fontSize: 11, color: '#22c27a', display: 'flex', alignItems: 'center', gap: 5, marginTop: 2 }}>
                                    <span className="ss-dot" style={{ background: '#22c27a', animation: 'ss-blink 2s ease infinite' }} />
                                    Online
                                </div>
                            </div>
                        </div>
                        <div style={{ display: 'flex', gap: 6, alignItems: 'center' }}>
                            {/* Reset context button — shown only when a shipment is active */}
                            {activeShipmentId && activeShipmentId !== shipmentId && (
                                <button
                                    onClick={() => sendMessage('check another shipment', undefined, undefined, true)}

                                    title="Switch shipment"
                                    style={{
                                        background: 'none', border: '0.5px solid rgba(226,75,74,0.3)',
                                        fontSize: 10, color: '#E24B4A',
                                        cursor: 'pointer', padding: '3px 8px', borderRadius: 6,
                                        letterSpacing: '0.04em',
                                        transition: 'background 0.15s ease',
                                    }}
                                    onMouseOver={e => { (e.target as HTMLElement).style.background = 'rgba(226,75,74,0.08)'; }}
                                    onMouseOut={e => { (e.target as HTMLElement).style.background = 'none'; }}
                                >
                                    ↩ Switch
                                </button>
                            )}
                            <button
                                onClick={clearChat}
                                title="Clear chat"
                                style={{
                                    background: 'none', border: 'none',
                                    fontSize: 11, color: 'var(--color-text-muted, #888)',
                                    cursor: 'pointer', padding: '4px 8px', borderRadius: 6,
                                    letterSpacing: '0.04em',
                                    transition: 'background 0.15s ease, color 0.15s ease',
                                }}
                                onMouseOver={e => { (e.target as HTMLElement).style.color = '#111'; }}
                                onMouseOut={e => { (e.target as HTMLElement).style.color = 'var(--color-text-muted, #888)'; }}
                            >
                                Clear
                            </button>
                        </div>
                    </div>


                    {/* Messages */}
                    <div style={{
                        flex: 1, overflowY: 'auto', padding: '14px 12px',
                        display: 'flex', flexDirection: 'column', gap: 9,
                    }}>
                        {messages.map((msg, i) => (
                            <div
                                key={i}
                                className="ss-msg-anim"
                                style={{
                                    display: 'flex',
                                    flexDirection: 'column',
                                    alignItems: msg.role === 'user' ? 'flex-end' : 'flex-start',
                                }}
                            >
                                <div style={{
                                    maxWidth: '82%',
                                    padding: '9px 13px',
                                    borderRadius: msg.role === 'user'
                                        ? '12px 12px 2px 12px'
                                        : '12px 12px 12px 2px',
                                    fontSize: 12, lineHeight: 1.65,
                                    background: msg.role === 'user'
                                        ? '#E24B4A'
                                        : 'var(--color-surface-2, #f5f5f5)',
                                    color: msg.role === 'user'
                                        ? '#fff'
                                        : 'var(--color-text, #111)',
                                    border: msg.role === 'bot'
                                        ? '0.5px solid rgba(0,0,0,0.08)'
                                        : 'none',
                                    whiteSpace: 'pre-wrap', wordBreak: 'break-word',
                                }}>
                                    {msg.text}
                                </div>

                                {/* Shipment chips — rendered below bot message bubble */}
                                {msg.role === 'bot' && msg.shipmentChips && msg.shipmentChips.length > 0 && (
                                    <div style={{
                                        display: 'flex', flexWrap: 'wrap', gap: 6,
                                        marginTop: 7, maxWidth: '90%',
                                    }}>
                                        {msg.shipmentChips.map((chip) => (
                                            <button
                                                key={chip.shipmentId}
                                                className="ss-chip"
                                                onClick={() => handleChipClick(chip)}
                                                style={{
                                                    fontSize: 11, padding: '5px 12px',
                                                    borderRadius: 20, cursor: 'pointer',
                                                    background: 'transparent',
                                                    border: '0.5px solid rgba(226,75,74,0.35)',
                                                    color: 'var(--color-text, #111)',
                                                    fontFamily: 'Rajdhani, sans-serif',
                                                    fontWeight: 600, letterSpacing: '0.03em',
                                                }}
                                            >
                                                {chip.label}
                                            </button>
                                        ))}
                                    </div>
                                )}
                            </div>
                        ))}


                        {loading && (
                            <div style={{ display: 'flex', justifyContent: 'flex-start' }}>
                                <div style={{
                                    padding: '10px 14px', borderRadius: '12px 12px 12px 2px',
                                    background: 'var(--color-surface-2, #f5f5f5)',
                                    border: '0.5px solid rgba(0,0,0,0.08)',
                                    display: 'flex', gap: 5, alignItems: 'center',
                                }}>
                                    {[0, 0.18, 0.36].map((delay, i) => (
                                        <span key={i} className="ss-dot" style={{
                                            background: 'var(--color-text-muted, #aaa)',
                                            animation: `ss-blink 1.1s ease ${delay}s infinite`,
                                        }} />
                                    ))}
                                </div>
                            </div>
                        )}
                        <div ref={bottomRef} />
                    </div>


                    {/* Quick actions */}
                    {messages.length <= 1 && (
                        <div style={{ padding: '0 12px 10px', display: 'flex', flexWrap: 'wrap', gap: 6 }}>
                            {quickActions.map(({ label, msg }) => (
                                <button
                                    key={label}
                                    className="ss-quick"
                                    onClick={() => handleSend(msg)}
                                    style={{
                                        fontSize: 10, padding: '5px 11px', borderRadius: 20,
                                        background: 'transparent', cursor: 'pointer',
                                        border: '0.5px solid rgba(0,0,0,0.15)',
                                        color: 'var(--color-text-muted, #888)',
                                    }}
                                >
                                    {label}
                                </button>
                            ))}
                        </div>
                    )}


                    {/* Input row */}
                    <div style={{
                        padding: '10px 12px',
                        borderTop: '0.5px solid rgba(0,0,0,0.08)',
                        display: 'flex', gap: 7, alignItems: 'center',
                    }}>
                        <input
                            ref={inputRef}
                            className="ss-input"
                            value={isListening && interimText ? interimText : input}
                            onChange={e => { if (!isListening) setInput(e.target.value); }}
                            onKeyDown={e => { if (e.key === 'Enter' && !isListening) handleSend(); }}
                            placeholder={isListening ? '🎤 Listening...' : 'Ask me anything...'}
                            disabled={loading}
                            style={{
                                flex: 1, fontSize: 12, padding: '8px 12px',
                                border: `0.5px solid ${isListening ? 'rgba(226,75,74,0.6)' : 'rgba(0,0,0,0.15)'}`,
                                borderRadius: 10, outline: 'none',
                                background: isListening ? 'rgba(226,75,74,0.04)' : 'var(--color-surface, #fff)',
                                color: isListening ? '#E24B4A' : 'var(--color-text, #111)',
                                transition: 'border-color 0.18s ease, box-shadow 0.18s ease, background 0.18s ease',
                                boxShadow: isListening ? '0 0 0 3px rgba(226,75,74,0.12)' : undefined,
                            }}
                        />

                        {/* Mic button — only shown if browser supports it */}
                        {voiceSupported && (
                            <button
                                className={`ss-mic-btn ${isListening ? 'ss-mic-listening' : ''}`}
                                onClick={toggleVoice}
                                disabled={loading}
                                title={isListening ? 'Stop listening' : 'Speak your message'}
                                aria-label={isListening ? 'Stop voice input' : 'Start voice input'}
                                style={{
                                    width: 34, height: 34, borderRadius: '50%', flexShrink: 0,
                                    background: isListening ? '#E24B4A' : speechState === 'error' ? '#f5a623' : 'rgba(226,75,74,0.1)',
                                    border: `1px solid ${isListening ? '#E24B4A' : 'rgba(226,75,74,0.3)'}`,
                                    cursor: loading ? 'not-allowed' : 'pointer',
                                    display: 'flex', alignItems: 'center', justifyContent: 'center', gap: 1,
                                    opacity: loading ? 0.5 : 1,
                                }}
                            >
                                {isListening ? (
                                    /* Animated waveform bars when listening */
                                    <>
                                        <span className="ss-voice-bar" />
                                        <span className="ss-voice-bar" />
                                        <span className="ss-voice-bar" />
                                        <span className="ss-voice-bar" />
                                    </>
                                ) : speechState === 'error' ? (
                                    /* Error icon */
                                    <svg width="14" height="14" viewBox="0 0 14 14" fill="none">
                                        <circle cx="7" cy="7" r="6" stroke="#f5a623" strokeWidth="1.5" />
                                        <path d="M7 4v4M7 10v.5" stroke="#f5a623" strokeWidth="1.5" strokeLinecap="round" />
                                    </svg>
                                ) : (
                                    /* Mic icon */
                                    <svg width="14" height="14" viewBox="0 0 24 24" fill="none">
                                        <rect x="9" y="2" width="6" height="12" rx="3" fill="#E24B4A" />
                                        <path d="M5 10a7 7 0 0 0 14 0" stroke="#E24B4A" strokeWidth="2" strokeLinecap="round" />
                                        <line x1="12" y1="20" x2="12" y2="22" stroke="#E24B4A" strokeWidth="2" strokeLinecap="round" />
                                        <line x1="8" y1="22" x2="16" y2="22" stroke="#E24B4A" strokeWidth="2" strokeLinecap="round" />
                                    </svg>
                                )}
                            </button>
                        )}

                        {/* Send button */}
                        <button
                            className="ss-send"
                            onClick={() => handleSend()}
                            disabled={loading || (!input.trim() && !isListening)}
                            style={{
                                width: 34, height: 34, borderRadius: '50%',
                                background: loading || (!input.trim() && !isListening) ? '#ccc' : '#E24B4A',
                                border: 'none', cursor: loading || (!input.trim() && !isListening) ? 'not-allowed' : 'pointer',
                                display: 'flex', alignItems: 'center', justifyContent: 'center',
                                flexShrink: 0,
                            }}
                        >
                            {SEND_ICON}
                        </button>
                    </div>
                </div>
            )}
        </>
    );
};
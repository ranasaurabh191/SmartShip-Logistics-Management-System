/// <reference types="@types/dom-speech-recognition" />
import { useCallback, useEffect, useRef, useState } from 'react';

declare global {
  interface Window {
    webkitSpeechRecognition: any;
  }
}

export type SpeechState = 'idle' | 'listening' | 'processing' | 'error';

interface UseSpeechRecognitionOptions {
  lang?: string;
  continuous?: boolean;
  onResult?: (transcript: string, isFinal: boolean) => void;
  onError?: (error: string) => void;
  onEnd?: () => void;
}

export interface UseSpeechRecognitionReturn {
  isSupported: boolean;
  speechState: SpeechState;
  interimText: string;
  start: () => void;
  stop: () => void;
  toggle: () => void;
  isListening: boolean;
}

// ── Feature detect ──
const getSpeechRecognition = (): (typeof SpeechRecognition) | null => {
  if (typeof window === 'undefined') return null;
  return (window.SpeechRecognition || window.webkitSpeechRecognition || null) as typeof SpeechRecognition | null;
};

export function useSpeechRecognition({
  lang = 'en-IN',
  continuous = false,
  onResult,
  onError,
  onEnd,    
}: UseSpeechRecognitionOptions = {}): UseSpeechRecognitionReturn {
  const SpeechRecognitionAPI = getSpeechRecognition();
  const isSupported = !!SpeechRecognitionAPI;

  const recognitionRef  = useRef<SpeechRecognition | null>(null);
  const [speechState, setSpeechState]   = useState<SpeechState>('idle');
  const [interimText, setInterimText]   = useState('');

  useEffect(() => {
    return () => {
      recognitionRef.current?.abort();
    };
  }, []);

  const start = useCallback(() => {
    if (!isSupported || speechState === 'listening') return;

    const recognition = new SpeechRecognitionAPI!();
    recognition.lang = lang;
    recognition.continuous = continuous;
    recognition.interimResults = true;
    recognition.maxAlternatives = 1;

    recognition.onstart = () => {
      setSpeechState('listening');
      setInterimText('');
    };

    recognition.onresult = (event: SpeechRecognitionEvent) => {
      let interim = '';
      let finalTranscript = '';
      for (let i = event.resultIndex; i < event.results.length; i++) {
        const result = event.results[i];
        if (result.isFinal) {
          finalTranscript += result[0].transcript;
        } else {
          interim += result[0].transcript;
        }
      }
      setInterimText(interim);
      if (finalTranscript) {
        setSpeechState('processing');
        setInterimText('');
        onResult?.(finalTranscript.trim(), true);
      } else if (interim) {
        onResult?.(interim, false);
      }
    };

    recognition.onerror = (event: SpeechRecognitionErrorEvent) => {
      const msg = event.error === 'not-allowed'
        ? 'Microphone permission denied.'
        : event.error === 'no-speech'
        ? 'No speech detected.'
        : `Voice error: ${event.error}`;
      setSpeechState('error');
      setInterimText('');
      onError?.(msg);
      setTimeout(() => setSpeechState('idle'), 2000);
    };

    recognition.onend = () => {
      setSpeechState(prev => prev === 'listening' ? 'idle' : prev);
      setInterimText('');
      onEnd?.();
      recognitionRef.current = null;
    };

    recognitionRef.current = recognition;
    recognition.start();
  }, [isSupported, lang, continuous, speechState, onResult, onError, onEnd]);

  const stop = useCallback(() => {
    recognitionRef.current?.stop();
    setSpeechState('idle');
    setInterimText('');
  }, []);

  const toggle = useCallback(() => {
    if (speechState === 'listening') stop();
    else start();
  }, [speechState, start, stop]);

  return {
    isSupported,
    speechState,
    interimText,
    start,
    stop,
    toggle,
    isListening: speechState === 'listening',
  };
}
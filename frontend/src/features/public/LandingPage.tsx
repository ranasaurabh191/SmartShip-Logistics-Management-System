import { useNavigate } from 'react-router-dom';
import { useEffect, useRef, useState } from 'react';
import { SmartShipLogo } from '../../shared/components/Logo';

// ─── Data ─────────────────────────────────────────────────────────────────────
const WHY_FEATURES = [
  { title: 'Largest Pincode Network', desc: 'Spanning 31,000+ pin codes. COD available on 19K+ pin codes — largest in India.' },
  { title: 'Automated Delivery Updates', desc: 'Reduce customer contacts by 20%. Customers notified at every step automatically.' },
  { title: 'Dedicated Account Manager', desc: 'Single point resolution. Let experts handle your end-to-end logistics operations.' },
  { title: 'Best Shipping Rates', desc: 'Lowest shipping rates, lowest COD charges, discounted insurance — all in one platform.' },
  { title: 'Optimised Performance', desc: '20% faster deliveries, 40% lower expenses, 20% higher delivery rate via NDR validation.' },
  { title: 'No Order Commitment', desc: 'Ship 10 or 10,000 orders. Scale freely without any contracts or minimum obligations.' },
  { title: 'Intelligent Courier Selection', desc: 'Auto-select carriers based on delivery SLAs, pickup SLAs, and RTO metrics.' },
  { title: 'Bulk Order Upload', desc: 'Upload thousands of orders in minutes with our automated workflow engine.' },
];

const HOW_STEPS = [
  { step: '01', title: 'Request Pickup', desc: 'Upload orders in bulk. Our system processes and schedules pickup automatically.' },
  { step: '02', title: 'Prepare Shipment', desc: 'Pack the parcel and paste the generated AWB shipping label on it.' },
  { step: '03', title: 'Parcel Picked Up', desc: 'Our courier partner collects the parcel directly from your location.' },
  { step: '04', title: 'Delivered on Time', desc: 'Shipment delivered to the customer with live tracking updates at every stage.' },
];

const INTEGRATIONS = ['Shopify', 'WooCommerce', 'Magento', 'Amazon', 'Flipkart', 'Meesho', 'OpenCart', 'Custom API'];

const SERVICES = [
  { name: 'Intelligent Routing', desc: 'Smart courier auto-selection engine', tag: 'ACTIVE' },
  { name: 'Real-time Tracking', desc: 'Live shipment status at every hub', tag: 'LIVE' },
  { name: 'NDR Validation', desc: 'AI-based failed delivery prevention', tag: 'AI MODE' },
  { name: 'COD Management', desc: 'Realtime cash-on-delivery workflows', tag: 'SYNC' },
  { name: 'Bulk Upload', desc: 'Process thousands of orders instantly', tag: 'NEW' },
];

// ─── Animated Canvas Background ───────────────────────────────────────────────
const ParticleCanvas = () => {
  const canvasRef = useRef<HTMLCanvasElement>(null);

  useEffect(() => {
    const canvas = canvasRef.current;
    if (!canvas) return;
    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    let animFrame: number;

    const resize = () => {
      canvas.width = window.innerWidth;
      canvas.height = window.innerHeight;
    };
    resize();
    window.addEventListener('resize', resize);

    // Particles
    const N = 60;
    const particles = Array.from({ length: N }, () => ({
      x: Math.random() * window.innerWidth,
      y: Math.random() * window.innerHeight,
      vx: (Math.random() - 0.5) * 0.3,
      vy: (Math.random() - 0.5) * 0.3,
      r: Math.random() * 1.2 + 0.3,
      alpha: Math.random() * 0.5 + 0.1,
    }));

    const draw = () => {
      ctx.clearRect(0, 0, canvas.width, canvas.height);

      // Move + draw particles
      for (const p of particles) {
        p.x += p.vx;
        p.y += p.vy;
        if (p.x < 0) p.x = canvas.width;
        if (p.x > canvas.width) p.x = 0;
        if (p.y < 0) p.y = canvas.height;
        if (p.y > canvas.height) p.y = 0;

        ctx.beginPath();
        ctx.arc(p.x, p.y, p.r, 0, Math.PI * 2);
        ctx.fillStyle = `rgba(224,0,26,${p.alpha})`;
        ctx.fill();
      }

      // Draw connecting lines between close particles
      for (let i = 0; i < particles.length; i++) {
        for (let j = i + 1; j < particles.length; j++) {
          const dx = particles[i].x - particles[j].x;
          const dy = particles[i].y - particles[j].y;
          const dist = Math.sqrt(dx * dx + dy * dy);
          if (dist < 140) {
            ctx.beginPath();
            ctx.strokeStyle = `rgba(224,0,26,${0.08 * (1 - dist / 140)})`;
            ctx.lineWidth = 0.5;
            ctx.moveTo(particles[i].x, particles[i].y);
            ctx.lineTo(particles[j].x, particles[j].y);
            ctx.stroke();
          }
        }
      }

      // Subtle red orb glow — top left
      const gradTL = ctx.createRadialGradient(0, 0, 0, 0, 0, 400);
      gradTL.addColorStop(0, 'rgba(224,0,26,0.06)');
      gradTL.addColorStop(1, 'transparent');
      ctx.fillStyle = gradTL;
      ctx.fillRect(0, 0, 500, 500);

      // Subtle red orb glow — bottom right
      const gradBR = ctx.createRadialGradient(canvas.width, canvas.height, 0, canvas.width, canvas.height, 500);
      gradBR.addColorStop(0, 'rgba(224,0,26,0.04)');
      gradBR.addColorStop(1, 'transparent');
      ctx.fillStyle = gradBR;
      ctx.fillRect(canvas.width - 600, canvas.height - 600, 600, 600);

      animFrame = requestAnimationFrame(draw);
    };

    draw();
    return () => {
      cancelAnimationFrame(animFrame);
      window.removeEventListener('resize', resize);
    };
  }, []);

  return (
    <canvas
      ref={canvasRef}
      style={{
        position: 'fixed',
        inset: 0,
        zIndex: 0,
        pointerEvents: 'none',
        opacity: 1,
      }}
    />
  );
};

// ─── Scroll Reveal Hook ───────────────────────────────────────────────────────
function useReveal() {
  const ref = useRef<HTMLDivElement>(null);
  const [visible, setVisible] = useState(false);

  useEffect(() => {
    const el = ref.current;
    if (!el) return;
    const obs = new IntersectionObserver(
      ([entry]) => { if (entry.isIntersecting) { setVisible(true); obs.disconnect(); } },
      { threshold: 0.12 }
    );
    obs.observe(el);
    return () => obs.disconnect();
  }, []);

  return { ref, visible };
}

// ─── Reveal wrapper ───────────────────────────────────────────────────────────
interface RevealProps {
  children: React.ReactNode;
  delay?: number;
  style?: React.CSSProperties;
}
const Reveal = ({ children, delay = 0, style }: RevealProps) => {
  const { ref, visible } = useReveal();
  return (
    <div
      ref={ref}
      style={{
        opacity: visible ? 1 : 0,
        transform: visible ? 'translateY(0)' : 'translateY(32px)',
        transition: `opacity 0.7s ${delay}s ease, transform 0.7s ${delay}s ease`,
        ...style,
      }}
    >
      {children}
    </div>
  );
};

// ─── Section label ────────────────────────────────────────────────────────────
const SectionLabel = ({ label }: { label: string }) => (
  <div style={{ display: 'inline-flex', alignItems: 'center', gap: 8, marginBottom: 14, padding: '4px 12px', border: '1px solid rgba(224,0,26,0.35)', borderRadius: 2 }}>
    <div style={{ width: 6, height: 6, borderRadius: '50%', background: '#e0001a', boxShadow: '0 0 6px rgba(224,0,26,0.8)' }} />
    <span style={{ fontFamily: "'Orbitron', monospace", fontSize: 9, fontWeight: 700, letterSpacing: '0.18em', textTransform: 'uppercase', color: '#888' }}>
      {label}
    </span>
  </div>
);

// ─── Main Component ───────────────────────────────────────────────────────────
export const LandingPage = () => {
  const navigate = useNavigate();
  const [navSolid, setNavSolid] = useState(false);
  const [heroVisible, setHeroVisible] = useState(false);

  useEffect(() => {
    // Hero entrance animation
    setTimeout(() => setHeroVisible(true), 80);
    // Nav becomes opaque on scroll
    const onScroll = () => setNavSolid(window.scrollY > 60);
    window.addEventListener('scroll', onScroll);
    return () => window.removeEventListener('scroll', onScroll);
  }, []);

  return (
    <div style={{ minHeight: '100vh', display: 'flex', flexDirection: 'column', background: '#0a0a0a', position: 'relative', overflowX: 'hidden' }}>

      {/* ── Animated network mesh background ── */}
      <ParticleCanvas />

      {/* ── Scanline overlay ── */}
      <div style={{
        position: 'fixed', inset: 0, zIndex: 1, pointerEvents: 'none',
        backgroundImage: 'repeating-linear-gradient(0deg, transparent, transparent 2px, rgba(255,255,255,0.012) 2px, rgba(255,255,255,0.012) 4px)',
      }} />

      {/* ═══════════════════════════════════════════
          NAVBAR — glassmorphism, slides in on load
      ═══════════════════════════════════════════ */}
      <nav style={{
        position: 'fixed', top: 0, left: 0, right: 0, zIndex: 200,
        padding: '16px 48px',
        display: 'flex', alignItems: 'center', justifyContent: 'space-between',
        background: navSolid ? 'rgba(8,8,8,0.97)' : 'rgba(8,8,8,0.6)',
        backdropFilter: 'blur(16px)',
        borderBottom: `1px solid ${navSolid ? 'rgba(224,0,26,0.18)' : 'rgba(255,255,255,0.04)'}`,
        transition: 'background 0.4s ease, border-color 0.4s ease',
        opacity: heroVisible ? 1 : 0,
        transform: heroVisible ? 'translateY(0)' : 'translateY(-20px)',
        // transition handled inline above
      }}>
        <SmartShipLogo />
        <div style={{ display: 'flex', alignItems: 'center', gap: 14 }}>
          <button
            className="ss-btn ss-btn-outline"
            style={{ padding: '8px 20px', fontSize: 11 }}
            onClick={() => navigate('/auth/login')}
          >
            Sign In
          </button>
          <button
            className="ss-btn"
            style={{ padding: '8px 20px', fontSize: 11 }}
            onClick={() => navigate('/auth/signup')}
          >
            Register Free
          </button>
        </div>
      </nav>

      {/* ═══════════════════════════════════════════
          SECTION 1 — HERO
      ═══════════════════════════════════════════ */}
      <section style={{
        position: 'relative', zIndex: 10,
        minHeight: '100vh',
        display: 'flex', alignItems: 'center',
        padding: '100px 80px 60px',
      }}>
        {/* Left hero content */}
        <div style={{ flex: 1, maxWidth: 560 }}>

          {/* System status pill */}
          <div style={{
            display: 'inline-flex', alignItems: 'center', gap: 10, marginBottom: 24,
            padding: '5px 14px',
            border: '1px solid rgba(224,0,26,0.3)', borderRadius: 2,
            background: 'rgba(224,0,26,0.04)',
            opacity: heroVisible ? 1 : 0,
            transform: heroVisible ? 'none' : 'translateY(16px)',
            transition: 'all 0.6s 0.1s ease',
          }}>
            <div style={{ width: 7, height: 7, borderRadius: '50%', background: '#00c48c', boxShadow: '0 0 7px rgba(0,196,140,0.8)', animation: 'pulse 2s infinite' }} />
            <span style={{ fontFamily: "'Orbitron', monospace", fontSize: 9, fontWeight: 700, letterSpacing: '0.16em', textTransform: 'uppercase', color: '#888' }}>
              India's Largest Pincode Network — 31,000+ Serviceable
            </span>
          </div>

          {/* Red accent line */}
          <div style={{
            width: 48, height: 2, background: '#e0001a', marginBottom: 20,
            opacity: heroVisible ? 1 : 0,
            transform: heroVisible ? 'scaleX(1)' : 'scaleX(0)',
            transformOrigin: 'left',
            transition: 'all 0.5s 0.25s ease',
          }} />

          {/* Main heading */}
          <h1 style={{
            fontFamily: "'Orbitron', monospace",
            fontSize: 'clamp(32px, 5.5vw, 76px)',
            fontWeight: 900,
            lineHeight: 1.05,
            letterSpacing: '-0.01em',
            color: '#ffffff',
            marginBottom: 24,
            opacity: heroVisible ? 1 : 0,
            transform: heroVisible ? 'translateY(0)' : 'translateY(40px)',
            transition: 'all 0.7s 0.3s ease',
          }}>
            SmartShip<br />
            <span style={{ color: '#e0001a', textShadow: '0 0 40px rgba(224,0,26,0.4)' }}>Shipping</span><br />
            Solution
          </h1>

          <p style={{
            fontSize: 14, color: '#888', lineHeight: 1.8, maxWidth: 440, marginBottom: 36,
            opacity: heroVisible ? 1 : 0,
            transform: heroVisible ? 'translateY(0)' : 'translateY(24px)',
            transition: 'all 0.7s 0.45s ease',
          }}>
            Intelligent courier selection, automated delivery updates, and end-to-end shipment lifecycle management — all in one platform. Reduce shipping costs by up to 40% and achieve 20% faster deliveries.
          </p>

          <div style={{
            display: 'flex', gap: 12, marginBottom: 56,
            opacity: heroVisible ? 1 : 0,
            transform: heroVisible ? 'translateY(0)' : 'translateY(20px)',
            transition: 'all 0.7s 0.55s ease',
          }}>
            <button className="ss-btn" style={{ fontSize: 12, padding: '12px 28px', letterSpacing: '0.1em' }} onClick={() => navigate('/auth/login')}>
              Get Started
            </button>
            <button className="ss-btn ss-btn-outline" style={{ fontSize: 12, padding: '12px 28px', letterSpacing: '0.1em' }} onClick={() => navigate('/auth/signup')}>
              Register Free
            </button>
          </div>

          {/* Stats strip */}
          <div style={{
            display: 'flex', gap: 36, flexWrap: 'wrap',
            paddingTop: 28, borderTop: '1px solid rgba(255,255,255,0.06)',
            opacity: heroVisible ? 1 : 0,
            transition: 'opacity 0.7s 0.7s ease',
          }}>
            {[
              { value: '31,000+', label: 'Pin Codes' },
              { value: '40%', label: 'Cost Savings' },
              { value: '20%', label: 'Faster Delivery' },
              { value: '99.9%', label: 'Platform Uptime' },
            ].map((stat, i) => (
              <div key={i}>
                <div style={{ fontFamily: "'Orbitron', monospace", fontSize: 22, fontWeight: 800, color: '#fff', letterSpacing: '-0.02em' }}>
                  {stat.value}
                </div>
                <div style={{ fontFamily: 'Rajdhani, sans-serif', fontSize: 10, color: '#555', marginTop: 3, letterSpacing: '0.14em', textTransform: 'uppercase' }}>
                  {stat.label}
                </div>
              </div>
            ))}
          </div>
        </div>

        {/* Right — Module panel (Armoury Crate style) */}
        <div style={{
          position: 'absolute', right: 72, top: '50%',
          transform: heroVisible ? 'translateY(-50%) translateX(0)' : 'translateY(-50%) translateX(60px)',
          opacity: heroVisible ? 1 : 0,
          transition: 'all 0.8s 0.5s cubic-bezier(0.16, 1, 0.3, 1)',
          width: 400,
          // Armoury Crate clipped-corner panel
          clipPath: 'polygon(0 20px, 20px 0, calc(100% - 20px) 0, 100% 20px, 100% calc(100% - 20px), calc(100% - 20px) 100%, 20px 100%, 0 calc(100% - 20px))',
          background: 'rgba(6,6,6,0.94)',
          border: '1px solid rgba(224,0,26,0.2)',
          boxShadow: '0 0 60px rgba(224,0,26,0.08), inset 0 0 40px rgba(0,0,0,0.5)',
        }}>
          {/* Panel inner */}
          <div style={{ padding: '22px 24px' }}>
            {/* Panel header */}
            <div style={{
              display: 'flex', alignItems: 'center', justifyContent: 'space-between',
              marginBottom: 18, paddingBottom: 14,
              borderBottom: '1px solid rgba(255,255,255,0.06)',
            }}>
              <div>
                <div style={{ fontFamily: "'Orbitron', monospace", fontSize: 10, fontWeight: 700, color: '#e0001a', letterSpacing: '0.2em', textTransform: 'uppercase' }}>
                  System Modules
                </div>
              </div>
              <div style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
                <div style={{ width: 7, height: 7, borderRadius: '50%', background: '#00c48c', boxShadow: '0 0 8px rgba(0,196,140,0.9)' }} />
                <span style={{ fontFamily: 'Rajdhani, sans-serif', fontSize: 9, fontWeight: 700, letterSpacing: '0.16em', color: '#00c48c', textTransform: 'uppercase' }}>ONLINE</span>
              </div>
            </div>

            {/* Service rows */}
            {SERVICES.map((svc, i) => (
              <div key={i} style={{
                display: 'flex', alignItems: 'center', justifyContent: 'space-between',
                padding: '13px 0',
                borderBottom: i < SERVICES.length - 1 ? '1px solid rgba(255,255,255,0.04)' : 'none',
                transition: 'background 0.2s',
              }}>
                <div>
                  <div style={{ fontFamily: "'Orbitron', monospace", fontSize: 11, fontWeight: 700, color: '#d4d4d4', letterSpacing: '0.06em', textTransform: 'uppercase', marginBottom: 3 }}>
                    {svc.name}
                  </div>
                  <div style={{ fontSize: 11, color: '#555', fontFamily: 'Inter, sans-serif' }}>{svc.desc}</div>
                </div>
                <span style={{
                  fontFamily: "'Orbitron', monospace",
                  fontSize: 9, fontWeight: 700,
                  letterSpacing: '0.1em', textTransform: 'uppercase',
                  color: '#e0001a',
                  border: '1px solid #e0001a',
                  padding: '4px 10px',
                  flexShrink: 0,
                  boxShadow: '0 0 8px rgba(224,0,26,0.25)',
                }}>
                  {svc.tag}
                </span>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* ═══════════════════════════════════════════
          SECTION 2 — WHY SMARTSHIP
      ═══════════════════════════════════════════ */}
      <section style={{ position: 'relative', zIndex: 10, padding: '96px 80px', borderTop: '1px solid rgba(255,255,255,0.04)' }}>
        <Reveal>
          <SectionLabel label="Why Choose Us" />
          <div style={{ width: 48, height: 2, background: '#e0001a', marginBottom: 18 }} />
          <h2 style={{ fontFamily: "'Orbitron', monospace", fontSize: 'clamp(24px, 3.5vw, 52px)', fontWeight: 800, color: '#fff', letterSpacing: '-0.01em', marginBottom: 12 }}>
            Why <span style={{ color: '#e0001a' }}>SmartShip?</span>
          </h2>
          <p style={{ color: '#666', fontSize: 14, maxWidth: 480, lineHeight: 1.7, marginBottom: 52 }}>
            Trusted by thousands of Indian online sellers — an all-in-one shipping platform for eCommerce growth.
          </p>
        </Reveal>

        {/* 4×2 feature grid — Armoury Crate module grid */}
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: 1, background: 'rgba(224,0,26,0.1)', border: '1px solid rgba(224,0,26,0.1)', borderRadius: 2, overflow: 'hidden' }}>
          {WHY_FEATURES.map((f, i) => (
            <Reveal key={i} delay={i * 0.06}>
              <div
                style={{
                  background: '#0d0d0d',
                  padding: '28px 24px',
                  height: '100%',
                  borderRight: (i + 1) % 4 !== 0 ? '1px solid rgba(255,255,255,0.05)' : 'none',
                  borderBottom: i < 4 ? '1px solid rgba(255,255,255,0.05)' : 'none',
                  transition: 'background 0.25s ease, box-shadow 0.25s ease',
                  cursor: 'default',
                  position: 'relative',
                  overflow: 'hidden',
                }}
                onMouseEnter={e => {
                  e.currentTarget.style.background = '#141414';
                  e.currentTarget.style.boxShadow = 'inset 0 0 30px rgba(224,0,26,0.04)';
                }}
                onMouseLeave={e => {
                  e.currentTarget.style.background = '#0d0d0d';
                  e.currentTarget.style.boxShadow = 'none';
                }}
              >
                {/* Red corner accent */}
                <div style={{ position: 'absolute', top: 0, right: 0, width: 0, height: 0, borderTop: '24px solid rgba(224,0,26,0.15)', borderLeft: '24px solid transparent' }} />
                {/* Step number */}
                <div style={{ fontFamily: "'Orbitron', monospace", fontSize: 11, fontWeight: 700, color: 'rgba(224,0,26,0.35)', letterSpacing: '0.08em', marginBottom: 14 }}>
                  {String(i + 1).padStart(2, '0')}
                </div>
                <div style={{ fontFamily: 'Rajdhani, sans-serif', fontSize: 15, fontWeight: 700, textTransform: 'uppercase', letterSpacing: '0.08em', color: '#fff', marginBottom: 10 }}>
                  {f.title}
                </div>
                <div style={{ fontSize: 12, color: '#666', lineHeight: 1.7, fontFamily: 'Inter, sans-serif' }}>
                  {f.desc}
                </div>
              </div>
            </Reveal>
          ))}
        </div>
      </section>

      {/* ═══════════════════════════════════════════
          SECTION 3 — HOW IT WORKS
      ═══════════════════════════════════════════ */}
      <section style={{ position: 'relative', zIndex: 10, padding: '96px 80px', borderTop: '1px solid rgba(255,255,255,0.04)', background: 'rgba(224,0,26,0.015)' }}>
        <Reveal>
          <SectionLabel label="Simple Process" />
          <div style={{ width: 48, height: 2, background: '#e0001a', marginBottom: 18 }} />
          <h2 style={{ fontFamily: "'Orbitron', monospace", fontSize: 'clamp(24px, 3.5vw, 52px)', fontWeight: 800, color: '#fff', letterSpacing: '-0.01em', marginBottom: 12 }}>
            How It <span style={{ color: '#e0001a' }}>Works?</span>
          </h2>
          <p style={{ color: '#666', fontSize: 14, maxWidth: 400, lineHeight: 1.7, marginBottom: 60 }}>
            Four simple steps — from pickup request to customer delivery.
          </p>
        </Reveal>

        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: 28, position: 'relative' }}>
          {/* Animated connector line */}
          <div style={{
            position: 'absolute', top: 38, left: '12.5%', right: '12.5%', height: 1, zIndex: 0,
            background: 'linear-gradient(to right, transparent, rgba(224,0,26,0.5) 20%, rgba(224,0,26,0.5) 80%, transparent)',
          }} />
          {/* Animated glow dot running across */}
          <div style={{
            position: 'absolute', top: 32, left: '12.5%', width: 14, height: 14, zIndex: 1, borderRadius: '50%',
            background: '#e0001a', boxShadow: '0 0 20px rgba(224,0,26,0.8)',
            animation: 'slideAcross 4s ease-in-out infinite',
          }} />

          {HOW_STEPS.map((s, i) => (
            <Reveal key={i} delay={i * 0.12} style={{ position: 'relative', zIndex: 2 }}>
              <div style={{ textAlign: 'center', padding: '0 8px' }}>
                <div style={{
                  width: 72, height: 72, borderRadius: '50%',
                  border: '1px solid rgba(224,0,26,0.4)',
                  background: 'linear-gradient(135deg, #0f0f0f, #1a1a1a)',
                  boxShadow: '0 0 24px rgba(224,0,26,0.15)',
                  display: 'flex', alignItems: 'center', justifyContent: 'center',
                  margin: '0 auto 24px',
                  transition: 'box-shadow 0.3s ease',
                }}>
                  <span style={{ fontFamily: "'Orbitron', monospace", fontSize: 20, fontWeight: 900, color: '#e0001a' }}>{s.step}</span>
                </div>
                <div style={{ fontFamily: 'Rajdhani, sans-serif', fontSize: 15, fontWeight: 700, textTransform: 'uppercase', letterSpacing: '0.08em', color: '#fff', marginBottom: 10 }}>
                  {s.title}
                </div>
                <div style={{ fontSize: 12, color: '#666', lineHeight: 1.7, fontFamily: 'Inter, sans-serif' }}>
                  {s.desc}
                </div>
              </div>
            </Reveal>
          ))}
        </div>
      </section>

      {/* ═══════════════════════════════════════════
          SECTION 4 — TRACK SHIPMENT
      ═══════════════════════════════════════════ */}
      <section style={{ position: 'relative', zIndex: 10, padding: '96px 80px', borderTop: '1px solid rgba(255,255,255,0.04)' }}>
        <Reveal>
          <SectionLabel label="Live Tracking" />
          <div style={{ width: 48, height: 2, background: '#e0001a', marginBottom: 18 }} />
          <h2 style={{ fontFamily: "'Orbitron', monospace", fontSize: 'clamp(24px, 3.5vw, 52px)', fontWeight: 800, color: '#fff', letterSpacing: '-0.01em', marginBottom: 12 }}>
            Track Your <span style={{ color: '#e0001a' }}>Shipment</span>
          </h2>
          <p style={{ color: '#666', fontSize: 14, maxWidth: 480, lineHeight: 1.7, marginBottom: 32 }}>
            Enter your AWB number to get real-time status of your order. Find the AWB in the confirmation Email or SMS.
          </p>
          <div style={{
            display: 'flex', maxWidth: 500,
            border: '1px solid rgba(224,0,26,0.3)', borderRadius: 2, overflow: 'hidden',
            boxShadow: '0 0 30px rgba(224,0,26,0.06)',
          }}>
            <input
              type="text"
              placeholder="Enter AWB / Tracking Number"
              style={{
                flex: 1, padding: '13px 16px',
                background: '#0f0f0f', border: 'none', outline: 'none',
                color: '#d4d4d4', fontSize: 13,
                fontFamily: 'Rajdhani, sans-serif', letterSpacing: '0.06em',
              }}
            />
            <button className="ss-btn" style={{ borderRadius: 0, padding: '13px 28px', fontSize: 11, letterSpacing: '0.12em' }}>
              TRACK
            </button>
          </div>
        </Reveal>
      </section>

      {/* ═══════════════════════════════════════════
          SECTION 5 — INTEGRATIONS
      ═══════════════════════════════════════════ */}
      <section style={{ position: 'relative', zIndex: 10, padding: '96px 80px', borderTop: '1px solid rgba(255,255,255,0.04)', background: 'rgba(224,0,26,0.015)' }}>
        <Reveal>
          <SectionLabel label="Sell Anywhere" />
          <div style={{ width: 48, height: 2, background: '#e0001a', marginBottom: 18 }} />
          <h2 style={{ fontFamily: "'Orbitron', monospace", fontSize: 'clamp(22px, 3vw, 48px)', fontWeight: 800, color: '#fff', letterSpacing: '-0.01em', marginBottom: 12 }}>
            Sell Anywhere,{' '}<span style={{ color: '#e0001a' }}>Ship with SmartShip</span>
          </h2>
          <p style={{ color: '#666', fontSize: 14, maxWidth: 480, lineHeight: 1.7, marginBottom: 40 }}>
            Native API integrations with all major eCommerce platforms and marketplaces.
          </p>
        </Reveal>
        <Reveal delay={0.1}>
          <div style={{ display: 'flex', gap: 10, flexWrap: 'wrap' }}>
            {INTEGRATIONS.map((name, i) => (
              <div key={i}
                style={{
                  padding: '10px 22px',
                  border: '1px solid rgba(255,255,255,0.07)',
                  background: '#0f0f0f',
                  fontFamily: 'Rajdhani, sans-serif',
                  fontSize: 12, fontWeight: 700,
                  letterSpacing: '0.1em', textTransform: 'uppercase',
                  color: '#666',
                  transition: 'all 0.2s ease',
                  cursor: 'default',
                }}
                onMouseEnter={e => { e.currentTarget.style.borderColor = 'rgba(224,0,26,0.5)'; e.currentTarget.style.color = '#fff'; e.currentTarget.style.boxShadow = '0 0 14px rgba(224,0,26,0.12)'; }}
                onMouseLeave={e => { e.currentTarget.style.borderColor = 'rgba(255,255,255,0.07)'; e.currentTarget.style.color = '#666'; e.currentTarget.style.boxShadow = 'none'; }}
              >
                {name}
              </div>
            ))}
          </div>
        </Reveal>
      </section>

      {/* ═══════════════════════════════════════════
          SECTION 6 — CTA BANNER
      ═══════════════════════════════════════════ */}
      <section style={{
        position: 'relative', zIndex: 10,
        padding: '72px 80px',
        borderTop: '1px solid rgba(224,0,26,0.18)',
        borderBottom: '1px solid rgba(224,0,26,0.12)',
        background: 'linear-gradient(135deg, rgba(224,0,26,0.07) 0%, rgba(224,0,26,0.02) 50%, transparent 100%)',
      }}>
        <Reveal>
          <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', flexWrap: 'wrap', gap: 28 }}>
            <div>
              <h2 style={{ fontFamily: "'Orbitron', monospace", fontSize: 'clamp(20px, 2.8vw, 40px)', fontWeight: 800, color: '#fff', letterSpacing: '-0.01em', margin: '0 0 10px' }}>
                Ready to Optimise Your <span style={{ color: '#e0001a' }}>Shipping?</span>
              </h2>
              <p style={{ color: '#666', fontSize: 14, margin: 0 }}>
                Join thousands of Indian eCommerce sellers. No commitment. Start for free today.
              </p>
            </div>
            <div style={{ display: 'flex', gap: 12 }}>
              <button className="ss-btn" style={{ fontSize: 12, padding: '13px 32px', letterSpacing: '0.1em' }} onClick={() => navigate('/auth/signup')}>
                Get Started Free
              </button>
              <button className="ss-btn ss-btn-outline" style={{ fontSize: 12, padding: '13px 32px', letterSpacing: '0.1em' }} onClick={() => navigate('/auth/login')}>
                Sign In
              </button>
            </div>
          </div>
        </Reveal>
      </section>

      {/* ═══════════════════════════════════════════
          FOOTER
      ═══════════════════════════════════════════ */}
      <footer style={{ position: 'relative', zIndex: 10, padding: '48px 80px', borderTop: '1px solid rgba(255,255,255,0.04)' }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', flexWrap: 'wrap', gap: 40 }}>
          <div>
            <SmartShipLogo />
            <p style={{ color: '#555', fontSize: 12, marginTop: 14, maxWidth: 260, lineHeight: 1.7, fontFamily: 'Inter, sans-serif' }}>
              One stop solution for all your shipping needs.<br />Easier. Faster. Cheaper.
            </p>
            <div style={{ marginTop: 18 }}>
              <span style={{
                fontFamily: 'Rajdhani, sans-serif', fontSize: 10, fontWeight: 700,
                letterSpacing: '0.12em', textTransform: 'uppercase',
                color: '#00c48c', border: '1px solid rgba(0,196,140,0.3)', padding: '4px 10px',
              }}>
                Toll Free: 1800-309-1122
              </span>
            </div>
          </div>

          <div style={{ display: 'flex', gap: 60, flexWrap: 'wrap' }}>
            <div>
              <div style={{ fontFamily: 'Rajdhani, sans-serif', fontSize: 9, fontWeight: 700, letterSpacing: '0.2em', textTransform: 'uppercase', color: '#333', marginBottom: 16 }}>Quick Links</div>
              {['Terms of Use', 'Privacy Policy', 'Track Shipment', 'Blog'].map(link => (
                <div key={link} style={{ fontSize: 12, color: '#555', marginBottom: 10, cursor: 'pointer', fontFamily: 'Inter, sans-serif', transition: 'color 0.15s' }}
                  onMouseEnter={e => e.currentTarget.style.color = '#e0001a'}
                  onMouseLeave={e => e.currentTarget.style.color = '#555'}
                >{link}</div>
              ))}
            </div>
            <div>
              <div style={{ fontFamily: 'Rajdhani, sans-serif', fontSize: 9, fontWeight: 700, letterSpacing: '0.2em', textTransform: 'uppercase', color: '#333', marginBottom: 16 }}>Contact</div>
              {['contact@smartship.in', 'info@smartship.in'].map(email => (
                <div key={email} style={{ fontSize: 12, color: '#555', marginBottom: 10, fontFamily: 'Inter, sans-serif' }}>{email}</div>
              ))}
              <div style={{ fontSize: 11, color: '#333', marginTop: 14, lineHeight: 1.7, fontFamily: 'Inter, sans-serif' }}>
                Unit No. 901–903, Tower C<br />
                Unitech Cyber Park, Sector 39<br />
                Gurugram, Haryana – 122003
              </div>
            </div>
          </div>
        </div>

        <div style={{ marginTop: 40, paddingTop: 20, borderTop: '1px solid rgba(255,255,255,0.04)', display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: 8 }}>
          <span style={{ fontFamily: 'Rajdhani, sans-serif', fontSize: 10, color: '#333', letterSpacing: '0.1em', textTransform: 'uppercase' }}>
            © {new Date().getFullYear()} SmartShip — All Rights Reserved
          </span>
          <div style={{ display: 'flex', alignItems: 'center', gap: 7 }}>
            <div style={{ width: 7, height: 7, borderRadius: '50%', background: '#00c48c', boxShadow: '0 0 6px rgba(0,196,140,0.8)' }} />
            <span style={{ fontFamily: 'Rajdhani, sans-serif', fontSize: 9, color: '#444', letterSpacing: '0.14em', textTransform: 'uppercase' }}>All Systems Operational</span>
          </div>
        </div>
      </footer>
    </div>
  );
};
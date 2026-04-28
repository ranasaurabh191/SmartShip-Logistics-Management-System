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


// ─── Animated Canvas Background ───────────────────────────────────────────────
const ParticleCanvas = () => {
  const canvasRef = useRef<HTMLCanvasElement>(null);

  useEffect(() => {
    const canvas = canvasRef.current;
    if (!canvas) return;
    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    let animFrame: number;
    let t = 0;
    const mouse = { x: -9999, y: -9999, px: -9999, py: -9999 };

    const resize = () => {
      canvas.width = window.innerWidth;
      canvas.height = window.innerHeight;
    };
    resize();
    window.addEventListener('resize', resize);

    const onMouseMove = (e: MouseEvent) => {
      mouse.px = mouse.x; mouse.py = mouse.y;
      mouse.x = e.clientX; mouse.y = e.clientY;
    };
    const onMouseLeave = () => { mouse.x = -9999; mouse.y = -9999; };
    window.addEventListener('mousemove', onMouseMove);
    window.addEventListener('mouseleave', onMouseLeave);

    // Particles — 3 depth layers
    const layers = [
      { depth: 0.2, count: 30, speed: 0.12, rMax: 1.0, color: [180, 0, 20] as const },
      { depth: 0.5, count: 50, speed: 0.22, rMax: 1.8, color: [224, 0, 0] as const },
      { depth: 1.0, count: 40, speed: 0.40, rMax: 2.8, color: [255, 60, 40] as const },
    ];

    const particles: {
      x: number; y: number; vx: number; vy: number;
      r: number; baseAlpha: number; depth: number;
      color: readonly [number, number, number];
      phase: number; pulseSpeed: number; wander: number;
    }[] = [];

    layers.forEach(layer => {
      for (let i = 0; i < layer.count; i++) {
        const angle = Math.random() * Math.PI * 2;
        const speed = (Math.random() * 0.5 + 0.5) * layer.speed;
        particles.push({
          x: Math.random() * window.innerWidth,
          y: Math.random() * window.innerHeight,
          vx: Math.cos(angle) * speed,
          vy: Math.sin(angle) * speed,
          r: Math.random() * (layer.rMax - 0.4) + 0.4,
          baseAlpha: Math.random() * 0.4 + 0.15,
          depth: layer.depth,
          color: layer.color,
          phase: Math.random() * Math.PI * 2,
          pulseSpeed: 0.008 + Math.random() * 0.012,
          wander: (Math.random() - 0.5) * 0.003,
        });
      }
    });

    const CONNECT = 10;
    const CONNECT_SQ = CONNECT * CONNECT;
    const MOUSE_RADIUS = 180;
    const MOUSE_RADIUS_SQ = MOUSE_RADIUS * MOUSE_RADIUS;
    const REPEL_RADIUS = 60;
    const REPEL_RADIUS_SQ = REPEL_RADIUS * REPEL_RADIUS;
    const ATTRACT_STRENGTH = 0.00018;
    const REPEL_STRENGTH = 0.8;
    const DAMPING = 0.985;

    const draw = () => {
      t += 0.016;
      const W = canvas.width;
      const H = canvas.height;
      ctx.clearRect(0, 0, W, H);

      // Vignette
      const vig = ctx.createRadialGradient(W / 2, H / 2, 0, W / 2, H / 2, Math.max(W, H) * 0.75);
      vig.addColorStop(0, 'rgba(0,0,0,0)');
      vig.addColorStop(1, 'rgba(0,0,0,0.55)');
      ctx.fillStyle = vig;
      ctx.fillRect(0, 0, W, H);

      // Breathing orbs
      const pulse = Math.sin(t * 0.6) * 0.5 + 0.5;
      const pulse2 = Math.sin(t * 0.4 + 1.2) * 0.5 + 0.5;

      const g1 = ctx.createRadialGradient(W * 0.15, H * 0.15, 0, W * 0.15, H * 0.15, 280 + pulse * 60);
      g1.addColorStop(0, `rgba(200,0,20,${0.06 + pulse * 0.04})`);
      g1.addColorStop(0.5, `rgba(160,0,10,${0.03 + pulse * 0.02})`);
      g1.addColorStop(1, 'rgba(0,0,0,0)');
      ctx.fillStyle = g1;
      ctx.fillRect(0, 0, W * 0.55, H * 0.55);

      const g2 = ctx.createRadialGradient(W * 0.85, H * 0.85, 0, W * 0.85, H * 0.85, 320 + pulse2 * 80);
      g2.addColorStop(0, `rgba(220,20,0,${0.07 + pulse2 * 0.04})`);
      g2.addColorStop(0.5, `rgba(140,0,0,${0.03 + pulse2 * 0.02})`);
      g2.addColorStop(1, 'rgba(0,0,0,0)');
      ctx.fillStyle = g2;
      ctx.fillRect(W * 0.45, H * 0.45, W * 0.55, H * 0.55);

      // Mouse orb glow
      if (mouse.x > 0) {
        const mg = ctx.createRadialGradient(mouse.x, mouse.y, 0, mouse.x, mouse.y, MOUSE_RADIUS);
        mg.addColorStop(0, 'rgba(255,40,20,0.08)');
        mg.addColorStop(0.4, 'rgba(250,0,0,0.06)');
        mg.addColorStop(1, 'rgba(0,0,0,0)');
        ctx.fillStyle = mg;
        ctx.fillRect(mouse.x - MOUSE_RADIUS, mouse.y - MOUSE_RADIUS, MOUSE_RADIUS * 2, MOUSE_RADIUS * 2);
      }

      // Update + draw particles
      for (const p of particles) {
        p.phase += p.pulseSpeed;
        const pulseFactor = Math.sin(p.phase) * 0.3 + 0.7;

        p.vx += (Math.random() - 0.5) * 0.004 + p.wander;
        p.vy += (Math.random() - 0.5) * 0.004;

        if (mouse.x > 0) {
          const dx = mouse.x - p.x;
          const dy = mouse.y - p.y;
          const dSq = dx * dx + dy * dy;
          if (dSq < REPEL_RADIUS_SQ) {
            const d = Math.sqrt(dSq) + 0.001;
            const force = (1 - d / REPEL_RADIUS) * REPEL_STRENGTH * p.depth;
            p.vx -= (dx / d) * force;
            p.vy -= (dy / d) * force;
          } else if (dSq < MOUSE_RADIUS_SQ) {
            const d = Math.sqrt(dSq) + 0.001;
            const force = ATTRACT_STRENGTH * p.depth * (1 - dSq / MOUSE_RADIUS_SQ);
            p.vx += (dx / d) * force * d;
            p.vy += (dy / d) * force * d;
          }
        }

        p.vx *= DAMPING;
        p.vy *= DAMPING;

        const spd = Math.sqrt(p.vx * p.vx + p.vy * p.vy);
        const maxSpd = 1.8 * p.depth;
        if (spd > maxSpd) { p.vx = (p.vx / spd) * maxSpd; p.vy = (p.vy / spd) * maxSpd; }

        p.x += p.vx;
        p.y += p.vy;

        if (p.x < -10) p.x = W + 10;
        if (p.x > W + 10) p.x = -10;
        if (p.y < -10) p.y = H + 10;
        if (p.y > H + 10) p.y = -10;

        const [r, g, b] = p.color;
        const finalAlpha = p.baseAlpha * pulseFactor;

        if (p.depth >= 0.8) {
          ctx.beginPath();
          ctx.arc(p.x, p.y, p.r * 3, 0, Math.PI * 2);
          ctx.fillStyle = `rgba(${r},${g},${b},${finalAlpha * 0.12})`;
          ctx.fill();
        }

        ctx.beginPath();
        ctx.arc(p.x, p.y, p.r, 0, Math.PI * 2);
        ctx.fillStyle = `rgba(${r},${g},${b},${finalAlpha})`;
        ctx.fill();
      }

      // Connections between same/adjacent depth layers
      for (let i = 0; i < particles.length; i++) {
        for (let j = i + 1; j < particles.length; j++) {
          if (Math.abs(particles[i].depth - particles[j].depth) > 0.35) continue;
          const dx = particles[i].x - particles[j].x;
          const dy = particles[i].y - particles[j].y;
          const dSq = dx * dx + dy * dy;
          if (dSq < CONNECT_SQ) {
            const d = Math.sqrt(dSq);
            const avgDepth = (particles[i].depth + particles[j].depth) * 0.5;
            const [r, g, b] = particles[i].color;
            ctx.beginPath();
            ctx.strokeStyle = `rgba(${r},${g},${b},${(1 - d / CONNECT) * 0.12 * avgDepth})`;
            ctx.lineWidth = avgDepth * 0.6;
            ctx.moveTo(particles[i].x, particles[i].y);
            ctx.lineTo(particles[j].x, particles[j].y);
            ctx.stroke();
          }
        }
      }

      // Mouse connections
      if (mouse.x > 0) {
        const mConnSq = (CONNECT * 1.4) * (CONNECT * 1.4);
        for (const p of particles) {
          const dx = mouse.x - p.x;
          const dy = mouse.y - p.y;
          const dSq = dx * dx + dy * dy;
          if (dSq < mConnSq) {
            const d = Math.sqrt(dSq);
            ctx.beginPath();
            ctx.strokeStyle = `rgba(255,60,30,${(1 - d / (CONNECT * 1.4)) * 0.35 * p.depth})`;
            ctx.lineWidth = p.depth * 0.8;
            ctx.moveTo(p.x, p.y);
            ctx.lineTo(mouse.x, mouse.y);
            ctx.stroke();
          }
        }
      }

      animFrame = requestAnimationFrame(draw);
    };

    draw();
    return () => {
      cancelAnimationFrame(animFrame);
      window.removeEventListener('resize', resize);
      window.removeEventListener('mousemove', onMouseMove);
      window.removeEventListener('mouseleave', onMouseLeave);
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
        opacity: 100,
      }}
    />
  );
};
// ─── Logistics Hero Animation ─────────────────────────────────────────────────
const LogisticsAnimation = () => {
  const canvasRef = useRef<HTMLCanvasElement>(null);

  useEffect(() => {
    const canvas = canvasRef.current;
    if (!canvas) return;
    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    let animFrame: number;
    let t = 0;

    const W = 620;
    const H = 620;
    canvas.width = W;
    canvas.height = H;

    // ── Hub nodes (logistics network) ──
    const HUB_COLOR = '#e0001a';
    const LINE_COLOR = 'rgba(224, 0, 26,0.51)';

    const hubs = [
      { x: 320, y: 280, r: 18, label: 'HUB', pulse: 0, isMain: true },
      { x: 100, y: 140, r: 8, label: 'DEL', pulse: 1.2, isMain: false },
      { x: 490, y: 100, r: 8, label: 'MUM', pulse: 0.6, isMain: false },
      { x: 110, y: 480, r: 8, label: 'BLR', pulse: 2.1, isMain: false },
      { x: 550, y: 350, r: 8, label: 'HYD', pulse: 1.7, isMain: false },
      { x: 500, y: 490, r: 7, label: 'CHN', pulse: 0.9, isMain: false },
      { x: 280, y: 550, r: 7, label: 'KOL', pulse: 1.4, isMain: false },
      { x: 260, y: 80, r: 6, label: 'PB', pulse: 2.5, isMain: false },
      { x: 150, y: 300, r: 6, label: 'AHM', pulse: 0.3, isMain: false },
      { x: 470, y: 250, r: 6, label: 'LKW', pulse: 1.9, isMain: false },
    ];

    const edges = [
      [0, 1], [0, 2], [0, 3], [0, 4], [0, 5], [0, 6], [0, 7],
      [1, 7], [1, 8], [2, 9], [2, 7], [3, 8], [3, 5], [4, 9], [4, 6], [9, 0], [8, 0]
    ];

    interface Parcel {
      edgeIdx: number;
      progress: number;
      speed: number;
      reverse: boolean;
      trail: { x: number; y: number }[];
    }

    const parcels: Parcel[] = edges.map(() => ({
      edgeIdx: 0,
      progress: Math.random(),
      speed: 0.0029 + Math.random() * 0.0012,
      reverse: Math.random() > 0.5,
      trail: [],
    })).map((p, i) => ({ ...p, edgeIdx: i }));



    const gridDots: { x: number; y: number; alpha: number; phase: number }[] = [];
    for (let r = 0; r < 14; r++) {
      for (let c = 0; c < 14; c++) {
        gridDots.push({
          x: (c / 13) * W,
          y: (r / 13) * H,
          alpha: Math.random() * 0.12 + 0.02,
          phase: Math.random() * Math.PI * 2,
        });
      }
    }

    // ── Draw a box parcel icon ──────────────────────────────────────────────
    const drawParcel = (
      cx: number, cy: number,
      angle: number,
      alpha: number
    ) => {
      const W_BOX = 11;
      const H_BOX = 9;

      ctx.save();
      ctx.translate(cx, cy);
      ctx.rotate(angle);
      ctx.globalAlpha = alpha;

      // Box body
      ctx.beginPath();
      ctx.rect(-W_BOX / 2, -H_BOX / 2, W_BOX, H_BOX);
      ctx.fillStyle = '#0d0d0d';
      ctx.fill();
      ctx.strokeStyle = '#e0001a';
      ctx.lineWidth = 1.2;
      ctx.stroke();

      // Lid top line
      ctx.beginPath();
      ctx.moveTo(-W_BOX / 2, -H_BOX / 2 + 3);
      ctx.lineTo(W_BOX / 2, -H_BOX / 2 + 3);
      ctx.strokeStyle = 'rgba(224,0,26,0.55)';
      ctx.lineWidth = 0.8;
      ctx.stroke();

      // Tape stripe (vertical center)
      ctx.beginPath();
      ctx.moveTo(0, -H_BOX / 2);
      ctx.lineTo(0, H_BOX / 2);
      ctx.strokeStyle = 'rgba(224,0,26,0.4)';
      ctx.lineWidth = 1.5;
      ctx.stroke();

      // Tape stripe (horizontal center)
      ctx.beginPath();
      ctx.moveTo(-W_BOX / 2, 0);
      ctx.lineTo(W_BOX / 2, 0);
      ctx.strokeStyle = 'rgba(224,0,26,0.4)';
      ctx.lineWidth = 1.5;
      ctx.stroke();

      // Glow dot in center
      ctx.beginPath();
      ctx.arc(0, 0, 1.2, 0, Math.PI * 2);
      ctx.fillStyle = '#c1c1c1';
      ctx.shadowBlur = 6;
      ctx.shadowColor = '#e0001a';
      ctx.fill();
      ctx.shadowBlur = 0;

      ctx.globalAlpha = 1;
      ctx.restore();
    };

    const draw = () => {
      t += 0.016;
      ctx.clearRect(0, 0, W, H);

      // Grid dots
      for (const d of gridDots) {
        const a = d.alpha * (0.6 + 0.4 * Math.sin(d.phase + t * 0.4));
        ctx.beginPath();
        ctx.arc(d.x, d.y, 0.8, 0, Math.PI * 2);
        ctx.fillStyle = `rgba(224,0,26,${a})`;
        ctx.fill();
      }

      // Edges
      for (const [a, b] of edges) {
        ctx.beginPath();
        ctx.moveTo(hubs[a].x, hubs[a].y);
        ctx.lineTo(hubs[b].x, hubs[b].y);
        ctx.strokeStyle = LINE_COLOR;
        ctx.lineWidth = 0.8;
        ctx.setLineDash([4, 6]);
        ctx.stroke();
        ctx.setLineDash([]);
      }

      // Parcels
      for (const p of parcels) {
        p.progress += p.speed;
        if (p.progress > 1) { p.progress = 0; p.reverse = !p.reverse; }

        const [ai, bi] = edges[p.edgeIdx];
        const from = p.reverse ? hubs[bi] : hubs[ai];
        const to = p.reverse ? hubs[ai] : hubs[bi];
        const px = from.x + (to.x - from.x) * p.progress;
        const py = from.y + (to.y - from.y) * p.progress;

        // Direction angle so box faces travel direction
        const angle = Math.atan2(to.y - from.y, to.x - from.x);

        p.trail.push({ x: px, y: py });
        if (p.trail.length > 10) p.trail.shift();

        // Trail
        for (let i = 0; i < p.trail.length - 1; i++) {
          const a = (i / p.trail.length) * 0.35;
          ctx.beginPath();
          ctx.moveTo(p.trail[i].x, p.trail[i].y);
          ctx.lineTo(p.trail[i + 1].x, p.trail[i + 1].y);
          ctx.strokeStyle = `rgba(224,0,26,${a})`;
          ctx.lineWidth = 1;
          ctx.stroke();
        }

        // Parcel box
        drawParcel(px, py, angle, 0.9);
      }

      // ── Hub nodes ──
      for (const h of hubs) {
        const pulse = Math.sin(t * 1.4 + h.pulse) * 0.5 + 0.5;

        if (h.isMain) {
          // Outer pulse ring
          ctx.beginPath();
          ctx.arc(h.x, h.y, h.r + 10 + pulse * 10, 0, Math.PI * 2);
          ctx.strokeStyle = `rgba(224,0,0,${0.08 + pulse * 0.12})`;
          ctx.lineWidth = 1;
          ctx.stroke();


        } else {
          // Subtle halo
          ctx.beginPath();
          ctx.arc(h.x, h.y, h.r + 4 + pulse * 4, 0, Math.PI * 2);
          ctx.fillStyle = `rgba(224,0,26,${0.04 + pulse * 0.06})`;
          ctx.fill();
        }

        // Node fill
        ctx.beginPath();
        ctx.arc(h.x, h.y, h.r, 0, Math.PI * 2);
        ctx.fillStyle = h.isMain ? HUB_COLOR : '#0d0d0d';
        ctx.strokeStyle = h.isMain ? HUB_COLOR : `rgba(224,0,26,${0.4 + pulse * 0.4})`;
        ctx.lineWidth = h.isMain ? 0 : 1.2;
        ctx.fill();
        ctx.stroke();

        // Inner dot for non-main
        if (!h.isMain) {
          ctx.beginPath();
          ctx.arc(h.x, h.y, 2.5, 0, Math.PI * 2);
          ctx.fillStyle = `rgba(224,224,224,${0.6 + pulse * 0.4})`;
          ctx.fill();
        }

        // Label
        ctx.font = `900 12px 'Orbitron', monospace`;
        ctx.fillStyle = h.isMain ? '#fff' : `rgba(224,0,26,${0.5 + pulse * 0.4})`;
        ctx.textAlign = 'center';
        ctx.fillText(h.label, h.x, h.y + (h.isMain ? 3 : h.r + 12));
      }



      // ── Bottom status bar ──
      const barY = H;
      ctx.font = `900 13px 'Orbitron', monospace`;
      ctx.fillStyle = 'rgb(252, 0, 29)';
      ctx.textAlign = 'left';
      ctx.fillText('NETWORKS ACTIVE', 470, barY);


      animFrame = requestAnimationFrame(draw);
    };

    draw();
    return () => cancelAnimationFrame(animFrame);
  }, []);

  return (
    <canvas
      ref={canvasRef}
      style={{
        opacity: 1.92,
        display: 'block',
        filter: 'drop-shadow(0 0 40px rgba(224,0,26,0.15))',
        marginRight: '-300px',
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
  <div style={{ display: 'inline-flex', alignItems: 'center', gap: 8, marginBottom: 14, padding: '4px 12px', border: '1px solid rgba(224,0,26,0.35)', borderRadius: 4 }}>
    <div style={{ width: 6, height: 6, borderRadius: '50%', background: '#e0001a', boxShadow: '0 0 6px rgba(224,0,26,0.8)' }} />
    <span style={{ fontFamily: "'Orbitron', monospace", fontSize: 9, fontWeight: 700, letterSpacing: '0.18em', textTransform: 'uppercase', color: '#e4e4e4ff' }}>
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
    <div style={{ minHeight: '100vh', padding: '1px 20px', display: 'flex', flexDirection: 'column', background: '#0a0a0a', position: 'relative', overflowX: 'hidden' }}>

      {/* ── Animated network mesh background ── */}
      <ParticleCanvas />

      {/* ── Scanline overlay ── */}
      <div style={{
        position: 'fixed', inset: 0, zIndex: 1, pointerEvents: 'none',
        backgroundImage: 'repeating-linear-gradient(0deg, transparent, transparent 2px, rgba(255,255,255,0.012) 2px, rgba(255,255,255,0.012) 4px)',
      }} />

      {/* ═══════════════════════════════════════════
          NAVBAR —
      ═══════════════════════════════════════════ */}
      <nav style={{
        position: 'fixed', top: 0, left: 0, right: 0, zIndex: 200,
        padding: '16px 100px',
        display: 'flex', alignItems: 'center', justifyContent: 'space-between',
        background: navSolid ? 'rgba(8,8,8,0.97)' : 'rgba(8,8,8,0.6)',
        backdropFilter: 'blur(16px)',
        borderBottom: `1px solid ${navSolid ? 'rgba(224,0,26,0.18)' : 'rgba(255,255,255,0.04)'}`,
        transition: 'all 0.6s 0.1s ease',
        opacity: heroVisible ? 1 : 0,
        transform: heroVisible ? 'translateY(0)' : 'translateY(-20px)',
      }}>
        <SmartShipLogo />
        <div style={{ display: 'flex', alignItems: 'center', gap: 14 }}>
          <button
            className="ss-btn ss-btn-outline"
            style={{ padding: '8px 20px', fontSize: 12 }}
            onClick={() => navigate('/auth/login')}
          >
            Sign In
          </button>
          <button
            className="ss-btn"
            style={{ padding: '8px 20px', fontSize: 12 }}
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
            <div style={{ width: 7, height: 7, borderRadius: '50%', background: '#00c48c', boxShadow: '0 0 7px rgba(0,196,140,0.8)', animation: 'blink 0.8s infinite' }} />
            <span style={{ fontFamily: "'Orbitron', monospace", fontSize: 10, fontWeight: 900, letterSpacing: '0.16em', textTransform: 'uppercase', color: '#cacacaff' }}>
              India's Largest Pincode Network — 31,000+ Serviceable
            </span>
          </div>




          {/* Main heading */}
          <h1 style={{
            fontFamily: "'Orbitron', monospace",
            fontSize: 'clamp(32px, 5.5vw, 86px)',
            fontWeight: 900,
            lineHeight: 1.05,
            letterSpacing: '-0.01em',
            color: '#ffffff',
            marginBottom: 2,
            opacity: heroVisible ? 1 : 0,
            transform: heroVisible ? 'translateY(0)' : 'translateY(40px)',
            transition: 'all 0.7s 0.3s ease',
          }}>
            SmartShip
            <br />
            <span style={{ color: '#e0001a', textShadow: '0 0 40px rgba(224,0,26,0.4)' }}>Shipping</span>
            <br />
            Solution
          </h1>
          <div style={{
            width: 381, height: 4, background: '#e0001a', marginBottom: 20,
            borderRadius: 5,
            opacity: heroVisible ? 1 : 0,
            transform: heroVisible ? 'scaleX(1)' : 'scaleX(0)',
            transformOrigin: 'left',
            transition: 'all 1.5s .5s ease',
          }} />

          <p style={{
            fontSize: 14, color: '#aeaeaeff', lineHeight: 1.8, maxWidth: 440, marginBottom: 36,
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
            <button className="ss-btn" style={{ fontSize: 13, padding: '12px 28px', letterSpacing: '0.1em' }} onClick={() => navigate('/auth/login')}>
              Get Started
            </button>
            <button className="ss-btn ss-btn-outline" style={{ fontSize: 13, padding: '12px 28px', letterSpacing: '0.1em', fontWeight: 900 }} onClick={() => navigate('/auth/signup')}>
              Register Free
            </button>
          </div>

          {/* Stats strip */}
          <div style={{
            display: 'flex', gap: 36, flexWrap: 'wrap',
            paddingTop: 2, borderTop: '1px solid rgba(255,255,255,0.06)',
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
                <div style={{ fontFamily: 'Rajdhani, sans-serif', fontSize: 10, fontWeight: 900, color: '#d2d2d2ff', marginTop: 3, letterSpacing: '0.24em', textTransform: 'uppercase' }}>
                  {stat.label}
                </div>
              </div>
            ))}
          </div>
        </div>
        {/* Right hero — Logistics Animation */}
        <div style={{
          flex: 1,
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          position: 'relative',
          overflow: 'visible',
          opacity: heroVisible ? 1 : 0,
          transform: heroVisible ? 'translateX(0)' : 'translateX(60px)',
          transition: 'opacity 0.8s 0.6s ease, transform 0.8s 0.6s ease',
        }}>
          {/* Subtle backing glow */}
          <div style={{
            position: 'absolute',
            borderRadius: '50%',
            background: 'radial-gradient(circle, rgba(224,0,26,0.06) 0%, transparent 70%)',
            pointerEvents: 'none',

          }} />
          <LogisticsAnimation />
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
          <p style={{ color: '#c3c3c3ff', fontSize: 14, maxWidth: 480, lineHeight: 1.7, marginBottom: 82 }}>
            Trusted by thousands of Indian online sellers — an all-in-one shipping platform for eCommerce growth.
          </p>
        </Reveal>

        {/* 4×2 feature grid — Armoury Crate module grid */}
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: 8, background: 'rgba(224, 0, 26, 0)', border: '1px solid rgba(224, 0, 26, 0)', borderRadius: 2, overflow: 'hidden' }}>
          {WHY_FEATURES.map((f, i) => (
            <Reveal key={i} delay={i * 0.06}>
              <div
                style={{
                  background: '#0d0d0d',
                  padding: '28px 24px',
                  height: '100%',
                  borderRight: (i + 1) % 4 !== 0 ? '1px solid rgba(255, 255, 255, 0)' : 'none',
                  borderBottom: i < 4 ? '1px solid rgba(255,255,255,0.05)' : 'none',
                  transition: 'background 0.25s ease, box-shadow 0.25s ease',
                  cursor: 'default',
                  position: 'relative',
                  overflow: 'hidden',
                }}
                onMouseEnter={e => {
                  e.currentTarget.style.background = '#141414';
                  e.currentTarget.style.boxShadow = 'inset 0 0 30px rgba(224, 0, 26, 0.38)';
                }}
                onMouseLeave={e => {
                  e.currentTarget.style.background = '#0d0d0d';
                  e.currentTarget.style.boxShadow = 'none';
                }}
              >
                {/* Red corner accent */}
                <div style={{ position: 'absolute', top: 0, right: 0, width: 0, height: 0, borderTop: '28px solid rgba(224, 0, 26, 0.28)', borderLeft: '24px solid transparent' }} />
                {/* Step number */}
                <div style={{ fontFamily: "'Orbitron', monospace", fontSize: 18, fontWeight: 700, color: 'rgba(255, 0, 30, 0.78)', letterSpacing: '0.08em', marginBottom: 14 }}>
                  {String(i + 1).padStart(2, '0')}
                </div>
                <div style={{ fontFamily: 'Rajdhani, sans-serif', fontSize: 18, fontWeight: 700, textTransform: 'uppercase', letterSpacing: '0.10em', color: '#fff', marginBottom: 10 }}>
                  {f.title}
                </div>
                <div style={{ fontSize: 12, color: '#a0a0a0ff', lineHeight: 1.7, fontFamily: 'Inter, sans-serif' }}>
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
          <p style={{ color: '#c3c3c3ff', fontSize: 14, maxWidth: 480, lineHeight: 1.7, marginBottom: 82 }}>
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
            position: 'absolute', top: 32, left: '12.5%', width: 12, height: 12, zIndex: 1, borderRadius: '50%',
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
                <div style={{ fontSize: 14, color: '#b2afafff', lineHeight: 1.7, fontFamily: 'Inter, sans-serif' }}>
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
        <SectionLabel label="Live Tracking" />
        <div style={{ width: 48, height: 2, background: '#e0001a', marginBottom: 18 }} />
        <h2 style={{ fontFamily: "'Orbitron', monospace", fontSize: 'clamp(24px, 3.5vw, 52px)', fontWeight: 800, color: '#fff', letterSpacing: '-0.01em', marginBottom: 12 }}>
          Track Your <span style={{ color: '#e0001a' }}>Shipment</span>
        </h2>
        <button
          className="ss-btn"
          style={{
            padding: '14px 32px',
            fontSize: 14,
            letterSpacing: '0.12em',
            cursor: 'pointer',
            boxShadow: '0 0 20px rgba(224,0,26,0.8)', 
          }}
          onClick={() => navigate('/auth/login')}
        >
          TRACK SHIPMENT
        </button>
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
          <p style={{ color: '#c3c3c3ff', fontSize: 14, maxWidth: 480, lineHeight: 1.7, marginBottom: 40 }}>
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
                  fontSize: 14, fontWeight: 700,
                  letterSpacing: '0.1em', textTransform: 'uppercase',
                  color: '#c2c2c2ff',
                  transition: 'all 0.2s ease',
                  cursor: 'default',
                  boxShadow: '0 0 30px rgba(224, 0, 26, 0.53)',
                }}
                onMouseEnter={e => { e.currentTarget.style.borderColor = 'rgba(224,0,26,0.5)'; e.currentTarget.style.color = '#fff'; e.currentTarget.style.boxShadow = '0 0 14px rgba(224,0,26,0.12)'; }}
                onMouseLeave={e => { e.currentTarget.style.borderColor = 'rgba(0, 0, 0, 0.5)'; e.currentTarget.style.color = '#c2c2c2ff'; e.currentTarget.style.boxShadow = '0 0 30px rgba(224, 0, 26, 0.53)'; }}
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
              <p style={{ color: '#b1b1b1ff', fontSize: 14, margin: 0 }}>
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
            <div style={{ marginTop: 18 }}>
              <SmartShipLogo />
            </div>
            <p style={{ color: '#a7a7a7ff', fontSize: 12, marginTop: 14, maxWidth: 260, lineHeight: 1.7, fontFamily: 'Inter, sans-serif' }}>
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
              <div style={{ fontFamily: 'Rajdhani, sans-serif', fontSize: 12, fontWeight: 700, letterSpacing: '0.2em', textTransform: 'uppercase', color: '#9b9b9bff', marginBottom: 16 }}>Quick Links</div>
              {['Terms of Use', 'Privacy Policy', 'Track Shipment', 'Blog'].map(link => (
                <div key={link} style={{ fontSize: 12, color: '#bababaff', marginBottom: 10, cursor: 'pointer', fontFamily: 'Inter, sans-serif', transition: 'color 0.15s' }}
                  onMouseEnter={e => e.currentTarget.style.color = '#e0001a'}
                  onMouseLeave={e => e.currentTarget.style.color = '#bababaff'}
                >{link}</div>
              ))}
            </div>
            <div>
              <div style={{ fontFamily: 'Rajdhani, sans-serif', fontSize: 12, fontWeight: 700, letterSpacing: '0.2em', textTransform: 'uppercase', color: '#9b9b9bff', marginBottom: 16 }}>Contact</div>
              {['contact@smartship.in', 'info@smartship.in'].map(email => (
                <div key={email} style={{ fontSize: 12, color: '#bababaff', marginBottom: 10, fontFamily: 'Inter, sans-serif' }}>{email}</div>
              ))}
              <div style={{ fontSize: 12, color: '#aaaaaaff', marginTop: 14, lineHeight: 1.7, fontFamily: 'Inter, sans-serif' }}>
                Unit No. 901–903, Tower C<br />
                Unitech Cyber Park, Sector 39<br />
                Gurugram, Haryana – 122003
              </div>
            </div>
          </div>
        </div>

        <div style={{ marginTop: 10, paddingTop: 10, borderTop: '1px solid rgba(255,255,255,0.04)', display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: 8 }}>


        </div>
      </footer>
    </div>
  );
};
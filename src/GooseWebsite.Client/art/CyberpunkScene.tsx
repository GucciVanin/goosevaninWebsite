import { useEffect, useRef, type CSSProperties } from 'react';

// Ships crossing the skyline: size is a share of the stage width, top a share of its height, dur/delay in seconds.
// Negative delays start the ships partway through their crossing, so the sky is never empty on first paint.
const ships: { w: string; top: string; dur: number; delay: number; alpha: number; back?: boolean }[] = [
  { w: '6%', top: '22%', dur: 34, delay: -6, alpha: 0.95 },
  { w: '4.2%', top: '33%', dur: 46, delay: -30, alpha: 0.8, back: true },
  { w: '3%', top: '15%', dur: 58, delay: -14, alpha: 0.65 },
  { w: '2.4%', top: '9%', dur: 72, delay: -52, alpha: 0.5, back: true },
];

/**
 * Animated cyberpunk hero scene (from Docs/Inspirations/cyberpunk-react-loop, extended). The art is one image; React
 * lays the motion layers over it and adds a gentle pointer parallax, which is skipped for touch screens and for
 * people who prefer reduced motion. Decoration only: hidden from assistive technology.
 */
export function CyberpunkScene({ className = '' }: { className?: string }) {
  const scene = useRef<HTMLElement>(null);

  useEffect(() => {
    const element = scene.current;
    const still = matchMedia('(prefers-reduced-motion: reduce)');
    const fine = matchMedia('(pointer: fine)');
    if (!element || still.matches || !fine.matches) {
      return;
    }

    const move = (event: PointerEvent) => {
      const x = (event.clientX / window.innerWidth - 0.5) * -14;
      const y = (event.clientY / window.innerHeight - 0.5) * -8;
      element.style.setProperty('--px', `${x.toFixed(1)}px`);
      element.style.setProperty('--py', `${y.toFixed(1)}px`);
    };
    window.addEventListener('pointermove', move, { passive: true });
    return () => window.removeEventListener('pointermove', move);
  }, []);

  return (
    <section ref={scene} className={`cyberpunk-scene ${className}`.trim()} aria-hidden="true">
      <div className="stage">
        <div className="art" />
        <div className="sky">
          <div className="sky__glow" />
          <div className="sky__clouds" />
        </div>
        <div className="haze haze--one" />
        <div className="haze haze--two" />
        <div className="neon neon--koi" />
        <div className="neon neon--kanji" />
        <div className="neon neon--left" />
        <div className="neon neon--cyan" />
        <div className="terminal-halo" />
        <div className="terminal">
          <div className="terminal__glow" />
          <div className="terminal__scan" />
          <div className="terminal__sweep" />
        </div>
        {ships.map((ship, index) => (
          <div
            key={index}
            className={`ship${ship.back ? ' ship--back' : ''}`}
            style={{ '--w': ship.w, '--top': ship.top, '--dur': `${ship.dur}s`, '--delay': `${ship.delay}s`, '--alpha': ship.alpha } as CSSProperties}
          >
            <div className="ship__trail" />
            <div className="ship__body" />
          </div>
        ))}
        <div className="grain" />
      </div>
    </section>
  );
}

// React port of the animated cyberpunk hero scene (Docs/Inspirations/cyberpunk-react-loop). The artwork stays
// intact and CSS layers add the motion; all of it stops under prefers-reduced-motion (see CyberpunkScene.css).
export function CyberpunkScene({ className = '' }: { className?: string }) {
  return (
    <section className={`cyberpunk-scene ${className}`.trim()} aria-hidden="true">
      <div className="cyberpunk-scene__art" />
      <div className="cyberpunk-scene__sky-glow" />
      <div className="cyberpunk-scene__haze cyberpunk-scene__haze--one" />
      <div className="cyberpunk-scene__haze cyberpunk-scene__haze--two" />
      <div className="cyberpunk-scene__ship cyberpunk-scene__ship--one"><i /></div>
      <div className="cyberpunk-scene__ship cyberpunk-scene__ship--two"><i /></div>
      <div className="cyberpunk-scene__ship cyberpunk-scene__ship--three"><i /></div>
      <div className="cyberpunk-scene__terminal-glow" />
      <div className="cyberpunk-scene__terminal-scanlines" />
      <div className="cyberpunk-scene__neon-flicker" />
      <div className="cyberpunk-scene__grain" />
    </section>
  );
}

import React from 'react';
import './CyberpunkScene.css';

/**
 * React-ready animated cyberpunk hero scene.
 * Put cyberpunk-city.png in public/ and import this component.
 * The base artwork stays intact; CSS overlays add subtle ambient motion.
 */
export default function CyberpunkScene({ className = '' }) {
  return (
    <section className={`cyberpunk-scene ${className}`} aria-label="Animated cyberpunk city background">
      <div className="cyberpunk-scene__art" />
      <div className="cyberpunk-scene__sky-glow" />
      <div className="cyberpunk-scene__haze cyberpunk-scene__haze--one" />
      <div className="cyberpunk-scene__haze cyberpunk-scene__haze--two" />

      {/* Extra silhouettes create gentle ship motion across the skyline. */}
      <div className="cyberpunk-scene__ship cyberpunk-scene__ship--one" aria-hidden="true"><i /></div>
      <div className="cyberpunk-scene__ship cyberpunk-scene__ship--two" aria-hidden="true"><i /></div>
      <div className="cyberpunk-scene__ship cyberpunk-scene__ship--three" aria-hidden="true"><i /></div>

      {/* Screen shimmer and scanlines sit over the laptop area in the artwork. */}
      <div className="cyberpunk-scene__terminal-glow" />
      <div className="cyberpunk-scene__terminal-scanlines" />
      <div className="cyberpunk-scene__neon-flicker" />
      <div className="cyberpunk-scene__grain" />
    </section>
  );
}

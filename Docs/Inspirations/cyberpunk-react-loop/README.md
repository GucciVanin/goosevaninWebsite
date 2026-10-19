# Cyberpunk animated React scene

This is a React-ready animated hero background based on the generated cyberpunk artwork. It adds subtle looping motion for the sky glow, atmospheric haze, flying ship silhouettes, terminal shimmer/scanlines, and neon flicker.

## Install

1. Copy `CyberpunkScene.jsx` and `CyberpunkScene.css` into your React app.
2. Copy `public/cyberpunk-city.png` into your app's `public/` directory.
3. Import and render the component:

```jsx
import CyberpunkScene from './CyberpunkScene';

export default function Home() {
  return (
    <main>
      <CyberpunkScene />
      {/* Place hero text or navigation in a sibling/overlay container as needed. */}
    </main>
  );
}
```

The scene fills the available width and has a responsive minimum height. For a hero with text layered on top, place the scene inside a positioned wrapper and overlay your content with `position: absolute` and a higher `z-index`.

## Note on realism

The original artwork is a flattened image. These CSS effects add motion on top, but do not truly animate the original laptop code or the original ships independently. For fully realistic motion, create separate transparent layers (sky, ships, terminal, hologram, foreground) or use a seamless rendered video/WebM loop. The component respects `prefers-reduced-motion`.

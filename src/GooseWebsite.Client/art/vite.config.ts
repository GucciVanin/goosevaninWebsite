import react from '@vitejs/plugin-react';
import { defineConfig } from 'vite';

// Builds the React islands into public/art, which Angular copies into the site. Run by `npm run build:art`
// (and automatically before `npm run build` and `npm start`).
export default defineConfig({
  root: __dirname,
  plugins: [react()],
  define: { 'process.env.NODE_ENV': '"production"' },
  build: {
    outDir: '../public/art',
    emptyOutDir: true,
    lib: { entry: 'gcv-scene-element.tsx', formats: ['es'], fileName: () => 'gcv-scene.js' },
    cssCodeSplit: false,
    minify: true,
    reportCompressedSize: true,
  },
});

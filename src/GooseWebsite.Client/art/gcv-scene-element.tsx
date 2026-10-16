import { createRoot, type Root } from 'react-dom/client';
import { CyberpunkScene } from './CyberpunkScene';
import styles from './CyberpunkScene.css?inline';

// <gcv-scene> is the React island the Angular pages embed. Shadow DOM keeps its styles out of the page and the page's
// styles out of it. The scene is decoration: it is hidden from assistive technology and ignores pointer input.
class GcvScene extends HTMLElement {
  private root: Root | null = null;

  connectedCallback(): void {
    const shadow = this.shadowRoot ?? this.attachShadow({ mode: 'open' });
    if (!this.root) {
      const style = document.createElement('style');
      style.textContent = styles;
      const mount = document.createElement('div');
      shadow.replaceChildren(style, mount);
      this.root = createRoot(mount);
    }

    this.root.render(<CyberpunkScene />);
  }

  disconnectedCallback(): void {
    // Defer so a move within the page (disconnect then connect) does not tear the scene down needlessly.
    queueMicrotask(() => {
      if (!this.isConnected) {
        this.root?.unmount();
        this.root = null;
      }
    });
  }
}

if (!customElements.get('gcv-scene')) {
  customElements.define('gcv-scene', GcvScene);
}

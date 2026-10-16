import { CUSTOM_ELEMENTS_SCHEMA, Component, OnInit, PLATFORM_ID, inject } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';

const ScriptId = 'gcv-art-script';

// The animated hero artwork. It is a React island: the <gcv-scene> element is defined by /art/gcv-scene.js (built from
// client/art), which is loaded the first time a page needs it, so pages without the scene never download React.
// The scene is decoration (hidden from assistive technology) and stops moving under prefers-reduced-motion.
@Component({
  selector: 'app-art-scene',
  schemas: [CUSTOM_ELEMENTS_SCHEMA],
  template: `<gcv-scene></gcv-scene>`,
  styles: `
    :host { display: block; height: 100%; }
    gcv-scene { display: block; height: 100%; }
  `,
})
export class ArtScene implements OnInit {
  private readonly browser = isPlatformBrowser(inject(PLATFORM_ID));

  ngOnInit(): void {
    if (!this.browser || document.getElementById(ScriptId)) {
      return;
    }

    const script = document.createElement('script');
    script.id = ScriptId;
    script.type = 'module';
    script.src = '/art/gcv-scene.js';
    document.head.appendChild(script);
  }
}

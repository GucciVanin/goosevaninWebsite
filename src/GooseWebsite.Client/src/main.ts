import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { App } from './app/app';

// Starts the standalone Angular application from one explicit browser entry point.
bootstrapApplication(App, appConfig).catch((err) => console.error(err));

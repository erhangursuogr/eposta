import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { App } from './app/app';

// Moment.js Türkçe locale
import 'moment/locale/tr';

bootstrapApplication(App, appConfig)
  .catch((err) => console.error(err));

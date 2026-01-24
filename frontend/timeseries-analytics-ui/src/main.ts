import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { App } from './app/app';
import 'ag-grid-enterprise';
import { ClientSideRowModelModule, ModuleRegistry } from 'ag-grid-community';
import { ServerSideRowModelModule } from 'ag-grid-enterprise';

ModuleRegistry.registerModules([
  ClientSideRowModelModule,
  ServerSideRowModelModule
]);

bootstrapApplication(App, appConfig)
  .catch((err) => console.error(err));

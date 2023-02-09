import {enableProdMode} from '@angular/core';
import {platformBrowserDynamic} from '@angular/platform-browser-dynamic';

import {AppModule} from './app/app.module';
import {environment} from './environments/environment';

export function getBaseUrl() {
  return document.getElementsByTagName('base')[0].href;
}

const providers = [
  {provide: 'BASE_URL', useFactory: getBaseUrl, deps: []}
];

if (environment.production) {
  enableProdMode();
}

platformBrowserDynamic(providers)
    .bootstrapModule(AppModule)
    .catch(err => console.log(err));

import { FormGroup } from '@angular/forms';

declare module '@angular/forms' {
  interface FormGroup {
    getControlValue(this: FormGroup, control: string): any;
    setControlValue(this: FormGroup, control: string, value: any): void;
  }
}

FormGroup.prototype.getControlValue = function(this: FormGroup, control: string): any {
  let value: any = this.controls[control]?.value;

  if (typeof(value) === 'string')
    value = value?.trim();

  return value;
}

FormGroup.prototype.setControlValue = function(this: FormGroup, control: string, value: any): void {
  this.controls[control]?.setValue(value);
}
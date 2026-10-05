import { Component, inject } from '@angular/core';
import { ApiService } from '../api.service';

@Component({
  selector: 'app-home',
  template: `
    <h1>Welcome, {{ api.me()?.firstName }}!</h1>
    <p>Open the menu to study a topic or see everyone's profile.</p>
  `,
})
export class Home {
  api = inject(ApiService);
}

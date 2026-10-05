import { Component } from '@angular/core';

@Component({
  selector: 'app-signin',
  template: `
    <main class="centered">
      <h1>You are not logged in</h1>
      <p>In the cohort's Discord server, run <code>/register</code> the first time, then <code>/login</code>.</p>
      <p>Click the link the bot sends you (it works once and expires after 60 minutes).</p>
    </main>
  `,
})
export class Signin {}

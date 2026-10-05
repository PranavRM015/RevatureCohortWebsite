import { Component, OnInit, inject, signal } from '@angular/core';
import { ApiService } from '../api.service';
import { Profile, TRACKS } from '../models';

@Component({
  selector: 'app-profiles',
  template: `
    <h1>Profiles</h1>

    <section class="link-box">
      <h2>Your profile link</h2>
      @if (!api.me()?.profileLink) {
        <p>You have not added a profile link yet. Paste a link to your portfolio, GitHub or LinkedIn.</p>
      }
      <div class="link-row">
        <input
          type="url"
          placeholder="https://..."
          [value]="link()"
          (input)="link.set($any($event.target).value)"
          aria-label="Profile link"
        />
        <button class="primary" type="button" (click)="save()">Save</button>
      </div>
      @if (message()) {
        <p class="hint">{{ message() }}</p>
      }
    </section>

    <div class="cards">
      @for (p of profiles(); track p.discordId) {
        <article class="card">
          <div class="bar" [style.background-color]="p.profileColor"></div>
          <h3>{{ p.username }}</h3>
          <p>{{ track(p) }} track</p>
          @if (p.profileLink) {
            <a [href]="p.profileLink" target="_blank" rel="noopener">{{ p.profileLink }}</a>
          } @else {
            <p class="hint">No link yet</p>
          }
        </article>
      }
    </div>
  `,
})
export class Profiles implements OnInit {
  api = inject(ApiService);

  profiles = signal<Profile[]>([]);
  link = signal('');
  message = signal('');

  async ngOnInit() {
    this.link.set(this.api.me()?.profileLink ?? '');
    this.profiles.set(await this.api.profiles());
  }

  track(p: Profile): string {
    return TRACKS[p.track] ?? 'Unknown';
  }

  async save() {
    this.message.set('');
    try {
      await this.api.updateMe({ profileLink: this.link().trim() });
      this.profiles.set(await this.api.profiles());
      this.message.set('Saved.');
    } catch {
      this.message.set('Enter a valid link starting with http:// or https://');
    }
  }
}

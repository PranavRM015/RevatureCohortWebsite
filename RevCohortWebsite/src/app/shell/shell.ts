import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { ApiService } from '../api.service';
import { Topic } from '../models';

@Component({
  selector: 'app-shell',
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  template: `
    <div class="layout" [class.closed]="!open()">
      <button class="burger" type="button" aria-label="Toggle menu" (click)="open.set(!open())">
        <span></span><span></span><span></span>
      </button>
      <aside class="side">
        <nav>
          <button class="nav-link" type="button" (click)="topicsOpen.set(!topicsOpen())">
            Topics {{ topicsOpen() ? '▾' : '▸' }}
          </button>
          @if (topicsOpen()) {
            <ul class="topic-list">
              @for (t of topics(); track t.slug) {
                <li><a [routerLink]="['/topics', t.slug]" routerLinkActive="active">{{ t.name }}</a></li>
              }
            </ul>
          }
          <a class="nav-link" routerLink="/people" routerLinkActive="active">Profiles</a>
        </nav>
      </aside>
      <section class="content"><router-outlet /></section>
    </div>
  `,
})
export class Shell implements OnInit {
  private api = inject(ApiService);

  open = signal(true);
  topicsOpen = signal(false);
  topics = signal<Topic[]>([]);

  async ngOnInit() {
    this.topics.set(await this.api.topics());
  }
}

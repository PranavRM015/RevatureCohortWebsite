import { Component, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { ApiService } from '../api.service';

function hsvToHex(h: number, s: number, v: number): string {
  const f = (n: number) => {
    const k = (n + h / 60) % 6;
    const value = v - v * s * Math.max(0, Math.min(k, 4 - k, 1));
    return Math.round(value * 255).toString(16).padStart(2, '0');
  };
  return ('#' + f(5) + f(3) + f(1)).toUpperCase();
}

function hexToHsv(hex: string): { h: number; s: number; v: number } {
  const r = parseInt(hex.slice(0, 2), 16) / 255;
  const g = parseInt(hex.slice(2, 4), 16) / 255;
  const b = parseInt(hex.slice(4, 6), 16) / 255;
  const max = Math.max(r, g, b);
  const min = Math.min(r, g, b);
  const d = max - min;
  let h = 0;
  if (d !== 0) {
    if (max === r) h = ((g - b) / d) % 6;
    else if (max === g) h = (b - r) / d + 2;
    else h = (r - g) / d + 4;
    h *= 60;
    if (h < 0) h += 360;
  }
  return { h, s: max === 0 ? 0 : d / max, v: max };
}

@Component({
  selector: 'app-color-picker',
  template: `
    <main class="picker">
      <h1>Choose your profile color</h1>
      <div
        class="sv"
        [style.background-color]="hueColor()"
        (pointerdown)="svDown($event)"
        (pointermove)="svMove($event)"
        (pointerup)="dragging = false"
      >
        <div class="thumb" [style.left.%]="s() * 100" [style.top.%]="(1 - v()) * 100"></div>
      </div>
      <input
        class="hue"
        type="range"
        min="0"
        max="360"
        step="1"
        [value]="h()"
        (input)="h.set(+$any($event.target).value)"
        aria-label="Hue"
      />
      <div class="hex-row">
        <span class="swatch" [style.background-color]="hex()"></span>
        <span class="hash">#</span>
        <input
          class="hex-input"
          maxlength="6"
          [value]="shown()"
          (input)="onHex($any($event.target).value)"
          (blur)="typed.set(null)"
          aria-label="Hex color"
        />
      </div>
      <button class="primary" type="button" [disabled]="saving()" (click)="select()">Select</button>
      @if (error()) {
        <p class="error">{{ error() }}</p>
      }
    </main>
  `,
})
export class ColorPicker {
  private api = inject(ApiService);
  private router = inject(Router);

  h = signal(0);
  s = signal(0);
  v = signal(1);
  typed = signal<string | null>(null);
  saving = signal(false);
  error = signal('');
  dragging = false;

  hex = computed(() => hsvToHex(this.h(), this.s(), this.v()));
  hueColor = computed(() => hsvToHex(this.h(), 1, 1));
  shown = computed(() => this.typed() ?? this.hex().slice(1));

  svDown(e: PointerEvent) {
    this.dragging = true;
    (e.currentTarget as HTMLElement).setPointerCapture(e.pointerId);
    this.svMove(e);
  }

  svMove(e: PointerEvent) {
    if (!this.dragging) return;
    const rect = (e.currentTarget as HTMLElement).getBoundingClientRect();
    const x = Math.min(Math.max(e.clientX - rect.left, 0), rect.width);
    const y = Math.min(Math.max(e.clientY - rect.top, 0), rect.height);
    this.s.set(x / rect.width);
    this.v.set(1 - y / rect.height);
    this.typed.set(null);
  }

  onHex(value: string) {
    const cleaned = value.replace(/[^0-9a-fA-F]/g, '').slice(0, 6);
    this.typed.set(cleaned);
    if (cleaned.length === 6) {
      const { h, s, v } = hexToHsv(cleaned);
      this.h.set(h);
      this.s.set(s);
      this.v.set(v);
    }
  }

  async select() {
    let color = this.hex();
    if (color === '#000000') color = '#000001';
    this.saving.set(true);
    this.error.set('');
    try {
      await this.api.updateMe({ profileColor: color });
      await this.router.navigateByUrl('/');
    } catch {
      this.error.set('Could not save your color. Please try again.');
    } finally {
      this.saving.set(false);
    }
  }
}

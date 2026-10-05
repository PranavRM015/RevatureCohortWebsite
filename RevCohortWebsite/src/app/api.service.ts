import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { Me, Profile, QuizQuestion, Topic } from './models';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private http = inject(HttpClient);
  private topicsCache?: Promise<Topic[]>;
  private quizCache?: Promise<Record<string, QuizQuestion[]>>;

  readonly me = signal<Me | null>(null);

  async loadMe(): Promise<Me | null> {
    try {
      const me = await firstValueFrom(this.http.get<Me>('/me'));
      this.me.set(me);
      return me;
    } catch {
      this.me.set(null);
      return null;
    }
  }

  async updateMe(body: { profileColor?: string; profileLink?: string }): Promise<Me> {
    const me = await firstValueFrom(this.http.put<Me>('/me', body));
    this.me.set(me);
    return me;
  }

  profiles(): Promise<Profile[]> {
    return firstValueFrom(this.http.get<Profile[]>('/profiles'));
  }

  topics(): Promise<Topic[]> {
    this.topicsCache ??= firstValueFrom(this.http.get<Topic[]>('/data/topics.json'));
    return this.topicsCache;
  }

  quiz(): Promise<Record<string, QuizQuestion[]>> {
    this.quizCache ??= firstValueFrom(this.http.get<Record<string, QuizQuestion[]>>('/data/quiz.json'));
    return this.quizCache;
  }
}

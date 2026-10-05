import { Component, inject, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { ApiService } from '../api.service';
import { formatText } from '../format';
import { QuizQuestion, Topic } from '../models';

@Component({
  selector: 'app-topic',
  template: `
    @if (topic(); as t) {
      <h1>{{ t.name }}</h1>

      @for (section of t.sections; track section.heading) {
        <h2>{{ section.heading }}</h2>
        <ul class="doc">
          @for (point of section.points; track point) {
            <li [innerHTML]="fmt(point)"></li>
          }
        </ul>
      }

      <h2 class="quiz-title">Quiz</h2>
      @if (questions().length === 0) {
        <p>No multiple choice questions for this topic yet.</p>
      } @else {
        <p class="hint">Answer as many as you like. Only the questions you answer are graded.</p>
        @for (q of questions(); track $index; let i = $index) {
          <fieldset class="question">
            <legend>{{ i + 1 }}. {{ q.question }}</legend>
            @for (a of q.answers; track $index) {
              <label
                class="option"
                [class.correct]="result() && a === q.correctAnswer"
                [class.wrong]="result() && picks()[i] === a && a !== q.correctAnswer"
              >
                <input
                  type="radio"
                  [name]="'q' + i"
                  [checked]="picks()[i] === a"
                  [disabled]="!!result()"
                  (change)="pick(i, a)"
                />
                <span>{{ a }}</span>
              </label>
            }
          </fieldset>
        }

        @if (result(); as r) {
          <div class="result">
            @if (r.answered === 0) {
              <p>You did not answer any questions, so there is nothing to grade.</p>
            } @else {
              <p>
                Score: <strong>{{ r.correct }} / {{ r.answered }}</strong> ({{ r.percent }}%)
              </p>
              @if (r.total > r.answered) {
                <p>{{ r.total - r.answered }} unanswered question(s) were not graded.</p>
              }
            }
            <button class="primary" type="button" (click)="retry()">Try again</button>
          </div>
        } @else {
          <button class="primary" type="button" (click)="submit()">Submit</button>
        }
      }
    } @else if (loaded()) {
      <h1>Topic not found</h1>
    }
  `,
})
export class TopicPage {
  private api = inject(ApiService);
  private route = inject(ActivatedRoute);

  topic = signal<Topic | null>(null);
  questions = signal<QuizQuestion[]>([]);
  picks = signal<Record<number, string>>({});
  result = signal<{ answered: number; correct: number; total: number; percent: number } | null>(null);
  loaded = signal(false);

  constructor() {
    this.route.paramMap.subscribe((params) => this.load(params.get('slug') ?? ''));
  }

  fmt(text: string): string {
    return formatText(text);
  }

  async load(slug: string) {
    this.loaded.set(false);
    this.retry();
    const topics = await this.api.topics();
    const topic = topics.find((t) => t.slug === slug) ?? null;
    this.topic.set(topic);
    if (topic) {
      const quiz = await this.api.quiz();
      this.questions.set((quiz[topic.name] ?? []).filter((q) => q.type === 'multiple-choice'));
    } else {
      this.questions.set([]);
    }
    this.loaded.set(true);
  }

  pick(index: number, answer: string) {
    this.picks.update((p) => ({ ...p, [index]: answer }));
  }

  submit() {
    const picks = this.picks();
    const qs = this.questions();
    const answeredIndexes = Object.keys(picks).map(Number);
    const correct = answeredIndexes.filter((i) => picks[i] === qs[i].correctAnswer).length;
    const answered = answeredIndexes.length;
    this.result.set({
      answered,
      correct,
      total: qs.length,
      percent: answered === 0 ? 0 : Math.round((correct / answered) * 100),
    });
  }

  retry() {
    this.picks.set({});
    this.result.set(null);
  }
}

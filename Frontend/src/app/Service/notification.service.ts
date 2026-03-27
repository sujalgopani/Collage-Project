import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

export type NotificationType = 'success' | 'error' | 'warning' | 'info';

export interface AppNotification {
  id: number;
  message: string;
  type: NotificationType;
  durationMs: number;
}

@Injectable({
  providedIn: 'root',
})
export class NotificationService {
  private readonly notificationsSubject = new BehaviorSubject<AppNotification[]>([]);
  readonly notifications$ = this.notificationsSubject.asObservable();
  private nextId = 1;

  show(message: string, type: NotificationType = 'info', durationMs = 3500): void {
    const trimmedMessage = (message || '').trim();
    if (!trimmedMessage) {
      return;
    }

    const notification: AppNotification = {
      id: this.nextId++,
      message: trimmedMessage,
      type,
      durationMs,
    };

    const current = this.notificationsSubject.getValue();
    this.notificationsSubject.next([...current, notification]);

    if (durationMs > 0) {
      setTimeout(() => this.dismiss(notification.id), durationMs);
    }
  }

  success(message: string, durationMs = 3500): void {
    this.show(message, 'success', durationMs);
  }

  error(message: string, durationMs = 4500): void {
    this.show(message, 'error', durationMs);
  }

  warning(message: string, durationMs = 4500): void {
    this.show(message, 'warning', durationMs);
  }

  info(message: string, durationMs = 3500): void {
    this.show(message, 'info', durationMs);
  }

  dismiss(id: number): void {
    const current = this.notificationsSubject.getValue();
    this.notificationsSubject.next(current.filter(n => n.id !== id));
  }

  clear(): void {
    this.notificationsSubject.next([]);
  }
}

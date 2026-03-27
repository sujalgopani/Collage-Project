import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { NotificationService } from '../Service/notification.service';

@Component({
  selector: 'app-notification-container',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './notification-container.html',
  styleUrl: './notification-container.css',
})
export class NotificationContainer {
  readonly notifications$;

  constructor(private notificationService: NotificationService) {
    this.notifications$ = this.notificationService.notifications$;
  }

  dismiss(id: number): void {
    this.notificationService.dismiss(id);
  }
}

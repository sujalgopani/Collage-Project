import { ChangeDetectorRef, Component } from '@angular/core';
import { Teacherservice } from '../Service/TeacherService/teacherservice';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { NotificationService } from '../Service/notification.service';

@Component({
  selector: 'app-teachergetsuggestion',
  imports: [FormsModule, CommonModule],
  templateUrl: './teachergetsuggestion.html',
  styleUrl: './teachergetsuggestion.css',
})
export class Teachergetsuggestion {
  suggestions: any[] = [];
  isLoading = false;

  constructor(
    private service: Teacherservice,
    private cdr: ChangeDetectorRef,
    private notification: NotificationService,
  ) {}

  ngOnInit(): void {
    this.loadSuggestions();
  }

  loadSuggestions() {
    this.isLoading = true;

    this.service.getstudentsuggestionforteacher().subscribe({
      next: (res: any) => {
        this.suggestions = (Array.isArray(res) ? res : []).map((item: any) => ({
          ...item,
          reply: (item?.reply ?? '').toString(),
        }));
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error(err);
        this.notification.error(
          err?.error?.message ||
            err?.error ||
            'Unable to load suggestions right now. Please try again.',
        );
        this.isLoading = false;
        this.cdr.detectChanges();
      },
    });
  }
  SendReply(s: any) {
    const reply = (s.reply || '').trim();
    s.replyError = '';

    if (!reply) {
      s.replyError = 'Reply is required.';
      return;
    }

    if (reply.length < 3) {
      s.replyError = 'Reply must be at least 3 characters.';
      return;
    }

    if (reply.length > 500) {
      s.replyError = 'Reply cannot exceed 500 characters.';
      return;
    }

    const payload = {
      id: s.id,
      reply,
    };

    s.isReplying = true;
    this.service.replysuggestion(payload).subscribe({
      next: () => {
        this.notification.success('Reply sent successfully.');

        // optional: clear input
        s.reply = '';
        s.replyError = '';
        s.isReplying = false;

        // optional: reload list
        this.loadSuggestions();
      },
      error: (err) => {
        console.error(err);
        s.isReplying = false;
        s.replyError =
          err?.error?.message ||
          err?.error ||
          'Unable to send reply right now. Please try again.';
        this.notification.error(s.replyError);
      },
    });
  }

  deleteSuggestion(id: number) {
    if (!confirm('Are you sure you want to delete this suggestion?')) {
      return;
    }

    const selected = this.suggestions.find((x) => x.id === id);
    if (selected) {
      selected.isDeleting = true;
    }

    this.service.deleteSuggestion(id).subscribe({
      next: () => {
        this.notification.success('Suggestion deleted successfully.');

        // reload data
        this.loadSuggestions();
      },
      error: (err) => {
        console.error(err);
        if (selected) {
          selected.isDeleting = false;
        }
        this.notification.error(
          err?.error?.message ||
            err?.error ||
            'Unable to delete suggestion right now. Please try again.',
        );
      },
    });
  }
  
}

import { CommonModule, isPlatformBrowser } from '@angular/common';
import {
  ChangeDetectorRef,
  Component,
  ElementRef,
  Inject,
  OnInit,
  PLATFORM_ID,
  ViewChild,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { finalize } from 'rxjs';
import {
  ChatbotAskResponse,
  ChatbotHistoryItem,
  ChatbotMessageRole,
  ChatbotService,
} from '../Service/chatbot/chatbot.service';

interface UiMessage {
  role: ChatbotMessageRole;
  content: string;
  createdAt: Date;
  formattedContent?: string;
}

@Component({
  selector: 'app-chatbot-widget',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './chatbot-widget.html',
  styleUrl: './chatbot-widget.css',
})
export class ChatbotWidget implements OnInit {
  @ViewChild('messagesContainer') private messagesContainer?: ElementRef<HTMLDivElement>;

  isOpen = false;
  isSending = false;
  draft = '';
  role = 'Student';
  messages: UiMessage[] = [];
  quickActions: string[] = [];

  private readonly historyLimit = 6;

  constructor(
    @Inject(PLATFORM_ID) private readonly platformId: object,
    private readonly chatbotService: ChatbotService,
    private readonly cdr: ChangeDetectorRef,
    private readonly router: Router,
  ) {}

  ngOnInit(): void {
    if (!isPlatformBrowser(this.platformId)) {
      return;
    }

    this.role = this.resolveRole();
    this.quickActions = this.getQuickActions(this.role);

    this.messages = [
      {
        role: 'assistant',
        content: this.getWelcomeMessage(this.role),
        formattedContent: this.formatAssistantMessage(this.getWelcomeMessage(this.role)),
        createdAt: new Date(),
      },
    ];
  }

  toggleOpen(): void {
    this.isOpen = !this.isOpen;
    if (this.isOpen) {
      this.scrollToBottom();
    }
  }

  closeChat(): void {
    this.isOpen = false;
  }

  sendQuickAction(question: string): void {
    if (this.isSending) {
      return;
    }

    this.draft = question;
    this.sendMessage();
  }

  sendMessage(): void {
    if (this.isSending) {
      return;
    }

    const message = this.draft.trim();
    if (!message) {
      return;
    }

    this.draft = '';
    this.pushMessage('user', message);
    this.isSending = true;

    this.chatbotService
      .ask(message, this.buildHistory())
      .pipe(
        finalize(() => {
          this.isSending = false;
          this.scrollToBottom();
          this.cdr.detectChanges();
        }),
      )
      .subscribe({
        next: (response: ChatbotAskResponse) => {
          const answer = (response.answer || '').trim();
          this.pushMessage('assistant', answer || 'I could not create a response. Please try again.');
          this.cdr.detectChanges();
        },
        error: (err: unknown) => {
          console.error('CHATBOT ERROR:', err);
          this.pushMessage('assistant', this.extractChatbotErrorMessage(err));
          this.cdr.detectChanges();
        },
      });
  }

  onInputKeydown(event: KeyboardEvent): void {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.sendMessage();
    }
  }

  onMessageClick(event: MouseEvent): void {
    const target = event.target as HTMLElement | null;
    const routeLink = target?.closest('a.app-route-link') as HTMLAnchorElement | null;

    if (!routeLink) {
      return;
    }

    const route = routeLink.getAttribute('href') ?? '';
    if (!this.isRoutePath(route)) {
      return;
    }

    event.preventDefault();
    this.closeChat();

    this.router.navigateByUrl(route).catch((error: unknown) => {
      console.error('Chat route navigation failed:', error);
    });
  }

  trackByIndex(index: number): number {
    return index;
  }

  private pushMessage(role: ChatbotMessageRole, content: string): void {
    const trimmed = content.trim();

    this.messages = [
      ...this.messages,
      {
        role,
        content: trimmed,
        createdAt: new Date(),
        formattedContent: role === 'assistant' ? this.formatAssistantMessage(trimmed) : undefined,
      },
    ];

    this.scrollToBottom();
  }

  private buildHistory(): ChatbotHistoryItem[] {
    return this.messages.slice(-this.historyLimit).map((item) => ({
      role: item.role,
      content: item.content,
    }));
  }

  private resolveRole(): string {
    const storedRole = sessionStorage.getItem('role') ?? localStorage.getItem('role');

    if (storedRole === 'Admin' || storedRole === 'Teacher' || storedRole === 'Student') {
      return storedRole;
    }

    return 'Student';
  }

  private getWelcomeMessage(role: string): string {
    if (role === 'Teacher') {
      return 'Hello! I can help with teacher panel.';
    }

    if (role === 'Admin') {
      return 'Hello! I can help with admin panel.';
    }

    return 'Hello! I can help with student panel.';
  }

  private getQuickActions(role: string): string[] {
    if (role === 'Teacher') {
      return ['How do I create a course?', 'Show upcoming exams', 'Check student suggestions'];
    }

    if (role === 'Admin') {
      return ['Manage courses', 'Check payments', 'Schedule live class'];
    }

    return ['How to enroll in course?', 'Where to attempt exams?', 'How to check result?'];
  }

  private scrollToBottom(): void {
    if (!this.isOpen || !this.messagesContainer) {
      return;
    }

    setTimeout(() => {
      const element = this.messagesContainer?.nativeElement;
      if (element) {
        element.scrollTop = element.scrollHeight;
      }
    }, 0);
  }

  private formatAssistantMessage(content: string): string {
    const normalized = content.replace(/\r\n?/g, '\n').trim();
    if (!normalized) {
      return '<p>I could not generate a response.</p>';
    }

    const blocks: string[] = [];
    const codeFenceRegex = /```([a-zA-Z0-9_-]+)?\n?([\s\S]*?)```/g;
    let lastIndex = 0;
    let match: RegExpExecArray | null;

    while ((match = codeFenceRegex.exec(normalized)) !== null) {
      const textBefore = normalized.slice(lastIndex, match.index);
      blocks.push(this.formatTextBlock(textBefore));

      const codeContent = this.escapeHtml((match[2] ?? '').trim());
      if (codeContent) {
        blocks.push(`<pre><code>${codeContent}</code></pre>`);
      }

      lastIndex = match.index + match[0].length;
    }

    blocks.push(this.formatTextBlock(normalized.slice(lastIndex)));
    return blocks.join('');
  }

  private formatTextBlock(block: string): string {
    const normalized = block.replace(/\r\n?/g, '\n').trim();
    if (!normalized) {
      return '';
    }

    const lines = normalized.split('\n');
    const html: string[] = [];
    const paragraphLines: string[] = [];
    let listType: 'ul' | 'ol' | null = null;

    const flushParagraph = () => {
      if (paragraphLines.length === 0) {
        return;
      }

      html.push(`<p>${paragraphLines.join('<br>')}</p>`);
      paragraphLines.length = 0;
    };

    const closeList = () => {
      if (!listType) {
        return;
      }

      html.push(`</${listType}>`);
      listType = null;
    };

    const openList = (nextListType: 'ul' | 'ol') => {
      if (listType === nextListType) {
        return;
      }

      closeList();
      html.push(`<${nextListType}>`);
      listType = nextListType;
    };

    for (const rawLine of lines) {
      const line = rawLine.trim();
      if (!line) {
        flushParagraph();
        closeList();
        continue;
      }

      const unorderedMatch = line.match(/^[-*\u2022]\s+(.+)/);
      if (unorderedMatch) {
        flushParagraph();
        openList('ul');
        html.push(`<li>${this.formatInline(unorderedMatch[1])}</li>`);
        continue;
      }

      const orderedMatch = line.match(/^\d+\.\s+(.+)/);
      if (orderedMatch) {
        flushParagraph();
        openList('ol');
        html.push(`<li>${this.formatInline(orderedMatch[1])}</li>`);
        continue;
      }

      closeList();
      paragraphLines.push(this.formatInline(line));
    }

    flushParagraph();
    closeList();
    return html.join('');
  }

  private formatInline(text: string): string {
    const segments = text.split(/(`[^`]+`)/g);

    return segments
      .map((segment) => {
        if (segment.length >= 3 && segment.startsWith('`') && segment.endsWith('`')) {
          const rawCode = segment.slice(1, -1).trim();
          const escapedCode = this.escapeHtml(rawCode);

          if (this.isRoutePath(rawCode)) {
            return `<a class="app-route-link route-chip" href="${rawCode}">${escapedCode}</a>`;
          }

          return `<code>${escapedCode}</code>`;
        }

        const escaped = this.escapeHtml(segment);
        const withBold = escaped.replace(/\*\*(.+?)\*\*/g, '<strong>$1</strong>');
        return this.linkifyRoutes(withBold);
      })
      .join('');
  }

  private linkifyRoutes(text: string): string {
    const routeRegex =
      /(^|[\s(>])((?:\/(?:admin|teacher|student)-dashboard(?:\/[a-zA-Z0-9_-]+)*))(?=$|[\s),.;!?])/g;

    return text.replace(
      routeRegex,
      (_match: string, prefix: string, route: string) =>
        `${prefix}<a class="app-route-link" href="${route}">${route}</a>`,
    );
  }

  private isRoutePath(value: string): boolean {
    const routePathRegex = /^\/(?:admin|teacher|student)-dashboard(?:\/[a-zA-Z0-9_-]+)*$/;
    return routePathRegex.test(value);
  }

  private escapeHtml(value: string): string {
    return value
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;')
      .replace(/'/g, '&#39;');
  }

  private extractChatbotErrorMessage(error: unknown): string {
    const err = error as any;

    const responseAnswer = err?.error?.answer;
    if (typeof responseAnswer === 'string' && responseAnswer.trim()) {
      return responseAnswer.trim();
    }

    const responseMessage = err?.error?.message;
    if (typeof responseMessage === 'string' && responseMessage.trim()) {
      return responseMessage.trim();
    }

    const plainError = err?.error;
    if (typeof plainError === 'string' && plainError.trim()) {
      return plainError.trim();
    }

    if (err?.status === 400) {
      return 'Please type at least 2 characters before sending your question.';
    }

    if (err?.status === 401 || err?.status === 403) {
      return 'Your session expired. Please login again and retry.';
    }

    return 'Assistant is temporarily unavailable. Please try again.';
  }
}


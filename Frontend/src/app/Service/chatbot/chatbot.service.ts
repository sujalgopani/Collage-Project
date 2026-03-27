import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

export type ChatbotMessageRole = 'user' | 'assistant';

export interface ChatbotHistoryItem {
  role: ChatbotMessageRole;
  content: string;
}

export interface ChatbotAskResponse {
  role: string;
  answer: string;
  usedFallback: boolean;
  generatedAtUtc: string;
}

@Injectable({
  providedIn: 'root',
})
export class ChatbotService {
  private readonly http = inject(HttpClient);
  private readonly askUrl = 'https://localhost:44385/api/chatbot/ask';

  ask(message: string, history: ChatbotHistoryItem[]): Observable<ChatbotAskResponse> {
    return this.http.post<ChatbotAskResponse>(this.askUrl, {
      message,
      history,
    });
  }
}

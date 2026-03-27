import { Inject, Injectable, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { BehaviorSubject, Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class SidebarService {
  private readonly sidebarOpen: BehaviorSubject<boolean>;
  readonly sidebarOpen$: Observable<boolean>;
  private readonly desktopBreakpoint = 992;

  constructor(@Inject(PLATFORM_ID) private platformId: Object) {
    const isDesktop = isPlatformBrowser(this.platformId)
      ? window.innerWidth >= this.desktopBreakpoint
      : true;

    this.sidebarOpen = new BehaviorSubject<boolean>(isDesktop);
    this.sidebarOpen$ = this.sidebarOpen.asObservable();
  }

  toggleSidebar() {
    this.setSidebar(!this.sidebarOpen.value);
  }

  setSidebar(isOpen: boolean) {
    this.sidebarOpen.next(isOpen);
  }

  syncWithViewport() {
    if (!isPlatformBrowser(this.platformId)) {
      return;
    }

    this.setSidebar(window.innerWidth >= this.desktopBreakpoint);
  }
}

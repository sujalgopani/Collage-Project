import { isPlatformBrowser } from '@angular/common';
import { HttpInterceptorFn } from '@angular/common/http';
import { inject, PLATFORM_ID } from '@angular/core';

export const addtokenheaderInterceptor: HttpInterceptorFn = (req, next) => {
  const platformId = inject(PLATFORM_ID);

  let token: string | null = null;

  if (isPlatformBrowser(platformId)) {
    token = sessionStorage.getItem('token');
  }

  const setHeaders: Record<string, string> = {};
  if (token) {
    setHeaders['Authorization'] = `Bearer ${token}`;
  }

  const isGetRequest = req.method.toUpperCase() === 'GET';
  if (isGetRequest) {
    req = req.clone({
      setHeaders,
      setParams: {
        _ts: Date.now().toString(),
      },
    });
  } else if (Object.keys(setHeaders).length > 0) {
    req = req.clone({ setHeaders });
  }

  return next(req);
};

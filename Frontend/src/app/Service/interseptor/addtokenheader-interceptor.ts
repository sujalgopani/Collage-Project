import { HttpInterceptorFn } from '@angular/common/http';

export const addtokenheaderInterceptor: HttpInterceptorFn = (req, next) => {

  const token = localStorage.getItem('token');

  if (token) {
    req = req.clone({
      setHeaders: {
        Authorization: `Bearer ${token}`
      }
    });
  }

  return next(req); // ✅ NEVER use next.handle()
};

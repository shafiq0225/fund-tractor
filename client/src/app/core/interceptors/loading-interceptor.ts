import { HttpEvent, HttpHandler, HttpInterceptor, HttpInterceptorFn, HttpRequest } from "@angular/common/http";
import { BusyService } from "../services/busy.service";
import { finalize, Observable } from "rxjs";
import { inject } from "@angular/core";

export const LoadingInterceptor: HttpInterceptorFn = (req, next) => {
  const busyService = inject(BusyService);
  busyService.busy();

  return next(req).pipe(
    finalize(() => busyService.idle())
  );
};

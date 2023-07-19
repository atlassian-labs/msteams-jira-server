import { Injectable, Injector } from '@angular/core';
import {
    HttpEvent,
    HttpErrorResponse,
    HttpInterceptor,
    HttpHandler,
    HttpHeaders,
    HttpRequest
} from '@angular/common/http';
import { throwError, empty, Observable } from 'rxjs';
import { tap } from 'rxjs/operators';

import { AdalService } from '@core/services';
import { logger } from '@core/services/logger.service';

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
    constructor(private readonly injector: Injector) { }

    public intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
        const adalService = this.injector.get(AdalService);
        let token: string;

        if (adalService.settings.clientId) {
            token = adalService.getCachedToken();
        }

        let headers = new HttpHeaders();

        if (token) {
            headers = headers.set('Authorization', `Bearer ${token}`);
        }

        const request = req.clone({ headers });
        return next.handle(request)
            .pipe(
                tap(
                    null,
                    error => {
                        if (error instanceof HttpErrorResponse) {
                            if (error.status === 401) {
                                return empty();
                            }

                            logger(error);
                            return throwError(error);
                        }
                    }
                )
            );
    }
}

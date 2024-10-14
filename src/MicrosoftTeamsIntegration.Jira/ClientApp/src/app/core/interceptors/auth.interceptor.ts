import { Injectable, Injector } from '@angular/core';
import { HttpEvent, HttpErrorResponse, HttpInterceptor, HttpHandler, HttpHeaders, HttpRequest } from '@angular/common/http';
import {throwError, Observable, from, EMPTY, catchError, lastValueFrom} from 'rxjs';

import {AuthService, ErrorService} from '@core/services';
import { logger } from '@core/services/logger.service';
import {StatusCode} from '@core/enums';

export const ALLOW_ANONYMOUS_ENDPOINTS = ['/api/app-settings'];

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
    constructor(private readonly injector: Injector) {
    }

    public intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
        return from(this.handle(req, next));
    }

    async handle(req: HttpRequest<any>, next: HttpHandler) {
        const authService = this.injector.get(AuthService);
        const errorService = this.injector.get(ErrorService);

        let headers = new HttpHeaders();

        if(!ALLOW_ANONYMOUS_ENDPOINTS.includes(req.url)) {
            const token = await authService.getToken();

            if (token) {
                headers = headers.set('Authorization', `Bearer ${token}`);
            }
        }

        const request = req.clone({headers});
        return lastValueFrom(next.handle(request)
            .pipe(
                catchError((error: HttpErrorResponse) => {
                    if (error.status === StatusCode.Unauthorized) {
                        errorService.goToLoginWithStatusCode(StatusCode.Unauthorized);
                        return EMPTY;
                    }

                    logger(error);
                    return throwError(() => error);
                })
            ));
    }
}

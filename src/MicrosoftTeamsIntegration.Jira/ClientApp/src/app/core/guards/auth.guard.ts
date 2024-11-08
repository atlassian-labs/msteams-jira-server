import { Injectable } from '@angular/core';
import {
    Router,
    ActivatedRouteSnapshot,
} from '@angular/router';

import { AuthService } from '@core/services';
import { logger } from '@core/services/logger.service';

import { StatusCode } from '@core/enums';

@Injectable()
export class AuthGuard {
    constructor(
        private readonly router: Router,
        private readonly authService: AuthService,
    ) { }

    public async canActivate(route: ActivatedRouteSnapshot): Promise<boolean> {
        try {
            const isAuthenticated = Boolean(await this.authService.getToken());

            logger(`AuthGuard::canActivate User is authenticated: ${isAuthenticated}`);

            return isAuthenticated;
        } catch (error) {
            await this.router.navigate(['/login', { ...route.params, status: StatusCode.Unauthorized }]);
            return false;
        }
    }
}

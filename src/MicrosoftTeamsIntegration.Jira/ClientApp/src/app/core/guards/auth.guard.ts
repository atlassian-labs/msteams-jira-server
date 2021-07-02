import { Injectable } from '@angular/core';
import {
    Router,
    CanActivate,
    ActivatedRouteSnapshot,
} from '@angular/router';

import { AuthService, AdalService } from '@core/services';
import { logger } from '@core/services/logger.service';

import { StatusCode } from '@core/enums';

@Injectable()
export class AuthGuard implements CanActivate {
    constructor(
        private readonly router: Router,
        private readonly authService: AuthService,
        private readonly adalService: AdalService
    ) { }

    public async canActivate(route: ActivatedRouteSnapshot): Promise<boolean> {
        const isAuthenticated = this.authService.isAuthenticated;

        logger(`AuthGuard::canActivate User is authenticated: ${isAuthenticated}`);

        if (isAuthenticated) {
            return true;
        }

        try {
            const token = await this.adalService.renewToken();
            return Boolean(token);
        } catch (error) {
            await this.router.navigate(['/login', { ...route.params, status: StatusCode.Unauthorized }]);
            return false;
        }
    }
}

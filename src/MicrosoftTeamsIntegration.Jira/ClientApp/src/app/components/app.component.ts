// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Component, OnInit, OnDestroy } from '@angular/core';
import {
    Router,
    NavigationStart,
    NavigationEnd,
    NavigationCancel,
    NavigationError
} from '@angular/router';

import { Subscription } from 'rxjs';

import { LoadingIndicatorService } from '@shared/services/loading-indicator.service';
import { RoutingState, UtilService } from '@core/services';
import * as microsoftTeams from '@microsoft/teams-js';
import {logger} from '@core/services/logger.service';

@Component({
    selector: 'app-root',
    templateUrl: './app.component.html',
    styleUrls: ['./app.component.scss']
})
export class AppComponent implements OnInit, OnDestroy {
    private subscription: Subscription | undefined;

    constructor(
        private readonly router: Router,
        private readonly loadingIndicatorService: LoadingIndicatorService,
        private readonly routingState: RoutingState,
        private readonly utilService: UtilService
    ) { }

    public async ngOnInit(): Promise<void> {
        this.routingState.loadRouting();

        await this.initMSTeams();

        if (await this.utilService.isMobile()) {
            document.body.classList.add('mobile');
        } else {
            document.body.classList.remove('mobile');
        }

        this.subscription = this.router.events.pipe().subscribe((event) => {
            if (event instanceof NavigationStart) {
                this.loadingIndicatorService.show();
                return;
            }

            if (event instanceof NavigationEnd
                || event instanceof NavigationCancel
                || event instanceof NavigationError
            ) {
                this.loadingIndicatorService.hide();
            }
        });
    }

    private async initMSTeams(): Promise<void> {
        await microsoftTeams.app.initialize();

        //  add pop up class to body
        if (window.location === window.parent.location) {
            document.body.classList.add('shownAtPopup');
        }

        const colors = {
            dark: { backgroundColor: '#2D2C2C', color: '#FFFFFF' },
            contrast: { backgroundColor: '#000000', color: '#FFFFFF' },
            default: { backgroundColor: '#FFFFFF', color: '#2B2B30' }
        } as any;

        const addThemeClassToBody = (theme: string): void => {
            const { body } = document;
            // Multiple arguments for add() & remove() are not supported in IE11.
            body.classList.remove('transparent');
            body.classList.remove('default');
            body.classList.remove('dark');
            body.classList.remove('contrast');
            body.classList.add(theme);
        };

        const applyTheme = (themeColors: { backgroundColor: string; color: string }, theme: string): void => {
            const oldChild = document.getElementById('data-theme-' + theme);

            const stylesheet = document.createElement('style');
            stylesheet.type = 'text/css';
            stylesheet.innerHTML = '\nbody { background-color:'
                + themeColors.backgroundColor
                + ' !important; color:'
                + themeColors.color
                + '; }\n';

            stylesheet.id = 'data-theme-' + theme;

            if (oldChild) {
                document.head.removeChild(oldChild);
            }

            document.head.appendChild(stylesheet);
        };

        const themeFromParams: string = this.utilService.getQueryParam('theme');
        if (themeFromParams) {
            const themeColors = colors[themeFromParams];
            addThemeClassToBody(themeFromParams);
            applyTheme(themeColors, themeFromParams);
        }

        const context = await microsoftTeams.app.getContext();
        if (context) {
            logger('Teams context:', context);
            localStorage.setItem('msTeamsContext', JSON.stringify({
                tid: context?.user?.tenant?.id,
                loginHint: context?.user?.loginHint,
                userObjectId: context?.user?.id,
                locale: context?.app?.locale
            }));

            const theme = context?.app?.theme as any;
            const themeColors = colors[theme];
            addThemeClassToBody(theme);
            applyTheme(themeColors, theme);
        }

        microsoftTeams.app.registerOnThemeChangeHandler(function (theme: string) {
            const themeColors = colors[theme];
            addThemeClassToBody(theme);
            applyTheme(themeColors, theme);
        });
    }

    public ngOnDestroy(): void {
        if (this.subscription) {
            this.subscription.unsubscribe();
        }
    }
}

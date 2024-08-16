import { Injectable } from '@angular/core';
import { Router, NavigationStart, RouterEvent } from '@angular/router';
import { filter } from 'rxjs/operators';

@Injectable({ providedIn: 'root' })
export class RoutingState {

    get previousUrl(): string {
        return this._previousUrl || '/issues';
    }
    private _previousUrl: string | any;

    private currentUrl: string | any;

    constructor(
        private router: Router
    ) { }

    public loadRouting(): void {
        this.currentUrl = this.extractCurrentRouteFromUrl(this.router.url);

        this.router.events
            .pipe(
                filter(event => event instanceof NavigationStart)
            )
            .subscribe((event: any) => {
                const url = this.extractCurrentRouteFromUrl(event.url);

                if (this.currentUrl !== url) {
                    this._previousUrl = this.currentUrl;
                    this.currentUrl = url;
                }
            });
    }

    private extractCurrentRouteFromUrl(url: string): string {
        if (!url) {
            return '';
        }

        // eslint-disable-next-line max-len
        /* After https://dev.azure.com/msteams-atlassian/Jira%20Cloud/_git/JiraCloud/commit/014046116467242bb52e956381e3ce886483f0a8?refName=refs%2Fheads%2Fmaster&_a=compare&path=%2Fmanifests%2Fproduction%2Fmanifest.json
        * config url params divided by ';' instead of '?', so when we try to extract component path from
        * '/config?v=2.0' and use it in router.navigate(['/config?v=2.0']) we will get the following url 'config%3Fv%3D2.0'
        * and 'Cannot match any routes. URL Segment' exception. So to avoid this and keep legacy code we need to check if url contains '?'
        */
        return url.indexOf('?') > 0 ?
            url.split('?')[0] :
            url.split(';')[0];
    }
}

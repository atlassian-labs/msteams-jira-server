// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router, ParamMap } from '@angular/router';
import { ApiService, AuthService } from '@core/services';
import { PageName } from '@core/enums';

@Component({
    selector: 'app-go-to-website',
    template: ``
})
export class GoToWebsiteComponent implements OnInit {
    private page: string;
    private jiraInstanceUrl: string;

    constructor(
        private apiService: ApiService,
        private authService: AuthService,
        private router: Router,
        private route: ActivatedRoute
    ) { }

    public async ngOnInit(): Promise<void> {
        this.route.paramMap.subscribe((params: ParamMap) => {
            this.page = params.get('page');
        });

        if (!this.authService.isAuthenticated) {
            window.localStorage.setItem('redirectUri', `https://${window.location.host}/#/go-to-website;page=${this.page}`);
            await this.authService.authenticate('./login.html', true);
        }
        
        const { jiraUrl } = await this.apiService.getJiraUrlForPersonalScope();
        const { jiraServerInstanceUrl } = await this.apiService.getCurrentUserData(jiraUrl);
        this.jiraInstanceUrl = jiraServerInstanceUrl;

        const url = this.buildRedirectUrl(this.page);

        if (url) {
            window.location.replace(url);
        } else {
            const message = "You can't perform this action";
            this.router.navigate(['/error'], { queryParams: { message } });
        }
    }

    private buildRedirectUrl(page: string): string {
        if (!this.jiraInstanceUrl) {
            return null;
        }

        let url = this.jiraInstanceUrl;

        switch(page) { 
            case PageName.Filters: {
                url += '/secure/ManageFilters.jspa?filterView=my';
                break;
            }
            case PageName.IssuesAssigned: {
                url += `/issues/?jql=${encodeURIComponent('assignee = currentUser() order by updated desc')}`;
                break;
            }
            case PageName.IssuesReported: {
                url += `/issues/?jql=${encodeURIComponent('reporter = currentUser() order by updated desc')}`;
                break;
            } 
            case PageName.IssuesWatched: {
                url += `/issues/?jql=${encodeURIComponent('assignee=currentUser() or watcher=currentUser() order by updated desc')}`;
                break;
            } 
            default: {
                break;
            }
        }

        return url;
    }
}

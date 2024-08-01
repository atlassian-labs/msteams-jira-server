// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import {inject, NgModule} from '@angular/core';
import {Routes, RouterModule, ActivatedRouteSnapshot} from '@angular/router';

import {
    LoginComponent, StaticTabComponent,
    IssuesComponent, ErrorComponent, FavoriteFiltersEmptyComponent,
    SettingsComponent, SettingsFiltersComponent,
    ConnectJiraComponent,
    CreateIssueDialogComponent, EditIssueDialogComponent,
    CreateCommentDialogComponent, GoToWebsiteComponent,
    CommentIssueDialogComponent
} from '@app/components';

import { AuthGuard } from '@core/guards/auth.guard';
import { SignoutDialogComponent } from '@app/components';

export const routes: Routes = [
    { path: 'login', component: LoginComponent },
    { path: 'error', component: ErrorComponent },
    {
        path: 'config',
        component: ConnectJiraComponent,
        canActivate: [(route: ActivatedRouteSnapshot) => inject(AuthGuard).canActivate(route)]
    },
    {
        path: 'issues/create',
        component: CreateIssueDialogComponent,
        canActivate: [(route: ActivatedRouteSnapshot) => inject(AuthGuard).canActivate(route)]
    },
    {
        path: 'issues/edit',
        component: EditIssueDialogComponent,
        canActivate: [(route: ActivatedRouteSnapshot) => inject(AuthGuard).canActivate(route)]
    },
    {
        path: 'issues',
        component: IssuesComponent,
        canActivate: [(route: ActivatedRouteSnapshot) => inject(AuthGuard).canActivate(route)]
    },
    {
        path: 'static-tab',
        component: StaticTabComponent
    },
    {
        path: 'favorite-filters-empty',
        component: FavoriteFiltersEmptyComponent,
        canActivate: [(route: ActivatedRouteSnapshot) => inject(AuthGuard).canActivate(route)]
    },
    {
        path: 'settings',
        component: SettingsComponent,
        canActivate: [(route: ActivatedRouteSnapshot) => inject(AuthGuard).canActivate(route)],
        children: [
            {
                path: '',
                component: SettingsFiltersComponent,
                canActivate: [(route: ActivatedRouteSnapshot) => inject(AuthGuard).canActivate(route)],
                outlet: 'settingsOutlet',
            }
        ]
    },
    {
        path: 'settings/signout-dialog',
        component: SignoutDialogComponent,
        canActivate: [(route: ActivatedRouteSnapshot) => inject(AuthGuard).canActivate(route)]
    },
    {
        path: 'connect-jira-server',
        component: ConnectJiraComponent
    },
    {
        path: 'issues/createComment',
        component: CreateCommentDialogComponent,
        canActivate: [(route: ActivatedRouteSnapshot) => inject(AuthGuard).canActivate(route)]
    },
    {
        path: 'issues/commentIssue',
        component: CommentIssueDialogComponent,
        canActivate: [(route: ActivatedRouteSnapshot) => inject(AuthGuard).canActivate(route)]
    },
    {
        path: 'go-to-website',
        component: GoToWebsiteComponent
    }
];

@NgModule({
    imports: [RouterModule.forRoot(routes, { useHash: true })],
    exports: [RouterModule]
})
export class RoutingModule { }

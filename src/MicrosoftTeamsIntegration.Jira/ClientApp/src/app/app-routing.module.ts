// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';

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
import { SignoutDialogComponent } from './components/settings/signout-dialog/signout-dialog.component';

export const routes: Routes = [
    { path: 'login', component: LoginComponent },
    { path: 'error', component: ErrorComponent },
    {
        path: 'config',
        component: ConnectJiraComponent,
        canActivate: [AuthGuard]
    },
    {
        path: 'issues/create',
        component: CreateIssueDialogComponent,
        canActivate: [AuthGuard]
    },
    {
        path: 'issues/edit',
        component: EditIssueDialogComponent,
        canActivate: [AuthGuard]
    },
    {
        path: 'issues',
        component: IssuesComponent,
        canActivate: [AuthGuard]
    },
    {
        path: 'static-tab',
        component: StaticTabComponent
    },
    {
        path: 'favorite-filters-empty',
        component: FavoriteFiltersEmptyComponent,
        canActivate: [AuthGuard]
    },
    {
        path: 'settings',
        component: SettingsComponent,
        canActivate: [AuthGuard],
        children: [
            {
                path: '',
                component: SettingsFiltersComponent,
                canActivate: [AuthGuard],
                outlet: 'settingsOutlet',
            }
        ]
    },
    {
        path: 'settings/signout-dialog',
        component: SignoutDialogComponent,
        canActivate: [AuthGuard]
    },
    {
        path: 'connect-jira-server',
        component: ConnectJiraComponent
    },
    {
        path: 'issues/createComment',
        component: CreateCommentDialogComponent,
        canActivate: [AuthGuard]
    },
    {
        path: 'issues/commentIssue',
        component: CommentIssueDialogComponent,
        canActivate: [AuthGuard]
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

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Injectable } from '@angular/core';
import * as microsoftTeams from '@microsoft/teams-js';

@Injectable({
    providedIn: 'root'
})
export class TeamsService {

    // App module methods
    async initialize(): Promise<void> {
        return await microsoftTeams.app.initialize();
    }

    async getContext(): Promise<microsoftTeams.app.Context> {
        return await microsoftTeams.app.getContext();
    }

    notifySuccess(): void {
        microsoftTeams.app.notifySuccess();
    }

    registerOnThemeChangeHandler(handler: (theme: string) => void): void {
        microsoftTeams.app.registerOnThemeChangeHandler(handler);
    }

    // Authentication module methods
    async authenticate(authenticateParameters: any): Promise<void> {
        await microsoftTeams.authentication.authenticate(authenticateParameters);
    }

    async getAuthToken(authTokenRequest?: any): Promise<string> {
        return await microsoftTeams.authentication.getAuthToken(authTokenRequest || { silent: true });
    }

    // Dialog module methods
    openDialog(dialogInfo: any, submitHandler?: (result: any) => void): void {
        microsoftTeams.dialog.url.open(dialogInfo, submitHandler);
    }

    submitDialog(result?: string | object): void {
        microsoftTeams.dialog.url.submit(result);
    }

    // Pages configuration methods
    setValidityState(validityState: boolean): void {
        microsoftTeams.pages.config.setValidityState(validityState);
    }

    registerOnSaveHandler(handler: (saveEvent: any) => void): void {
        microsoftTeams.pages.config.registerOnSaveHandler(handler);
    }

    async setConfig(instanceSettings: any): Promise<void> {
        await microsoftTeams.pages.config.setConfig(instanceSettings);
    }
}

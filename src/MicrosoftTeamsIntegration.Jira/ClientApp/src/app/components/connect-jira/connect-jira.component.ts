import { Component, OnInit } from '@angular/core';
import { UntypedFormGroup, Validators, UntypedFormControl, AbstractControl } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import {
    ApiService,
    AuthService,
    ErrorService,
    AppInsightsService
} from '@core/services';
import { LoadingIndicatorService } from '@shared/services/loading-indicator.service';
import { ApplicationType, AddonStatus } from '@core/enums';
import * as microsoftTeams from '@microsoft/teams-js';
import { AnalyticsService, EventAction, UiEventSubject } from '@core/services/analytics.service';

@Component({
    selector: 'app-connect-jira',
    templateUrl: './connect-jira.component.html',
    styleUrls: ['./connect-jira.component.scss'],
    standalone: false
})

export class ConnectJiraComponent implements OnInit {

    constructor(
        private router: Router,
        private route: ActivatedRoute,
        private apiService: ApiService,
        private authService: AuthService,
        private errorService: ErrorService,
        private appInsightsService: AppInsightsService,
        private loadingIndicatorService: LoadingIndicatorService,
        private analyticsService: AnalyticsService,
    ) { }

    public get jiraId(): AbstractControl | any {
        return this.connectForm?.get('jiraId');
    }

    public get verificationCode(): AbstractControl | any {
        return this.loginForm?.get('verificationCode');
    }

    public static JIRA_ID_STORAGE_KEY = 'jira.jiraId';
    private endpoint: string | undefined;

    private readonly SETTINGS_PAGE_ROUTE = '/settings';

    public connectForm: UntypedFormGroup | any;
    public loginForm: UntypedFormGroup | any;
    public showAddonStatusError = false;
    public jiraServerId: string | undefined;
    public displayedUrl: string | undefined;
    public loading = false;
    public showLoginForm = false;
    public errorMessage: string | undefined;
    public addonStatusErrorMessage: string | undefined;
    public addonVersion = '';
    public jiraAuthUrl = '';
    public authClicked = false;
    public authDisabled = false;

    private application: string | undefined;
    private enabledLoadingIndicatorHiding = true;

    public async ngOnInit(): Promise<void> {
        this.loadingIndicatorService.show();

        this.parseParams();

        this.appInsightsService.logNavigation('ConnectJiraComponent', this.route);
        this.analyticsService.sendScreenEvent(
            'connectToJira',
            EventAction.viewed,
            UiEventSubject.taskModule,
            'connectToJira', { application: this.application });

        this.buildConnectForm();

        this.loadingIndicatorService.hide();

        microsoftTeams.app.notifySuccess();
    }

    public async onSubmitConnectForm(): Promise<void> {
        this.showAddonStatusError = false;
        this.loadingIndicatorService.show();
        let showAddonExceptionOnError = true;

        this.analyticsService.sendUiEvent(
            'connectToJira',
            EventAction.clicked,
            UiEventSubject.button,
            'connectJira',
            {source: 'connectToJira'});

        try {
            let jiraId: string = this.jiraId.value;

            localStorage.setItem(ConnectJiraComponent.JIRA_ID_STORAGE_KEY, jiraId);

            if (this.isValidUrl(jiraId)) {
                try {
                    jiraId = await this.apiService.getJiraId(jiraId);
                } catch (error) {
                    // eslint-disable-next-line max-len
                    this.addonStatusErrorMessage = `Jira Data Center is not found or not accessible. Please try to visit the URL to get Jira ID: <a href="${jiraId}/plugins/servlet/teams/getJiraServerId" target="_blank">${jiraId}/plugins/servlet/teams/getJiraServerId</a> or visit <a href="https://confluence.atlassian.com/msteamsjiraserver/microsoft-teams-for-jira-server-documentation-1027116656.html" target="_blank" rel="noreferrer noopener">documentation</a> for more details`;
                    this.showAddonStatusError = true;
                    return;
                }
            }

            this.jiraServerId = jiraId;

            this.addonStatusErrorMessage = `Please check if ${this.jiraServerId} is valid Jira unique ID or Jira base URL
                 and Jira Data Center for Microsoft Teams app for your organization is installed.`;

            const { addonStatus, addonVersion } = await this.apiService.getAddonStatus(jiraId);
            const addonIsInstalled = addonStatus === AddonStatus.Installed || addonStatus === AddonStatus.Connected;
            const userExists = addonStatus === AddonStatus.Connected;
            // if addon is installed - show login form
            if (addonIsInstalled) {
                if (userExists) {
                    const savingResult = await this.apiService.saveJiraServerId(this.jiraServerId);
                    if (savingResult.isSuccess) {
                        this.moveForward();
                    } else {
                        this.errorMessage = savingResult.message;
                    }
                } else {
                    const { isSuccess, message } = await this.apiService.submitLoginInfo(this.jiraServerId);
                    if (isSuccess) {
                        showAddonExceptionOnError = false;
                        this.jiraAuthUrl = message;
                    } else {
                        this.errorMessage = message;
                        this.authDisabled = true;
                    }
                    this.loadingIndicatorService.hide();
                    this.showLoginForm = true;
                    this.buildLoginForm();
                    this.addonVersion = addonVersion;
                }
            } else {
                this.showAddonStatusError = true;
            }
        } catch (error) {
            if (showAddonExceptionOnError) {
                this.showAddonStatusError = true;
            }
        } finally {
            if (this.enabledLoadingIndicatorHiding) {
                this.loadingIndicatorService.hide();
            }
        }
    }

    isValidUrl(urlString: string): boolean {
        try {
            new URL(urlString);
            return true;
        } catch (err) {
            return false;
        }
    }

    public async onSubmitLoginForm(): Promise<void> {
        this.showAddonStatusError = false;
        this.loadingIndicatorService.show();

        this.analyticsService.sendUiEvent(
            'connectToJira',
            EventAction.clicked,
            UiEventSubject.button,
            'submitJiraConnection',
            {source: 'connectToJira'});

        try {
            const oauthToken: string = (new RegExp('[\?&]oauth_token=([^&#]*)').exec(this.jiraAuthUrl) as any)[1];
            const { isSuccess, message } =
                await this.apiService.submitLoginInfo(this.jiraServerId as string, oauthToken, this.verificationCode.value);

            if (isSuccess) {
                const savingResult = await this.apiService.saveJiraServerId(this.jiraServerId as string);
                if (savingResult.isSuccess) {
                    this.jiraAuthUrl = savingResult.message;
                    this.analyticsService.sendTrackEvent(
                        'connectToJira',
                        'successful',
                        'signin',
                        '',
                        {source: 'connectToJira'});
                    await this.moveForward();
                } else {
                    this.analyticsService.sendTrackEvent(
                        'connectToJira',
                        'failed',
                        'signin',
                        '',
                        {source: 'connectToJira', errorMessage: this.errorMessage});
                    this.errorMessage = savingResult.message;
                }
            } else {
                this.errorMessage = message;
            }
        } catch (error) {
            this.showAddonStatusError = true;
        } finally {
            if (this.enabledLoadingIndicatorHiding) {
                this.loadingIndicatorService.hide();
            }
        }
    }

    private parseParams(): void {
        const { endpoint, application } = this.route.snapshot.params;
        this.endpoint = endpoint || this.SETTINGS_PAGE_ROUTE;
        this.application = application || ApplicationType.JiraServerTab;
    }

    private buildConnectForm(): void {
        this.connectForm = new UntypedFormGroup({
            jiraId: new UntypedFormControl( null, Validators.required)
        });

        this.jiraId.setValue(localStorage.getItem(ConnectJiraComponent.JIRA_ID_STORAGE_KEY));
    }

    private buildLoginForm(): void {
        this.loginForm = new UntypedFormGroup({
            verificationCode: new UntypedFormControl(null, Validators.required)
        });
    }

    private async moveForward(): Promise<void> {
        try {
            if (this.application === ApplicationType.JiraServerTab) {
                await this.handleTab();
                return;
            }
            await this.authService.authenticateMicrosoftAccount();
        } catch (error) {
            this.errorService.showDefaultError(error as any);
        }
    }

    private async handleTab(): Promise<void> {
        try {
            const { displayName } = await this.apiService.getMyselfData(this.jiraServerId as string);
            if (!this.endpoint || this.endpoint === 'undefined') {
                this.endpoint = this.SETTINGS_PAGE_ROUTE;
            }

            // if endpoint is a static file e.g. loginResult.html - use location.replace() method
            if (this.endpoint.indexOf('.html') !== -1) {
                window.location.replace('https://' + window.location.host + this.endpoint);
            } else {
                await this.router.navigate([
                    this.endpoint,
                    { ...this.route.snapshot.params, jiraUrl: this.jiraServerId, displayName, endpoint: undefined }
                ]);
            }
        } catch (error) {
            throw error;
        }
    }
}

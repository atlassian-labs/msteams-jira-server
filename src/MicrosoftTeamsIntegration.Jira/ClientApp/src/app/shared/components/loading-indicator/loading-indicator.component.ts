import { Component, OnInit, OnDestroy } from '@angular/core';
import { Subscription } from 'rxjs/internal/Subscription';

import { LoadingIndicatorService } from '@shared/services/loading-indicator.service';

@Component({
    selector: 'app-loading-indicator',
    template: `
        <div class="align-center" *ngIf="loading">
            <mat-spinner [diameter]="50"></mat-spinner>
        </div>
    `
})
export class LoadingIndicatorComponent implements OnInit, OnDestroy {
    public loading = false;

    private subscription: Subscription;

    constructor(
        private readonly loadingIndicatorService: LoadingIndicatorService
    ) { }

    public ngOnInit(): void {
        this.subscription = this.loadingIndicatorService.subscribe((isLoading: boolean) => this.loading = isLoading);
    }

    public ngOnDestroy(): void {
        this.subscription.unsubscribe();
    }
}

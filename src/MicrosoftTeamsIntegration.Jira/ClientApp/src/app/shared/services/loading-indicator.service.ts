import { Injectable, OnDestroy } from '@angular/core';
import { BehaviorSubject, Subscription } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class LoadingIndicatorService implements OnDestroy {
    private loading$ = new BehaviorSubject(false);

    public show(): void {
        this.loading$.next(true);
    }

    public hide(): void {
        this.loading$.next(false);
    }

    public subscribe(
        next?: (value?: any) => void,
        error?: (error?: any) => void,
        complete?: () => void
    ): Subscription {
        return this.loading$.subscribe(next, error, complete);
    }

    public getState(): boolean {
        return this.loading$.value;
    }

    public ngOnDestroy(): void {
        this.loading$.complete();
    }
}

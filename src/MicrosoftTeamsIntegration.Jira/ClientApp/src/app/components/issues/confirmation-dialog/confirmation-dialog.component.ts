import { Component, OnInit, Inject, OnDestroy } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { ConfirmationDialogData, DialogType } from '@core/models/dialogs/issue-dialog.model';
import { Subscription } from 'rxjs';

@Component({
    selector: 'app-confirmation-dialog',
    templateUrl: './confirmation-dialog.component.html',
    styleUrls: ['./confirmation-dialog.component.scss', './confirmation-dialog.component.themes.scss']
})
export class ConfirmationDialogComponent implements OnInit, OnDestroy {

    private subscription: Subscription = new Subscription();
    public dialogIconClass: string;

    constructor(
        @Inject(MAT_DIALOG_DATA) public data: ConfirmationDialogData,
        private dialogRef: MatDialogRef<ConfirmationDialogComponent>
    ) { }

    ngOnInit() {
        this.subscribeToCloseEvents();
        this.dialogIconClass = this.getIconClass(this.data.dialogType);
    }

    public ngOnDestroy(): void {
        this.subscription.unsubscribe();
    }

    public close(): void {
        this.dialogRef.close();
    }

    private subscribeToCloseEvents(): void {
        const keydownEventsSubscription = this.dialogRef.keydownEvents().subscribe((keyEvent: KeyboardEvent) => {
            // 27 - keyCode of 'Escape' button
            if (keyEvent.keyCode === 27) {
                this.close();
            }
        });

        this.subscription.add(keydownEventsSubscription);
    }

    private getIconClass(dialogType: DialogType): string {
        switch (dialogType) {
            case DialogType.SuccessSmall:
                return 'aui-icon aui-icon-small aui-iconfont-approve';
            case DialogType.SuccessLarge:
                return 'aui-icon aui-icon-large aui-iconfont-approve';
            case DialogType.ErrorSmall:
                return 'aui-icon aui-icon-small aui-iconfont-error';
            case DialogType.ErrorLarge:
                return 'aui-icon aui-icon-large aui-iconfont-error';
            case DialogType.WarningSmall:
                return 'aui-icon aui-icon-large aui-iconfont-warning';
            case DialogType.ErrorLarge:
                return 'aui-icon aui-icon-large aui-iconfont-warning';
            default:
                return '';
        }
    }
}

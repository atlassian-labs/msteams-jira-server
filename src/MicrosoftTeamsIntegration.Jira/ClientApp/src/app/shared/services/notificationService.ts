import {SuccessSnackbarComponent} from '@app/components/snack-bars/success-bar/success-snackbar.component';
import {MatSnackBar, MatSnackBarConfig} from '@angular/material/snack-bar';
import {MatSnackBarRef} from '@angular/material/snack-bar/typings/snack-bar-ref';
import {Injectable} from '@angular/core';
import {ErrorSnackbarComponent} from '@app/components/snack-bars/error-bar/error-snackbar.component';

@Injectable({ providedIn: 'root' })
export class NotificationService {
    constructor(
        private snackBar: MatSnackBar
    ) { }

    public notifySuccess(message: string, duration: number = 3000, showCloseButton: boolean = false): MatSnackBarRef<SuccessSnackbarComponent> {
        const configSuccess: MatSnackBarConfig = {
            panelClass: 'successful-alert',
            duration: duration,
            verticalPosition: 'top'
        };
        return this.snackBar.openFromComponent(SuccessSnackbarComponent, {
            data: {
                message: message,
                showCloseButton: showCloseButton
            },...configSuccess
        });
    }

    public notifyError(message: string, duration: number = 3000, showCloseButton: boolean = false): MatSnackBarRef<ErrorSnackbarComponent> {
        const configSuccess: MatSnackBarConfig = {
            panelClass: 'error-alert',
            duration: duration,
            verticalPosition: 'top'
        };
        return this.snackBar.openFromComponent(ErrorSnackbarComponent, {
            data: {
                message: message,
                showCloseButton: showCloseButton
            },...configSuccess
        });
    }
}

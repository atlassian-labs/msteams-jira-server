import { TestBed } from '@angular/core/testing';
import { MatSnackBar } from '@angular/material/snack-bar';
import { NotificationService } from './notificationService';
import { SuccessSnackbarComponent } from '@app/components/snack-bars/success-bar/success-snackbar.component';
import { ErrorSnackbarComponent } from '@app/components/snack-bars/error-bar/error-snackbar.component';

describe('NotificationService', () => {
    let service: NotificationService;
    let snackBar: jasmine.SpyObj<MatSnackBar>;

    beforeEach(() => {
        const spy = jasmine.createSpyObj('MatSnackBar', ['openFromComponent']);

        TestBed.configureTestingModule({
            providers: [
                NotificationService,
                { provide: MatSnackBar, useValue: spy }
            ]
        });
        service = TestBed.inject(NotificationService);
        snackBar = TestBed.inject(MatSnackBar) as jasmine.SpyObj<MatSnackBar>;
    });

    it('should be created', () => {
        expect(service).toBeTruthy();
    });

    it('should notify success', () => {
        const message = 'Success message';
        const duration = 3000;
        const showCloseButton = true;

        service.notifySuccess(message, duration, showCloseButton);

        expect(snackBar.openFromComponent).toHaveBeenCalledWith(SuccessSnackbarComponent, {
            data: { message, showCloseButton },
            panelClass: 'successful-alert',
            duration,
            verticalPosition: 'top'
        });
    });

    it('should notify error', () => {
        const message = 'Error message';
        const duration = 3000;
        const showCloseButton = true;

        service.notifyError(message, duration, showCloseButton);

        expect(snackBar.openFromComponent).toHaveBeenCalledWith(ErrorSnackbarComponent, {
            data: { message, showCloseButton },
            panelClass: 'error-alert',
            duration,
            verticalPosition: 'top'
        });
    });

    it('should notify success with default parameters', () => {
        const message = 'Success message';

        service.notifySuccess(message);

        expect(snackBar.openFromComponent).toHaveBeenCalledWith(SuccessSnackbarComponent, {
            data: { message, showCloseButton: false },
            panelClass: 'successful-alert',
            duration: 3000,
            verticalPosition: 'top'
        });
    });

    it('should notify error with default parameters', () => {
        const message = 'Error message';

        service.notifyError(message);

        expect(snackBar.openFromComponent).toHaveBeenCalledWith(ErrorSnackbarComponent, {
            data: { message, showCloseButton: false },
            panelClass: 'error-alert',
            duration: 3000,
            verticalPosition: 'top'
        });
    });
});

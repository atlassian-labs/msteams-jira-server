// eslint-disable-next-line max-len
import {MAT_SNACK_BAR_DATA, MatSnackBarRef, SimpleSnackBar} from '@angular/material/snack-bar';
import {Component, Inject} from '@angular/core';
// ...

@Component({
    selector: 'app-error-snack-bar',
    templateUrl: 'error-snackbar.component.html',
    styleUrls: ['./error-snackbar.component.scss'],
    standalone: false
})

export class ErrorSnackbarComponent {
    constructor(
        public snackBarRef: MatSnackBarRef<SimpleSnackBar>,
        @Inject(MAT_SNACK_BAR_DATA) public data: any) { }
}

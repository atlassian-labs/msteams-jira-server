// eslint-disable-next-line max-len
import {MAT_LEGACY_SNACK_BAR_DATA as MAT_SNACK_BAR_DATA, MatLegacySnackBarRef as MatSnackBarRef, LegacySimpleSnackBar as SimpleSnackBar} from '@angular/material/legacy-snack-bar';
import {Component, Inject} from '@angular/core';
// ...

@Component({
    selector: 'app-error-snack-bar',
    templateUrl: 'error-snackbar.component.html',
    styleUrls: ['./error-snackbar.component.scss']
})

export class ErrorSnackbarComponent {
    constructor(
        public snackBarRef: MatSnackBarRef<SimpleSnackBar>,
        @Inject(MAT_SNACK_BAR_DATA) public data: any) { }
}

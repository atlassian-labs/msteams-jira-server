import {MatSnackBarRef, SimpleSnackBar, MAT_SNACK_BAR_DATA} from '@angular/material/snack-bar';
import {Component, Inject} from '@angular/core';
// ...

@Component({
    selector: 'app-successful-snack-bar',
    templateUrl: 'success-snackbar.component.html',
    styleUrls: ['./success-snackbar.component.scss']
})

export class SuccessSnackbarComponent {
    constructor(
        public snackBarRef: MatSnackBarRef<SimpleSnackBar>,
        @Inject(MAT_SNACK_BAR_DATA) public data: any) { }
}

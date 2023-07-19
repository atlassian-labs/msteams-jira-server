import { AbstractControl } from "@angular/forms";

export class StringValidators {
    static isNotEmptyString(control: AbstractControl) {
        if (!control || control.value.trim() === '') {
            return { emptyString: true };
        }
        return null;
    }
}

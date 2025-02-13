import { AbstractControl, FormControl } from '@angular/forms';
import { StringValidators } from './string.validators';

describe('StringValidators', () => {
    it('should return null for non-empty string', () => {
        const control = new FormControl('test');
        const result = StringValidators.isNotEmptyString(control);
        expect(result).toBeNull();
    });

    it('should return error object for empty string', () => {
        const control = new FormControl('');
        const result = StringValidators.isNotEmptyString(control);
        expect(result).toEqual({ emptyString: true });
    });

    it('should return error object for string with only spaces', () => {
        const control = new FormControl('   ');
        const result = StringValidators.isNotEmptyString(control);
        expect(result).toEqual({ emptyString: true });
    });

    it('should return null for string with leading and trailing spaces', () => {
        const control = new FormControl('  test  ');
        const result = StringValidators.isNotEmptyString(control);
        expect(result).toBeNull();
    });

    it('should return error object for null control', () => {
        const result = StringValidators.isNotEmptyString(null as unknown as AbstractControl);
        expect(result).toEqual({ emptyString: true });
    });

    it('should return error object for undefined control', () => {
        const result = StringValidators.isNotEmptyString(undefined as unknown as AbstractControl);
        expect(result).toEqual({ emptyString: true });
    });
});

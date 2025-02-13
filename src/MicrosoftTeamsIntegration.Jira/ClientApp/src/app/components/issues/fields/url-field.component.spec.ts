import { ComponentFixture, TestBed } from '@angular/core/testing';
import { UrlFieldComponent } from './url-field.component';
import { UntypedFormGroup, ReactiveFormsModule } from '@angular/forms';
import { By } from '@angular/platform-browser';

describe('UrlFieldComponent', () => {
    let component: UrlFieldComponent;
    let fixture: ComponentFixture<UrlFieldComponent>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            declarations: [UrlFieldComponent],
            imports: [ReactiveFormsModule]
        }).compileComponents();

        fixture = TestBed.createComponent(UrlFieldComponent);
        component = fixture.componentInstance;
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should initialize with default value', () => {
        component.data = {
            formControlName: 'urlField',
            defaultValue: 'http://example.com'
        };
        component.formGroup = new UntypedFormGroup({});
        component.ngOnInit();

        expect(component.selectedValue).toBe('http://example.com');
    });

    it('should validate URL correctly', () => {
        const validUrls = [
            'http://example.com',
            'https://example.com',
            'http://www.example.com',
            'https://www.example.com',
            'http://example.com/path',
            'https://example.com/path'
        ];

        const invalidUrls = [
            'example',
            'http//example.com',
            'https//example.com',
            'http://example',
            'https://example',
            'http://example.',
            'https://example.'
        ];

        validUrls.forEach(url => {
            component.onChange({ target: { value: url } });
            expect(component.validationError).toBeFalse();
        });

        invalidUrls.forEach(url => {
            component.onChange({ target: { value: url } });
            expect(component.validationError).toBeTrue();
        });
    });

    it('should handle empty URL correctly', () => {
        component.onChange({ target: { value: '' } });
        expect(component.validationError).toBeFalse();
    });
});

import { TimeToResolutionIconDirective } from './time.to.resolution-derictive';
import { ElementRef } from '@angular/core';

describe('TimeToResolutionIconDirective', () => {
    let directive: TimeToResolutionIconDirective;
    let elementRef: ElementRef;

    beforeEach(() => {
        elementRef = new ElementRef(document.createElement('div'));
        directive = new TimeToResolutionIconDirective(elementRef);
    });

    function rgbToHex(rgb: string): string {
        const result = rgb.match(/\d+/g);
        if (!result) {
            return '';
        }
        const r = parseInt(result[0], 10);
        const g = parseInt(result[1], 10);
        const b = parseInt(result[2], 10);

        return `#${r.toString(16).padStart(2, '0')}${g.toString(16)
            .padStart(2, '0')}${b.toString(16)
            .padStart(2, '0')}`
            .toUpperCase();
    }

    it('should create an instance', () => {
        expect(directive).toBeTruthy();
    });

    it('should hide element if timeValue is not provided', () => {
        directive.timeValue = undefined;

        directive.ngOnInit();

        expect(elementRef.nativeElement.style.display).toBe('none');
    });

    it('should set correct class and color for work hours and non-breached state', () => {
        directive.timeValue = 3600000; // 1 hour
        directive.isWorkHours = true;
        directive.isBreached = false;

        directive.ngOnInit();

        expect(elementRef.nativeElement.className).toContain('aui-icon-ongoing');
        expect(rgbToHex(elementRef.nativeElement.style.color)).toBe('#00A3BF');
    });

    it('should set correct class and color for non-work hours', () => {
        directive.timeValue = 3600000; // 1 hour
        directive.isWorkHours = false;
        directive.isBreached = false;
        directive.ngOnInit();
        expect(elementRef.nativeElement.className).toContain('aui-iconfont-pause');
        expect(rgbToHex(elementRef.nativeElement.style.color)).toBe('#00A3BF');
    });

    it('should set correct class and color for breached state', () => {
        directive.timeValue = 3600000; // 1 hour
        directive.isWorkHours = true;
        directive.isBreached = true;

        directive.ngOnInit();

        expect(elementRef.nativeElement.className).toContain('aui-icon-ongoing');
        expect(rgbToHex(elementRef.nativeElement.style.color)).toBe('#DE350B');
    });

    it('should set correct color for timeValue less than cutOfTimeInMilliseconds', () => {
        directive.timeValue = 1800000; // 30 minutes
        directive.isWorkHours = true;
        directive.isBreached = false;

        directive.ngOnInit();

        expect(elementRef.nativeElement.className).toContain('aui-icon-ongoing');
        expect(rgbToHex(elementRef.nativeElement.style.color)).toBe('#FFAB00');
    });
});

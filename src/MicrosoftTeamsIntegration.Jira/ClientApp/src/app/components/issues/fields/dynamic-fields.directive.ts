import { Directive, ViewContainerRef } from '@angular/core';

@Directive({
    // eslint-disable-next-line @angular-eslint/directive-selector
    selector: '[dynamic-fields-host]',
    standalone: false
})

export class DynamicFieldsDirective {
    constructor(public viewContainerRef: ViewContainerRef) { }
}

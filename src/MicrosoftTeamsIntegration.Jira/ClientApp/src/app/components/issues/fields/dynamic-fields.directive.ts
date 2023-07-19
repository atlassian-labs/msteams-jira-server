import { Directive, ViewContainerRef } from '@angular/core';

@Directive({
    selector: '[dynamic-fields-host]',
})

export class DynamicFieldsDirective {
    constructor(public viewContainerRef: ViewContainerRef) { }
}
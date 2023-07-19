import { Component, Input, AfterViewInit, ChangeDetectorRef } from '@angular/core';

@Component({
    selector: 'app-truncated-text',
    template: `
        <div *ngIf="text"
              class="truncated-text"
			  #elem
              matTooltip="{{ elem.scrollWidth > elem.clientWidth ? text  : '' }}"
              matTooltipShowDelay="{{ showDelay }}">
			  {{ text }}
		</div>
    `,
    styles: [`
        :host {
            display: block;
            /* min-width and flex properties are used to correctly display truncated text in flex container
                see https://css-tricks.com/flexbox-truncated-text/
            */
            min-width: 1px;
            min-height: 1px;
        }
    `]
})
export class TruncatedTextComponent implements AfterViewInit {
    constructor(private readonly changeDetectorRef: ChangeDetectorRef) { }

    @Input() text: string;
    @Input() showDelay = 100;

    public ngAfterViewInit(): void {
        this.changeDetectorRef.detectChanges();
    }
}

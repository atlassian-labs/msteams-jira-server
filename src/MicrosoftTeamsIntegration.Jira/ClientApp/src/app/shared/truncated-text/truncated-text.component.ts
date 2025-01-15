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
    `],
    standalone: false
})
export class TruncatedTextComponent implements AfterViewInit {
    @Input() text: string | undefined;
    @Input() showDelay = 100;
    constructor(private readonly changeDetectorRef: ChangeDetectorRef) { }

    public ngAfterViewInit(): void {
        this.changeDetectorRef.detectChanges();
    }
}

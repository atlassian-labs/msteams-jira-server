import {
    Directive,
    OnInit,
    Input,
    ElementRef,
} from '@angular/core';

@Directive({
    // eslint-disable-next-line @angular-eslint/directive-selector
    selector: '[timeToResolution]'
})
export class TimeToResolutionIconDirective implements OnInit {
    @Input() timeValue: number;
    @Input() isWorkHours: boolean;
    @Input() isBreached: boolean;

    private readonly cutOfTimeInMilliseconds = 1800000; // 1 800 000 ms = 30 minutes critical time to resolution

    private readonly jiraIconsClasses = 'aui-icon aui-icon-small sla-status-icon';
    private readonly clockIconClass = 'aui-icon-ongoing';
    private readonly pauseIconClass = 'aui-iconfont-pause';

    private readonly breachedStateColor = '#de350b';
    private readonly workStateColor = '#00a3bf';
    private readonly expiredStateColor = '#ffab00';

    constructor(private readonly elRef: ElementRef) { }

    public ngOnInit(): void {
        const elem = this.elRef.nativeElement as HTMLElement;

        if (!this.timeValue) {
            elem.style.display = 'none';
            return;
        }

        elem.className = this.jiraIconsClasses + ' ' + this.getIconClass();
        elem.style.color = this.getStateColor();
    }

    private getIconClass(): string {
        // end of the day || day-off || weekend || nobody works
        if (!this.isWorkHours) {
            return this.pauseIconClass;
        }

        return this.clockIconClass;
    }

    private getStateColor(): string {
        return this.isBreached ?
            this.breachedStateColor :
            this.timeValue <= this.cutOfTimeInMilliseconds ? this.expiredStateColor : this.workStateColor;
    }
}


import { Component, Input, Output, EventEmitter } from '@angular/core';
import { NormalizedIssue } from '@core/models';

@Component({
    selector: 'app-issues-table-mobile',
    styleUrls: ['./issues-table-mobile.scss'],
    templateUrl: './issues-table-mobile.component.html'
})
export class IssuesTableMobileComponent {
    @Input() public data: NormalizedIssue[];
    @Output() public issueClicked = new EventEmitter<string>();
}

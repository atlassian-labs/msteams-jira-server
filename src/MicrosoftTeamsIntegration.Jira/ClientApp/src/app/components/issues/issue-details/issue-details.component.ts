import { Component, OnInit, Input } from '@angular/core';
import { Issue } from '@core/models';
import { DomSanitizer } from '@angular/platform-browser';

@Component({
    selector: 'app-issue-details',
    templateUrl: './issue-details.component.html',
    styleUrls: ['./issue-details.component.scss']
})
export class IssueDetailsComponent implements OnInit {
    public title: string | any;
    public subtitle: string | any;
    public issueTypeImg: string | any;
    public priorityImg: string | any;

    @Input() issue: Issue | any;

    constructor(public sanitizer: DomSanitizer) { }

    ngOnInit() {
        if (this.issue) {
            this.title = `${this.issue.key} : ${this.issue.fields.summary}`;
            this.subtitle = this.getSubtittle(this.issue);

            if (this.issue.fields && this.issue.fields.issuetype) {
                this.issueTypeImg = !(this.issue.fields.issuetype.iconUrl === undefined ||
                    this.issue.fields.issuetype.iconUrl === null)
                    ? this.issue.fields.issuetype.iconUrl : null;
            }

            if (this.issue.fields && this.issue.fields.priority) {
                this.priorityImg = !(this.issue.fields.priority.iconUrl === undefined ||
                    this.issue.fields.priority.iconUrl === null
                )
                    ? this.issue.fields.priority.iconUrl : null;
            }
        }
    }

    public sanitizeUrl(url: string | any): any {
        return this.sanitizer?.bypassSecurityTrustUrl(url);
    }

    private getSubtittle(issue: Issue): string {
        if (issue) {
            const subtitleParts = [];
            if (issue.fields && issue.fields.status) {
                subtitleParts.push(issue.fields.status.name);
            }
            subtitleParts.push(this.getAssignee(issue));
            if (issue.fields && issue.fields.updated){
                subtitleParts.push(new Date(issue.fields.updated).toLocaleDateString());
            }

            return subtitleParts.join(' | ');
        }
        return '';
    }

    private getAssignee(issue: Issue): string {
        if (issue) {
            if (issue.fields && issue.fields.assignee) {
                return issue.fields.assignee.displayName;
            }
        }
        return 'Unassigned';
    }
}

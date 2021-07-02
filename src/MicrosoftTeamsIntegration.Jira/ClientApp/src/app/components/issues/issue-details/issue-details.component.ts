import { Component, OnInit, Input } from '@angular/core';
import { Issue } from '@core/models';
import { DomSanitizer } from '@angular/platform-browser';
import { isNullOrUndefined } from 'util';

@Component({
  selector: 'app-issue-details',
  templateUrl: './issue-details.component.html',
  styleUrls: ['./issue-details.component.scss']
})
export class IssueDetailsComponent implements OnInit {
  public title: string;
  public subtitle: string;
  public issueTypeImg: string;
  public priorityImg: string;

  @Input() issue: Issue;

  constructor(private sanitizer: DomSanitizer) { }

  ngOnInit() {
    if (this.issue) {
      this.title = `${this.issue.key} : ${this.issue.fields.summary}`;
      this.subtitle = this.getSubtittle(this.issue);

      if (this.issue.fields && this.issue.fields.issuetype) {
        this.issueTypeImg = !isNullOrUndefined(this.issue.fields.issuetype.iconUrl) 
          ? this.issue.fields.issuetype.iconUrl : null;
      }

      if (this.issue.fields && this.issue.fields.priority) {
        this.priorityImg = !isNullOrUndefined(this.issue.fields.priority.iconUrl) 
          ? this.issue.fields.priority.iconUrl : null;
      }
    }
  }

  private getSubtittle(issue: Issue): string {
    if (issue) {
      let subtitleParts = [];
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

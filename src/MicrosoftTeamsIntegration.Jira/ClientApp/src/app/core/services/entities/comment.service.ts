import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { JiraComment } from '@core/models';
import { IssueUpdateCommentOptions, IssueAddCommentOptions } from '@core/models/Jira/issue-comment-options.model';

@Injectable({ providedIn: 'root' })
export class IssueCommentService {
    constructor(
        private readonly http: HttpClient
    ) { }

    public addComment(options: IssueAddCommentOptions): Promise<JiraComment> {
        return this.http
            .post<JiraComment>('/api/addCommentAndGetItBack', options)
            .toPromise() as any;
    }

    public updateComment(options: IssueUpdateCommentOptions): Promise<JiraComment> {
        return this.http
            .put<JiraComment>('/api/updateComment', options)
            .toPromise() as any;
    }
}

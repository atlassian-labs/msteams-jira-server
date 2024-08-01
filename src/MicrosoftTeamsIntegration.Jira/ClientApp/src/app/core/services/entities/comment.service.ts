import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { JiraComment } from '@core/models';
import { IssueUpdateCommentOptions, IssueAddCommentOptions } from '@core/models/Jira/issue-comment-options.model';
import {firstValueFrom} from 'rxjs';

@Injectable({ providedIn: 'root' })
export class IssueCommentService {
    constructor(
        private readonly http: HttpClient
    ) { }

    public addComment(options: IssueAddCommentOptions): Promise<JiraComment> {
        return firstValueFrom(this.http
            .post<JiraComment>('/api/addCommentAndGetItBack', options));
    }

    public updateComment(options: IssueUpdateCommentOptions): Promise<JiraComment> {
        return firstValueFrom(this.http
            .put<JiraComment>('/api/updateComment', options));
    }
}

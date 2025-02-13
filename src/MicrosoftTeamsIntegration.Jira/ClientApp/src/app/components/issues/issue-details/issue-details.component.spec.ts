import { ComponentFixture, TestBed } from '@angular/core/testing';
import { IssueDetailsComponent } from './issue-details.component';
import { DomSanitizer } from '@angular/platform-browser';
import { Issue } from '@core/models';

describe('IssueDetailsComponent', () => {
    let component: IssueDetailsComponent;
    let fixture: ComponentFixture<IssueDetailsComponent>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            declarations: [IssueDetailsComponent],
            providers: [
                { provide: DomSanitizer, useValue: { bypassSecurityTrustUrl: (url: string) => url } }
            ]
        }).compileComponents();

        fixture = TestBed.createComponent(IssueDetailsComponent);
        component = fixture.componentInstance;
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should initialize with issue details', () => {
        const issue: Issue = {
            key: 'TEST-1',
            fields: {
                summary: 'Test summary',
                issuetype: { iconUrl: 'http://example.com/issuetype.png' },
                priority: { iconUrl: 'http://example.com/priority.png' },
                status: { name: 'In Progress' },
                assignee: { displayName: 'Test User' },
                updated: '2023-10-01T00:00:00.000Z'
            }
        } as Issue;

        component.issue = issue;
        component.ngOnInit();

        expect(component.title).toBe('TEST-1 : Test summary');
        expect(component.issueTypeImg).toBe('http://example.com/issuetype.png');
        expect(component.priorityImg).toBe('http://example.com/priority.png');
    });

    it('should sanitize URL', () => {
        const sanitizedUrl = component.sanitizeUrl('http://example.com');
        expect(sanitizedUrl).toBe('http://example.com');
    });

    it('should handle issue without priority and issue type icons', () => {
        const issue: Issue = {
            key: 'TEST-1',
            fields: {
                summary: 'Test summary',
                status: { name: 'In Progress' },
                assignee: { displayName: 'Test User' },
                updated: '2023-10-01T00:00:00.000Z'
            }
        } as Issue;

        component.issue = issue;
        component.ngOnInit();

        expect(component.issueTypeImg).toBeUndefined();
        expect(component.priorityImg).toBeUndefined();
    });
});

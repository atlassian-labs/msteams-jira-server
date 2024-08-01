import { TestBed } from '@angular/core/testing';

import { IssuesService } from '@core/services/entities/issues.service';
import { } from 'jasmine'; // Fix warnings with decribe, beforeEach and other jasmine methods

describe('Service: Issues',
    () => {
        let service: IssuesService;

        beforeEach(() => {
            TestBed.configureTestingModule({
                providers: [IssuesService]
            });

            service = TestBed.inject(IssuesService);
        });

        const filtersSet = [
            {
                actual: 'status in ("Done","In Progress")',
                expected: 'Done, In Progress'
            },
            {
                actual: 'status in ("Done") AND type in ("Task") AND priority in ("High")',
                expected: 'Done, Task, High'
            },
            {
                actual: 'filter in ("My filter")',
                expected: '0'
            },
            { actual: 'status', expected: '' }
        ];

        it('should get filters from jql query',
            () => {
                for (const filter of filtersSet) {
                    const filterFromQuery = service.getFiltersFromQuery(filter.actual);
                    expect(filterFromQuery).toBe(filter.expected);
                }
            });

        const optionsSet = [
            {
                options: { jql: 'all-issues' },
                expected: 'order by created DESC'
            },
            {
                options: { jql: 'all-issues', projectKey: '(PRO)' },
                expected: 'project="(PRO)" order by created DESC'
            },
            {
                options: { jql: 'status in ("Done")', projectKey: '(PRO)' },
                expected: 'status in ("Done") AND project="(PRO)" order by created DESC'
            },
            {
                options: { jql: 'status in ("Done")', projectKey: '(PRO)' },
                expected: 'status in ("Done") AND project="(PRO)" order by created DESC'
            },
            {
                options: { jql: ' status = "In Progress" ', projectKey: '', page: 'IssuesAssigned'},
                expected: 'status = "In Progress" AND (assignee=currentUser()) order by created DESC'
            },
            {
                options: { jql: 'ORDER BY lastViewed DESC', projectKey: '', page: 'MyFilters', accountId: 'microsoft' },
                expected: 'ORDER BY lastViewed DESC'
            }
        ];
        it('should create jql query from options',
            () => {
                for (const option of optionsSet) {
                    const jqlQuery = service.createJqlQuery(option.options);
                    expect(jqlQuery).toBe(option.expected);
                }
            });
    });

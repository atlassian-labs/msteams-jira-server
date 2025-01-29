import { ComponentFixture, TestBed } from '@angular/core/testing';
import { IssuesTableMobileComponent } from './issues-table-mobile.component';
import { CUSTOM_ELEMENTS_SCHEMA } from '@angular/core';

describe('IssuesTableMobileComponent', () => {
    let component: IssuesTableMobileComponent;
    let fixture: ComponentFixture<IssuesTableMobileComponent>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            declarations: [IssuesTableMobileComponent],
            schemas: [CUSTOM_ELEMENTS_SCHEMA]
        }).compileComponents();

        fixture = TestBed.createComponent(IssuesTableMobileComponent);
        component = fixture.componentInstance;
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should emit issueClicked event when an issue is clicked', () => {
        spyOn(component.issueClicked, 'emit');
        const issueId = 'TEST-1';
        component.issueClicked.emit(issueId);
        expect(component.issueClicked.emit).toHaveBeenCalledWith(issueId);
    });
});

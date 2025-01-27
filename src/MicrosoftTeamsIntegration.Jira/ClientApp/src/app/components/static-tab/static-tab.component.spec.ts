import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { StaticTabComponent } from './static-tab.component';
import { ActivatedRoute, Router } from '@angular/router';
import { of } from 'rxjs';

describe('StaticTabComponent', () => {
    let component: StaticTabComponent;
    let fixture: ComponentFixture<StaticTabComponent>;
    let router: jasmine.SpyObj<Router>;
    let route: jasmine.SpyObj<ActivatedRoute>;

    beforeEach(async () => {
        const routerSpy = jasmine.createSpyObj('Router', ['navigate']);
        const routeSpy = jasmine.createSpyObj('ActivatedRoute', ['paramMap'], { paramMap: of({ get: (key: string) => 'testPage' }),
            snapshot: { params: { page: 'testPage' } } });

        await TestBed.configureTestingModule({
            declarations: [StaticTabComponent],
            imports: [RouterTestingModule],
            providers: [
                { provide: Router, useValue: routerSpy },
                { provide: ActivatedRoute, useValue: routeSpy }
            ]
        }).compileComponents();

        fixture = TestBed.createComponent(StaticTabComponent);
        component = fixture.componentInstance;
        router = TestBed.inject(Router) as jasmine.SpyObj<Router>;
        route = TestBed.inject(ActivatedRoute) as jasmine.SpyObj<ActivatedRoute>;
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should navigate to the correct route on initialization', () => {
        component.ngOnInit();

        expect(router.navigate).toHaveBeenCalledWith(['/issues',
            { page: 'testPage', application: 'jiraServerStaticTab' }]);
    });
});

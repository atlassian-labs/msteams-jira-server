import { ComponentFixture, TestBed } from '@angular/core/testing';
import { LoadingIndicatorComponent } from './loading-indicator.component';
import { LoadingIndicatorService } from '@shared/services/loading-indicator.service';
import { of } from 'rxjs';
import { CUSTOM_ELEMENTS_SCHEMA } from '@angular/core';

describe('LoadingIndicatorComponent', () => {
    let component: LoadingIndicatorComponent;
    let fixture: ComponentFixture<LoadingIndicatorComponent>;
    let loadingIndicatorService: jasmine.SpyObj<LoadingIndicatorService>;

    beforeEach(async () => {
        const loadingIndicatorServiceSpy = jasmine.createSpyObj('LoadingIndicatorService', ['subscribe']);

        await TestBed.configureTestingModule({
            declarations: [LoadingIndicatorComponent],
            providers: [
                { provide: LoadingIndicatorService, useValue: loadingIndicatorServiceSpy }
            ],
            schemas: [CUSTOM_ELEMENTS_SCHEMA]
        }).compileComponents();

        fixture = TestBed.createComponent(LoadingIndicatorComponent);
        component = fixture.componentInstance;
        loadingIndicatorService = TestBed.inject(LoadingIndicatorService) as jasmine.SpyObj<LoadingIndicatorService>;
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should subscribe to loading indicator service on init', () => {
        const loadingObservable = of(true);
        loadingIndicatorService.subscribe.and.returnValue(loadingObservable.subscribe(isLoading => component.loading = isLoading));

        component.ngOnInit();

        expect(loadingIndicatorService.subscribe).toHaveBeenCalled();
        expect(component.loading).toBeTrue();
    });

    it('should unsubscribe from loading indicator service on destroy', () => {
        const subscription = jasmine.createSpyObj('Subscription', ['unsubscribe']);
        component['subscription'] = subscription;

        component.ngOnDestroy();

        expect(subscription.unsubscribe).toHaveBeenCalled();
    });
});

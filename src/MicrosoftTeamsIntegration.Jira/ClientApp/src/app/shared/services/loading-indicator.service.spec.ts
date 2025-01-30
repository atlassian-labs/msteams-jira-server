import { TestBed } from '@angular/core/testing';
import { LoadingIndicatorService } from './loading-indicator.service';

describe('LoadingIndicatorService', () => {
    let service: LoadingIndicatorService;

    beforeEach(() => {
        TestBed.configureTestingModule({});
        service = TestBed.inject(LoadingIndicatorService);
    });

    it('should be created', () => {
        expect(service).toBeTruthy();
    });

    it('should show loading indicator', () => {
        service.show();
        expect(service.getState()).toBeTrue();
    });

    it('should hide loading indicator', () => {
        service.show();
        service.hide();
        expect(service.getState()).toBeFalse();
    });

    it('should subscribe to loading state changes', () => {
        let loadingState = false;
        service.subscribe(state => loadingState = state);
        service.show();
        expect(loadingState).toBeTrue();
        service.hide();
        expect(loadingState).toBeFalse();
    });

    it('should complete the loading$ subject on destroy', () => {
        const completeSpy = spyOn(service['loading$'], 'complete').and.callThrough();
        service.ngOnDestroy();
        expect(completeSpy).toHaveBeenCalled();
    });
});

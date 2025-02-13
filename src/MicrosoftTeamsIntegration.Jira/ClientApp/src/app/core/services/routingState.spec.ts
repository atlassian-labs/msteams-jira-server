import { TestBed } from '@angular/core/testing';
import { Router, NavigationStart } from '@angular/router';
import { of } from 'rxjs';
import { RoutingState } from './routingState';

describe('RoutingState', () => {
    let service: RoutingState;
    let router: jasmine.SpyObj<Router>;
    let routerUrl: jasmine.Spy;

    beforeEach(() => {
        const routerSpy = { events: of(new NavigationStart(1, '/new-url')) as any, url: '/initial' };

        TestBed.configureTestingModule({
            providers: [
                RoutingState,
                { provide: Router, useValue: routerSpy }
            ]
        });

        service = TestBed.inject(RoutingState);
        router = TestBed.inject(Router) as jasmine.SpyObj<Router>;
    });

    it('should be created', () => {
        expect(service).toBeTruthy();
    });

    it('should return default previousUrl if _previousUrl is not set', () => {
        expect(service.previousUrl).toBe('/issues');
    });

    it('should return _previousUrl if it is set', () => {
        (service as any)._previousUrl = '/test';
        expect(service.previousUrl).toBe('/test');
    });

    it('should update currentUrl and _previousUrl on NavigationStart event', () => {
        service.loadRouting();

        expect((service as any).currentUrl).toBe('/new-url');
        expect((service as any)._previousUrl).toBe('/initial');
    });

    it('should not update _previousUrl if currentUrl is the same as new url', () => {
        const overriddenRouter =
            jasmine.createSpyObj('Router', [], { events: of(new NavigationStart(1, '/initial')) as any, url: '/initial' });

        TestBed.resetTestingModule();
        TestBed.configureTestingModule({
            providers: [
                RoutingState,
                { provide: Router, useValue: overriddenRouter }
            ]
        }).compileComponents();

        service = TestBed.inject(RoutingState);

        service.loadRouting();

        expect((service as any)._previousUrl).toBeUndefined();
    });

    it('should extract current route from url correctly', () => {
        expect((service as any).extractCurrentRouteFromUrl('/config?v=2.0')).toBe('/config');
        expect((service as any).extractCurrentRouteFromUrl('/config;v=2.0')).toBe('/config');
        expect((service as any).extractCurrentRouteFromUrl('/config')).toBe('/config');
    });
});

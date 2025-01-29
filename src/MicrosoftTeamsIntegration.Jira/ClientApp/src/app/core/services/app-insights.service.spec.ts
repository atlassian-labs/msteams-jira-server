import { TestBed } from '@angular/core/testing';
import { AppInsightsService } from './app-insights.service';
import { ApplicationInsights, IPageViewTelemetry } from '@microsoft/applicationinsights-web';
import { ActivatedRoute } from '@angular/router';

describe('AppInsightsService', () => {
    let service: AppInsightsService;
    let appInsights: jasmine.SpyObj<ApplicationInsights>;

    beforeEach(() => {
        const appInsightsSpy =
            jasmine.createSpyObj('ApplicationInsights', ['loadAppInsights', 'trackPageView', 'trackException', 'trackEvent']);

        TestBed.configureTestingModule({
            providers: [
                AppInsightsService,
                { provide: ApplicationInsights, useValue: appInsightsSpy }
            ]
        });

        service = TestBed.inject(AppInsightsService);
        appInsights = TestBed.inject(ApplicationInsights) as jasmine.SpyObj<ApplicationInsights>;
    });

    it('should be created', () => {
        expect(service).toBeTruthy();
    });

    it('should track page view', () => {
        const name = 'TestPage';
        const properties = { key: 'value' };
        const uri = 'test-uri';
        service.trackPageView(name, properties, uri);

        expect(appInsights.trackPageView).toHaveBeenCalledTimes(1);
    });

    it('should track exception', () => {
        const exception = new Error('Test error');
        const handledAt = 'TestComponent';
        const errorDetails = { key: 'value' };
        service.trackException(exception, handledAt, errorDetails);

        expect(appInsights.trackException).toHaveBeenCalledWith(
            {
                id: handledAt,
                exception,
                severityLevel: 3
            },
            {
                handledAt,
                key: JSON.stringify('value')
            }
        );
    });

    it('should track event', () => {
        const name = 'TestEvent';
        const properties = { key: 'value' };
        service.trackEvent(name, properties);

        expect(appInsights.trackEvent).toHaveBeenCalledWith(
            { name },
            { key: JSON.stringify('value') }
        );
    });

    it('should track warning', () => {
        const exception = new Error('Test warning');
        const handledAt = 'TestComponent';
        const properties = { key: 'value' };
        service.trackWarning(exception, handledAt, properties);

        expect(appInsights.trackException).toHaveBeenCalledWith(
            {
                id: handledAt,
                exception,
                severityLevel: 2
            },
            properties
        );
    });

    it('should log navigation', () => {
        const componentName = 'TestComponent';
        const route = { snapshot: { params: { key: 'value' } } } as unknown as ActivatedRoute;
        service.logNavigation(componentName, route);

        expect(appInsights.trackPageView).toHaveBeenCalledWith({
            name: componentName,
            uri: undefined,
            properties: { params: JSON.stringify(route.snapshot.params) }
        });
    });
});

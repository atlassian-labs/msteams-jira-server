import { logger } from './logger.service';
import { environment } from '../../../environments/environment';

describe('LoggerService', () => {
    let consoleLogSpy: jasmine.Spy;
    const originalEnvironment = environment.production;

    beforeEach(() => {
        consoleLogSpy = spyOn(console, 'log');
    });

    afterEach(() => {
        environment.production = originalEnvironment;
    });

    it('should not call console.log when in production', () => {
        environment.production = true;
        logger('test message');
        expect(consoleLogSpy).not.toHaveBeenCalled();
    });
});

import { TestBed } from '@angular/core/testing';
import { UtilService } from './util.service';

describe('UtilService', () => {
    let service: UtilService;

    beforeEach(() => {
        TestBed.configureTestingModule({
            providers: [UtilService]
        });

        service = TestBed.inject(UtilService);
    });

    it('should be created', () => {
        expect(service).toBeTruthy();
    });

    it('should return predefined filters', () => {
        const filters = service.getFilters();
        expect(filters.length).toBe(7);
        expect(filters[0].value).toBe('all-issues');
    });

    it('should encode special characters', () => {
        const encodedValue = service.encode('test!()*\'');
        expect(encodedValue).toBe('test%21%28%29%2a%27');
    });

    it('should get and set Microsoft Teams context', () => {
        const context = { tid: 'test-tenant', loginHint: 'test-login', userObjectId: 'test-user', locale: 'en-US' };
        localStorage.setItem('msTeamsContext', JSON.stringify(context));

        const retrievedContext = service.getMsTeamsContext();
        expect(retrievedContext.tid).toBe('test-tenant');

        service.setTeamsContext('new-tenant');
        const updatedContext = service.getMsTeamsContext();
        expect(updatedContext.tid).toBe('new-tenant');
    });

    it('should get user client ID', () => {
        localStorage.setItem('userClientId', 'test-client-id');
        const clientId = service.getUserClientId();
        expect(clientId).toBe('test-client-id');
    });

    it('should get AAD instance', () => {
        localStorage.setItem('microsoftLoginBaseUrl', 'https://login.microsoftonline.com');
        const aadInstance = service.getAADInstance();
        expect(aadInstance).toBe('https://login.microsoftonline.com/');
    });

    it('should convert string to null', () => {
        expect(service.convertStringToNull('null')).toBeNull();
        expect(service.convertStringToNull('undefined')).toBeNull();
        expect(service.convertStringToNull('test')).toBe('test');
    });

    it('should capitalize first letter', () => {
        expect(service.capitalizeFirstLetter('test')).toBe('Test');
    });

    it('should capitalize first letter and join', () => {
        expect(service.capitalizeFirstLetterAndJoin('test', 'case')).toBe('TestCase');
    });

    it('should get default user icon', () => {
        expect(service.getDefaultUserIcon('small')).toBe('/assets/useravatar24x24.png');
        expect(service.getDefaultUserIcon('medium')).toBe('/assets/useravatar32x32.png');
    });

    it('should perform JSON copy', () => {
        const obj = { a: 1, b: 2 };
        const copiedObj = service.jsonCopy(obj);
        expect(copiedObj).toEqual(obj);
    });

    it('should compare JSON objects', () => {
        const obj1 = { a: 1, b: 2 };
        const obj2 = { a: 1, b: 2 };
        const obj3 = { a: 2, b: 3 };
        expect(service.jsonEqual(obj1, obj2)).toBeTrue();
        expect(service.jsonEqual(obj1, obj3)).toBeFalse();
    });

    it('should append params to link', () => {
        const link = 'https://example.com';
        const params = { param1: 'value1', param2: 'value2' };
        const updatedLink = service.appendParamsToLink(link, params);
        expect(updatedLink).toBe('https://example.com?param1=value1&param2=value2&');
    });

    it('should get Jira server ID', () => {
        localStorage.setItem('jiraServer.jiraId', 'test-jira-id');
        const jiraId = service.getJiraServerId();
        expect(jiraId).toBe('test-jira-id');
    });

    it('should get query param', () => {
        const paramValue = service.getQueryParam('param1', 'https://example.com?param1=value1&param2=value2');
        expect(paramValue).toBe('value1');
    });

    it('should return empty string if query param is not found', () => {
        const paramValue = service.getQueryParam('param3', 'https://example.com?param1=value1&param2=value2');
        expect(paramValue).toBe('');
    });

    it('should handle URL without query params', () => {
        const paramValue = service.getQueryParam('param1', 'https://example.com');
        expect(paramValue).toBe('');
    });

    it('should handle malformed URL', () => {
        const paramValue = service.getQueryParam('param1', 'malformed-url');
        expect(paramValue).toBe('');
    });

    it('should detect Electron environment', () => {
        spyOnProperty(navigator, 'userAgent', 'get').and.returnValue('electron');
        expect(service.isElectron()).toBeTrue();
    });

    it('should get minimum addon version', () => {
        expect(service.getMinAddonVersion()).toBe('2022.08.103');
    });

    it('should get upgrade addon message', () => {
        expect(service.getUpgradeAddonMessage())
            .toBe('Please upgrade Jira Server for Microsoft Teams app on your Jira Data Center to perform projects search.');
    });

    it('should check if addon is updated', () => {
        spyOn(service, 'getMinAddonVersion').and.returnValue('2022.08.103');
        expect(service.isAddonUpdated('2022.08.104')).toBeTrue();
    });
});

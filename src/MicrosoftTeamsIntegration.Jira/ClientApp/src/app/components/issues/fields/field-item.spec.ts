import { FieldItem } from './field-item';
import { Type } from '@angular/core';

describe('FieldItem', () => {
    it('should create an instance with the given component and data', () => {
        class TestComponent {}
        const data = { key: 'value' };
        const fieldItem = new FieldItem(TestComponent as Type<any>, data);

        expect(fieldItem).toBeTruthy();
        expect(fieldItem.component).toBe(TestComponent);
        expect(fieldItem.data).toBe(data);
    });

    it('should handle undefined data', () => {
        class TestComponent {}
        const fieldItem = new FieldItem(TestComponent as Type<any>, undefined);

        expect(fieldItem).toBeTruthy();
        expect(fieldItem.component).toBe(TestComponent);
        expect(fieldItem.data).toBeUndefined();
    });

    it('should handle null data', () => {
        class TestComponent {}
        const fieldItem = new FieldItem(TestComponent as Type<any>, null);

        expect(fieldItem).toBeTruthy();
        expect(fieldItem.component).toBe(TestComponent);
        expect(fieldItem.data).toBeNull();
    });
});

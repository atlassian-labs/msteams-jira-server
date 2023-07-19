import { Injectable } from '@angular/core';
import { SelectOption } from '@shared/models/select-option.model';

@Injectable({ providedIn: 'root' })
export class SettingsService {

    public buildOptionsFor<T>(response: T[]): SelectOption[] {
        const predicate = {
            hasKey: (x: T) => ({ id: Number(x['id']), label: x['name'] as string, value: x['key'] as string }),
            default: (x: T) => ({ id: Number(x['id']), label: x['name'] as string, value: x['name'] as string })
        };

        return response.map((x) => x.hasOwnProperty('key') ? predicate.hasKey(x) : predicate.default(x));
    }
}

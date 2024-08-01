import { Injectable } from '@angular/core';
import { SelectOption } from '@shared/models/select-option.model';

@Injectable({ providedIn: 'root' })
export class SettingsService {

    public buildOptionsFor<T>(response: T[]): SelectOption[] {
        const predicate = {
            hasKey: (x: T) => ({ id: Number((x as any)['id']), label: (x as any)['name'] as string, value: (x as any)['key'] as string }),
            default: (x: T) => ({ id: Number((x as any)['id']), label: (x as any)['name'] as string, value: (x as any)['name'] as string })
        };

        return response.map((x: any) => x.hasOwnProperty('key') ? predicate.hasKey(x) : predicate.default(x));
    }
}

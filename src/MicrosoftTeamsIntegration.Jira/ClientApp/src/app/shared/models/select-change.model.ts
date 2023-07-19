import { SelectOption } from '@shared/models/select-option.model';

/**
 * Value emitted on selection change.
 */
export interface SelectChange {
    options: SelectOption[];
    isAll?: boolean;
}

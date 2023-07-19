export interface DropDownOption<T> {
    id: number | string | null;
    value: T | null;
    label: string;
    icon?: string;
}

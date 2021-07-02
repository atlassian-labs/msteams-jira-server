export interface SelectOption {
    id: number | string | null;
    value: string | null;
    label: string;
    customData?: Map<string, any>;
}

import { Component, Input, Output, EventEmitter } from '@angular/core';

import { SelectOption } from '@shared/models/select-option.model';
import { SelectChange } from '@shared/models/select-change.model';

@Component({
    selector: 'app-select',
    templateUrl: './select.component.html',
    styleUrls: ['./select.component.scss']
})
export class SelectComponent {
    protected _options: SelectOption[];
    protected _multiple = false;

    @Input()
    get options(): SelectOption[] {
        return this._options;
    }
    set options(options: SelectOption[]) {
        this._options = options;
        this.selectedOptions = [];
    }

    @Input()
    get multiple() {
        return this._multiple;
    }
    set multiple(value: boolean) {
        this._multiple = value;

        if (value && this.options && this.options.indexOf(this._allOption) < 0) {
            this.options.unshift(this._allOption);
        }
    }

    @Input() public placeholder = '';
    @Input() public title = '';

    @Output() optionSelect = new EventEmitter<SelectChange>();

    public isOpened = false;

    private selectedOptions: SelectOption[] = [];

    private readonly _allOption = {
        id: -1,
        value: null,
        label: 'All'
    } as SelectOption;

    public onCheckboxClicked(option: SelectOption): void {
        this.changeSelectedOptions(option);

        if (this.selectedOptions.length === this.options.length) {
            this.optionSelect.emit({ options: [this._allOption], isAll: true });
        } else {
            this.optionSelect.emit({ options: this.selectedOptions, isAll: false });
        }
    }

    public optionClicked(option: SelectOption): void {
        if (this.selectedOptions[0] && this.selectedOptions[0].value === option.value) {
            this.toggle();
            return;
        }

        this.selectedOptions = [option];

        this.optionSelect.emit({ options: this.selectedOptions });

        this.toggle();
    }

    public isOptionSelected(option: SelectOption): boolean {
        return this.selectedOptions.length && !!this.selectedOptions.some(opt => opt.id === option.id);
    }

    public toggle(): void {
        this.isOpened = !this.isOpened;
    }

    public close(): void {
        this.isOpened = false;
    }

    public get labelText(): string {
        if (this.multiple) {
            return this.selectedOptions.join(', ');
        }

        return this.selectedOptions.length ? this.selectedOptions[0].label : this.placeholder;
    }

    private changeSelectedOptions(option: SelectOption): void {

        // if all option is selected
        if (option.id === this._allOption.id) {
            this.selectedOptions = this.selectedOptions.length === this.options.length - 1 ? [] : this.options;
            return;
        }

        const selectedOptionIndex = this.selectedOptions.findIndex(opt => opt.id === option.id);
        if (this.isOptionSelected(option)) {
            this.selectedOptions.splice(selectedOptionIndex, 1);
            return;
        }

        this.selectedOptions.push(option);
    }
}

import { Component, Input, Output, EventEmitter } from '@angular/core';

import { SelectOption } from '@shared/models/select-option.model';
import { SelectChange } from '@shared/models/select-change.model';

@Component({
    selector: 'app-tree',
    templateUrl: './tree.component.html',
    styleUrls: ['./tree.component.scss']
})
export class TreeComponent {
    @Input() public title: string;
    @Input() public options: SelectOption[] = [];
    @Output() public optionSelect = new EventEmitter<SelectChange>();

    public isExpanded = false;
    public selectedOptions: SelectOption[] = [];

    public toggle(): void {
        this.isExpanded = !this.isExpanded;
    }

    public isOptionSelected(option: SelectOption): boolean {
        return this.selectedOptions.indexOf(option) > -1;
    }

    public isAllOptionsSelected(): boolean {
        return this.selectedOptions.length === this.options.length;
    }

    public onOptionClicked(option: SelectOption): void {
        if (this.isOptionSelected(option)) {
            const optionIndex = this.selectedOptions.indexOf(option);
            this.selectedOptions.splice(optionIndex, 1);
        } else {
            this.selectedOptions.push(option);
        }

        this.optionSelect.emit({ options: this.selectedOptions, isAll: this.isAllOptionsSelected() });
    }
}

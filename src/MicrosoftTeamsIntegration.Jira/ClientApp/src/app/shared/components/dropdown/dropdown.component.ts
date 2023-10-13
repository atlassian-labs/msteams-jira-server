import { Component, Input, Output, OnInit, EventEmitter, forwardRef, OnDestroy, HostListener, ViewChild } from '@angular/core';
import { coerceBooleanProperty } from '@angular/cdk/coercion';

import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { Subject, noop } from 'rxjs';
import { debounceTime } from 'rxjs/operators';
import { UtilService } from '@core/services';
import { DropDownOption } from '@shared/models/dropdown-option.model';
import { DomSanitizer } from '@angular/platform-browser';

export const CUSTOM_DROPDOWN_CONTROL_VALUE_ACCESSOR: any = {
    provide: NG_VALUE_ACCESSOR,
    // eslint-disable-next-line @typescript-eslint/no-use-before-define
    useExisting: forwardRef(() => DropDownComponent),
    multi: true
};

@Component({
    selector: 'app-dropdown',
    templateUrl: './dropdown.component.html',
    styleUrls: ['./dropdown.component.scss'],
    providers: [CUSTOM_DROPDOWN_CONTROL_VALUE_ACCESSOR]
})
export class DropDownComponent<T> implements OnInit, OnDestroy, ControlValueAccessor {
    public opened = false;

    @Input()
    get disabled(): boolean {
        return this._disabled;
    }
    set disabled(value: boolean) {
        this._disabled = coerceBooleanProperty(value);
    }
    protected _disabled = false;

    @Input()
    get options(): DropDownOption<T>[] {
        return this._options;
    }
    set options(options: DropDownOption<T>[]) {
        this._options = options && options.length ? options : [this.DEFAULT_LOADING_OPTION];

        if (!this.selected || this.placeholder ||
            (this._options.find(opt => this.utilService.jsonEqual(opt, this.selected)) === undefined && !this.searchable)) {
            if (this.placeholder) {
                this.selected = {
                    id: null,
                    value: null,
                    label: this.placeholder
                };
            } else {
                this.selected = this._options[0];
            }
        }

        if (!this.searchable &&
            this._options.length < 2 &&
            this.selected !== this.DEFAULT_LOADING_OPTION) {
            this._disabled = true;
        } else {
            this._disabled = false;
        }

        if (this.searchable) {
            this.filteredOptions = options;
        }
    }
    protected _options: DropDownOption<T>[] = [];

    @Input()
    get selected(): DropDownOption<T> {
        return this._selected;
    }
    set selected(option: DropDownOption<T>) {
        if (!option) {
            return;
        }

        this.previouslySelected = this._selected || option;
        this._selected = option;

        this._onChange(option.value);
    }
    protected _selected: DropDownOption<T>;

    @Input()
    get filteredOptions(): DropDownOption<T>[] {
        if (!this.selected) {
            return this.options;
        }

        if (!this.searchable) {
            return this.options;
        }

        return this._filteredOptions;
    }
    set filteredOptions(options: DropDownOption<T>[]) {
        this._filteredOptions = options;
    }
    protected _filteredOptions: DropDownOption<T>[] = [];

    @Input() public label: string;
    @Input() public optionsHeader: string;
    @Input() public optionsFooter: string;
    @Input() public errorMessage: string;

    @Input() public loading = false;
    @Input() public required = false;

    @Input() public searchable = false;

    /**
	 * If true then searching array is this.options.
	 * In other case filteredOptions should be set from outside via this.filteredOptions property
	 */
    @Input() public isInnerSearch = false;

    /**
	 * Search input debounce time in milliseconds
	 */
    @Input() public debounceTime = 150;

    @Input() public iconSize: 'sm' | 'md' = 'md';

    @Input() public placeholder = '';

    @Output() public optionSelect = new EventEmitter<DropDownOption<T>>();

    @Output() public searchChange = new EventEmitter<string>();

    private searchChangeDebouncer = new Subject<string>();
    private searchChangeDebouncerSubscription = null;

    private _onChange: Function = noop;
    private _onTouched: Function = noop;

    private readonly DEFAULT_LOADING_OPTION: DropDownOption<T> = {
        id: null,
        value: null,
        label: 'Loading...'
    };

    private previouslySelected: DropDownOption<T>;

    @HostListener('blur') ontouchend(): void {
        this._onTouched();
    }

    constructor(
        public domSanitizer: DomSanitizer,
        private utilService: UtilService
    ) { }

    public ngOnInit(): void {
        this.options = this.options || [this.DEFAULT_LOADING_OPTION];

        if (this.searchable && !this.isInnerSearch) {
            this.searchChangeDebouncerSubscription = this.searchChangeDebouncer
                .pipe(debounceTime(this.debounceTime))
                .subscribe(value => this.searchChange.emit(value));
        }
    }

    public setIconClasses(): { [className: string]: boolean } {
        return {
            'icon': true,
            'dropdown__option-icon': true,
            'icon--md': this.iconSize === 'md',
            'icon--sm': this.iconSize === 'sm',
        };
    }

    // Implementation of ControlValueAccessor
    public writeValue(value: T): void {
        this.selected = this.options.find(opt => this.utilService.jsonEqual(opt.value, value));
    }

    public registerOnChange(fn: (_: any) => void): void {
        this._onChange = fn;
    }

    public registerOnTouched(fn: (_: any) => void): void {
        this._onTouched = fn;
    }

    public setDisabledState?(isDisabled: boolean): void {
        this.disabled = isDisabled;
    }
    //

    public optionClicked(option: DropDownOption<T>): void {
        if (this.selected === option) {
            this.close();
            return;
        }

        this.selected = option;

        this.optionSelect.emit(option);

        this.close();
    }

    public onSearchInput(input: string): void {
        if (!input || !input.trim().length) {
            this.filteredOptions = this.isInnerSearch ? this.options : [];
            return;
        }

        if (!this.isInnerSearch) {
            this.searchChangeDebouncer.next(input);
            return;
        }

        const normalizeText = (text: string) => text.replace(/ /g, '').toLowerCase();
        this.filteredOptions = this.options.filter(option => normalizeText(option.label).includes(normalizeText(input)));
    }

    public setPreviousValue(): void {
        this.selected = this.previouslySelected;
    }

    public open(): void {
        this.opened = true;
    }

    public close(): void {
        if (this.searchable) {
            this.filteredOptions = this.options;

            // change label to force change detection
            if (this.selected.label.endsWith(' ')) {
                this.selected.label = this.selected.label.trim();
            } else {
                this.selected.label += ' ';
            }
        }

        this.opened = false;
    }

    public toggle(): void {
        if (this.opened || this.disabled) {
            this.close();
        } else {
            this.open();
        }
    }

    public ngOnDestroy(): void {
        if (this.searchChangeDebouncerSubscription) {
            this.searchChangeDebouncerSubscription.unsubscribe();
        }
    }
}

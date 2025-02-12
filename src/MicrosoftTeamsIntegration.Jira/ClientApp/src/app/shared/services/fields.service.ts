import { Injectable } from '@angular/core';
import { JiraIssueFieldMeta } from '@core/models/Jira/jira-issue-field-meta.model';
import { FieldItem } from '@app/components/issues/fields/field-item';
import { SelectFieldComponent } from '@app/components/issues/fields/select-field.component';
import { TextFieldSingleComponent } from '@app/components/issues/fields/text-field-single.component';
import { DropdownUtilService } from './dropdown.util.service';
import { DatePickerFieldComponent } from '@app/components/issues/fields/datepicker-field.component';
import { TextFieldMultiComponent } from '@app/components/issues/fields/text-field-multi.component';
import { RadioSelectFieldComponent } from '@app/components/issues/fields/radio-select-field.component';
import { CheckboxSelectFieldComponent } from '@app/components/issues/fields/checkbox-select-field';
import { TextFieldNumberComponent } from '@app/components/issues/fields/text-field-number.component';
import { UserPickerFieldComponent } from '@app/components/issues/fields/userpicker-field.component';
import { LabelsFieldComponent } from '@app/components/issues/fields/labels-field.component';
import { SprintFieldComponent } from '@app/components/issues/fields/sprint-field.component';
import { EpicFieldComponent } from '@app/components/issues/fields/epic-field.component';
import { UrlFieldComponent } from '@app/components/issues/fields/url-field.component';
import { SelectCascadingFieldComponent } from '@app/components/issues/fields/select-cascading-field.component';
import { Issue } from '@core/models';

@Injectable({
    providedIn: 'root'
})
export class FieldsService {
    // list of allowed fields with display order
    private allowedDynamicFields = [
        { id: 'project', order: 0 },
        { id: 'issuetype', order: 1 },
        { id: 'summary', order: 2 },
        { id: 'description', order: 3 },
        { id: 'priority', order: 4 },
        { id: 'assignee', order: 5 },
        { id: 'labels', order: 6 },
        { id: 'duedate', order: 7 },
        { id: 'versions', order: 8 },
        { id: 'fixVersions', order: 9 },
        { id: 'components', order: 10 },
        { id: 'environment', order: 11 },
        // sprint is a custom field in Jira, so we need set the order to it separately. Reserve order number
        { id: 'sprint', order: 12 },
        // epic link is a custom field in Jira, so we need set the order to it separately. Reserve order number
        { id: 'epic', order: 13 },
        // epic name is a custom field in Jira, so we need set the order to it separately. Reserve order number
        { id: 'epicName', order: 14 },
    ];

    private allowedCustomFiledTypes = [
        'com.atlassian.jira.plugin.system.customfieldtypes:multiselect', // Select list (multiline)
        'com.atlassian.jira.plugin.system.customfieldtypes:select', // Select list (single choice)
        'com.atlassian.jira.plugin.system.customfieldtypes:cascadingselect', // Select list (cascading)
        'com.atlassian.jira.plugin.system.customfieldtypes:textfield', // Text field (single line)
        'com.atlassian.jira.plugin.system.customfieldtypes:url', // URL field
        'com.atlassian.jira.plugin.system.customfieldtypes:textarea', // Text field (multiline)
        'com.atlassian.jira.plugin.system.customfieldtypes:datepicker', // Date picker
        'com.atlassian.jira.plugin.system.customfieldtypes:radiobuttons', // Radio button
        'com.atlassian.jira.plugin.system.customfieldtypes:multicheckboxes', // Checkboxes
        'com.atlassian.jira.plugin.system.customfieldtypes:float', // Number field
        'com.atlassian.jira.plugin.system.customfieldtypes:userpicker', // User picker (single user)
        'com.pyxis.greenhopper.jira:gh-sprint', // sprint
        'com.pyxis.greenhopper.jira:gh-epic-link', // epic link
        'com.pyxis.greenhopper.jira:gh-epic-label', // epic name
    ];

    constructor(
        private dropdownUtilService: DropdownUtilService,
    ) { }

    // get all allowed fields, including custom fields
    public getAllowedFields(fields: any, customRequiredOnly: boolean = false): JiraIssueFieldMeta<any>[] {

        if (!fields) {
            return [];
        }

        const result = Object.keys(fields).
            filter(x => this.allowedDynamicFields.
                find(f => f.id === x) ||
                (customRequiredOnly ?
                    x.includes('customfield_') && this.allowedCustomFiledTypes.includes(fields[x].schema.custom) && fields[x].required :
                    x.includes('customfield_') && this.allowedCustomFiledTypes.includes(fields[x].schema.custom))).
            map(x => {
                const rObj = fields[x];
                rObj.key = fields[x].key || x;
                return rObj;
            });
        return result;
    }

    public getAllowedTransformedFields(fields: any, formValue: any) {
        const issueModel = { } as Partial<any>;

        this.getAllowedFields(fields).forEach(field => {
            if(formValue[field.key] === null) {
                issueModel[field.key] = null;
            }
            if (formValue[field.key]) {
                if (field.allowedValues && field.schema.type !== 'option-with-child') {
                    if (Array.isArray(formValue[field.key])) {
                        issueModel[field.key] = formValue[field.key].map((x: any) => ({ id: x }));
                    } else {
                        issueModel[field.key] = {
                            id: formValue[field.key]
                        };
                    }

                } else {
                    if (field.schema.type === 'user') {
                        issueModel[field.key] = {
                            name: formValue[field.key]
                        };
                    } else {
                        issueModel[field.key] = formValue[field.key];
                    }
                }
            }
        });

        return issueModel;
    }

    public getCustomFieldTemplates(fields: any, jiraUrl: string, issueRaw?: Issue): FieldItem[] {
        if (!fields) {
            return [];
        }

        // set the order to fields to display dynamic components according to it
        Object.keys(fields).forEach(x => {
            const allowedFieldMatch = this.allowedDynamicFields.find(f => f.id === x);

            if (allowedFieldMatch) {
                fields[x].order = allowedFieldMatch.order;
            } else if (x.includes('customfield_')) {
                // set order for standard custom fields
                if (fields[x].schema.custom === 'com.pyxis.greenhopper.jira:gh-sprint') {
                    fields[x].order = 12;
                } else if (fields[x].schema.custom === 'com.pyxis.greenhopper.jira:gh-epic-link') {
                    fields[x].order = 13;
                } else if (fields[x].schema.custom === 'com.pyxis.greenhopper.jira:gh-epic-label') {
                    fields[x].order = 14;
                } else {
                    fields[x].order = 9999;
                }
            } else {
                fields[x].order = -1;
            }
        });

        // get allowed fields (including custom fields), and sort them by predefined order
        const dynamicFields = Object.keys(fields).filter(x =>
            this.allowedDynamicFields.find(f => f.id === x) ||
            x.includes('customfield_')).map(x => {
            const rObj = fields[x];
            rObj.key = fields[x].key || x;
            return rObj;
        }).sort(this.orderDynamicFields);

        const dynamicFieldsData: FieldItem[] = [];

        // get dynamic fields templates
        dynamicFields.forEach(dynamicField => {
            // get templates for:
            // Custom Select list (multiline), Fix Versions, Affected Versions, Components
            const defaultValueFromIssue = issueRaw?.fields?.[dynamicField.key] ?? null;
            if (defaultValueFromIssue) {
                dynamicField.hasDefaultValue = true;
                dynamicField.defaultValue = defaultValueFromIssue;
            }
            if ((dynamicField.schema.custom && dynamicField.schema.custom ===
                'com.atlassian.jira.plugin.system.customfieldtypes:multiselect') ||
                dynamicField.schema.system === 'versions' ||
                dynamicField.schema.system === 'fixVersions' ||
                dynamicField.schema.system === 'components') {
                dynamicFieldsData.push(new FieldItem(SelectFieldComponent, {
                    name: dynamicField.name,
                    allowedValues: dynamicField.allowedValues.map(this.dropdownUtilService.mapAllowedValueToSelectOption),
                    defaultValue: this.getDefaultValueFromField(dynamicField),
                    placeholder: 'Select value',
                    formControlName: dynamicField.key,
                    disabled: null,
                    multiple: true,
                    required: dynamicField.required
                }));
            }
            // Custom Select list (single choice)
            if (dynamicField.schema.custom && dynamicField.schema.custom ===
                'com.atlassian.jira.plugin.system.customfieldtypes:select') {
                dynamicFieldsData.push(new FieldItem(SelectFieldComponent, {
                    name: dynamicField.name,
                    allowedValues: dynamicField.allowedValues.map(this.dropdownUtilService.mapAllowedValueToSelectOption),
                    defaultValue: this.getDefaultValueFromField(dynamicField),
                    placeholder: 'Select value',
                    formControlName: dynamicField.key,
                    disabled: null,
                    multiple: false,
                    required: dynamicField.required
                }));
            }
            // Custom Select list (cascading)
            if (dynamicField.schema.custom && dynamicField.schema.custom ===
                'com.atlassian.jira.plugin.system.customfieldtypes:cascadingselect') {
                dynamicFieldsData.push(new FieldItem(SelectCascadingFieldComponent, {
                    name: dynamicField.name,
                    allowedValues: dynamicField.allowedValues,
                    defaultValue: this.getDefaultValueFromField(dynamicField),
                    placeholder: 'Select value',
                    formControlName: dynamicField.key,
                    disabled: null,
                    required: dynamicField.required
                }));
            }
            // Custom Text field (single line)
            if (dynamicField.schema.custom && (dynamicField.schema.custom ===
                'com.atlassian.jira.plugin.system.customfieldtypes:textfield' ||
                dynamicField.schema.custom === 'com.pyxis.greenhopper.jira:gh-epic-label')) {
                dynamicFieldsData.push(new FieldItem(TextFieldSingleComponent, {
                    name: dynamicField.name,
                    defaultValue: this.getDefaultValueFromField(dynamicField),
                    placeholder: 'Enter value',
                    formControlName: dynamicField.key,
                    disabled: null,
                    required: dynamicField.required
                }));
            }
            // Custom Text field (multiline)
            if ((dynamicField.schema.custom && dynamicField.schema.custom ===
                'com.atlassian.jira.plugin.system.customfieldtypes:textarea') ||
                dynamicField.schema.system === 'environment') {
                dynamicFieldsData.push(new FieldItem(TextFieldMultiComponent, {
                    name: dynamicField.name,
                    defaultValue: this.getDefaultValueFromField(dynamicField),
                    placeholder: 'Enter value',
                    formControlName: dynamicField.key,
                    disabled: null,
                    required: dynamicField.required
                }));
            }
            // Custom Date picker, Due Date
            if ((dynamicField.schema.custom && dynamicField.schema.custom ===
                'com.atlassian.jira.plugin.system.customfieldtypes:datepicker') ||
                dynamicField.schema.system === 'duedate') {
                dynamicFieldsData.push(new FieldItem(DatePickerFieldComponent, {
                    name: dynamicField.name,
                    defaultValues: this.getDefaultValueFromField(dynamicField),
                    placeholder: 'Choose a date',
                    formControlName: dynamicField.key,
                    disabled: null,
                    required: dynamicField.required
                }));
            }
            // Custom Radio button
            if (dynamicField.schema.custom && dynamicField.schema.custom ===
                'com.atlassian.jira.plugin.system.customfieldtypes:radiobuttons') {
                dynamicFieldsData.push(new FieldItem(RadioSelectFieldComponent, {
                    name: dynamicField.name,
                    allowedValues: dynamicField.allowedValues.map(this.dropdownUtilService.mapAllowedValueToSelectOption),
                    defaultValue: this.getDefaultValueFromField(dynamicField),
                    placeholder: 'Select value',
                    formControlName: dynamicField.key,
                    disabled: null,
                    required: dynamicField.required
                }));
            }
            // Custom Checkboxes
            if (dynamicField.schema.custom && dynamicField.schema.custom ===
                'com.atlassian.jira.plugin.system.customfieldtypes:multicheckboxes') {
                dynamicFieldsData.push(new FieldItem(CheckboxSelectFieldComponent, {
                    name: dynamicField.name,
                    allowedValues: dynamicField.allowedValues.map(this.dropdownUtilService.mapAllowedValueToSelectOption),
                    defaultValue: this.getDefaultValueFromField(dynamicField),
                    placeholder: 'Select value',
                    formControlName: dynamicField.key,
                    disabled: null,
                    required: dynamicField.required
                }));
            }
            // Custom Number field
            if (dynamicField.schema.custom && dynamicField.schema.custom ===
                'com.atlassian.jira.plugin.system.customfieldtypes:float') {
                dynamicFieldsData.push(new FieldItem(TextFieldNumberComponent, {
                    name: dynamicField.name,
                    defaultValue: this.getDefaultValueFromField(dynamicField),
                    placeholder: 'Enter value',
                    formControlName: dynamicField.key,
                    disabled: null,
                    required: dynamicField.required
                }));
            }
            // Custom User picker (single user)
            if (dynamicField.schema.custom && dynamicField.schema.custom ===
                'com.atlassian.jira.plugin.system.customfieldtypes:userpicker') {
                dynamicFieldsData.push(new FieldItem(UserPickerFieldComponent, {
                    name: dynamicField.name,
                    defaultValue: this.getDefaultValueFromField(dynamicField),
                    formControlName: dynamicField.key,
                    jiraUrl: jiraUrl,
                    disabled: null,
                    required: dynamicField.required
                }));
            }
            // Labels
            if (dynamicField.schema.system === 'labels') {
                dynamicFieldsData.push(new FieldItem(LabelsFieldComponent, {
                    name: dynamicField.name,
                    placeholder: 'Select labels',
                    formControlName: dynamicField.key,
                    addTagText: '(New label)',
                    jiraUrl: jiraUrl,
                    defaultValue: this.getDefaultValueFromField(dynamicField),
                    disabled: null,
                    required: dynamicField.required
                }));
            }
            // Sprints
            if (dynamicField.schema.custom && dynamicField.schema.custom ===
                'com.pyxis.greenhopper.jira:gh-sprint') {

                const projectId = fields['project'] && fields['project'].allowedValues && fields['project'].allowedValues.length > 0 ?
                    fields['project'].allowedValues[0].id :
                    null;
                dynamicFieldsData.push(new FieldItem(SprintFieldComponent, {
                    name: dynamicField.name,
                    defaultValue: this.getDefaultValueFromField(dynamicField),
                    placeholder: 'Select sprint',
                    formControlName: dynamicField.key,
                    jiraUrl: jiraUrl,
                    projectKeyOrId: projectId,
                    disabled: null,
                    required: dynamicField.required
                }));
            }
            // Epic Link
            if (dynamicField.schema.custom && dynamicField.schema.custom ===
                'com.pyxis.greenhopper.jira:gh-epic-link') {

                const projectId = fields['project'] && fields['project'].allowedValues && fields['project'].allowedValues.length > 0 ?
                    fields['project'].allowedValues[0].id :
                    null;

                dynamicFieldsData.push(new FieldItem(EpicFieldComponent, {
                    name: dynamicField.name,
                    defaultValue: this.getDefaultValueFromField(dynamicField),
                    placeholder: 'Select epic',
                    formControlName: dynamicField.key,
                    jiraUrl: jiraUrl,
                    projectKeyOrId: projectId,
                    disabled: null,
                    required: dynamicField.required
                }));
            }
            // Custom URL field
            if (dynamicField.schema.custom && dynamicField.schema.custom ===
                'com.atlassian.jira.plugin.system.customfieldtypes:url') {
                dynamicFieldsData.push(new FieldItem(UrlFieldComponent, {
                    name: dynamicField.name,
                    defaultValue: this.getDefaultValueFromField(dynamicField),
                    placeholder: 'Enter URL',
                    formControlName: dynamicField.key,
                    disabled: null,
                    required: dynamicField.required
                }));
            }
        });

        return dynamicFieldsData;
    }

    private getDefaultValueFromField(dynamicField: any) {
        try {
            if (dynamicField.hasDefaultValue) {
                return this.getDefaultValue(dynamicField.defaultValue);
            }
        } catch (e) {
            console.log(`Cannot get default value for field ${dynamicField.key}. Error: ${e}`);
        }
        return null;
    }

    public getDefaultValue(value: any) {
        let defaultValue = null;
        try {
            if (value) {
                if (Array.isArray(value)) {
                    defaultValue = value.map((x: { id: any }) => x.id ? x.id : x);
                } else if (value.id) {
                    if (value.child) {
                        defaultValue = value;
                    } else {
                        defaultValue = value.id;
                    }
                } else {
                    defaultValue = value;
                }
            }
        } catch (e) {
            console.log(`Cannot get default value for ${value}. Error: ${e}`);
        }
        return defaultValue;
    }

    private orderDynamicFields(a: any, b: any) {
        // order by order filed, then by name
        if (a.order < b.order) {
            return -1;
        }
        if (a.order > b.order) {
            return 1;
        }
        if (a.order === b.order) {
            if (a.name < b.name) {
                return -1;
            }
            if (a.name > b.name) {
                return 1;
            }
            return 0;
        }
        return 0;
    }
}

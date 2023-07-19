// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

import { ClickOutsideModule } from 'ng-click-outside';
import { SpinnerModule } from 'angular2-spinner/dist';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule, MatSpinner } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';

import { SharedComponents } from '@shared/components';
import { TruncatedTextComponent } from '@shared/truncated-text/truncated-text.component';

@NgModule({
    imports: [
        CommonModule,
        FormsModule,
        SpinnerModule,
        MatDialogModule,
        MatButtonModule,
        MatTooltipModule,
        MatProgressSpinnerModule,
        MatIconModule,
        ClickOutsideModule
    ],
    declarations: [
        SharedComponents,
        TruncatedTextComponent
    ],
    exports: [
        SharedComponents,
        MatIconModule,
        MatSpinner,
        TruncatedTextComponent
    ]
})
export class SharedModule { }

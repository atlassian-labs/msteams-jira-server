// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Component } from '@angular/core';

@Component({
    selector: 'app-favorite-filters-empty',
    template: `
        <div class="align-center">
            It seems that you don’t have any filters yet.
            <div class="clr1"></div>
            To use this tab you will need to create and save your own Jira filters:
            <span>
                Please check
                <a href="https://confluence.atlassian.com/jiracorecloud/saving-your-search-as-a-filter-765593721.html"
                   target="_blank" rel="noreferrer noopener">
                    Atlassian documentation
                </a>
                for detailed steps.
            </span>
        </div>`,
})
export class FavoriteFiltersEmptyComponent { }

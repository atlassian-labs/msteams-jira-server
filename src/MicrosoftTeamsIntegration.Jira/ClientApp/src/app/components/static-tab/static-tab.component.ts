// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router, ParamMap } from '@angular/router';

@Component({
    selector: 'app-static-tab',
    template: '',
    standalone: false
})
export class StaticTabComponent implements OnInit {
    private page: string | any;

    constructor(
        private router: Router,
        private route: ActivatedRoute
    ) { }

    public ngOnInit(): void {
        this.route.paramMap.subscribe((params: ParamMap) => {
            this.page = params.get('page');
        });

        const application = 'jiraServerStaticTab';

        this.router.navigate([
            '/issues',
            { ...this.route.snapshot.params, application }
        ]);
    }
}

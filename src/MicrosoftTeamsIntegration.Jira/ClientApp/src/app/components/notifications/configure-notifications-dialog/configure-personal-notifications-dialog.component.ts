import { Component, OnInit } from '@angular/core';
import { UntypedFormGroup } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';

@Component({
    selector: 'app-configure-personal-notifications-dialog',
    templateUrl: './configure-personal-notifications-dialog.component.html',
    styleUrls: ['./configure-personal-notifications-dialog.component.scss'],
    standalone: false
})
export class ConfigurePersonalNotificationsDisalogComponent implements OnInit {
    public notificationsForm: UntypedFormGroup | undefined;
    public jiraId: string | any;

    constructor(private route: ActivatedRoute) { }

    public async ngOnInit() {
        const { jiraId } = this.route.snapshot.params;

        this.jiraId = jiraId;

        await this.createForm();
    }

    private async createForm(): Promise<void> {
        this.notificationsForm = new UntypedFormGroup({});
    }
}

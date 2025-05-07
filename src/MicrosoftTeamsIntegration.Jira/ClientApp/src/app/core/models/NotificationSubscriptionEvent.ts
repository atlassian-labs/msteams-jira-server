import { NotificationSubscription } from './NotificationSubscription';

export interface NotificationSubscriptionEvent {
    subscription: NotificationSubscription;
    action: NotificationSubscriptionAction;
}

export enum NotificationSubscriptionAction {
    Create = 'Created',
    Update = 'Updated',
    Delete = 'Deleted',
    Enabled = 'Enabled',
    Disabled = 'Disabled',
}

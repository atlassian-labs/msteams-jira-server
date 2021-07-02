// TODO: Find out how to rename these entities
export interface StartStopTime {
    iso8601: Date;
    jira: Date;
    friendly: string;
    epochMillis: number;
}

export interface BreachTime {
    iso8601: Date;
    jira: Date;
    friendly: string;
    epochMillis: number;
}

export interface GoalDuration {
    millis: number;
    friendly: string;
}

export interface ElapsedTime {
    millis: number;
    friendly: string;
}

export interface RemainingTime {
    millis: number;
    friendly: string;
}

export interface IssueCycle {
    startTime: StartStopTime;
    breached: boolean;
    withinCalendarHours: boolean;
    goalDuration: GoalDuration;
    elapsedTime: ElapsedTime;
    remainingTime: RemainingTime;
}

export interface CompletedCycle extends IssueCycle {
    stopTime: StartStopTime;
}

export interface OngoingCycle extends IssueCycle {
    breachTime: BreachTime;
    paused: boolean;
}

export interface JiraSlaField {
    id: string;
    name: string;
    completedCycles: CompletedCycle[];
    ongoingCycle: OngoingCycle;
}

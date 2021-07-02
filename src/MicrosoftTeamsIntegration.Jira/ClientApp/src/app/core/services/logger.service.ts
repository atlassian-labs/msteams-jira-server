import { environment } from '../../../environments/environment';

const noopFunction = (...args: any[]): void => {};

export const logger = environment.production ? noopFunction : console.log;

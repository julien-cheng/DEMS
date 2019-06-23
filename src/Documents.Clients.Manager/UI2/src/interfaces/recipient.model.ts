import { IItemQueryTypeBase } from './view.model';

export interface IRecipient extends IItemQueryTypeBase {
  created?: Date;
  modified?: Date;
  expirationDate?: Date;
  email?: string;
  magicLink?: string;
  passwordHash?: string;
  firstName?: string;
  lastName?: string;
}

import { IBatchOperation } from '../index';

export interface IAllowedOperation {
    displayName: string;
    batchOperation: IBatchOperation;
    icons?: string;
    type?:string;
    isSingleton?: boolean;
    isDisabled?: boolean;
}

export interface Operation {
    OperationName: string;
}

export interface Version {
    Major: number;
    Minor: number;
    Patch: number;
    Operations: Operation[];
}

export interface Service {
    ServiceId: string;
    ServiceName: string;
    Versions: Version[];
}

export interface Selection {
    service: string;
    version: string;
    operation: string;
}

export interface ValidationResult {
    type?: string;
    isValid: boolean;
    validationResultMessages: string[];
    responseContent?: string;
    title?: string;
}

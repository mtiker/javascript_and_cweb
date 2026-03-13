export class AppError extends Error {
    constructor(message, code = "APP_ERROR") {
        super(message);
        this.name = "AppError";
        this.code = code;
    }
}
export class ValidationError extends AppError {
    constructor(message) {
        super(message, "VALIDATION_ERROR");
        this.name = "ValidationError";
    }
}
export class StorageError extends AppError {
    constructor(message) {
        super(message, "STORAGE_ERROR");
        this.name = "StorageError";
    }
}
export class NotFoundError extends AppError {
    constructor(message) {
        super(message, "NOT_FOUND");
        this.name = "NotFoundError";
    }
}

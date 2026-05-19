import type { ErrorRequestHandler } from 'express';

export class AppError extends Error {
  constructor(
    public statusCode: number,
    message: string
  ) {
    super(message);
    this.name = 'AppError';
  }
}

export const errorHandler: ErrorRequestHandler = (err, _request, response, next) => {
  if (response.headersSent) {
    return next(err);
  }

  if (err instanceof AppError) {
    response.status(err.statusCode).json({ messages: [err.message] });
  } else {
    console.error(err);
    response.status(500).json({ messages: ['Internal server error'] });
  }
};

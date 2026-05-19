import type { RequestHandler } from 'express';
import { verifyAccessToken } from '../auth.js';

export const authenticate: RequestHandler = (request, response, next) => {
  const authHeader = request.headers.authorization;

  if (!authHeader || !authHeader.startsWith('Bearer ')) {
    response.status(401).json({ messages: ['Unauthorized'] });
    return;
  }

  const token = authHeader.slice('Bearer '.length);

  try {
    const decoded = verifyAccessToken(token);
    request.user = { userId: decoded.userId, email: decoded.email };
    next();
  } catch {
    response.status(401).json({ messages: ['Invalid or expired token'] });
  }
};

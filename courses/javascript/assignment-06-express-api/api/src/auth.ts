import bcrypt from 'bcrypt';
import jwt from 'jsonwebtoken';
import { randomBytes } from 'crypto';

const JWT_SECRET = process.env.JWT_SECRET;

if (!JWT_SECRET) {
  throw new Error('JWT_SECRET is not configured');
}

export async function hashPassword(password: string): Promise<string> {
  return bcrypt.hash(password, 10);
}

export async function comparePassword(password: string, hashedPassword: string): Promise<boolean> {
  return bcrypt.compare(password, hashedPassword);
}

export function generateTokens(payload: {
  userId: string;
  email: string;
}): { token: string; refreshToken: string } {
  const token = jwt.sign(payload, JWT_SECRET!, { expiresIn: '15m' });
  const refreshToken = randomBytes(32).toString('base64url');

  return { token, refreshToken };
}

export function verifyAccessToken(token: string): { userId: string; email: string } {
  const decoded = jwt.verify(token, JWT_SECRET!) as { userId: string; email: string };
  return { userId: decoded.userId, email: decoded.email };
}

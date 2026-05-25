import { Router, Request, Response, NextFunction } from 'express';
import rateLimit from 'express-rate-limit';
import type { QueryResult, QueryResultRow } from 'pg';
import { getPool } from '../db/database.js';
import { generateTokens, hashPassword, comparePassword } from '../auth.js';
import { ILoginData, IRegisterData, IRefreshTokenModel, IJwtResponse, IApiMessage } from '../types/index.js';

const router = Router();

interface QueryExecutor {
  query<T extends QueryResultRow = QueryResultRow>(text: string, values?: unknown[]): Promise<QueryResult<T>>;
}

const authRateLimiter = rateLimit({
  windowMs: 15 * 60 * 1000,
  limit: 20,
  standardHeaders: true,
  legacyHeaders: false,
  message: { messages: ['Too many authentication attempts, please try again later'] } as IApiMessage
});

async function storeRefreshToken(userId: string, token: string, executor: QueryExecutor): Promise<void> {
  await executor.query(
    "INSERT INTO refresh_tokens (user_id, token, expires_at) VALUES ($1, $2, NOW() + INTERVAL '7 days')",
    [userId, token]
  );
}

router.post('/Register', authRateLimiter, async (req: Request, res: Response, next: NextFunction) => {
  try {
    const { email, password, firstName, lastName } = req.body as IRegisterData;

    const errors: string[] = [];
    if (!email) errors.push('Email is required');
    if (!password) errors.push('Password is required');
    if (password && password.length < 8) errors.push('Password must be at least 8 characters');
    if (!firstName) errors.push('First name is required');
    if (!lastName) errors.push('Last name is required');

    if (errors.length > 0) {
      return res.status(400).json({ messages: errors } as IApiMessage);
    }

    const hashedPassword = await hashPassword(password);
    const pool = getPool();

    let userId: string;
    try {
      const result = await pool.query<{ id: string }>(
        'INSERT INTO users (email, password_hash, first_name, last_name) VALUES ($1, $2, $3, $4) RETURNING id',
        [email, hashedPassword, firstName, lastName]
      );
      userId = result.rows[0].id;
    } catch (error: unknown) {
      // Unique violation (code 23505) on email
      if (error && typeof error === 'object' && 'code' in error && (error as { code: string }).code === '23505') {
        return res.status(400).json({ messages: ['Email already registered'] } as IApiMessage);
      }
      throw error;
    }

    const { token, refreshToken } = generateTokens({ userId, email });
    await storeRefreshToken(userId, refreshToken, pool);

    res.status(200).json({
      token,
      refreshToken,
      firstName,
      lastName,
    } as IJwtResponse);
  } catch (error) {
    next(error);
  }
});

router.post('/Login', authRateLimiter, async (req: Request, res: Response, next: NextFunction) => {
  try {
    const { email, password } = req.body as ILoginData;

    const pool = getPool();

    const result = await pool.query<{
      id: string;
      password_hash: string;
      first_name: string;
      last_name: string;
    }>(
      'SELECT id, password_hash, first_name, last_name FROM users WHERE email = $1',
      [email]
    );

    // 404 on miss — no user enumeration
    if (result.rows.length === 0) {
      return res.status(404).json({ messages: ['Invalid email or password'] } as IApiMessage);
    }

    const user = result.rows[0];

    const passwordMatch = await comparePassword(password, user.password_hash);
    if (!passwordMatch) {
      return res.status(404).json({ messages: ['Invalid email or password'] } as IApiMessage);
    }

    const { token, refreshToken } = generateTokens({ userId: user.id, email });
    await storeRefreshToken(user.id, refreshToken, pool);

    res.status(200).json({
      token,
      refreshToken,
      firstName: user.first_name,
      lastName: user.last_name,
    } as IJwtResponse);
  } catch (error) {
    next(error);
  }
});

router.post('/RefreshToken', async (req: Request, res: Response, next: NextFunction) => {
  const pool = getPool();
  const client = await pool.connect();

  try {
    const { refreshToken } = req.body as IRefreshTokenModel;

    await client.query('BEGIN');

    const result = await client.query<{ user_id: string }>(
      'DELETE FROM refresh_tokens WHERE token = $1 AND expires_at > NOW() RETURNING user_id',
      [refreshToken]
    );

    if (result.rows.length === 0) {
      await client.query('ROLLBACK');
      return res.status(400).json({ messages: ['Invalid or expired refresh token'] } as IApiMessage);
    }

    const userId = result.rows[0].user_id;

    const userResult = await client.query<{
      email: string;
      first_name: string;
      last_name: string;
    }>(
      'SELECT email, first_name, last_name FROM users WHERE id = $1',
      [userId]
    );

    if (userResult.rows.length === 0) {
      await client.query('ROLLBACK');
      return res.status(400).json({ messages: ['Invalid or expired refresh token'] } as IApiMessage);
    }

    const user = userResult.rows[0];

    const { token: newToken, refreshToken: newRefreshToken } = generateTokens({ userId, email: user.email });
    await storeRefreshToken(userId, newRefreshToken, client);
    await client.query('COMMIT');

    res.status(200).json({
      token: newToken,
      refreshToken: newRefreshToken,
      firstName: user.first_name,
      lastName: user.last_name,
    } as IJwtResponse);
  } catch (error) {
    await client.query('ROLLBACK').catch(() => undefined);
    next(error);
  } finally {
    client.release();
  }
});

export default router;

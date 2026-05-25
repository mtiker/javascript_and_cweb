import { randomUUID } from 'crypto';
import type { Express } from 'express';
import { DataType, newDb } from 'pg-mem';
import request from 'supertest';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

interface TestContext {
  app: Express;
  closePool: () => Promise<void>;
}

interface AuthResponse {
  token: string;
  refreshToken: string;
  firstName: string;
  lastName: string;
}

interface TodoCategoryResponse {
  id: string;
  categoryName: string;
  categorySort: number;
  syncDt: string;
  tag: string | null;
}

interface TodoPriorityResponse {
  id: string;
  priorityName: string;
  prioritySort: number;
  syncDt: string;
}

interface TodoTaskResponse {
  id: string;
  taskName: string;
  taskSort: number;
  createdDt: string;
  dueDt: string | null;
  isCompleted: boolean;
  isArchived: boolean;
  todoCategoryId: string;
  todoPriorityId: string;
  syncDt: string;
}

let context: TestContext;

async function createTestContext(): Promise<TestContext> {
  vi.resetModules();

  process.env.JWT_SECRET = 'test-secret-at-least-32-characters-long';
  process.env.DATABASE_URL = 'postgresql://test:test@localhost:5432/test';
  process.env.CORS_ORIGIN = 'http://localhost:5173';

  const db = newDb({ autoCreateForeignKeyIndices: true });
  db.public.registerFunction({
    name: 'gen_random_uuid',
    returns: DataType.uuid,
    impure: true,
    implementation: () => randomUUID()
  });
  db.public.registerFunction({
    name: 'char_length',
    args: [DataType.text],
    returns: DataType.integer,
    implementation: (value: string) => value.length
  });

  const { Pool } = db.adapters.createPg();
  vi.doMock('pg', () => ({ Pool }));

  const { default: app } = await import('../src/app.js');
  const { closePool, initializeDatabase } = await import('../src/db/database.js');
  await initializeDatabase();

  return { app, closePool };
}

async function registerUser(app: Express, suffix: string): Promise<AuthResponse> {
  const response = await request(app)
    .post('/api/v1/Account/Register')
    .send({
      email: `user-${suffix}@example.com`,
      password: 'Password1!',
      firstName: `First${suffix}`,
      lastName: `Last${suffix}`
    });

  expect(response.status).toBe(200);
  expect(response.body.token).toEqual(expect.any(String));
  expect(response.body.refreshToken).toEqual(expect.any(String));

  return response.body as AuthResponse;
}

async function createCategory(app: Express, token: string): Promise<TodoCategoryResponse> {
  const response = await request(app)
    .post('/api/v1/TodoCategories')
    .set('Authorization', `Bearer ${token}`)
    .send({ categoryName: 'Work', categorySort: 1, tag: 'work' });

  expect(response.status).toBe(201);
  return response.body as TodoCategoryResponse;
}

async function createPriority(app: Express, token: string): Promise<TodoPriorityResponse> {
  const response = await request(app)
    .post('/api/v1/TodoPriorities')
    .set('Authorization', `Bearer ${token}`)
    .send({ priorityName: 'High', prioritySort: 1, syncDt: new Date().toISOString() });

  expect(response.status).toBe(200);
  return response.body as TodoPriorityResponse;
}

async function createTask(app: Express, token: string): Promise<TodoTaskResponse> {
  const category = await createCategory(app, token);
  const priority = await createPriority(app, token);

  const response = await request(app)
    .post('/api/v1/TodoTasks')
    .set('Authorization', `Bearer ${token}`)
    .send({
      taskName: 'Prepare defense notes',
      taskSort: 1,
      createdDt: '2026-05-25T08:00:00.000Z',
      dueDt: null,
      isCompleted: false,
      isArchived: false,
      todoCategoryId: category.id,
      todoPriorityId: priority.id
    });

  expect(response.status).toBe(200);
  return response.body as TodoTaskResponse;
}

beforeEach(async () => {
  context = await createTestContext();
});

afterEach(async () => {
  await context?.closePool();
  vi.resetModules();
  vi.restoreAllMocks();
});

describe('Account API', () => {
  it('registers, logs in, and rejects refresh token reuse', async () => {
    const registered = await registerUser(context.app, 'auth');

    const loginResponse = await request(context.app)
      .post('/api/v1/Account/Login')
      .send({ email: 'user-auth@example.com', password: 'Password1!' });

    expect(loginResponse.status).toBe(200);
    const loggedIn = loginResponse.body as AuthResponse;

    const firstRefresh = await request(context.app)
      .post('/api/v1/Account/RefreshToken')
      .send({ jwt: registered.token, refreshToken: loggedIn.refreshToken });

    expect(firstRefresh.status).toBe(200);
    expect(firstRefresh.body.refreshToken).not.toBe(loggedIn.refreshToken);

    const reusedRefresh = await request(context.app)
      .post('/api/v1/Account/RefreshToken')
      .send({ jwt: registered.token, refreshToken: loggedIn.refreshToken });

    expect(reusedRefresh.status).toBe(400);
    expect(reusedRefresh.body.messages).toContain('Invalid or expired refresh token');
  });

  it('allows only one concurrent refresh for a single refresh token', async () => {
    const auth = await registerUser(context.app, 'parallel-refresh');

    const refreshRequests = await Promise.all([
      request(context.app)
        .post('/api/v1/Account/RefreshToken')
        .send({ jwt: auth.token, refreshToken: auth.refreshToken }),
      request(context.app)
        .post('/api/v1/Account/RefreshToken')
        .send({ jwt: auth.token, refreshToken: auth.refreshToken })
    ]);

    expect(refreshRequests.map((response) => response.status).sort()).toEqual([200, 400]);
  });
});

describe('TodoTasks API', () => {
  it('requires authentication and prevents cross-user task reads', async () => {
    const owner = await registerUser(context.app, 'owner');
    const otherUser = await registerUser(context.app, 'other');
    const task = await createTask(context.app, owner.token);

    const anonymousList = await request(context.app).get('/api/v1/TodoTasks');
    expect(anonymousList.status).toBe(401);

    const ownerRead = await request(context.app)
      .get(`/api/v1/TodoTasks/${task.id}`)
      .set('Authorization', `Bearer ${owner.token}`);
    expect(ownerRead.status).toBe(200);
    expect(ownerRead.body.taskName).toBe('Prepare defense notes');

    const crossUserRead = await request(context.app)
      .get(`/api/v1/TodoTasks/${task.id}`)
      .set('Authorization', `Bearer ${otherUser.token}`);
    expect(crossUserRead.status).toBe(404);
  });

  it('covers task create, list, update, and delete flow', async () => {
    const auth = await registerUser(context.app, 'crud');
    const task = await createTask(context.app, auth.token);

    const listResponse = await request(context.app)
      .get('/api/v1/TodoTasks')
      .set('Authorization', `Bearer ${auth.token}`);
    expect(listResponse.status).toBe(200);
    expect(listResponse.body).toHaveLength(1);

    const emptyUpdate = await request(context.app)
      .put(`/api/v1/TodoTasks/${task.id}`)
      .set('Authorization', `Bearer ${auth.token}`)
      .send({});
    expect(emptyUpdate.status).toBe(400);

    const updateResponse = await request(context.app)
      .put(`/api/v1/TodoTasks/${task.id}`)
      .set('Authorization', `Bearer ${auth.token}`)
      .send({ taskName: 'Updated defense notes', isCompleted: true });
    expect(updateResponse.status).toBe(200);
    expect(updateResponse.body.taskName).toBe('Updated defense notes');
    expect(updateResponse.body.isCompleted).toBe(true);

    const deleteResponse = await request(context.app)
      .delete(`/api/v1/TodoTasks/${task.id}`)
      .set('Authorization', `Bearer ${auth.token}`);
    expect(deleteResponse.status).toBe(200);

    const readDeleted = await request(context.app)
      .get(`/api/v1/TodoTasks/${task.id}`)
      .set('Authorization', `Bearer ${auth.token}`);
    expect(readDeleted.status).toBe(404);
  });
});

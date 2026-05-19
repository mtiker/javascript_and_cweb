import { getPool } from './database.js';
import { ITodoTask, ICreateTaskDTO } from '../types/index.js';

interface TaskRow {
  id: string;
  task_name: string;
  task_sort: number;
  created_dt: Date;
  due_dt: Date | null;
  is_completed: boolean;
  is_archived: boolean;
  todo_category_id: string;
  todo_priority_id: string;
  sync_dt: Date;
}

class TaskRepository {
  async findAll(userId: string): Promise<ITodoTask[]> {
    const pool = getPool();
    const result = await pool.query<TaskRow>(
      'SELECT id, task_name, task_sort, created_dt, due_dt, is_completed, is_archived, todo_category_id, todo_priority_id, sync_dt FROM todo_tasks WHERE user_id = $1 ORDER BY task_sort ASC',
      [userId]
    );

    return result.rows.map((row) => this.mapRowToTask(row));
  }

  async findById(id: string, userId: string): Promise<ITodoTask | null> {
    const pool = getPool();
    const result = await pool.query<TaskRow>(
      'SELECT id, task_name, task_sort, created_dt, due_dt, is_completed, is_archived, todo_category_id, todo_priority_id, sync_dt FROM todo_tasks WHERE id = $1 AND user_id = $2',
      [id, userId]
    );

    if (result.rows.length === 0) return null;
    return this.mapRowToTask(result.rows[0]);
  }

  async create(dto: ICreateTaskDTO, userId: string): Promise<ITodoTask> {
    const pool = getPool();
    const result = await pool.query<TaskRow>(
      'INSERT INTO todo_tasks (task_name, task_sort, created_dt, due_dt, is_completed, is_archived, todo_category_id, todo_priority_id, user_id, sync_dt) VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9, NOW()) RETURNING id, task_name, task_sort, created_dt, due_dt, is_completed, is_archived, todo_category_id, todo_priority_id, sync_dt',
      [dto.taskName, dto.taskSort, dto.createdDt, dto.dueDt ?? null, dto.isCompleted, dto.isArchived, dto.todoCategoryId, dto.todoPriorityId, userId]
    );

    return this.mapRowToTask(result.rows[0]);
  }

  async update(id: string, dto: Partial<ICreateTaskDTO>, userId: string): Promise<ITodoTask | null> {
    const pool = getPool();
    const fields: string[] = ['sync_dt = NOW()'];
    const params: unknown[] = [];
    let paramIndex = 1;

    if (dto.taskName !== undefined) {
      fields.push(`task_name = $${paramIndex}`);
      params.push(dto.taskName);
      paramIndex++;
    }
    if (dto.taskSort !== undefined) {
      fields.push(`task_sort = $${paramIndex}`);
      params.push(dto.taskSort);
      paramIndex++;
    }
    if (dto.createdDt !== undefined) {
      fields.push(`created_dt = $${paramIndex}`);
      params.push(dto.createdDt);
      paramIndex++;
    }
    if (dto.dueDt !== undefined) {
      fields.push(`due_dt = $${paramIndex}`);
      params.push(dto.dueDt ?? null);
      paramIndex++;
    }
    if (dto.isCompleted !== undefined) {
      fields.push(`is_completed = $${paramIndex}`);
      params.push(dto.isCompleted);
      paramIndex++;
    }
    if (dto.isArchived !== undefined) {
      fields.push(`is_archived = $${paramIndex}`);
      params.push(dto.isArchived);
      paramIndex++;
    }
    if (dto.todoCategoryId !== undefined) {
      fields.push(`todo_category_id = $${paramIndex}`);
      params.push(dto.todoCategoryId);
      paramIndex++;
    }
    if (dto.todoPriorityId !== undefined) {
      fields.push(`todo_priority_id = $${paramIndex}`);
      params.push(dto.todoPriorityId);
      paramIndex++;
    }

    params.push(id);
    params.push(userId);

    const query = `UPDATE todo_tasks SET ${fields.join(', ')} WHERE id = $${paramIndex} AND user_id = $${paramIndex + 1} RETURNING id, task_name, task_sort, created_dt, due_dt, is_completed, is_archived, todo_category_id, todo_priority_id, sync_dt`;
    const result = await pool.query<TaskRow>(query, params);

    if (result.rows.length === 0) return null;
    return this.mapRowToTask(result.rows[0]);
  }

  async delete(id: string, userId: string): Promise<boolean> {
    const pool = getPool();
    const result = await pool.query(
      'DELETE FROM todo_tasks WHERE id = $1 AND user_id = $2',
      [id, userId]
    );
    return result.rowCount ? result.rowCount > 0 : false;
  }

  private mapRowToTask(row: TaskRow): ITodoTask {
    return {
      id: row.id,
      taskName: row.task_name,
      taskSort: row.task_sort,
      createdDt: row.created_dt instanceof Date ? row.created_dt.toISOString() : String(row.created_dt),
      dueDt: row.due_dt ? (row.due_dt instanceof Date ? row.due_dt.toISOString() : String(row.due_dt)) : null,
      isCompleted: row.is_completed,
      isArchived: row.is_archived,
      todoCategoryId: row.todo_category_id,
      todoPriorityId: row.todo_priority_id,
      syncDt: row.sync_dt instanceof Date ? row.sync_dt.toISOString() : String(row.sync_dt),
    };
  }
}

export const taskRepository = new TaskRepository();

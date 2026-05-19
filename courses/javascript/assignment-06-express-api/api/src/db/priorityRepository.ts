import { getPool } from './database.js';
import { ITodoPriority, ICreatePriorityDTO } from '../types/index.js';

interface PriorityRow {
  id: string;
  priority_name: string;
  priority_sort: number;
  sync_dt: Date;
}

class PriorityRepository {
  async findAll(userId: string): Promise<ITodoPriority[]> {
    const pool = getPool();
    const result = await pool.query<PriorityRow>(
      'SELECT id, priority_name, priority_sort, sync_dt FROM todo_priorities WHERE user_id = $1 ORDER BY priority_sort ASC',
      [userId]
    );
    return result.rows.map((row) => this.mapRow(row));
  }

  async findById(id: string, userId: string): Promise<ITodoPriority | null> {
    const pool = getPool();
    const result = await pool.query<PriorityRow>(
      'SELECT id, priority_name, priority_sort, sync_dt FROM todo_priorities WHERE id = $1 AND user_id = $2',
      [id, userId]
    );
    if (result.rows.length === 0) return null;
    return this.mapRow(result.rows[0]);
  }

  async create(dto: ICreatePriorityDTO, userId: string): Promise<ITodoPriority> {
    const pool = getPool();
    const result = await pool.query<PriorityRow>(
      'INSERT INTO todo_priorities (priority_name, priority_sort, user_id, sync_dt) VALUES ($1, $2, $3, NOW()) RETURNING id, priority_name, priority_sort, sync_dt',
      [dto.priorityName, dto.prioritySort, userId]
    );
    return this.mapRow(result.rows[0]);
  }

  async update(id: string, dto: Partial<ICreatePriorityDTO>, userId: string): Promise<ITodoPriority | null> {
    const pool = getPool();
    const fields: string[] = ['sync_dt = NOW()'];
    const params: unknown[] = [];
    let paramIndex = 1;

    if (dto.priorityName !== undefined) {
      fields.push(`priority_name = $${paramIndex}`);
      params.push(dto.priorityName);
      paramIndex++;
    }
    if (dto.prioritySort !== undefined) {
      fields.push(`priority_sort = $${paramIndex}`);
      params.push(dto.prioritySort);
      paramIndex++;
    }

    params.push(id);
    params.push(userId);

    const query = `UPDATE todo_priorities SET ${fields.join(', ')} WHERE id = $${paramIndex} AND user_id = $${paramIndex + 1} RETURNING id, priority_name, priority_sort, sync_dt`;
    const result = await pool.query<PriorityRow>(query, params);

    if (result.rows.length === 0) return null;
    return this.mapRow(result.rows[0]);
  }

  async delete(id: string, userId: string): Promise<boolean> {
    const pool = getPool();
    const result = await pool.query(
      'DELETE FROM todo_priorities WHERE id = $1 AND user_id = $2',
      [id, userId]
    );
    return result.rowCount ? result.rowCount > 0 : false;
  }

  private mapRow(row: PriorityRow): ITodoPriority {
    return {
      id: row.id,
      priorityName: row.priority_name,
      prioritySort: row.priority_sort,
      syncDt: row.sync_dt instanceof Date ? row.sync_dt.toISOString() : String(row.sync_dt),
    };
  }
}

export const priorityRepository = new PriorityRepository();

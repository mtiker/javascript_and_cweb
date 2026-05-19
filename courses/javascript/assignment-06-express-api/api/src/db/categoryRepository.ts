import { getPool } from './database.js';
import { ITodoCategory, ICreateCategoryDTO } from '../types/index.js';

interface CategoryRow {
  id: string;
  category_name: string;
  category_sort: number;
  sync_dt: Date;
  tag: string | null;
}

class CategoryRepository {
  async findAll(userId: string): Promise<ITodoCategory[]> {
    const pool = getPool();
    const result = await pool.query<CategoryRow>(
      'SELECT id, category_name, category_sort, sync_dt, tag FROM todo_categories WHERE user_id = $1 ORDER BY category_sort ASC',
      [userId]
    );
    return result.rows.map((row) => this.mapRow(row));
  }

  async findById(id: string, userId: string): Promise<ITodoCategory | null> {
    const pool = getPool();
    const result = await pool.query<CategoryRow>(
      'SELECT id, category_name, category_sort, sync_dt, tag FROM todo_categories WHERE id = $1 AND user_id = $2',
      [id, userId]
    );
    if (result.rows.length === 0) return null;
    return this.mapRow(result.rows[0]);
  }

  async create(dto: ICreateCategoryDTO, userId: string): Promise<ITodoCategory> {
    const pool = getPool();
    const result = await pool.query<CategoryRow>(
      'INSERT INTO todo_categories (category_name, category_sort, tag, user_id, sync_dt) VALUES ($1, $2, $3, $4, NOW()) RETURNING id, category_name, category_sort, sync_dt, tag',
      [dto.categoryName, dto.categorySort, dto.tag ?? null, userId]
    );
    return this.mapRow(result.rows[0]);
  }

  async update(id: string, dto: Partial<ICreateCategoryDTO>, userId: string): Promise<ITodoCategory | null> {
    const pool = getPool();
    const fields: string[] = ['sync_dt = NOW()'];
    const params: unknown[] = [];
    let paramIndex = 1;

    if (dto.categoryName !== undefined) {
      fields.push(`category_name = $${paramIndex}`);
      params.push(dto.categoryName);
      paramIndex++;
    }
    if (dto.categorySort !== undefined) {
      fields.push(`category_sort = $${paramIndex}`);
      params.push(dto.categorySort);
      paramIndex++;
    }
    if (dto.tag !== undefined) {
      fields.push(`tag = $${paramIndex}`);
      params.push(dto.tag ?? null);
      paramIndex++;
    }

    params.push(id);
    params.push(userId);

    const query = `UPDATE todo_categories SET ${fields.join(', ')} WHERE id = $${paramIndex} AND user_id = $${paramIndex + 1} RETURNING id, category_name, category_sort, sync_dt, tag`;
    const result = await pool.query<CategoryRow>(query, params);

    if (result.rows.length === 0) return null;
    return this.mapRow(result.rows[0]);
  }

  async delete(id: string, userId: string): Promise<boolean> {
    const pool = getPool();
    const result = await pool.query(
      'DELETE FROM todo_categories WHERE id = $1 AND user_id = $2',
      [id, userId]
    );
    return result.rowCount ? result.rowCount > 0 : false;
  }

  private mapRow(row: CategoryRow): ITodoCategory {
    return {
      id: row.id,
      categoryName: row.category_name,
      categorySort: row.category_sort,
      syncDt: row.sync_dt instanceof Date ? row.sync_dt.toISOString() : String(row.sync_dt),
      tag: row.tag,
    };
  }
}

export const categoryRepository = new CategoryRepository();

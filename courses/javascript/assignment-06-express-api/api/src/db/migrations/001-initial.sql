-- Users (internal only — never exposed via API)
CREATE TABLE IF NOT EXISTS users (
  id            UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
  email         TEXT        UNIQUE NOT NULL,
  password_hash TEXT        NOT NULL,
  first_name    TEXT        NOT NULL,
  last_name     TEXT        NOT NULL,
  created_at    TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- TodoCategories
CREATE TABLE IF NOT EXISTS todo_categories (
  id            UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
  category_name TEXT        NOT NULL CHECK (char_length(category_name) <= 128),
  category_sort INTEGER     NOT NULL DEFAULT 0,
  sync_dt       TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  tag           TEXT,
  user_id       UUID        NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  created_at    TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- TodoPriorities
CREATE TABLE IF NOT EXISTS todo_priorities (
  id            UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
  priority_name TEXT        NOT NULL CHECK (char_length(priority_name) <= 128),
  priority_sort INTEGER     NOT NULL DEFAULT 0,
  sync_dt       TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  user_id       UUID        NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  created_at    TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- TodoTasks
CREATE TABLE IF NOT EXISTS todo_tasks (
  id               UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
  task_name        TEXT        NOT NULL CHECK (char_length(task_name) <= 128),
  task_sort        INTEGER     NOT NULL DEFAULT 0,
  created_dt       TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  due_dt           TIMESTAMPTZ,
  is_completed     BOOLEAN     NOT NULL DEFAULT FALSE,
  is_archived      BOOLEAN     NOT NULL DEFAULT FALSE,
  todo_category_id UUID        REFERENCES todo_categories(id) ON DELETE SET NULL,
  todo_priority_id UUID        REFERENCES todo_priorities(id) ON DELETE SET NULL,
  sync_dt          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  user_id          UUID        NOT NULL REFERENCES users(id) ON DELETE CASCADE
);

-- Refresh tokens
CREATE TABLE IF NOT EXISTS refresh_tokens (
  id         UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id    UUID        NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  token      TEXT        UNIQUE NOT NULL,
  expires_at TIMESTAMPTZ NOT NULL,
  created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

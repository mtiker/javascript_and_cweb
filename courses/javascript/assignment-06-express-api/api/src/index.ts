import 'dotenv/config';
import app from './app.js';
import { initializeDatabase, closePool } from './db/database.js';

async function start() {
  try {
    await initializeDatabase();

    const port = process.env.PORT ?? 3001;
    app.listen(port, () => {
      console.log(`Express API listening on port ${port}`);
    });
  } catch (error) {
    console.error('Failed to start server:', error);
    process.exit(1);
  }
}

process.on('SIGTERM', async () => {
  console.log('SIGTERM received, shutting down gracefully...');
  await closePool();
  process.exit(0);
});

process.on('SIGINT', async () => {
  console.log('SIGINT received, shutting down gracefully...');
  await closePool();
  process.exit(0);
});

start();

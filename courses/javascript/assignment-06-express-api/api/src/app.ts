import cors from 'cors';
import express from 'express';
import accountRouter from './routes/account.js';
import categoriesRouter from './routes/categories.js';
import prioritiesRouter from './routes/priorities.js';
import tasksRouter from './routes/tasks.js';
import { errorHandler } from './middleware/errorHandler.js';

const app = express();

app.use(
  cors({
    origin: process.env.CORS_ORIGIN?.split(',') ?? true,
    credentials: true
  })
);
app.use(express.json());
app.use(express.static('public'));

app.use('/api/v1/Account', accountRouter);
app.use('/api/v1/TodoTasks', tasksRouter);
app.use('/api/v1/TodoCategories', categoriesRouter);
app.use('/api/v1/TodoPriorities', prioritiesRouter);

app.get('/api/v1/health', (_request, response) => {
  response.json({ status: 'ok', db: 'postgres' });
});

app.use(errorHandler);

export default app;

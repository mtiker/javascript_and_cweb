import cors from 'cors';
import express from 'express';
import helmet from 'helmet';
import accountRouter from './routes/account.js';
import categoriesRouter from './routes/categories.js';
import prioritiesRouter from './routes/priorities.js';
import tasksRouter from './routes/tasks.js';
import { errorHandler } from './middleware/errorHandler.js';

const app = express();
const corsOrigins = process.env.CORS_ORIGIN
  ?.split(',')
  .map((origin) => origin.trim())
  .filter((origin) => origin.length > 0);

app.set('trust proxy', 1);
app.use(helmet({ contentSecurityPolicy: false }));
app.use(
  cors({
    origin: corsOrigins && corsOrigins.length > 0 ? corsOrigins : false,
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

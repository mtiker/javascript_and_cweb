import { Router, Request, Response, NextFunction } from 'express';
import { authenticate } from '../middleware/authenticate.js';
import { taskRepository } from '../db/taskRepository.js';
import { ITodoTask, ICreateTaskDTO, IApiMessage } from '../types/index.js';

const router = Router();

router.use(authenticate);

router.get('/', async (req: Request, res: Response, next: NextFunction) => {
  try {
    const userId = req.user!.userId;
    const items = await taskRepository.findAll(userId);
    res.status(200).json(items as ITodoTask[]);
  } catch (error) {
    next(error);
  }
});

router.get('/:id', async (req: Request, res: Response, next: NextFunction) => {
  try {
    const userId = req.user!.userId;
    const id = req.params.id as string;
    const item = await taskRepository.findById(id, userId);
    if (!item) return res.status(404).json({ messages: ['Not found'] } as IApiMessage);
    res.status(200).json(item as ITodoTask);
  } catch (error) {
    next(error);
  }
});

router.post('/', async (req: Request, res: Response, next: NextFunction) => {
  try {
    const userId = req.user!.userId;
    const body = req.body as ICreateTaskDTO;
    const created = await taskRepository.create(body, userId);
    res.status(200).json(created as ITodoTask);
  } catch (error) {
    next(error);
  }
});

router.put('/:id', async (req: Request, res: Response, next: NextFunction) => {
  try {
    const userId = req.user!.userId;
    const id = req.params.id as string;
    const body = req.body as Partial<ICreateTaskDTO>;
    const updated = await taskRepository.update(id, body, userId);
    if (!updated) return res.status(404).json({ messages: ['Not found'] } as IApiMessage);
    res.status(200).json(updated as ITodoTask);
  } catch (error) {
    next(error);
  }
});

router.delete('/:id', async (req: Request, res: Response, next: NextFunction) => {
  try {
    const userId = req.user!.userId;
    const id = req.params.id as string;
    const deleted = await taskRepository.delete(id, userId);
    if (!deleted) return res.status(404).json({ messages: ['Not found'] } as IApiMessage);
    res.status(200).end();
  } catch (error) {
    next(error);
  }
});

export default router;

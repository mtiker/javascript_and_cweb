import { Router, Request, Response, NextFunction } from 'express';
import { authenticate } from '../middleware/authenticate.js';
import { priorityRepository } from '../db/priorityRepository.js';
import { ITodoPriority, ICreatePriorityDTO, IApiMessage } from '../types/index.js';

const router = Router();

router.use(authenticate);

router.get('/', async (req: Request, res: Response, next: NextFunction) => {
  try {
    const userId = req.user!.userId;
    const items = await priorityRepository.findAll(userId);
    res.status(200).json(items as ITodoPriority[]);
  } catch (error) {
    next(error);
  }
});

router.get('/:id', async (req: Request, res: Response, next: NextFunction) => {
  try {
    const userId = req.user!.userId;
    const id = req.params.id as string;
    const item = await priorityRepository.findById(id, userId);
    if (!item) return res.status(404).json({ messages: ['Not found'] } as IApiMessage);
    res.status(200).json(item as ITodoPriority);
  } catch (error) {
    next(error);
  }
});

router.post('/', async (req: Request, res: Response, next: NextFunction) => {
  try {
    const userId = req.user!.userId;
    const body = req.body as ICreatePriorityDTO;
    const created = await priorityRepository.create(body, userId);
    res.status(200).json(created as ITodoPriority);
  } catch (error) {
    next(error);
  }
});

router.put('/:id', async (req: Request, res: Response, next: NextFunction) => {
  try {
    const userId = req.user!.userId;
    const id = req.params.id as string;
    const body = req.body as Partial<ICreatePriorityDTO>;
    const updated = await priorityRepository.update(id, body, userId);
    if (!updated) return res.status(404).json({ messages: ['Not found'] } as IApiMessage);
    res.status(200).end();
  } catch (error) {
    next(error);
  }
});

router.delete('/:id', async (req: Request, res: Response, next: NextFunction) => {
  try {
    const userId = req.user!.userId;
    const id = req.params.id as string;
    const deleted = await priorityRepository.delete(id, userId);
    if (!deleted) return res.status(404).json({ messages: ['Not found'] } as IApiMessage);
    res.status(200).end();
  } catch (error) {
    next(error);
  }
});

export default router;

import { Router, Request, Response, NextFunction } from 'express';
import { authenticate } from '../middleware/authenticate.js';
import { categoryRepository } from '../db/categoryRepository.js';
import { ITodoCategory, ICreateCategoryDTO, IApiMessage } from '../types/index.js';

const router = Router();

router.use(authenticate);

router.get('/', async (req: Request, res: Response, next: NextFunction) => {
  try {
    const userId = req.user!.userId;
    const items = await categoryRepository.findAll(userId);
    res.status(200).json(items as ITodoCategory[]);
  } catch (error) {
    next(error);
  }
});

router.get('/:id', async (req: Request, res: Response, next: NextFunction) => {
  try {
    const userId = req.user!.userId;
    const id = req.params.id as string;
    const item = await categoryRepository.findById(id, userId);
    if (!item) return res.status(404).json({ messages: ['Not found'] } as IApiMessage);
    res.status(200).json(item as ITodoCategory);
  } catch (error) {
    next(error);
  }
});

router.post('/', async (req: Request, res: Response, next: NextFunction) => {
  try {
    const userId = req.user!.userId;
    const body = req.body as ICreateCategoryDTO;
    const created = await categoryRepository.create(body, userId);
    res.status(201).json(created as ITodoCategory);
  } catch (error) {
    next(error);
  }
});

router.put('/:id', async (req: Request, res: Response, next: NextFunction) => {
  try {
    const userId = req.user!.userId;
    const id = req.params.id as string;
    const body = req.body as Partial<ICreateCategoryDTO> & { id?: string };

    if (body.id !== undefined && body.id !== id) {
      return res.status(400).json({ messages: ['ID in body must match URL parameter'] } as IApiMessage);
    }

    const updated = await categoryRepository.update(id, body, userId);
    if (!updated) return res.status(404).json({ messages: ['Not found'] } as IApiMessage);
    res.status(200).json(updated as ITodoCategory);
  } catch (error) {
    next(error);
  }
});

router.delete('/:id', async (req: Request, res: Response, next: NextFunction) => {
  try {
    const userId = req.user!.userId;
    const id = req.params.id as string;
    const deleted = await categoryRepository.delete(id, userId);
    if (!deleted) return res.status(404).json({ messages: ['Not found'] } as IApiMessage);
    res.status(204).end();
  } catch (error) {
    next(error);
  }
});

export default router;

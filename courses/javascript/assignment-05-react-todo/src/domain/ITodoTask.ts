export interface ITodoTask {
  readonly id: string;
  taskName?: string | null;
  taskSort: number;
  createdDt: string;
  dueDt?: string | null;
  isCompleted: boolean;
  isArchived: boolean;
  todoCategoryId: string;
  todoPriorityId: string;
  syncDt: string;
}

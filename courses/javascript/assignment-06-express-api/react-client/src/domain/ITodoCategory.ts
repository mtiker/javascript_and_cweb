export interface ITodoCategory {
  readonly id: string;
  categoryName?: string | null;
  categorySort: number;
  syncDt: string;
  tag?: string | null;
}

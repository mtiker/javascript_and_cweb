export interface ITodoPriority {
  readonly id: string;
  priorityName?: string | null;
  prioritySort: number;
  syncDt: string;
}

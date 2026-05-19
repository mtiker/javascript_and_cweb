import type { ITodoPriority } from "@/domain";
import apiClient from "./apiClient";

export const TodoPriorityService = {
  getPriorities: async (): Promise<ITodoPriority[]> => {
    const { data } = await apiClient.get<ITodoPriority[]>("/api/v1/TodoPriorities");
    return data;
  },

  createPriority: async (
    priorityName: string,
    prioritySort: number,
  ): Promise<ITodoPriority> => {
    const { data } = await apiClient.post<ITodoPriority>("/api/v1/TodoPriorities", {
      priorityName,
      prioritySort,
      syncDt: new Date().toISOString(),
    });
    return data;
  },

  updatePriority: async (priority: ITodoPriority): Promise<void> => {
    await apiClient.put(`/api/v1/TodoPriorities/${priority.id}`, {
      id: priority.id,
      priorityName: priority.priorityName,
      prioritySort: priority.prioritySort,
      syncDt: priority.syncDt,
    });
  },

  deletePriority: async (id: string): Promise<void> => {
    await apiClient.delete(`/api/v1/TodoPriorities/${id}`);
  },
};

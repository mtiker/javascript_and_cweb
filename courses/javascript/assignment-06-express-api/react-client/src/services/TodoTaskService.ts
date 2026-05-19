import type { ITodoTask } from "@/domain";
import apiClient from "./apiClient";

type TodoTaskCreate = Omit<ITodoTask, "id">;

function toTaskPayload(todo: TodoTaskCreate): TodoTaskCreate {
  return {
    taskName: todo.taskName ?? "",
    taskSort: todo.taskSort,
    createdDt: todo.createdDt,
    dueDt: todo.dueDt ?? null,
    isCompleted: todo.isCompleted,
    isArchived: todo.isArchived,
    todoCategoryId: todo.todoCategoryId,
    todoPriorityId: todo.todoPriorityId,
    syncDt: todo.syncDt,
  };
}

export const TodoTaskService = {
  getTasks: async (): Promise<ITodoTask[]> => {
    const { data } = await apiClient.get<ITodoTask[]>("/api/v1/TodoTasks");
    return data;
  },

  getTask: async (id: string): Promise<ITodoTask> => {
    const { data } = await apiClient.get<ITodoTask>(`/api/v1/TodoTasks/${id}`);
    return data;
  },

  createTask: async (todo: TodoTaskCreate): Promise<ITodoTask> => {
    const { data } = await apiClient.post<ITodoTask>(
      "/api/v1/TodoTasks",
      toTaskPayload(todo),
    );
    return data;
  },

  updateTask: async (todo: ITodoTask): Promise<ITodoTask> => {
    const { data } = await apiClient.put<ITodoTask>(
      `/api/v1/TodoTasks/${todo.id}`,
      { id: todo.id, ...toTaskPayload(todo) },
    );
    return data;
  },

  deleteTask: async (id: string): Promise<void> => {
    await apiClient.delete(`/api/v1/TodoTasks/${id}`);
  },
};

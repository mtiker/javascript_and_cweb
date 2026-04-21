import { apiClient } from "@/api/http";
import { mapTodoTaskDto, toTaskCreateDto, toTaskUpdateDto } from "@/lib/todo-mappers";
import type { RawTodoTaskDto, TodoTaskDraft, TodoTaskEntity } from "@/types/todo";

export async function listTasks() {
  const response = await apiClient.get<RawTodoTaskDto[]>("/TodoTasks");
  return response.data.map(mapTodoTaskDto);
}

export async function createTask(draft: TodoTaskDraft) {
  const response = await apiClient.post<RawTodoTaskDto>("/TodoTasks", toTaskCreateDto(draft));
  return mapTodoTaskDto(response.data);
}

export async function updateTask(task: TodoTaskEntity, draft: TodoTaskDraft) {
  const response = await apiClient.put<RawTodoTaskDto>(
    `/TodoTasks/${task.id}`,
    toTaskUpdateDto(task, draft),
  );
  return mapTodoTaskDto(response.data);
}

export async function deleteTask(id: string) {
  await apiClient.delete(`/TodoTasks/${id}`);
}

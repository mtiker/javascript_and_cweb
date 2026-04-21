import { apiClient } from "@/api/http";
import {
  mapTodoCategoryDto,
  mapTodoPriorityDto,
  toCategoryCreateDto,
  toCategoryUpdateDto,
  toPriorityCreateDto,
  toPriorityUpdateDto,
} from "@/lib/todo-mappers";
import type {
  RawTodoCategoryDto,
  RawTodoPriorityDto,
  TodoCategoryDraft,
  TodoCategoryEntity,
  TodoPriorityDraft,
  TodoPriorityEntity,
} from "@/types/todo";

export async function listCategories() {
  const response = await apiClient.get<RawTodoCategoryDto[]>("/TodoCategories");
  return response.data.map(mapTodoCategoryDto);
}

export async function createCategory(draft: TodoCategoryDraft) {
  const response = await apiClient.post<RawTodoCategoryDto>(
    "/TodoCategories",
    toCategoryCreateDto(draft),
  );
  return mapTodoCategoryDto(response.data);
}

export async function updateCategory(category: TodoCategoryEntity, draft: TodoCategoryDraft) {
  const response = await apiClient.put<RawTodoCategoryDto>(
    `/TodoCategories/${category.id}`,
    toCategoryUpdateDto(category, draft),
  );
  return mapTodoCategoryDto(response.data);
}

export async function deleteCategory(id: string) {
  await apiClient.delete(`/TodoCategories/${id}`);
}

export async function listPriorities() {
  const response = await apiClient.get<RawTodoPriorityDto[]>("/TodoPriorities");
  return response.data.map(mapTodoPriorityDto);
}

export async function createPriority(draft: TodoPriorityDraft) {
  const response = await apiClient.post<RawTodoPriorityDto>(
    "/TodoPriorities",
    toPriorityCreateDto(draft),
  );
  return mapTodoPriorityDto(response.data);
}

export async function updatePriority(priority: TodoPriorityEntity, draft: TodoPriorityDraft) {
  const response = await apiClient.put<RawTodoPriorityDto | undefined>(
    `/TodoPriorities/${priority.id}`,
    toPriorityUpdateDto(priority, draft),
  );

  if (response.data) {
    return mapTodoPriorityDto(response.data);
  }

  return {
    ...priority,
    name: draft.name.trim(),
    sortOrder: draft.sortOrder,
  };
}

export async function deletePriority(id: string) {
  await apiClient.delete(`/TodoPriorities/${id}`);
}

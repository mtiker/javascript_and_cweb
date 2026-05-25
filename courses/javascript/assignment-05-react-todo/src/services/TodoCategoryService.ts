import type { ITodoCategory } from "@/domain";
import apiClient from "./apiClient";

export const TodoCategoryService = {
  getCategories: async (): Promise<ITodoCategory[]> => {
    const { data } = await apiClient.get<ITodoCategory[]>("/api/v1/TodoCategories");
    return data;
  },

  createCategory: async (
    categoryName: string,
    categorySort: number,
  ): Promise<ITodoCategory> => {
    const { data } = await apiClient.post<ITodoCategory>("/api/v1/TodoCategories", {
      categoryName,
      categorySort,
      syncDt: new Date().toISOString(),
    });
    return data;
  },

  updateCategory: async (category: ITodoCategory): Promise<ITodoCategory> => {
    const { data } = await apiClient.put<ITodoCategory>(
      `/api/v1/TodoCategories/${category.id}`,
      {
        id: category.id,
        categoryName: category.categoryName,
        categorySort: category.categorySort,
        syncDt: category.syncDt,
      },
    );
    return data;
  },

  deleteCategory: async (id: string): Promise<void> => {
    await apiClient.delete(`/api/v1/TodoCategories/${id}`);
  },
};

import type { ITodoCategory } from "@/domain";
import apiClient from "./apiClient";

const MAX_INT_32 = 2_147_483_647;

function getSafeSortValue(): number {
  return Math.min(Math.floor(Date.now() / 1000), MAX_INT_32);
}

export const TodoCategoryService = {
  getCategories: async (): Promise<ITodoCategory[]> => {
    const { data } = await apiClient.get<ITodoCategory[]>("/api/v1/TodoCategories");
    return data;
  },

  createCategory: async (
    categoryName: string,
    categorySort?: number,
  ): Promise<ITodoCategory> => {
    const { data } = await apiClient.post<ITodoCategory>("/api/v1/TodoCategories", {
      categoryName,
      categorySort: categorySort ?? getSafeSortValue(),
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

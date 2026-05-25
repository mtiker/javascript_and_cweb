"use client";

import {
  createContext,
  useCallback,
  useContext,
  useReducer,
  type ReactNode,
} from "react";
import { TodoTaskService } from "@/services/TodoTaskService";
import { TodoCategoryService } from "@/services/TodoCategoryService";
import { TodoPriorityService } from "@/services/TodoPriorityService";
import { getErrorMessage } from "@/utils/errorUtils";
import type { ITodoPriority, ITodoTask } from "@/domain";
import {
  initialTodoState,
  todoReducer,
  type TodoState,
} from "@/reducers/todoReducer";

type TodoTaskCreate = Omit<ITodoTask, "id">;

interface TodoContextType {
  state: TodoState;
  fetchTodos: () => Promise<void>;
  fetchCategories: () => Promise<void>;
  fetchPriorities: () => Promise<void>;
  createTodo: (todo: TodoTaskCreate) => Promise<void>;
  updateTodo: (todo: ITodoTask) => Promise<void>;
  deleteTodo: (id: string) => Promise<void>;
  createCategory: (categoryName: string) => Promise<void>;
  deleteCategory: (id: string) => Promise<void>;
  updateCategory: (id: string, categoryName: string) => Promise<void>;
  createPriority: (priorityName: string, prioritySort: number) => Promise<void>;
  updatePriority: (priority: ITodoPriority) => Promise<void>;
  deletePriority: (id: string) => Promise<void>;
}

const TodoContext = createContext<TodoContextType | undefined>(undefined);

// Pick the next sort value as `max(existing) + 1`. Two items created in the
// same second therefore still get distinct, monotonically increasing sort
// values — a Date.now()-based default collides whenever the user clicks
// "Add" twice quickly.
function nextSortValue<T>(items: readonly T[], getter: (item: T) => number): number {
  if (items.length === 0) return 1;
  return items.reduce((max, item) => Math.max(max, getter(item)), 0) + 1;
}

interface TodoProviderProps {
  children: ReactNode;
}

export function TodoProvider({ children }: TodoProviderProps) {
  const [state, dispatch] = useReducer(todoReducer, initialTodoState);

  const fetchTodos = useCallback(async () => {
    dispatch({ type: "FETCH_START" });
    try {
      const items = await TodoTaskService.getTasks();
      dispatch({ type: "FETCH_TODOS_SUCCESS", payload: items });
    } catch (error) {
      dispatch({
        type: "SET_ERROR",
        payload: getErrorMessage(error, "Failed to fetch todos"),
      });
      throw error;
    }
  }, []);

  const fetchCategories = useCallback(async () => {
    dispatch({ type: "FETCH_START" });
    try {
      const categories = await TodoCategoryService.getCategories();
      dispatch({ type: "FETCH_CATEGORIES_SUCCESS", payload: categories });
    } catch (error) {
      dispatch({
        type: "SET_ERROR",
        payload: getErrorMessage(error, "Failed to fetch categories"),
      });
      throw error;
    }
  }, []);

  const fetchPriorities = useCallback(async () => {
    dispatch({ type: "FETCH_START" });
    try {
      const priorities = await TodoPriorityService.getPriorities();
      dispatch({ type: "FETCH_PRIORITIES_SUCCESS", payload: priorities });
    } catch (error) {
      dispatch({
        type: "SET_ERROR",
        payload: getErrorMessage(error, "Failed to fetch priorities"),
      });
      throw error;
    }
  }, []);

  const createTodo = useCallback(async (todo: TodoTaskCreate) => {
    try {
      const created = await TodoTaskService.createTask(todo);
      dispatch({ type: "ADD_TODO", payload: created });
    } catch (error) {
      dispatch({
        type: "SET_ERROR",
        payload: getErrorMessage(error, "Failed to create todo"),
      });
      throw error;
    }
  }, []);

  const updateTodo = useCallback(async (todo: ITodoTask) => {
    try {
      await TodoTaskService.updateTask(todo);
      dispatch({ type: "UPDATE_TODO", payload: todo });
    } catch (error) {
      dispatch({
        type: "SET_ERROR",
        payload: getErrorMessage(error, "Failed to update todo"),
      });
      throw error;
    }
  }, []);

  const deleteTodo = useCallback(async (id: string) => {
    try {
      await TodoTaskService.deleteTask(id);
      dispatch({ type: "DELETE_TODO", payload: id });
    } catch (error) {
      dispatch({
        type: "SET_ERROR",
        payload: getErrorMessage(error, "Failed to delete todo"),
      });
      throw error;
    }
  }, []);

  const createCategory = useCallback(
    async (categoryName: string) => {
      try {
        const categorySort = nextSortValue(
          state.categories,
          (c) => c.categorySort,
        );
        const created = await TodoCategoryService.createCategory(
          categoryName,
          categorySort,
        );
        dispatch({ type: "ADD_CATEGORY", payload: created });
      } catch (error) {
        dispatch({
          type: "SET_ERROR",
          payload: getErrorMessage(error, "Failed to create category"),
        });
        throw error;
      }
    },
    [state.categories],
  );

  const deleteCategory = useCallback(async (id: string) => {
    try {
      await TodoCategoryService.deleteCategory(id);
      dispatch({ type: "DELETE_CATEGORY", payload: id });
    } catch (error) {
      dispatch({
        type: "SET_ERROR",
        payload: getErrorMessage(error, "Failed to delete category"),
      });
      throw error;
    }
  }, []);

  const updateCategory = useCallback(
    async (id: string, categoryName: string) => {
      try {
        const existing = state.categories.find((c) => c.id === id);
        const categorySort =
          existing?.categorySort ??
          nextSortValue(state.categories, (c) => c.categorySort);
        const syncDt = existing?.syncDt ?? new Date().toISOString();

        const updated = await TodoCategoryService.updateCategory({
          id,
          categoryName,
          categorySort,
          syncDt,
        });

        dispatch({ type: "UPDATE_CATEGORY", payload: updated });
      } catch (error) {
        dispatch({
          type: "SET_ERROR",
          payload: getErrorMessage(error, "Failed to update category"),
        });
        throw error;
      }
    },
    [state.categories],
  );

  const createPriority = useCallback(
    async (priorityName: string, prioritySort: number) => {
      try {
        const created = await TodoPriorityService.createPriority(
          priorityName,
          prioritySort,
        );
        dispatch({ type: "ADD_PRIORITY", payload: created });
      } catch (error) {
        dispatch({
          type: "SET_ERROR",
          payload: getErrorMessage(error, "Failed to create priority"),
        });
        throw error;
      }
    },
    [],
  );

  const updatePriority = useCallback(async (priority: ITodoPriority) => {
    try {
      await TodoPriorityService.updatePriority(priority);
      dispatch({ type: "UPDATE_PRIORITY", payload: priority });
    } catch (error) {
      dispatch({
        type: "SET_ERROR",
        payload: getErrorMessage(error, "Failed to update priority"),
      });
      throw error;
    }
  }, []);

  const deletePriority = useCallback(async (id: string) => {
    try {
      await TodoPriorityService.deletePriority(id);
      dispatch({ type: "DELETE_PRIORITY", payload: id });
    } catch (error) {
      dispatch({
        type: "SET_ERROR",
        payload: getErrorMessage(error, "Failed to delete priority"),
      });
      throw error;
    }
  }, []);

  const value: TodoContextType = {
    state,
    fetchTodos,
    fetchCategories,
    fetchPriorities,
    createTodo,
    updateTodo,
    deleteTodo,
    createCategory,
    deleteCategory,
    updateCategory,
    createPriority,
    updatePriority,
    deletePriority,
  };

  return <TodoContext.Provider value={value}>{children}</TodoContext.Provider>;
}

export function useTodo(): TodoContextType {
  const context = useContext(TodoContext);
  if (context === undefined) {
    throw new Error("useTodo must be used within a TodoProvider");
  }
  return context;
}

export default TodoProvider;

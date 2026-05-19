import type { ITodoCategory, ITodoPriority, ITodoTask } from "@/domain";

export interface TodoState {
  items: ITodoTask[];
  categories: ITodoCategory[];
  priorities: ITodoPriority[];
  loading: boolean;
  error: string | null;
}

export type TodoAction =
  | { type: "FETCH_START" }
  | { type: "FETCH_TODOS_SUCCESS"; payload: ITodoTask[] }
  | { type: "FETCH_CATEGORIES_SUCCESS"; payload: ITodoCategory[] }
  | { type: "FETCH_PRIORITIES_SUCCESS"; payload: ITodoPriority[] }
  | { type: "ADD_TODO"; payload: ITodoTask }
  | { type: "UPDATE_TODO"; payload: ITodoTask }
  | { type: "DELETE_TODO"; payload: string }
  | { type: "ADD_CATEGORY"; payload: ITodoCategory }
  | { type: "UPDATE_CATEGORY"; payload: ITodoCategory }
  | { type: "DELETE_CATEGORY"; payload: string }
  | { type: "ADD_PRIORITY"; payload: ITodoPriority }
  | { type: "UPDATE_PRIORITY"; payload: ITodoPriority }
  | { type: "DELETE_PRIORITY"; payload: string }
  | { type: "SET_ERROR"; payload: string };

export const initialTodoState: TodoState = {
  items: [],
  categories: [],
  priorities: [],
  loading: false,
  error: null,
};

export function todoReducer(state: TodoState, action: TodoAction): TodoState {
  switch (action.type) {
    case "FETCH_START":
      return { ...state, loading: true, error: null };

    case "FETCH_TODOS_SUCCESS":
      return { ...state, items: action.payload, loading: false, error: null };

    case "FETCH_CATEGORIES_SUCCESS":
      return { ...state, categories: action.payload, loading: false, error: null };

    case "FETCH_PRIORITIES_SUCCESS":
      return { ...state, priorities: action.payload, loading: false, error: null };

    case "ADD_TODO":
      return { ...state, items: [...state.items, action.payload], loading: false, error: null };

    case "UPDATE_TODO":
      return {
        ...state,
        items: state.items.map((item) =>
          item.id === action.payload.id ? action.payload : item,
        ),
        loading: false,
        error: null,
      };

    case "DELETE_TODO":
      return {
        ...state,
        items: state.items.filter((item) => item.id !== action.payload),
        loading: false,
        error: null,
      };

    case "ADD_CATEGORY":
      return {
        ...state,
        categories: [...state.categories, action.payload],
        loading: false,
        error: null,
      };

    case "UPDATE_CATEGORY":
      return {
        ...state,
        categories: state.categories.map((category) =>
          category.id === action.payload.id ? action.payload : category,
        ),
        loading: false,
        error: null,
      };

    case "DELETE_CATEGORY":
      return {
        ...state,
        categories: state.categories.filter((category) => category.id !== action.payload),
        loading: false,
        error: null,
      };

    case "ADD_PRIORITY":
      return {
        ...state,
        priorities: [...state.priorities, action.payload],
        loading: false,
        error: null,
      };

    case "UPDATE_PRIORITY":
      return {
        ...state,
        priorities: state.priorities.map((priority) =>
          priority.id === action.payload.id ? action.payload : priority,
        ),
        loading: false,
        error: null,
      };

    case "DELETE_PRIORITY":
      return {
        ...state,
        priorities: state.priorities.filter((priority) => priority.id !== action.payload),
        loading: false,
        error: null,
      };

    case "SET_ERROR":
      return { ...state, loading: false, error: action.payload };

    default:
      return state;
  }
}

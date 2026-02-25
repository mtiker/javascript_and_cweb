import { SortDirection } from "./types.js";

export function groupBy<T, K extends PropertyKey>(
  items: readonly T[],
  selector: (item: T) => K
): Record<K, T[]> {
  const grouped = {} as Record<K, T[]>;

  for (const item of items) {
    const key = selector(item);
    if (!grouped[key]) {
      grouped[key] = [];
    }
    grouped[key].push(item);
  }

  return grouped;
}

export function sortBy<T, V extends string | number | Date | null | undefined>(
  items: readonly T[],
  selector: (item: T) => V,
  direction: SortDirection = "asc"
): T[] {
  const directionFactor = direction === "asc" ? 1 : -1;
  const result = [...items];

  result.sort((left, right) => {
    const leftValue = selector(left);
    const rightValue = selector(right);

    if (leftValue === rightValue) {
      return 0;
    }

    if (leftValue == null) {
      return 1;
    }
    if (rightValue == null) {
      return -1;
    }

    const leftComparable =
      leftValue instanceof Date ? leftValue.getTime() : leftValue;
    const rightComparable =
      rightValue instanceof Date ? rightValue.getTime() : rightValue;

    if (leftComparable < rightComparable) {
      return -1 * directionFactor;
    }

    return 1 * directionFactor;
  });

  return result;
}

export function uniqueBy<T, K extends PropertyKey>(
  items: readonly T[],
  selector: (item: T) => K
): T[] {
  const seen = new Set<K>();
  const unique: T[] = [];

  for (const item of items) {
    const key = selector(item);
    if (seen.has(key)) {
      continue;
    }
    seen.add(key);
    unique.push(item);
  }

  return unique;
}

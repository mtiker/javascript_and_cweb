export function groupBy(items, selector) {
  const grouped = {};

  for (const item of items) {
    const key = selector(item);
    if (!grouped[key]) {
      grouped[key] = [];
    }
    grouped[key].push(item);
  }

  return grouped;
}

export function sortBy(items, selector, direction = "asc") {
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

export function uniqueBy(items, selector) {
  const seen = new Set();
  const unique = [];

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

export function createSingleFlight<T>() {
  let activePromise: Promise<T> | null = null;

  return {
    run(factory: () => Promise<T>) {
      if (!activePromise) {
        activePromise = factory().finally(() => {
          activePromise = null;
        });
      }

      return activePromise;
    },
  };
}

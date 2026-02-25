import { STORAGE_KEY } from "./constants.js";
import { StorageError } from "./errors.js";
import { Task } from "./types.js";
import { nextTick } from "./utils.js";

export class TaskStorage {
  private readonly storage: Storage;
  private readonly key: string;
  private lock: Promise<unknown>;

  constructor(storage: Storage = window.localStorage, key = STORAGE_KEY) {
    this.storage = storage;
    this.key = key;
    this.lock = Promise.resolve();
  }

  private async withLock<T>(job: () => T | Promise<T>): Promise<T> {
    const run = async (): Promise<T> => {
      await nextTick();
      return job();
    };

    this.lock = this.lock.then(run, run);
    return this.lock as Promise<T>;
  }

  public async readAll(): Promise<Task[]> {
    return this.withLock(() => {
      try {
        const raw = this.storage.getItem(this.key);
        if (!raw) {
          return [];
        }

        const parsed: unknown = JSON.parse(raw);
        if (!Array.isArray(parsed)) {
          throw new Error("stored data is not an array");
        }

        return parsed as Task[];
      } catch (error) {
        const reason = error instanceof Error ? error.message : String(error);
        throw new StorageError(`failed to read storage: ${reason}`);
      }
    });
  }

  public async writeAll(tasks: Task[]): Promise<void> {
    return this.withLock(() => {
      try {
        this.storage.setItem(this.key, JSON.stringify(tasks));
      } catch (error) {
        const reason = error instanceof Error ? error.message : String(error);
        throw new StorageError(`failed to write storage: ${reason}`);
      }
    });
  }
}

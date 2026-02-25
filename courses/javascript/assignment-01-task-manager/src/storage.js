import { STORAGE_KEY } from "./constants.js";
import { StorageError } from "./errors.js";
import { nextTick } from "./utils.js";

export class TaskStorage {
  constructor(storage = window.localStorage, key = STORAGE_KEY) {
    this.storage = storage;
    this.key = key;
    this.lock = Promise.resolve();
  }

  async withLock(job) {
    const run = async () => {
      await nextTick();
      return job();
    };

    this.lock = this.lock.then(run, run);
    return this.lock;
  }

  async readAll() {
    return this.withLock(() => {
      try {
        const raw = this.storage.getItem(this.key);
        if (!raw) {
          return [];
        }

        const parsed = JSON.parse(raw);
        if (!Array.isArray(parsed)) {
          throw new Error("Stored value is not an array.");
        }

        return parsed;
      } catch (error) {
        throw new StorageError(
          `Failed to read tasks from storage: ${error.message}`
        );
      }
    });
  }

  async writeAll(tasks) {
    return this.withLock(() => {
      if (!Array.isArray(tasks)) {
        throw new StorageError("writeAll expects an array of tasks.");
      }

      try {
        this.storage.setItem(this.key, JSON.stringify(tasks));
      } catch (error) {
        throw new StorageError(
          `Failed to write tasks to storage: ${error.message}`
        );
      }
    });
  }
}

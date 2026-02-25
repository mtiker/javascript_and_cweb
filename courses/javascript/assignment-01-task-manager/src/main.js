import { TaskStorage } from "./storage.js";
import { TaskService } from "./task-service.js";
import { TaskManagerUI } from "./ui.js";

async function bootstrap() {
  const storage = new TaskStorage(window.localStorage);
  const service = new TaskService(storage);
  const ui = new TaskManagerUI(service);
  await ui.init();
}

window.addEventListener("DOMContentLoaded", async () => {
  try {
    await bootstrap();
  } catch (error) {
    console.error(error);
    const fallback = document.getElementById("status-message");
    if (fallback) {
      fallback.textContent =
        "Fatal startup error. Open browser console for technical details.";
      fallback.dataset.tone = "error";
    }
  }
});

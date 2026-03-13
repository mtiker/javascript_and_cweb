import { TaskService } from "./service.js";
import { TaskStorage } from "./storage.js";
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
    }
    catch (error) {
        console.error(error);
        const status = document.getElementById("status-message");
        if (status) {
            status.textContent = "Fatal startup error. Open console for technical details.";
            status.setAttribute("data-tone", "error");
        }
    }
});

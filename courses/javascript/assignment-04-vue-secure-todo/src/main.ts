import { createPinia } from "pinia";
import { createApp } from "vue";
import App from "./App.vue";
import router from "./router";
import { useAuthStore } from "./stores/auth";
import "./styles.css";

const app = createApp(App);
const pinia = createPinia();

app.use(pinia);
useAuthStore(pinia).initialize();
app.use(router);
app.mount("#app");

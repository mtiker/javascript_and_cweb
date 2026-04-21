import { createRouter, createWebHistory } from "vue-router";
import DashboardView from "@/views/DashboardView.vue";
import CatalogsView from "@/views/CatalogsView.vue";
import LoginView from "@/views/LoginView.vue";
import RegisterView from "@/views/RegisterView.vue";
import TasksView from "@/views/TasksView.vue";
import { useAuthStore } from "@/stores/auth";
import { useCatalogStore } from "@/stores/catalogs";

declare module "vue-router" {
  interface RouteMeta {
    guestOnly?: boolean;
    requiresAuth?: boolean;
    title?: string;
  }
}

const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: "/",
      redirect: "/login",
    },
    {
      path: "/login",
      name: "login",
      component: LoginView,
      meta: {
        guestOnly: true,
        title: "Login",
      },
    },
    {
      path: "/register",
      name: "register",
      component: RegisterView,
      meta: {
        guestOnly: true,
        title: "Register",
      },
    },
    {
      path: "/app/dashboard",
      name: "dashboard",
      component: DashboardView,
      meta: {
        requiresAuth: true,
        title: "Dashboard",
      },
    },
    {
      path: "/app/tasks",
      name: "tasks",
      component: TasksView,
      meta: {
        requiresAuth: true,
        title: "Tasks",
      },
    },
    {
      path: "/app/catalogs",
      name: "catalogs",
      component: CatalogsView,
      meta: {
        requiresAuth: true,
        title: "Catalogs",
      },
    },
    {
      path: "/:pathMatch(.*)*",
      redirect: "/",
    },
  ],
});

router.beforeEach(async (to) => {
  const authStore = useAuthStore();
  authStore.initialize();

  if (to.meta.requiresAuth && !authStore.isAuthenticated) {
    return {
      name: "login",
      query: {
        redirect: to.fullPath,
      },
    };
  }

  if (to.meta.guestOnly && authStore.isAuthenticated) {
    return {
      name: "dashboard",
    };
  }

  if (authStore.isAuthenticated && to.path.startsWith("/app")) {
    const catalogStore = useCatalogStore();

    try {
      await catalogStore.ensureLoaded();
    } catch {
      if (!authStore.isAuthenticated) {
        return {
          name: "login",
          query: {
            redirect: to.fullPath,
          },
        };
      }

      return true;
    }

    if (!catalogStore.isReadyForTasks && to.name !== "catalogs") {
      return {
        name: "catalogs",
      };
    }
  }

  return true;
});

router.afterEach((to) => {
  document.title = `${to.meta.title ?? "Vue Secure Todo"} | Assignment 04`;
});

export default router;

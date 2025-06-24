import { createRouter, createWebHistory } from 'vue-router'
import HomeView from '../views/HomeView.vue'
import Login from '../views/Login.vue';

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/',
      name: 'home',
      component: HomeView,
    },
    {
      path: '/about',
      name: 'about',
      // route level code-splitting
      // this generates a separate chunk (About.[hash].js) for this route
      // which is lazy-loaded when the route is visited.
      component: () => import('../views/AboutView.vue'),
    },
    {
      path: '/Login',
      name: 'Login',
       component: () => import('../views/Login.vue'),
    },
    {
      path: '/User_Home',
      name: 'User_Home',
       component: () => import('../views/User_Home.vue'),
    },
    {
      path: '/Admin_Home',
      name: 'admin_home',
       component: () => import('../views/Admin_Home.vue'),
    },
    {
      path: '/ShiftSubmit',
      name: 'ShiftSubmit',
       component: () => import('../views/ShiftSubmit.vue'),
    },
    {
      path: '/ShiftCheck',
      name: 'ShiftCheck',
       component: () => import('../views/ShiftCheck.vue'),
    },
    {
      path: '/ShiftCreate',
      name: 'ShiftCreate',
       component: () => import('../views/ShiftCreate.vue'),
    },
    {
      path: '/ShiftCreateDaily',
      name: 'ShiftCreateDaily',
       component: () => import('../views/ShiftCreateDaily.vue'),
    },
    {
      path: '/Attendance',
      name: 'Attendance',
       component: () => import('../views/Attendance.vue'),
    },
    {
      path: '/UserManage',
      name: 'UserManage',
       component: () => import('../views/UserManage.vue'),
    },
  ],
})

export default router

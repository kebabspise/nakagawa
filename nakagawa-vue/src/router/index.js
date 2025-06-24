import { createRouter, createWebHistory } from 'vue-router'
import User_Home from '../views/User_Home.vue'
import GanttSample from '../components/GanttSample.vue' // 追加

const routes = [
  { path: '/', component: User_Home },
  { path: '/gantt', component: GanttSample } // 追加
]

const router = createRouter({
  history: createWebHistory(),
  routes
})

export default router

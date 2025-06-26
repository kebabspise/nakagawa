<!-- src/components/UserHeader.vue -->
<template>
  <div class="user-header">
    <div class="user-info" v-if="userName">
      <span class="user-name">{{ userName }}さんのページ</span>
      <button @click="handleLogout" class="logout-btn">ログアウト</button>
    </div>
  </div>
</template>

<script setup>
import { useUser } from '../components/useUser'
import { useRouter } from 'vue-router'

const { userName, clearUser } = useUser()
const router = useRouter()

const handleLogout = () => {
  if (confirm('ログアウトしますか？')) {
    clearUser()
    router.push('/')
  }
}
</script>

<style scoped>
.user-header {
  position: fixed;
  top: 0;
  right: 0;
  width: 100%;
  background-color: #f8f9fa;
  border-bottom: 1px solid #dee2e6;
  z-index: 999;
  padding: 10px 20px;
  display: flex;
  justify-content: flex-end;
}

.user-info {
  display: flex;
  align-items: center;
  gap: 15px;
}

.user-name {
  font-weight: bold;
  color: #495057;
}

.logout-btn {
  background-color: #dc3545;
  color: white;
  border: none;
  padding: 5px 12px;
  border-radius: 4px;
  cursor: pointer;
  font-size: 14px;
  transition: background-color 0.3s;
}

.logout-btn:hover {
  background-color: #c82333;
}
</style>
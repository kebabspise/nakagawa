<template>
  <div>
    <!-- ユーザーヘッダー -->
    <UserHeader />
    
    <div class="main-content">
      <h1>{{ title }}</h1>
      <h2>{{ title2 }}</h2>

      <div class="button-group">
        <button @click="showTimeClockPopup = true" class="main-btn clock-btn">打刻</button>
        <button @click="goToShiftSubmit" class="main-btn shift-btn">シフト提出</button>
        <button @click="goToShiftCheck" class="main-btn check-btn">シフト確認</button>
      </div>

      <TimeClockPopup 
        :show="showTimeClockPopup"
        @close="showTimeClockPopup = false"
        @clock-recorded="onClockRecorded"
      />
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useUser } from '../components/useUser'
import TimeClockPopup from '../components/TimeClock.vue'
import UserHeader from '../components/UserHeader.vue'

const router = useRouter()
const { currentUser, userId } = useUser()

const title = ref('一般ユーザーのホーム画面です')
const title2 = ref('サブタイトルです。')
const showTimeClockPopup = ref(false)

// ページ読み込み時にユーザー情報をチェック
onMounted(() => {
  if (!currentUser.value) {
    // ユーザー情報がない場合はログインページにリダイレクト
    router.push('/')
  }
})

function onClockRecorded(data) {
  console.log('打刻完了:', data)
}

function goToShiftSubmit() {
  router.push('/ShiftSubmit')
}

function goToShiftCheck() {
  router.push('/ShiftCheck')
}
</script>

<style scoped>
.main-content {
  padding-top: 60px; /* ヘッダーの高さ分のスペース */
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  min-height: calc(100vh - 60px);
}

h1 {
  color: #333;
  margin-bottom: 1rem;
  text-align: center;
}

h2 {
  color: #666;
  margin-bottom: 2rem;
  font-weight: normal;
  text-align: center;
}

.button-group {
  display: flex;
  flex-direction: column;
  gap: 15px;
  width: 100%;
  max-width: 300px;
}

.main-btn {
  padding: 15px 30px;
  border: none;
  border-radius: 8px;
  font-size: 18px;
  font-weight: bold;
  cursor: pointer;
  transition: all 0.3s ease;
  box-shadow: 0 2px 5px rgba(0, 0, 0, 0.1);
}

.main-btn:hover {
  transform: translateY(-2px);
  box-shadow: 0 4px 10px rgba(0, 0, 0, 0.2);
}

.clock-btn {
  background-color: #007bff;
  color: white;
}

.clock-btn:hover {
  background-color: #0056b3;
}

.shift-btn {
  background-color: #28a745;
  color: white;
}

.shift-btn:hover {
  background-color: #1e7e34;
}

.check-btn {
  background-color: #ffc107;
  color: #212529;
}

.check-btn:hover {
  background-color: #e0a800;
}
</style>
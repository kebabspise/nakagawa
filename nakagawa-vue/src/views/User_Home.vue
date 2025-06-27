<template>
  <div class="container">
    <UserHeader />

    <div class="content-card">
      <h1 class="title">{{ title }}</h1>
      <h2 class="subtitle">{{ title2 }}</h2>

      <div class="button-group">
        <BackButton />
        <button @click="showTimeClockPopup = true" class="main-btn clock-btn">🕒 打刻</button>
        <button @click="goToShiftSubmit" class="main-btn shift-btn">📝 シフト提出</button>
        <button @click="goToShiftCheck" class="main-btn check-btn">📅 シフト確認</button>
      </div>
    </div>

    <TimeClockPopup 
      :show="showTimeClockPopup"
      @close="showTimeClockPopup = false"
      @clock-recorded="onClockRecorded"
    />
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useUser } from '../components/useUser'
import TimeClockPopup from '../components/TimeClock.vue'
import UserHeader from '../components/UserHeader.vue'
import BackButton from '../components/BackButton.vue'

const router = useRouter()
const { currentUser } = useUser()
const title = ref('ようこそ！')
const title2 = ref('必要な操作を選んでください')
const showTimeClockPopup = ref(false)

onMounted(() => {
  if (!currentUser.value) {
    alert('ログインしてください。')
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
.container {
  min-height: 100vh;
  display: flex;
  justify-content: center;
  align-items: center;
  background-color: #ffffff; /* ← 白背景、または none にしてOK */
  font-family: 'Segoe UI', sans-serif;
}

/* カード全体を少し右へずらす */
.content-card {
  background: #ffffff;
  border-radius: 20px;
  padding: 50px 40px;
  width: 90%;
  max-width: 500px;
  box-shadow: 0 10px 30px rgba(0, 0, 0, 0.1);
  text-align: center;
  transform: translateX(50%); /* ← 少し右にずらすポイント！ */
}

/* タイトル */
.title {
  font-size: 30px;
  font-weight: 700;
  color: #333;
  margin-bottom: 12px;
}

.subtitle {
  font-size: 18px;
  color: #666;
  margin-bottom: 35px;
}

/* ボタン縦並び */
.button-group {
  display: flex;
  flex-direction: column;
  gap: 18px;
  width: 100%;
}

/* ボタン共通スタイル */
.main-btn {
  padding: 16px;
  font-size: 17px;
  font-weight: bold;
  border: none;
  border-radius: 12px;
  cursor: pointer;
  transition: 0.2s;
  width: 100%;
}

/* ボタン色設定 */
.clock-btn {
  background-color: #4a90e2;
  color: white;
}
.clock-btn:hover {
  background-color: #357ab8;
}

.shift-btn {
  background-color: #34c38f;
  color: white;
}
.shift-btn:hover {
  background-color: #2ca77a;
}

.check-btn {
  background-color: #f4b400;
  color: white;
}
.check-btn:hover {
  background-color: #d89c00;
}

/* スマホでは真ん中に */
@media (max-width: 768px) {
  .content-card {
    transform: none;
  }
}
</style>

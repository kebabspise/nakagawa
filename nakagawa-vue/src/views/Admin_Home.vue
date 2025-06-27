<template>
  <div class="container">
    <UserHeader />

    <div class="content-card">
      <h1 class="title">管理者のホーム画面です</h1>
      <h2 class="subtitle">必要な操作を選んでください</h2>

      <!-- ログインユーザー情報の表示（任意） -->
      <p v-if="currentUser" class="user-info">ユーザー: {{ currentUser.name }}</p>

      <div class="button-group">
        <BackButton />
        <button @click="showTimeClockPopup = true" class="main-btn clock-btn">🕒 打刻</button>
        <button @click="goToShiftCreate" class="main-btn create-btn">📋 シフト作成</button>
        <button @click="goToAttendance" class="main-btn attendance-btn">📊 勤怠管理</button>
        <button @click="goToUserManage" class="main-btn user-btn">👥 ユーザー管理</button>
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
const { currentUser, userId } = useUser()
const showTimeClockPopup = ref(false)

// ページ読み込み時にログイン状態を確認
onMounted(() => {
  if (!currentUser.value) {
    alert('ログインしてください。')
    router.push('/') // 未ログインならログインページへリダイレクト
  }
})

function onClockRecorded(data) {
  console.log('打刻完了（管理者）:', data)
}

// ページ遷移関数
function goToShiftCreate() {
  router.push('/ShiftCreate')
}

function goToAttendance() {
  router.push('/Attendance')
}

function goToUserManage() {
  router.push('/UserManage')
}
</script>

<style scoped>
.container {
  min-height: 100vh;
  display: flex;
  justify-content: center;
  align-items: center;
  background-color: #ffffff;
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
  margin-bottom: 20px;
}

/* ユーザー情報 */
.user-info {
  font-size: 16px;
  color: #888;
  margin-bottom: 25px;
  padding: 8px 16px;
  background-color: #f8f9fa;
  border-radius: 8px;
  display: inline-block;
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

.create-btn {
  background-color: #8e44ad;
  color: white;
}
.create-btn:hover {
  background-color: #7d3c98;
}

.attendance-btn {
  background-color: #e74c3c;
  color: white;
}
.attendance-btn:hover {
  background-color: #c0392b;
}

.user-btn {
  background-color: #f39c12;
  color: white;
}
.user-btn:hover {
  background-color: #e67e22;
}

/* スマホでは真ん中に */
@media (max-width: 768px) {
  .content-card {
    transform: none;
  }
  
  .title {
    font-size: 24px;
  }
  
  .subtitle {
    font-size: 16px;
  }
}
</style>
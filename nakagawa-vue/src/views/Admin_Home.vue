<script setup>
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import TimeClockPopup from '../components/TimeClock.vue'
import { useUser } from '../components/useUser' // 追加

const router = useRouter()

// ユーザー情報取得（useUser composable）
const { currentUser, userId } = useUser()

const title = ref('管理者のホーム画面です')
const title2 = ref('サブタイトルです。')

const showTimeClockPopup = ref(false)

// ページ読み込み時にログイン状態を確認
onMounted(() => {
  if (!currentUser.value) {
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

<template>
  <div style="display: flex; flex-direction: column; align-items: center; justify-content: center; min-height: 60vh;">
    <h1>{{ title }}</h1>
    <h2>{{ title2 }}</h2>
    
    <!-- ログインユーザー情報の表示（任意） -->
    <p v-if="currentUser">ユーザー: {{ currentUser.name }}</p>

    <!-- 打刻ボタン -->
    <button @click="showTimeClockPopup = true" style="margin-top: 32px; padding: 12px 32px; font-size: 1.2rem;">
      打刻
    </button>

    <!-- 打刻ポップアップ -->
    <TimeClockPopup 
      :show="showTimeClockPopup"
      @close="showTimeClockPopup = false"
      @clock-recorded="onClockRecorded"
    />

    <!-- 管理メニュー -->
    <button @click="goToShiftCreate" style="margin-top: 32px; padding: 12px 32px; font-size: 1.2rem;">シフト作成</button>
    <button @click="goToAttendance" style="margin-top: 32px; padding: 12px 32px; font-size: 1.2rem;">勤怠管理</button>
    <button @click="goToUserManage" style="margin-top: 32px; padding: 12px 32px; font-size: 1.2rem;">ユーザー管理</button>
  </div>
</template>

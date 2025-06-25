<script setup>
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import TimeClockPopup from '../components/TimeClock.vue'

const router = useRouter()

const title = ref('管理者のホーム画面です')
const title2 = ref('サブタイトルです。')

const showTimeClockPopup = ref(false)

function onClockRecorded(data) {
  console.log('打刻完了（管理者）:', data)
}

// ページ遷移
function goToShiftCreate() {
  router.push('/ShiftCreate')
}
function goToShiftSubmit() {
  router.push('/ShiftSubmit')
}
function goToUserManage() {
  router.push('/UserManage')
}
</script>

<template>
  <div style="display: flex; flex-direction: column; align-items: center; justify-content: center; min-height: 60vh;">
    <h1>{{ title }}</h1>
    <h2>{{ title2 }}</h2>

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
    <button @click="goToShiftSubmit" style="margin-top: 32px; padding: 12px 32px; font-size: 1.2rem;">勤怠管理</button>
    <button @click="goToUserManage" style="margin-top: 32px; padding: 12px 32px; font-size: 1.2rem;">ユーザー管理</button>
  </div>
</template>

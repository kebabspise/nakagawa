<script setup>
import { ref } from 'vue'
import { useRouter } from 'vue-router'

const router = useRouter()

const title = ref('一般ユーザーのホーム画面です')
const title2 = ref('サブタイトルです。')

const showPopup = ref(false)
const showCheckInConfirm = ref(false)
const showCheckOutConfirm = ref(false)

// ページ遷移（他のボタン用）
function goToTimeCard() {
  router.push('/timecard')
}
function goToShiftSubmit() {
  router.push('/shift-submit')
}
function goToShiftCheck() {
  router.push('/user-manage')
}

// ポップアップ表示切替
function togglePopup() {
  showPopup.value = !showPopup.value
  showCheckInConfirm.value = false
  showCheckOutConfirm.value = false
}

// 入店打刻処理
function handleCheckIn() {
  showCheckInConfirm.value = true
  showCheckOutConfirm.value = false
}

// 退店打刻処理
function handleCheckOut() {
  showCheckOutConfirm.value = true
  showCheckInConfirm.value = false
}
</script>

<template>
  <div style="display: flex; flex-direction: column; align-items: center; justify-content: center; min-height: 60vh;">
    <h1>{{ title }}</h1>
    <h2>{{ title2 }}</h2>

    <!-- 入店・退店ボタン（ポップアップ表示） -->
    <button @click="togglePopup" style="margin-top: 32px; padding: 12px 32px; font-size: 1.2rem;">
      入店・退店
    </button>

    <!-- ポップアップメニュー -->
    <div v-if="showPopup" style="margin-top: 24px; padding: 16px; border: 1px solid #ccc; border-radius: 8px; background: #f9f9f9;">
      <p>打刻を選んでください</p>
      <button @click="handleCheckIn" style="margin: 8px;">入店打刻</button>
      <button @click="handleCheckOut" style="margin: 8px;">退店打刻</button>
      <button @click="togglePopup" style="margin: 8px;">閉じる</button>

      <!-- 入店確認 -->
      <div v-if="showCheckInConfirm" style="margin-top: 16px;">
        <p>入店しますか？</p>
        <button style="margin: 4px;">はい</button>
        <button style="margin: 4px;">いいえ</button>
      </div>

      <!-- 退店確認 -->
      <div v-if="showCheckOutConfirm" style="margin-top: 16px;">
        <p>退店しますか？</p>
        <button style="margin: 4px;">はい</button>
        <button style="margin: 4px;">いいえ</button>
      </div>
    </div>

    <!-- 他のページ遷移ボタン -->
    <button @click="goToTimeCard" style="margin-top: 32px; padding: 12px 32px; font-size: 1.2rem;">シフト作成</button>
    <button @click="goToShiftSubmit" style="margin-top: 32px; padding: 12px 32px; font-size: 1.2rem;">勤怠管理</button>
    <button @click="goToShiftCheck" style="margin-top: 32px; padding: 12px 32px; font-size: 1.2rem;">ユーザー管理</button>
  </div>
</template>

<template>
  <div class="container">
    <FullCalendar
      :plugins="calendarPlugins"
      :initialView="'dayGridMonth'"
      :events="events"
      @dateClick="handleDateClick"
    />
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import FullCalendar from '@fullcalendar/vue'
import dayGridPlugin from '@fullcalendar/daygrid'
import interactionPlugin from '@fullcalendar/interaction'
import axios from 'axios'

// カレンダープラグイン設定
const calendarPlugins = [dayGridPlugin, interactionPlugin]
const events = ref([])  // カレンダーに表示するイベント

// 初回ロード時に希望シフトを取得
onMounted(() => {
  loadShiftRequests()
})

// シフト希望データ取得
async function loadShiftRequests() {
  try {
    const res = await axios.get('/shift-request')
    events.value = res.data.map(item => ({
      title: `${item.start_work}〜${item.end_work}`,
      date: item.date, // `date` だけの場合、終日扱いになる
    }))
  } catch (e) {
    console.error('取得失敗:', e)
  }
}

// 日付クリック時の処理
async function handleDateClick(info) {
  const date = info.dateStr
  const start = prompt(`${date} 出勤時間を入力してください（例: 14:00）`)
  const end = prompt(`${date} 退勤時間を入力してください（例: 18:00）`)

  if (start && end) {
    try {
      // サーバーにPOST
      await axios.post('/shift-request', {
        date: date,
        start_work: `${date} ${start}`,
        end_work: `${date} ${end}`
      })

      // カレンダーにも即時反映
      events.value.push({
        title: `${start}〜${end}`,
        date: date
      })
    } catch (e) {
      console.error('登録失敗:', e)
    }
  }
}
</script>

<style scoped>
.container {
  max-width: 800px;
  margin: 0 auto;
  padding: 2rem;
}
</style>
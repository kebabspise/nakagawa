// 2. Vue コンポーネント例
<template>
  <div>
    <FullCalendar
      :options="calendarOptions"
    />
  </div>
</template>

<script setup>
import { ref } from 'vue'
import FullCalendar from '@fullcalendar/vue3'
import dayGridPlugin from '@fullcalendar/daygrid'
import interactionPlugin from '@fullcalendar/interaction'

// カレンダーのオプション設定
const calendarOptions = ref({
  plugins: [
    dayGridPlugin,    // 月表示プラグイン（必須）
    interactionPlugin // クリックイベントなどの操作用
  ],
  initialView: 'dayGridMonth', // 初期表示ビュー
  headerToolbar: {
    left: 'prev,next today',
    center: 'title',
    right: 'dayGridMonth,dayGridWeek'
  },
  events: [
    {
      title: 'サンプルイベント',
      date: '2024-01-15'
    }
  ],
  locale: 'ja', // 日本語表示
  height: 'auto'
})
</script>


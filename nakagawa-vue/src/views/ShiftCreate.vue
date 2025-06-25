<template>
  <div>
    <FullCalendar
      :options="advancedCalendarOptions"
    />
  </div>
</template>
 
<script setup>
import { ref } from 'vue'
import FullCalendar from '@fullcalendar/vue3'
import dayGridPlugin from '@fullcalendar/daygrid'
import timeGridPlugin from '@fullcalendar/timegrid'
import listPlugin from '@fullcalendar/list'
import interactionPlugin from '@fullcalendar/interaction'





// ↓↓↓　ここから下が編集箇所（希望シフト取得）　↓↓↓

import axios from 'axios'




const events = ref([])

onMounted(async () => {
  // ここでAPIからシフトデータをGETで取得
  // 例: /api/shifts など
  try {
    const res = await axios.get('/api/shifts')
    // 取得したデータをeventsにセット
    // 例: [{ title: 'シフトA', start: '2024-07-01T09:00:00', end: '2024-07-01T17:00:00' }, ...]
    events.value = res.data
  } catch (e) {
    console.error('シフトデータの取得に失敗しました', e)
  }
})

// 　↑↑↑　ここまでが編集箇所（希望シフト取得）　↑↑↑







const advancedCalendarOptions = ref({
  plugins: [
    dayGridPlugin,
    timeGridPlugin,   // 週表示・日表示用
    listPlugin,       // リスト表示用
    interactionPlugin
  ],
  initialView: 'dayGridMonth',
  headerToolbar: {
    left: 'prev,next today',
    center: 'title',
    right: 'dayGridMonth,timeGridWeek,timeGridDay,listWeek'
  },
  events: [
    {
      title: 'ミーティング',
      start: '2024-01-15T10:00:00',
      end: '2024-01-15T11:00:00'
    }
  ],


  
  events, // ここでAPIから取得したイベントデータを使用
  // events: events.value, // 直接参照する場合はこれを使用
  // events: ref([]), // 初期値を空の配列にする場合  
  locale: 'ja',
  height: 'auto',
  // イベントクリック時の処理
  eventClick: function(info) {
    alert('Event: ' + info.event.title)
  }
})
</script>
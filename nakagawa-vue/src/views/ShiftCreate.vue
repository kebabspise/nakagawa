<template>
  <div>
    <FullCalendar
      :options="advancedCalendarOptions"
    />
    <!-- 修正: カレンダーにイベントが一つもない場合のみ表示 -->
    <div v-if="events.length === 0" style="margin-top: 10px; color: red;">
      希望がありません
    </div>
  </div>
</template>
 
<script setup>
import { onMounted, ref } from 'vue'
import FullCalendar from '@fullcalendar/vue3'
import dayGridPlugin from '@fullcalendar/daygrid'
import timeGridPlugin from '@fullcalendar/timegrid'
import listPlugin from '@fullcalendar/list'
import interactionPlugin from '@fullcalendar/interaction'
import axios from 'axios'

const events = ref([])
// ↓↓↓ 追加：eventCountMapの定義
const eventCountMap = ref({})

onMounted(async () => {
  try {
    const res = await axios.get('/api/shifts') //()内はURLを指定
    events.value = res.data // 取得したシフトデータをeventsに格納

    // 日付ごとのイベント数をカウントする
    const countMap = {}
    events.value.forEach(ev => {
      const date = ev.start.slice(0, 10)
      countMap[date] = (countMap[date] || 0) + 1
    })
    eventCountMap.value = countMap
  } catch (e) {
    console.error('シフトデータの取得に失敗しました', e)
  }
})


// 日付セルのカスタムクラスを返す関数です
function dayCellClassNames(arg){
  const date = arg.date.toISOString().slice(0,10)
  if (eventCountMap.value[date] !== undefined && eventCountMap.value[date] < 5) {
    // イベントがある日付には特定のクラスを追加
    return ['few-events']
  }
  return []
}

// 　↑↑↑　ここまでが編集箇所（希望シフト取得）　↑↑↑
// ここまで人手不足アラートのプログラムです

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



  // 日付セルのクラスを設定
  // 再度修正に移ります。 

/*
  events: [
    {
      title: 'ミーティング',
      start: '2024-01-15T10:00:00',
      end: '2024-01-15T11:00:00'
    }
  ],
*/

  
  events, // ここでAPIから取得したイベントデータを使用
  // events: events.value, // 直接参照する場合はこれを使用
  // events: ref([]), // 初期値を空の配列にする場合  
  locale: 'ja',
  height: 'auto',
  // イベントクリック時の処理
  eventClick: function(info) {
    alert('Event: ' + info.event.title)
  },
  dayCellClassNames

})
</script>


<!-- styleを追加していきます -->
<style>
.few-events {
  background-color: #ffcccc !important; /* 希望シフトが5件以上の日付セルの背景色 */
}
</style>
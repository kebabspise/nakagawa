<template>
  <div>
    <FullCalendar
      :options="advancedCalendarOptions"
    />
    <!-- 修正: カレンダーにイベントが一つもない場合のみ表示 -->
    <div v-if="events.length === 0" style="margin-top: 10px; color: red;">
      希望がありません
    </div>
    <!-- 編集用モーダル -->
    <div v-if="isEditModalOpen" class="modal-overlay">
      <div class="modal">
        <h3>シフト編集</h3>
        <form @submit.prevent="saveShift">
          <div>
            <label>タイトル: <input v-model="editShift.title" required /></label>
          </div>
          <div>
            <label>開始: <input type="datetime-local" v-model="editShift.start" required /></label>
          </div>
          <div>
            <label>終了: <input type="datetime-local" v-model="editShift.end" required /></label>
          </div>
          <div style="margin-top:10px;">
            <button type="submit">保存</button>
            <button type="button" @click="deleteShift" style="margin-left:8px; color:red;">削除</button>
            <button type="button" @click="isEditModalOpen=false" style="margin-left:8px;">キャンセル</button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>
 
<script setup>
import { onMounted, ref, reactive } from 'vue'
import FullCalendar from '@fullcalendar/vue3'
import dayGridPlugin from '@fullcalendar/daygrid'
import timeGridPlugin from '@fullcalendar/timegrid'
import listPlugin from '@fullcalendar/list'
import interactionPlugin from '@fullcalendar/interaction'
import axios from 'axios'

const events = ref([])
const eventCountMap = ref({})
const isEditModalOpen = ref(false)
const editShift = reactive({ id:'', user_id: '', name: '', work_start: '', work_end: '' })

onMounted(async () => {
  await fetchShifts()
})

async function fetchShifts() {
  try {
    const res = await axios.get('http://localhost:5174/api/Shift_requests')
    events.value = res.data
    const countMap = {}
    events.value.forEach(ev => {
      const date = ev.woek_start.slice(0, 10)
      countMap[date] = (countMap[date] || 0) + 1
    })
    eventCountMap.value = countMap
  } catch (e) {
    console.error('シフトデータの取得に失敗しました', e)
  }
}

// 日付セルのカスタムクラスを返す関数です
function dayCellClassNames(arg){
  const date = arg.date.toISOString().slice(0,10)
  if (eventCountMap.value[date] !== undefined && eventCountMap.value[date] < 5) {
    // イベントがある日付には特定のクラスを追加
    return ['few-events']
  }
  return []
}

function handleEventClick(info) {
  // 編集用モーダルにデータをセット
  editShift.id = info.event.id
  editShift.user_id = info.event.user_id
  editShift.name = info.event.name
  editShift.woek_start = info.event.work_start ? info.event.work_start.toISOString().slice(0,16) : ''
  editShift.work_end = info.event.work_end ? info.event.work_end.toISOString().slice(0,16) : ''
  isEditModalOpen.value = true
}

async function saveShift() {
  try {
    await axios.put(`http://localhost:5174/api/Shift_requests${editShift.id}`, {
      work_start: editShift.work_start,
      work_end: editShift.work_end
    })
    isEditModalOpen.value = false
    await fetchShifts()
  } catch (e) {
    alert('保存に失敗しました')
  }
}

async function deleteShift() {
  if (!confirm('本当に削除しますか？')) return
  try {
    await axios.delete(`http://localhost:5174/api/Shift_requests${editShift.id}`)
    isEditModalOpen.value = false
    await fetchShifts()
  } catch (e) {
    alert('削除に失敗しました')
  }
}

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

  // interactionPluginのselectやeventClickでフォームを開く
  selectable: true,
  select: function(info) {
    // シフト追加フォームを表示
  },
  eventClick: handleEventClick,

  // 日付セルのクラスを設定
  // 再度修正に移ります。 

/*
  events: [
    {
      id: '1',
      user: 'Aさん',
      work_start: '2024-01-15T10:00:00',
      work_end: '2024-01-15T11:00:00'
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
.modal-overlay {
  position: fixed;
  top: 0; left: 0; right: 0; bottom: 0;
  background: rgba(0,0,0,0.3);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
}
.modal {
  background: #fff;
  padding: 24px;
  border-radius: 8px;
  min-width: 320px;
  box-shadow: 0 2px 8px rgba(0,0,0,0.2);
}
</style>
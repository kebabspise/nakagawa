<template>
  <div>
    <div style="margin-bottom: 20px;">
      <h2>シフト管理</h2>
      <div style="margin-bottom: 10px;">
        <label>
          <input type="checkbox" v-model="showRequests" /> シフト申請を表示
        </label>
        <label style="margin-left: 20px;">
          <input type="checkbox" v-model="showConfirms" /> 確定シフトを表示
        </label>
      </div>
      <button @click="openCreateModal" style="background: #28a745; color: white; padding: 8px 16px; border: none; border-radius: 4px; cursor: pointer;">
        新規シフト作成
      </button>
    </div>
    
    <FullCalendar
      :options="advancedCalendarOptions"
    />
    
    <!-- イベントがない場合の表示 -->
    <div v-if="displayEvents.length === 0" style="margin-top: 10px; color: red;">
      表示するシフトがありません
    </div>
    
    <!-- 新規作成用モーダル -->
    <div v-if="isCreateModalOpen" class="modal-overlay">
      <div class="modal">
        <h3>新規シフト作成</h3>
        <form @submit.prevent="createShift">
          <div>
            <label>従業員:
              <select v-model="newShift.user_id" required>
                <option value="">選択してください</option>
                <option v-for="user in users" :key="user.id" :value="user.id">
                  {{ user.name }}
                </option>
              </select>
            </label>
          </div>
          <div>
            <label>開始: <input type="datetime-local" v-model="newShift.start" required /></label>
          </div>
          <div>
            <label>終了: <input type="datetime-local" v-model="newShift.end" required /></label>
          </div>
          <div style="margin-top:10px;">
            <button type="submit" style="background: #28a745; color: white;">作成</button>
            <button type="button" @click="isCreateModalOpen=false" style="margin-left:8px;">キャンセル</button>
          </div>
        </form>
      </div>
    </div>
    
    <!-- 編集用モーダル -->
    <div v-if="isEditModalOpen" class="modal-overlay">
      <div class="modal">
        <h3>{{ editShift.isRequest ? 'シフト申請詳細' : 'シフト編集' }}</h3>
        <form @submit.prevent="saveShift">
          <div>
            <label>従業員: {{ editShift.userName }}</label>
          </div>
          <div>
            <label>開始: 
              <input 
                type="datetime-local" 
                v-model="editShift.start" 
                :readonly="editShift.isRequest"
                required 
              />
            </label>
          </div>
          <div>
            <label>終了: 
              <input 
                type="datetime-local" 
                v-model="editShift.end" 
                :readonly="editShift.isRequest"
                required 
              />
            </label>
          </div>
          <div style="margin-top:10px;">
            <div v-if="editShift.isRequest">
              <!-- シフト申請の場合：確定のみ可能 -->
              <button type="button" @click="confirmShift" style="background: #28a745; color: white; margin-right: 8px;">
                確定
              </button>
            </div>
            <div v-else>
              <!-- 確定シフトの場合：編集・削除可能 -->
              <button type="submit" style="background: #007bff; color: white;">保存</button>
              <button type="button" @click="deleteShift" style="margin-left:8px; background: #dc3545; color: white;">削除</button>
            </div>
            <button type="button" @click="isEditModalOpen=false" style="margin-left:8px;">キャンセル</button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>
 
<script setup>
import { onMounted, ref, reactive, computed } from 'vue'
import FullCalendar from '@fullcalendar/vue3'
import dayGridPlugin from '@fullcalendar/daygrid'
import timeGridPlugin from '@fullcalendar/timegrid'
import listPlugin from '@fullcalendar/list'
import interactionPlugin from '@fullcalendar/interaction'
import axios from 'axios'

const shiftRequests = ref([])
const shiftConfirms = ref([])
const eventCountMap = ref({})
const isEditModalOpen = ref(false)
const isCreateModalOpen = ref(false)
const showRequests = ref(true)
const showConfirms = ref(true)

const editShift = reactive({ 
  id: '', 
  user_id: '', 
  userName: '', 
  start: '', 
  end: '',
  isRequest: false
})

const newShift = reactive({
  user_id: '',
  start: '',
  end: ''
})

const users = ref([])

// 表示するイベントを計算
const displayEvents = computed(() => {
  const events = []
  
  if (showRequests.value) {
    events.push(...shiftRequests.value.map(shift => ({
      id: `req_${shift.id}`,
      title: `【申請】${getUserName(shift.user_id)} (${formatJstDateTime(shift.work_start)} - ${formatJstDateTime(shift.work_end)})`,
      start: shift.work_start,
      end: shift.work_end,
      backgroundColor: '#ffc107',
      borderColor: '#ffc107',
      extendedProps: {
        originalId: shift.id,
        user_id: shift.user_id,
        userName: getUserName(shift.user_id),
        work_start: shift.work_start,
        work_end: shift.work_end,
        isRequest: true
      }
    })))
  }
  
  if (showConfirms.value) {
    events.push(...shiftConfirms.value.map(shift => ({
      id: `conf_${shift.id}`,
      title: `【確定】${getUserName(shift.user_id)} (${formatJstDateTime(shift.work_start)} - ${formatJstDateTime(shift.work_end)})`,
      start: shift.work_start,
      end: shift.work_end,
      backgroundColor: '#28a745',
      borderColor: '#28a745',
      extendedProps: {
        originalId: shift.id,
        user_id: shift.user_id,
        userName: getUserName(shift.user_id),
        work_start: shift.work_start,
        work_end: shift.work_end,
        isRequest: false
      }
    })))
  }
  
  return events
})

onMounted(async () => {
  await fetchUsers()
  await fetchShiftRequests()
  await fetchShiftConfirms()
})

// ユーザー情報を取得
async function fetchUsers() {
  try {
    const res = await axios.get('http://localhost:5174/api/Users')
    users.value = res.data
  } catch (e) {
    console.error('ユーザーデータの取得に失敗しました', e)
  }
}

// ユーザーIDからユーザー名を取得する関数
function getUserName(userId) {
  const user = users.value.find(u => u.id === userId)
  return user ? user.name : '不明なユーザー'
}

// UTCからJSTに変換する関数
function utcToJst(utcDateString) {
  if (!utcDateString) return ''
  const date = new Date(utcDateString)
  const jstDate = new Date(date.getTime() + (9 * 60 * 60 * 1000))
  return jstDate.toISOString().slice(0, 16)
}

// JSTからUTCに変換する関数
function jstToUtc(jstDateString) {
  if (!jstDateString) return null
  const date = new Date(jstDateString)
  const utcDate = new Date(date.getTime() - (9 * 60 * 60 * 1000))
  return utcDate.toISOString()
}

// JSTで表示するためのフォーマット関数
function formatJstDateTime(utcDateString) {
  if (!utcDateString) return ''
  const date = new Date(utcDateString)
  const jstDate = new Date(date.getTime() + (9 * 60 * 60 * 1000))
  return jstDate.toLocaleString('ja-JP', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
    hour12: false
  })
}

// シフト申請を取得（読み取り専用）
async function fetchShiftRequests() {
  try {
    const res = await axios.get('http://localhost:5174/api/Shift_requests')
    shiftRequests.value = res.data
    updateEventCountMap()
  } catch (e) {
    console.error('シフト申請データの取得に失敗しました', e)
  }
}

// 確定シフトを取得
async function fetchShiftConfirms() {
  try {
    const res = await axios.get('http://localhost:5174/api/Shift_confirms')
    shiftConfirms.value = res.data
    updateEventCountMap()
  } catch (e) {
    console.error('確定シフトデータの取得に失敗しました', e)
  }
}

// イベント数のマップを更新
function updateEventCountMap() {
  const countMap = {}
  const allShifts = [...shiftRequests.value, ...shiftConfirms.value]
  
  allShifts.forEach(shift => {
    if (shift.work_start) {
      const jstDate = new Date(new Date(shift.work_start).getTime() + (9 * 60 * 60 * 1000))
      const date = jstDate.toISOString().slice(0, 10)
      countMap[date] = (countMap[date] || 0) + 1
    }
  })
  eventCountMap.value = countMap
}

// 日付セルのカスタムクラスを返す関数
function dayCellClassNames(arg) {
  const date = arg.date.toISOString().slice(0, 10)
  if (eventCountMap.value[date] !== undefined && eventCountMap.value[date] < 5) {
    return ['few-events']
  }
  return []
}

// イベントクリック処理
function handleEventClick(info) {
  editShift.id = info.event.extendedProps.originalId
  editShift.user_id = info.event.extendedProps.user_id
  editShift.userName = info.event.extendedProps.userName
  editShift.start = utcToJst(info.event.extendedProps.work_start)
  editShift.end = utcToJst(info.event.extendedProps.work_end)
  editShift.isRequest = info.event.extendedProps.isRequest
  isEditModalOpen.value = true
}

// 新規作成モーダルを開く
function openCreateModal() {
  newShift.user_id = ''
  newShift.start = ''
  newShift.end = ''
  isCreateModalOpen.value = true
}

// 新規シフト作成（確定シフトのみ）
async function createShift() {
  try {
    const workStart = newShift.start
    const workEnd = newShift.end
    
    await axios.post('http://localhost:5174/api/Shift_confirms', {
      user_id: newShift.user_id,
      work_start: workStart,
      work_end: workEnd,
      work_date: workStart ? new Date(workStart).toISOString().slice(0, 10) : null
    })
    
    isCreateModalOpen.value = false
    await fetchShiftConfirms()
    alert('シフトが作成されました')
  } catch (e) {
    console.error('作成エラー:', e)
    alert('作成に失敗しました')
  }
}

// シフト保存（確定シフトのみ）
async function saveShift() {
  if (editShift.isRequest) return // 申請は編集不可
  
  try {
    const workStart = editShift.start
    const workEnd = editShift.end
    
    await axios.put(`http://localhost:5174/api/Shift_confirms/${editShift.id}`, {
      id: editShift.id,
      user_id: editShift.user_id,
      work_start: workStart,
      work_end: workEnd,
      work_date: workStart ? new Date(workStart).toISOString().slice(0, 10) : null
    })
    
    isEditModalOpen.value = false
    await fetchShiftConfirms()
    alert('シフトが保存されました')
  } catch (e) {
    console.error('保存エラー:', e)
    alert('保存に失敗しました')
  }
}

// シフト削除（確定シフトのみ）
async function deleteShift() {
  if (editShift.isRequest) return // 申請は削除不可
  
  if (!confirm('本当に削除しますか？')) return
  try {
    await axios.delete(`http://localhost:5174/api/Shift_confirms/${editShift.id}`)
    isEditModalOpen.value = false
    await fetchShiftConfirms()
    alert('シフトが削除されました')
  } catch (e) {
    console.error('削除エラー:', e)
    alert('削除に失敗しました')
  }
}

// シフトを確定する（申請から確定へ移行）
async function confirmShift() {
  if (!editShift.isRequest) return
  
  if (!confirm('このシフトを確定しますか？')) return
  try {
    const workStart = editShift.start
    const workEnd = editShift.end
    
    // shift_confirmsに追加
    await axios.post('http://localhost:5174/api/Shift_confirms', {
      user_id: editShift.user_id,
      work_start: workStart,
      work_end: workEnd,
      work_date: workStart ? new Date(workStart).toISOString().slice(0, 10) : null
    })
    
    isEditModalOpen.value = false
    await fetchShiftRequests()
    await fetchShiftConfirms()
    alert('シフトが確定されました')
  } catch (e) {
    console.error('確定エラー:', e)
    alert('確定に失敗しました')
  }
}

const advancedCalendarOptions = computed(() => ({
  plugins: [
    dayGridPlugin,
    timeGridPlugin,
    listPlugin,
    interactionPlugin
  ],
  initialView: 'dayGridMonth',
  headerToolbar: {
    left: 'prev,next today',
    center: 'title',
    right: 'dayGridMonth,timeGridWeek,timeGridDay,listWeek'
  },
  selectable: true,
  select: function(info) {
    // カレンダー上での範囲選択時の処理
    const startJst = utcToJst(info.start.toISOString())
    const endJst = utcToJst(info.end.toISOString())
    newShift.start = startJst
    newShift.end = endJst
    openCreateModal()
  },
  eventClick: handleEventClick,
  events: displayEvents.value,
  locale: 'ja',
  height: 'auto',
  dayCellClassNames,
  timeZone: 'Asia/Tokyo',
  eventTimeFormat: {
    hour: '2-digit',
    minute: '2-digit',
    meridiem: false
  }
}))
</script>

<style>
.few-events {
  background-color: #ffcccc !important;
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
  min-width: 400px;
  box-shadow: 0 2px 8px rgba(0,0,0,0.2);
}
.modal label {
  display: block;
  margin-bottom: 12px;
  font-weight: bold;
}
.modal input, .modal select {
  width: 100%;
  padding: 8px;
  border: 1px solid #ddd;
  border-radius: 4px;
  margin-top: 4px;
}
.modal input[readonly] {
  background-color: #f8f9fa;
  color: #6c757d;
}
.modal button {
  padding: 8px 16px;
  border: none;
  border-radius: 4px;
  cursor: pointer;
  background: #007bff;
  color: white;
}
.modal button:hover {
  opacity: 0.8;
}
label input[type="checkbox"] {
  width: auto;
  margin-right: 8px;
}
</style>
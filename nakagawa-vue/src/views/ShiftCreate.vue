<!-- ShiftCreate -->
<template>
  <div class="shift-create-container">
    <!-- 戻るボタン -->
    <div class="back-button-wrapper">
      <BackButton />
    </div>

    <!-- ヘッダー -->
    <div class="header">
      <h2>シフト管理</h2>
      <div class="controls">
        <div class="store-tabs">
          <button 
            v-for="store in stores" 
            :key="store.id"
            @click="activeStore = store.id"
            :class="['store-tab', { active: activeStore === store.id }]"
          >
            {{ store.name }}
          </button>
        </div>
        <div class="checkbox-group">
          <label class="checkbox-label">
            <input type="checkbox" v-model="showRequests" /> シフト申請を表示
          </label>
          <label class="checkbox-label">
            <input type="checkbox" v-model="showConfirms" /> 確定シフトを表示
          </label>
        </div>
        <button @click="openCreateModal" class="btn btn-primary">
          新規シフト作成
        </button>
      </div>
    </div>
    
    <FullCalendar
      :key="activeStore"
      :options="advancedCalendarOptions"
    />
    
    <!-- イベントがない場合の表示 -->
    <div v-if="displayEvents.length === 0" class="no-events">
      {{ stores.find(s => s.id === activeStore)?.name }}のシフトがありません
    </div>
    
    <!-- 新規作成用モーダル -->
    <div v-if="isCreateModalOpen" class="popup-overlay" @click="closeCreateModal">
      <div class="popup-content" @click.stop>
        <div class="popup-header">
          <h3>新規シフト作成 - {{ stores.find(s => s.id === activeStore)?.name }}</h3>
          <button @click="isCreateModalOpen=false" class="close-btn">&times;</button>
        </div>
        
        <form @submit.prevent="createShift" class="shift-form">
          <div class="form-group">
            <label>店舗:</label>
            <div class="store-info">{{ stores.find(s => s.id === activeStore)?.name }}</div>
          </div>
          <div class="form-group">
            <label for="user_select">従業員:</label>
            <select v-model="newShift.user_id" required class="form-control" id="user_select">
              <option value="">選択してください</option>
              <option v-for="user in storeUsers" :key="user.id" :value="user.id">
                {{ user.name }}
              </option>
            </select>
          </div>
          <div class="form-group">
            <label for="start_time">開始時間:</label>
            <input type="datetime-local" v-model="newShift.start" required class="form-control" id="start_time" />
          </div>
          <div class="form-group">
            <label for="end_time">終了時間:</label>
            <input type="datetime-local" v-model="newShift.end" required class="form-control" id="end_time" />
          </div>
          <div class="work-hours-display" v-if="calculateNewShiftHours() > 0">
            <span>勤務時間: {{ calculateNewShiftHours() }}時間</span>
          </div>
          <div class="form-actions">
            <button type="submit" class="btn btn-primary" :disabled="isSubmitting">
              {{ isSubmitting ? '作成中...' : '作成' }}
            </button>
            <button type="button" @click="isCreateModalOpen=false" class="btn btn-secondary">キャンセル</button>
          </div>
        </form>
      </div>
    </div>
    
    <!-- 編集用モーダル -->
    <div v-if="isEditModalOpen" class="popup-overlay" @click="closeEditModal">
      <div class="popup-content" @click.stop>
        <div class="popup-header">
          <h3>{{ editShift.isRequest ? 'シフト申請詳細' : 'シフト編集' }}</h3>
          <button @click="isEditModalOpen=false" class="close-btn">&times;</button>
        </div>
        
        <form @submit.prevent="saveShift" class="shift-form">
          <div class="form-group">
            <label>店舗:</label>
            <div class="store-info">{{ editShift.storeName }}</div>
          </div>
          <div class="form-group">
            <label>従業員:</label>
            <div class="user-info">{{ editShift.userName }}</div>
          </div>
          <div class="form-group">
            <label for="edit_start">開始時間:</label>
            <input 
              type="datetime-local" 
              v-model="editShift.start" 
              :readonly="editShift.isRequest"
              required 
              class="form-control"
              :class="{ 'readonly-input': editShift.isRequest }"
              id="edit_start"
            />
          </div>
          <div class="form-group">
            <label for="edit_end">終了時間:</label>
            <input 
              type="datetime-local" 
              v-model="editShift.end" 
              :readonly="editShift.isRequest"
              required 
              class="form-control"
              :class="{ 'readonly-input': editShift.isRequest }"
              id="edit_end"
            />
          </div>
          <div class="work-hours-display" v-if="calculateEditShiftHours() > 0">
            <span>勤務時間: {{ calculateEditShiftHours() }}時間</span>
          </div>
          <div class="form-actions">
            <div v-if="editShift.isRequest">
              <!-- シフト申請の場合：確定のみ可能 -->
              <button type="button" @click="confirmShift" class="btn btn-primary" :disabled="isSubmitting">
                {{ isSubmitting ? '確定中...' : '確定' }}
              </button>
            </div>
            <div v-else>
              <!-- 確定シフトの場合：編集・削除可能 -->
              <button type="submit" class="btn btn-primary" :disabled="isSubmitting">
                {{ isSubmitting ? '保存中...' : '保存' }}
              </button>
              <button type="button" @click="deleteShift" class="btn btn-danger" :disabled="isSubmitting">削除</button>
            </div>
            <button type="button" @click="isEditModalOpen=false" class="btn btn-secondary">キャンセル</button>
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
import BackButton from '../components/BackButton.vue'

const shiftRequests = ref([])
const shiftConfirms = ref([])
const confirmedRequestIds = ref([]) // 確定済み申請IDのリスト
const shiftConfirmToRequestMap = ref({}) // 確定シフトIDと申請IDの対応マップ
const eventCountMap = ref({})
const isEditModalOpen = ref(false)
const isCreateModalOpen = ref(false)
const showRequests = ref(true)
const showConfirms = ref(true)
const isSubmitting = ref(false)
const activeStore = ref(1) // デフォルトは店舗1

const stores = ref([
  { id: 1, name: '1号店' },
  { id: 2, name: '2号店' },
  { id: 3, name: '3号店' }
])

const editShift = reactive({ 
  id: '', 
  user_id: '', 
  userName: '', 
  storeName: '',
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

// 現在のアクティブな店舗のユーザーのみを取得
const storeUsers = computed(() => {
  return users.value.filter(user => user.store === activeStore.value)
})

// 表示するイベントを計算（現在のアクティブな店舗のみ）
const displayEvents = computed(() => {
  const events = []
  
  if (showRequests.value) {
    // 確定済みでない申請のみ表示（現在の店舗のみ）
    const visibleRequests = shiftRequests.value.filter(shift => 
      !confirmedRequestIds.value.includes(shift.id) && 
      getUserStore(shift.user_id) === activeStore.value
    )
    
    events.push(...visibleRequests.map(shift => ({
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
        storeName: getStoreName(getUserStore(shift.user_id)),
        work_start: shift.work_start,
        work_end: shift.work_end,
        isRequest: true
      }
    })))
  }
  
  if (showConfirms.value) {
    // 確定シフト（現在の店舗のみ）
    const storeConfirms = shiftConfirms.value.filter(shift => 
      getUserStore(shift.user_id) === activeStore.value
    )
    
    events.push(...storeConfirms.map(shift => ({
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
        storeName: getStoreName(getUserStore(shift.user_id)),
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

// ユーザーIDから店舗番号を取得する関数
function getUserStore(userId) {
  const user = users.value.find(u => u.id === userId)
  return user ? user.store : 1
}

// 店舗番号から店舗名を取得する関数
function getStoreName(storeId) {
  const store = stores.value.find(s => s.id === storeId)
  return store ? store.name : '不明な店舗'
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

// イベント数のマップを更新（現在の店舗のみ）
function updateEventCountMap() {
  const countMap = {}
  
  // 表示される申請のみをカウント（現在の店舗のみ）
  const visibleRequests = shiftRequests.value.filter(shift => 
    !confirmedRequestIds.value.includes(shift.id) && 
    getUserStore(shift.user_id) === activeStore.value
  )
  const storeConfirms = shiftConfirms.value.filter(shift => 
    getUserStore(shift.user_id) === activeStore.value
  )
  const allShifts = [...visibleRequests, ...storeConfirms]
  
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
  editShift.storeName = info.event.extendedProps.storeName
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

// モーダルを閉じる
function closeCreateModal() {
  isCreateModalOpen.value = false
}

function closeEditModal() {
  isEditModalOpen.value = false
}

// 新規シフト勤務時間計算
function calculateNewShiftHours() {
  if (!newShift.start || !newShift.end) return 0
  const start = new Date(newShift.start)
  const end = new Date(newShift.end)
  return Math.max(0, (end - start) / (1000 * 60 * 60))
}

// 編集シフト勤務時間計算
function calculateEditShiftHours() {
  if (!editShift.start || !editShift.end) return 0
  const start = new Date(editShift.start)
  const end = new Date(editShift.end)
  return Math.max(0, (end - start) / (1000 * 60 * 60))
}

// 新規シフト作成（確定シフトのみ）
async function createShift() {
  if (new Date(newShift.start) >= new Date(newShift.end)) {
    alert('終了時間は開始時間より後に設定してください')
    return
  }

  isSubmitting.value = true
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
  } finally {
    isSubmitting.value = false
  }
}

// シフト保存（確定シフトのみ）
async function saveShift() {
  if (editShift.isRequest) return // 申請は編集不可
  
  if (new Date(editShift.start) >= new Date(editShift.end)) {
    alert('終了時間は開始時間より後に設定してください')
    return
  }

  isSubmitting.value = true
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
  } finally {
    isSubmitting.value = false
  }
}

// シフト削除（確定シフトのみ）
async function deleteShift() {
  if (editShift.isRequest) return // 申請は削除不可
  
  if (!confirm('本当に削除しますか？')) return
  
  isSubmitting.value = true
  try {
    const confirmId = editShift.id
    
    // 削除前に対応する申請IDを取得
    const correspondingRequestId = shiftConfirmToRequestMap.value[confirmId]
    
    await axios.delete(`http://localhost:5174/api/Shift_confirms/${confirmId}`)
    
    // 対応する申請がある場合は再表示
    if (correspondingRequestId) {
      const index = confirmedRequestIds.value.indexOf(correspondingRequestId)
      if (index > -1) {
        confirmedRequestIds.value.splice(index, 1)
      }
      // マップからも削除
      delete shiftConfirmToRequestMap.value[confirmId]
    }
    
    isEditModalOpen.value = false
    await fetchShiftConfirms()
    updateEventCountMap()
    
    alert('シフトが削除されました')
  } catch (e) {
    console.error('削除エラー:', e)
    alert('削除に失敗しました')
  } finally {
    isSubmitting.value = false
  }
}

// シフトを確定する（申請から確定へ移行し、申請を非表示にする）
async function confirmShift() {
  if (!editShift.isRequest) return
  
  if (!confirm('このシフトを確定しますか？')) return
  
  isSubmitting.value = true
  try {
    const workStart = editShift.start
    const workEnd = editShift.end
    const requestId = editShift.id
    
    // 1. shift_confirmsに追加
    const response = await axios.post('http://localhost:5174/api/Shift_confirms', {
      user_id: editShift.user_id,
      work_start: workStart,
      work_end: workEnd,
      work_date: workStart ? new Date(workStart).toISOString().slice(0, 10) : null
    })
    
    // 2. 確定済み申請IDリストに追加（データは削除しない）
    confirmedRequestIds.value.push(requestId)
    
    // 3. 確定シフトIDと申請IDの対応を記録
    const newConfirmId = response.data.id || response.data
    if (newConfirmId) {
      shiftConfirmToRequestMap.value[newConfirmId] = requestId
    }
    
    isEditModalOpen.value = false
    await fetchShiftConfirms()
    updateEventCountMap() // イベント数マップを更新
    alert('シフトが確定されました')
  } catch (e) {
    console.error('確定エラー:', e)
    alert('確定に失敗しました')
    await fetchShiftConfirms()
  } finally {
    isSubmitting.value = false
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

<style scoped>
.shift-create-container {
  padding: 20px;
  max-width: 1200px;
  margin: 0 auto;
}

.header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 20px;
  flex-wrap: wrap;
  gap: 15px;
  border-bottom: 2px solid #eee;
  padding-bottom: 10px;
}

.header h2 {
  margin: 0;
  color: #333;
  font-size: 24px;
}

.controls {
  display: flex;
  flex-wrap: wrap;
  gap: 15px;
  align-items: center;
}

.store-tabs {
  display: flex;
  gap: 10px;
  flex-wrap: wrap;
}

.store-tab {
  background-color: #f1f1f1;
  border: 1px solid #ccc;
  padding: 8px 12px;
  border-radius: 4px;
  cursor: pointer;
  font-weight: bold;
  color: #333;
}

.store-tab.active {
  background-color: #28a745;
  color: white;
  border-color: #28a745;
}

.checkbox-group {
  display: flex;
  gap: 15px;
  flex-wrap: wrap;
}

.checkbox-label {
  display: flex;
  align-items: center;
  font-size: 14px;
  color: #495057;
}

.checkbox-label input[type="checkbox"] {
  margin-right: 6px;
}

.no-events {
  margin-top: 20px;
  text-align: center;
  color: #721c24;
  background-color: #f8d7da;
  padding: 15px;
  border-radius: 4px;
  border: 1px solid #f5c6cb;
}

.popup-overlay {
  position: fixed;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  background-color: rgba(0, 0, 0, 0.5);
  z-index: 1000;
  display: flex;
  justify-content: center;
  align-items: center;
}

.popup-content {
  background: white;
  border-radius: 8px;
  width: 90%;
  max-width: 500px;
  box-shadow: 0 4px 8px rgba(0,0,0,0.1);
  overflow-y: auto;
  max-height: 90vh;
}

.popup-header {
  background-color: #28a745;
  color: white;
  padding: 20px;
  display: flex;
  justify-content: space-between;
  align-items: center;
  border-radius: 8px 8px 0 0;
}

.popup-header h3 {
  margin: 0;
  font-size: 18px;
}

.close-btn {
  font-size: 24px;
  background: none;
  border: none;
  color: white;
  cursor: pointer;
}

.close-btn:hover {
  opacity: 0.8;
}

.shift-form {
  padding: 20px;
}

.form-group {
  margin-bottom: 15px;
}

.form-group label {
  font-weight: bold;
  margin-bottom: 5px;
  display: block;
}

.form-control {
  width: 100%;
  padding: 10px;
  font-size: 15px;
  border: 1px solid #ddd;
  border-radius: 4px;
}

.readonly-input {
  background-color: #e9ecef;
  color: #6c757d;
}

.user-info,
.store-info {
  background-color: #f8f9fa;
  padding: 10px;
  border-radius: 4px;
  font-weight: bold;
}

.work-hours-display {
  background-color: #e9f7ef;
  color: #155724;
  padding: 10px;
  border-radius: 4px;
  font-weight: bold;
  text-align: center;
  margin-top: 10px;
}

.form-actions {
  display: flex;
  flex-wrap: wrap;
  justify-content: flex-end;
  gap: 10px;
}

.btn {
  padding: 10px 15px;
  font-weight: bold;
  font-size: 14px;
  border: none;
  border-radius: 4px;
  cursor: pointer;
}

.btn:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.btn-primary {
  background-color: #28a745;
  color: white;
}

.btn-primary:hover:not(:disabled) {
  background-color: #218838;
}

.btn-secondary {
  background-color: #6c757d;
  color: white;
}

.btn-secondary:hover {
  background-color: #5a6268;
}

.btn-danger {
  background-color: #dc3545;
  color: white;
}

.btn-danger:hover:not(:disabled) {
  background-color: #c82333;
}

:deep(.fc-event) {
  font-size: 12px;
  cursor: pointer;
}

:deep(.fc-daygrid-day) {
  cursor: pointer;
}

:deep(.fc-daygrid-day:hover) {
  background-color: #f1f1f1;
}

.few-events {
  background-color: #fff3cd !important;
}

/* レスポンシブ */
@media (max-width: 768px) {
  .header {
    flex-direction: column;
    align-items: flex-start;
  }

  .controls {
    flex-direction: column;
    align-items: stretch;
    gap: 10px;
  }

  .form-actions {
    flex-direction: column;
    align-items: stretch;
  }

  .btn {
    width: 100%;
  }
}

</style>
<template>
  <div class="shift-submit-container">
    <div class="header">
      <h2>シフト希望提出</h2>
      <div class="user-info" v-if="currentUser">
        <span>ユーザー: {{ currentUser.name }}</span>
      </div>
    </div>

    <FullCalendar :options="calendarOptions" />
    
    <!-- シフト希望入力ポップアップ -->
    <div v-if="showPopup" class="popup-overlay" @click="closePopup">
      <div class="popup-content" @click.stop>
        <div class="popup-header">
          <h3>{{ selectedDate }} のシフト希望</h3>
          <button @click="closePopup" class="close-btn">&times;</button>
        </div>
        
        <form @submit.prevent="submitShiftRequest" class="shift-form">
          <input type="hidden" v-model="shiftRequest.id">
          
          <div class="form-group">
            <label for="start_work">希望開始時間:</label>
            <input
              type="datetime-local"
              id="start_work"
              v-model="shiftRequest.work_start"
              required
              class="form-control"
            >
          </div>
          
          <div class="form-group">
            <label for="end_work">希望終了時間:</label>
            <input
              type="datetime-local"
              id="end_work"
              v-model="shiftRequest.work_end"
              required
              class="form-control"
            >
          </div>
          
          <div class="work-hours-display" v-if="calculateWorkHours() > 0">
            <span>勤務時間: {{ calculateWorkHours() }}時間</span>
          </div>
          
          <div class="form-actions">
            <button type="submit" class="btn btn-primary" :disabled="isSubmitting">
              {{ isSubmitting ? '送信中...' : '希望を提出' }}
            </button>
            <button type="button" @click="deleteShiftRequest" v-if="shiftRequest.id" class="btn btn-danger">削除</button>
            <button type="button" @click="closePopup" class="btn btn-secondary">キャンセル</button>
          </div>
        </form>
      </div>
    </div>

    <!-- 提出済みシフト希望一覧 -->
    <div class="submitted-shifts">
      <h3>提出済みシフト希望</h3>
      <div v-if="submittedRequests.length === 0" class="no-requests">
        まだシフト希望が提出されていません
      </div>
      <div v-else class="requests-list">
        <div v-for="request in submittedRequests" :key="request.id" class="request-item">
          <div class="request-date">
            {{ formatDate(request.work_start) }}
          </div>
          <div class="request-time">
            {{ formatTime(request.work_start) }} - {{ formatTime(request.work_end) }}
            ({{ calculateHours(request.work_start, request.work_end) }}時間)
          </div>
          <button @click="editShiftRequest(request)" class="btn btn-sm btn-outline">編集</button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted, computed } from 'vue'
import FullCalendar from '@fullcalendar/vue3'
import dayGridPlugin from '@fullcalendar/daygrid'
import interactionPlugin from '@fullcalendar/interaction'
import { useUser } from '../components/useUser'

// Props（APIベースURLのみ）
const props = defineProps({
  apiBaseUrl: {
    type: String,
    default: 'http://localhost:5174/api'
  }
})

// ユーザー情報の取得
const { currentUser, userId } = useUser()
const userDbId = ref(null) // DBのUser.idを保持

// UTC時刻をJSTに変換する関数
const utcToJst = (utcString) => {
  if (!utcString) return ''
  const utcDate = new Date(utcString)
  // JSTはUTC+9時間
  const jstDate = new Date(utcDate.getTime() + (9 * 60 * 60 * 1000))
  return jstDate.toISOString().slice(0, 16) // YYYY-MM-DDTHH:mm形式
}

// JST時刻をUTCに変換する関数
const jstToUtc = (jstString) => {
  if (!jstString) return ''
  const jstDate = new Date(jstString)
  // JSTからUTCに変換（9時間戻す）
  const utcDate = new Date(jstDate.getTime() - (9 * 60 * 60 * 1000))
  return utcDate.toISOString()
}

// UTC時刻からJSTのDateオブジェクトを作成
const utcToJstDate = (utcString) => {
  if (!utcString) return null
  const utcDate = new Date(utcString)
  return new Date(utcDate.getTime() + (9 * 60 * 60 * 1000))
}

// onMountedでDBのUser.idを取得
onMounted(async () => {
  if (!userId.value) {
    alert('ユーザー情報が取得できません。ログインし直してください。')
    return
  }

  // DBのUser.idを取得（user_id=社員番号から）
  try {
    const res = await fetch(`${props.apiBaseUrl}/Users/by-userid/${userId.value}`)
    if (res.ok) {
      const user = await res.json()
      userDbId.value = user.id // ここがDBのUser.id
    } else {
      throw new Error(`ステータス: ${res.status}`)
    }
  } catch (err) {
    console.error('User.idの取得に失敗:', err)
    alert('ユーザー情報の取得に失敗しました')
    return
  }

  await loadSubmittedRequests()
  updateCalendarEvents()
})

// リアクティブデータ
const showPopup = ref(false)
const selectedDate = ref('')
const submittedRequests = ref([])
const isSubmitting = ref(false)

// シフト希望入力用のデータ
const shiftRequest = reactive({
  id: null,
  work_start: '',
  work_end: '',
  user_id: null
})

// カレンダーのオプション設定
const calendarOptions = ref({
  plugins: [dayGridPlugin, interactionPlugin],
  initialView: 'dayGridMonth',
  headerToolbar: {
    left: 'prev,next today',
    center: 'title',
    right: 'dayGridMonth'
  },
  locale: 'ja',
  height: 'auto',
  selectable: true,
  // 日付クリック時の処理
  dateClick: function(info) {
    handleDateClick(info.dateStr)
  },
  // イベントクリック時の処理
  eventClick: function(info) {
    const requestId = parseInt(info.event.id)
    const request = submittedRequests.value.find(r => r.id === requestId)
    if (request) {
      editShiftRequest(request)
    }
  },
  events: []
})

// 提出済みシフト希望を取得（userDbIdを使用）
const loadSubmittedRequests = async () => {
  if (!userDbId.value) return

  try {
    const response = await fetch(`${props.apiBaseUrl}/Shift_requests/by-userid/${userDbId.value}`)
    if (response.ok) {
      submittedRequests.value = await response.json()
    } else {
      console.error('シフト希望の取得に失敗しました:', response.status)
    }
  } catch (error) {
    console.error('シフト希望の取得に失敗しました:', error)
  }
}

// 日付クリック処理
const handleDateClick = (dateStr) => {
  if (!userId.value) {
    alert('ユーザー情報が取得できません。')
    return
  }
  
  selectedDate.value = dateStr
  
  // 既存のシフト希望があるかチェック（JST基準で日付比較）
  const existingRequest = submittedRequests.value.find(request => {
    if (!request.work_start) return false
    const jstDate = utcToJstDate(request.work_start)
    const requestDateStr = jstDate.toISOString().split('T')[0]
    return requestDateStr === dateStr
  })
  
  if (existingRequest) {
    editShiftRequest(existingRequest)
  } else {
    // 新規入力
    resetShiftRequest()
    shiftRequest.work_start = `${dateStr}T09:00`
    shiftRequest.work_end = `${dateStr}T17:00`
  }
  
  showPopup.value = true
}

// 既存シフト希望編集（UTC → JST変換）
const editShiftRequest = (request) => {
  shiftRequest.id = request.id
  shiftRequest.work_start = utcToJst(request.work_start)
  shiftRequest.work_end = utcToJst(request.work_end)
  shiftRequest.user_id = request.user_id
  
  // selectedDateもJST基準で設定
  const jstDate = utcToJstDate(request.work_start)
  selectedDate.value = jstDate.toISOString().split('T')[0]
  showPopup.value = true
}

// 新規入力用reset（userDbId使用）
const resetShiftRequest = () => {
  shiftRequest.id = null
  shiftRequest.work_start = ''
  shiftRequest.work_end = ''
  shiftRequest.user_id = userDbId.value
}

// ポップアップを閉じる
const closePopup = () => {
  showPopup.value = false
  resetShiftRequest()
}

// 勤務時間計算（JST基準）
const calculateWorkHours = () => {
  if (!shiftRequest.work_start || !shiftRequest.work_end) return 0
  const start = new Date(shiftRequest.work_start)
  const end = new Date(shiftRequest.work_end)
  return Math.max(0, (end - start) / (1000 * 60 * 60))
}

// submitShiftRequest
const submitShiftRequest = async () => {
  if (!userDbId.value) {
    alert('ユーザー情報が取得できません。')
    return
  }

  if (new Date(shiftRequest.work_start) >= new Date(shiftRequest.work_end)) {
    alert('終了時間は開始時間より後に設定してください')
    return
  }

  isSubmitting.value = true

  try {
    const requestData = {
      work_start: shiftRequest.work_start, // JST → UTC変換 out
      work_end: shiftRequest.work_end, // JST → UTC変換 out
      user_id: userDbId.value
    }

    let response
    if (shiftRequest.id) {
      response = await fetch(`${props.apiBaseUrl}/Shift_requests/${shiftRequest.id}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ id: shiftRequest.id, ...requestData })
      })
    } else {
      response = await fetch(`${props.apiBaseUrl}/Shift_requests`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(requestData)
      })
    }

    if (response.ok) {
      await loadSubmittedRequests()
      updateCalendarEvents()
      closePopup()
      alert('シフト希望が正常に提出されました')
    } else {
      const errorText = await response.text()
      console.error('Server error:', errorText)
      alert(`エラーが発生しました: ${response.status}: ${errorText}`)
    }
  } catch (error) {
    console.error('送信エラー:', error)
    alert('通信エラーが発生しました')
  } finally {
    isSubmitting.value = false
  }
}

// シフト希望を削除
const deleteShiftRequest = async () => {
  if (!confirm('このシフト希望を削除しますか？')) return
  
  try {
    const response = await fetch(`${props.apiBaseUrl}/Shift_requests/${shiftRequest.id}`, {
      method: 'DELETE'
    })
    
    if (response.ok) {
      submittedRequests.value = submittedRequests.value.filter(r => r.id !== shiftRequest.id)
      updateCalendarEvents()
      closePopup()
      alert('シフト希望が削除されました')
    } else {
      let errorMessage = '削除に失敗しました'
      try {
        const errorData = await response.json()
        errorMessage = errorData.message || errorMessage
      } catch (e) {
        console.error('エラーレスポンスの解析に失敗:', e)
      }
      alert(errorMessage)
    }
  } catch (error) {
    console.error('削除エラー:', error)
    alert('通信エラーが発生しました')
  }
}

// カレンダーのイベントを更新（JST表示）
const updateCalendarEvents = () => {
  const events = submittedRequests.value.map(request => {
    if (!request.work_start || !request.work_end) return null
    
    const startDate = new Date(request.work_start)
    const endDate = new Date(request.work_end)
    const workHours = ((endDate - startDate) / (1000 * 60 * 60)).toFixed(1)
    
    return {
      id: request.id.toString(),
      title: `希望: ${startDate.toLocaleTimeString('ja-JP', {hour: '2-digit', minute: '2-digit'})} - ${endDate.toLocaleTimeString('ja-JP', {hour: '2-digit', minute: '2-digit'})} (${workHours}h)`,
      start: startDate, // FullCalendarはUTCで管理するが、表示時にJSTで表示される ???
      end: endDate,
      backgroundColor: '#28a745',
      borderColor: '#28a745'
    }
  }).filter(event => event !== null)
  
  calendarOptions.value.events = events
}


// 日付フォーマット（JST表示）
const formatDate = (dateString) => {
  if (!dateString) return ''
  const jstDate = new Date(dateString)
  return jstDate.toLocaleDateString('ja-JP')
}

// 時間フォーマット（JST表示）
const formatTime = (dateString) => {
  if (!dateString) return ''
  const jstDate = new Date(dateString)
  return jstDate.toLocaleTimeString('ja-JP', {hour: '2-digit', minute: '2-digit'})
}

// 勤務時間計算（文字列から、JST基準）
const calculateHours = (start, end) => {
  if (!start || !end) return '0'
  const startDate = utcToJstDate(start)
  const endDate = utcToJstDate(end)
  return ((endDate - startDate) / (1000 * 60 * 60)).toFixed(1)
}
</script>

<style scoped>
.shift-submit-container {
  padding: 20px;
  max-width: 1200px;
  margin: 0 auto;
}

.header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 20px;
  padding-bottom: 10px;
  border-bottom: 2px solid #eee;
}

.header h2 {
  color: #333;
  margin: 0;
}

.user-info {
  background-color: #f8f9fa;
  padding: 8px 16px;
  border-radius: 4px;
  font-weight: bold;
  color: #495057;
}

.popup-overlay {
  position: fixed;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  background-color: rgba(0, 0, 0, 0.5);
  display: flex;
  justify-content: center;
  align-items: center;
  z-index: 1000;
}

.popup-content {
  background: white;
  border-radius: 8px;
  padding: 0;
  width: 90%;
  max-width: 500px;
  box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
}

.popup-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 20px;
  border-bottom: 1px solid #eee;
  background-color: #28a745;
  color: white;
  border-radius: 8px 8px 0 0;
}

.popup-header h3 {
  margin: 0;
}

.close-btn {
  background: none;
  border: none;
  font-size: 24px;
  cursor: pointer;
  color: white;
  padding: 0;
  width: 30px;
  height: 30px;
  display: flex;
  align-items: center;
  justify-content: center;
}

.close-btn:hover {
  opacity: 0.8;
}

.shift-form {
  padding: 20px;
}

.form-group {
  margin-bottom: 20px;
}

.form-group label {
  display: block;
  margin-bottom: 5px;
  font-weight: bold;
  color: #333;
}

.form-control {
  width: 100%;
  padding: 10px;
  border: 1px solid #ddd;
  border-radius: 4px;
  font-size: 16px;
}

.form-control:focus {
  outline: none;
  border-color: #28a745;
  box-shadow: 0 0 0 2px rgba(40, 167, 69, 0.25);
}

.work-hours-display {
  background-color: #e9f7ef;
  padding: 10px;
  border-radius: 4px;
  margin-bottom: 20px;
  text-align: center;
  font-weight: bold;
  color: #155724;
}

.form-actions {
  display: flex;
  gap: 10px;
  justify-content: flex-end;
  margin-top: 20px;
}

.btn {
  padding: 10px 20px;
  border: none;
  border-radius: 4px;
  cursor: pointer;
  font-size: 14px;
  font-weight: bold;
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

.btn-danger {
  background-color: #dc3545;
  color: white;
}

.btn-danger:hover {
  background-color: #c82333;
}

.btn-secondary {
  background-color: #6c757d;
  color: white;
}

.btn-secondary:hover {
  background-color: #545b62;
}

.submitted-shifts {
  margin-top: 40px;
  background-color: #f8f9fa;
  padding: 20px;
  border-radius: 8px;
}

.submitted-shifts h3 {
  margin-top: 0;
  color: #333;
}

.no-requests {
  text-align: center;
  color: #666;
  padding: 20px;
}

.requests-list {
  display: grid;
  gap: 10px;
}

.request-item {
  background: white;
  padding: 15px;
  border-radius: 4px;
  display: flex;
  justify-content: space-between;
  align-items: center;
  border-left: 4px solid #28a745;
}

.request-date {
  font-weight: bold;
  color: #333;
}

.request-time {
  color: #666;
  font-size: 14px;
}

.btn-sm {
  padding: 5px 10px;
  font-size: 12px;
}

.btn-outline {
  background: none;
  border: 1px solid #28a745;
  color: #28a745;
}

.btn-outline:hover {
  background-color: #28a745;
  color: white;
}

/* FullCalendar のスタイル調整 */
:deep(.fc-event) {
  font-size: 12px;
  cursor: pointer;
}

:deep(.fc-daygrid-day) {
  cursor: pointer;
}

:deep(.fc-daygrid-day:hover) {
  background-color: #f8f9fa;
}
</style>
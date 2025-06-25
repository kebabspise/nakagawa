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

// Props（親コンポーネントから渡される）
const props = defineProps({
  userId: {
    type: Number,
    required: true
  },
  apiBaseUrl: {
    type: String,
    default: 'http://localhost:5174/api'
  }
})

// リアクティブデータ
const showPopup = ref(false)
const selectedDate = ref('')
const submittedRequests = ref([])
const isSubmitting = ref(false)
const currentUser = ref(null)

// シフト希望入力用のデータ （プロパティ名を修正）
const shiftRequest = reactive({
  id: null,
  work_start: '', // start_work → work_start に変更
  work_end: '',   // end_work → work_end に変更
  userId: props.userId
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

// コンポーネントマウント時の処理
onMounted(async () => {
  await loadUserInfo()
  await loadSubmittedRequests()
  updateCalendarEvents()
})

// ユーザー情報を取得
const loadUserInfo = async () => {
  try {
    const response = await fetch(`${props.apiBaseUrl}/users/${props.userId}`)
    if (response.ok) {
      currentUser.value = await response.json()
    } else {
      console.error('ユーザー情報の取得に失敗しました:', response.status)
    }
  } catch (error) {
    console.error('ユーザー情報の取得に失敗しました:', error)
  }
}

// 提出済みシフト希望を取得 （APIエンドポイントとフィルタリングを修正）
const loadSubmittedRequests = async () => {
  try {
    // APIエンドポイントをC#コントローラーに合わせて修正
    const response = await fetch(`${props.apiBaseUrl}/Shift_requests`)
    if (response.ok) {
      const allRequests = await response.json()
      // クライアントサイドでuserIdフィルタリング
      submittedRequests.value = allRequests.filter(request => request.userId === props.userId)
    } else {
      console.error('シフト希望の取得に失敗しました:', response.status)
    }
  } catch (error) {
    console.error('シフト希望の取得に失敗しました:', error)
  }
}

// 日付クリック処理 （プロパティ名を修正）
const handleDateClick = (dateStr) => {
  selectedDate.value = dateStr
  
  // 既存のシフト希望があるかチェック
  const existingRequest = submittedRequests.value.find(request => 
    request.work_start.startsWith(dateStr) // start_work → work_start に変更
  )
  
  if (existingRequest) {
    editShiftRequest(existingRequest)
  } else {
    // 新規入力
    resetShiftRequest()
    shiftRequest.work_start = `${dateStr}T09:00` // start_work → work_start に変更
    shiftRequest.work_end = `${dateStr}T17:00`   // end_work → work_end に変更
  }
  
  showPopup.value = true
}

// 既存シフト希望編集 （プロパティ名を修正）
const editShiftRequest = (request) => {
  shiftRequest.id = request.id
  shiftRequest.work_start = request.work_start.slice(0, 16) // start_work → work_start に変更
  shiftRequest.work_end = request.work_end.slice(0, 16)     // end_work → work_end に変更
  shiftRequest.userId = request.userId
  selectedDate.value = request.work_start.split('T')[0] // start_work → work_start に変更
  showPopup.value = true
}

// シフト希望データリセット （プロパティ名を修正）
const resetShiftRequest = () => {
  shiftRequest.id = null
  shiftRequest.work_start = '' // start_work → work_start に変更
  shiftRequest.work_end = ''   // end_work → work_end に変更
  shiftRequest.userId = props.userId
}

// ポップアップを閉じる
const closePopup = () => {
  showPopup.value = false
  resetShiftRequest()
}

// 勤務時間計算 （プロパティ名を修正）
const calculateWorkHours = () => {
  if (!shiftRequest.work_start || !shiftRequest.work_end) return 0 // プロパティ名変更
  const start = new Date(shiftRequest.work_start)
  const end = new Date(shiftRequest.work_end)
  return Math.max(0, (end - start) / (1000 * 60 * 60))
}

// シフト希望を提出 （APIとプロパティ名を修正）
const submitShiftRequest = async () => {
  // バリデーション
  if (new Date(shiftRequest.work_start) >= new Date(shiftRequest.work_end)) {
    alert('終了時間は開始時間より後に設定してください')
    return
  }
  
  isSubmitting.value = true
  
  try {
    // リクエストデータを準備（C#モデルに合わせる） Work_start, Work_end に変更
    const requestData = {
      Work_start: new Date(shiftRequest.work_start).toISOString(),
      Work_end: new Date(shiftRequest.work_end).toISOString(),
      UserId: shiftRequest.userId
    }
    
    let response
    if (shiftRequest.id) {
      // 更新
      response = await fetch(`${props.apiBaseUrl}/Shift_requests/${shiftRequest.id}`, {
        method: 'PUT',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ 
          Id: shiftRequest.id,
          Work_start: requestData.Work_start,
          Work_end: requestData.Work_end,
          UserId: requestData.UserId
        })
      })
    } else {
      // 新規作成
      response = await fetch(`${props.apiBaseUrl}/Shift_requests`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(requestData)
      })
    }
    
    if (response.ok) {
      const savedRequest = await response.json()
      
      if (shiftRequest.id) {
        // 既存データを更新
        const index = submittedRequests.value.findIndex(r => r.id === shiftRequest.id)
        if (index !== -1) {
          submittedRequests.value[index] = savedRequest
        }
      } else {
        // 新規データを追加
        submittedRequests.value.push(savedRequest)
      }
      
      updateCalendarEvents()
      closePopup()
      alert('シフト希望が正常に提出されました')
    } else {
      // エラーレスポンスの処理
      let errorMessage = 'サーバーエラーが発生しました'
      try {
        const errorData = await response.json()
        errorMessage = errorData.message || errorMessage
      } catch (e) {
        console.error('エラーレスポンスの解析に失敗:', e)
      }
      alert(`エラーが発生しました: ${errorMessage}`)
    }
  } catch (error) {
    console.error('シフト希望の提出に失敗しました:', error)
    alert('通信エラーが発生しました')
  } finally {
    isSubmitting.value = false
  }
}

// シフト希望を削除 （APIエンドポイントを修正）
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

// カレンダーのイベントを更新 （プロパティ名を修正）
const updateCalendarEvents = () => {
  const events = submittedRequests.value.map(request => {
    const startDate = new Date(request.work_start) // start_work → work_start に変更
    const endDate = new Date(request.work_end)     // end_work → work_end に変更
    const workHours = ((endDate - startDate) / (1000 * 60 * 60)).toFixed(1)
    
    return {
      id: request.id.toString(),
      title: `希望: ${startDate.toLocaleTimeString('ja-JP', {hour: '2-digit', minute: '2-digit'})} - ${endDate.toLocaleTimeString('ja-JP', {hour: '2-digit', minute: '2-digit'})} (${workHours}h)`,
      start: request.work_start, // start_work → work_start に変更
      end: request.work_end,     // end_work → work_end に変更
      backgroundColor: '#28a745',
      borderColor: '#28a745'
    }
  })
  
  calendarOptions.value.events = events
}

// 日付フォーマット
const formatDate = (dateString) => {
  return new Date(dateString).toLocaleDateString('ja-JP')
}

// 時間フォーマット
const formatTime = (dateString) => {
  return new Date(dateString).toLocaleTimeString('ja-JP', {hour: '2-digit', minute: '2-digit'})
}

// 勤務時間計算（文字列から）
const calculateHours = (start, end) => {
  const startDate = new Date(start)
  const endDate = new Date(end)
  return ((endDate - startDate) / (1000 * 60 * 60)).toFixed(1)
}
</script>


<style scoped>
/* 既存のスタイルはそのまま使用 */
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

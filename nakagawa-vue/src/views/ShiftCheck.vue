<!-- ShiftCheck -->
<template>
  <div class="shift-check-container">
    <!-- 戻るボタン -->
    <div class="back-button-wrapper">
      <BackButton />
    </div>

    <!-- ヘッダー -->
    <div class="header">
      <h2>シフトカレンダー</h2>
      <div class="user-info" v-if="currentUser">
        <span>ユーザー: {{ currentUser.name }}</span>
        <span class="store-info">店舗: {{ currentUser.store }}号店</span>
      </div>
    </div>

    <!-- ローディング表示 -->
    <div v-if="loading" class="loading">
      <p>データを読み込み中...</p>
    </div>

    <!-- エラー表示 -->
    <div v-if="error" class="error">
      <p>{{ error }}</p>
      <button @click="fetchData" class="btn btn-primary">再読み込み</button>
    </div>

    <!-- FullCalendar -->
    <div v-if="!loading && !error">
      <FullCalendar :options="calendarOptions" />
    </div>

    <!-- 日付詳細モーダル -->
    <div v-if="showModal" class="popup-overlay" @click="closeModal">
      <div class="popup-content" @click.stop>
        <div class="popup-header">
          <h3>{{ formatDateHeader(selectedDate) }}</h3>
          <button @click="closeModal" class="close-btn">&times;</button>
        </div>
        
        <div class="modal-body">
          <div v-if="selectedShifts.length === 0" class="no-shifts">
            この日にシフトはありません
          </div>
          
          <div v-else class="shifts-list">
            <div v-for="shift in selectedShifts" :key="shift.id" class="shift-card">
              <div class="shift-card-header">
                <span class="shift-user-name">{{ shift.userName }}</span>
                <span class="shift-user-id">(ID: {{ shift.user_id }})</span>
              </div>
              <div class="shift-card-body">
                <div class="shift-time-info">
                  <div class="shift-time">
                    <span class="time-label">勤務時間:</span>
                    <span class="time-value">{{ formatTime(shift.work_start) }} 〜 {{ formatTime(shift.work_end) }}</span>
                  </div>
                  <div class="shift-duration">
                    <span class="duration-label">勤務時間:</span>
                    <span class="duration-value">{{ calculateWorkingHours(shift.work_start, shift.work_end) }}</span>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- シフト統計表示 -->
    <div class="statistics-section">
      <h3>シフト統計 ({{ currentUser?.store }}号店)</h3>
      <div class="stats-grid">
        <div class="stat-card">
          <div class="stat-number">{{ totalShifts }}</div>
          <div class="stat-label">総シフト数</div>
        </div>
        <div class="stat-card">
          <div class="stat-number">{{ totalEmployees }}</div>
          <div class="stat-label">勤務予定者数</div>
        </div>
        <div class="stat-card">
          <div class="stat-number">{{ averageHoursPerDay.toFixed(1) }}</div>
          <div class="stat-label">1日平均勤務時間</div>
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
import BackButton from '../components/BackButton.vue'

// Props（APIベースURLのみ）
const props = defineProps({
  apiBaseUrl: {
    type: String,
    default: 'http://localhost:5174/api'
  }
})

// ユーザー情報の取得
const { currentUser } = useUser()

// リアクティブデータ
const shifts = ref([])
const users = ref([])
const loading = ref(false)
const error = ref(null)
const showModal = ref(false)
const selectedDate = ref('')
const selectedShifts = ref([])

// 日付文字列をローカル日付として解釈する関数（タイムゾーンのずれを防ぐ）
const parseLocalDate = (dateString) => {
  if (!dateString) return null
  
  // ISO 8601形式の日付文字列から日付部分のみを抽出
  const dateOnly = dateString.split('T')[0]
  const [year, month, day] = dateOnly.split('-').map(Number)
  
  // ローカルタイムゾーンでDateオブジェクトを作成
  return new Date(year, month - 1, day)
}

// 日時文字列をローカル日時として解釈する関数
const parseLocalDateTime = (dateTimeString) => {
  if (!dateTimeString) return null
  
  // ISO 8601形式の日時文字列を解析
  const [datePart, timePart] = dateTimeString.split('T')
  const [year, month, day] = datePart.split('-').map(Number)
  const [hour, minute, second] = timePart.split(':').map(Number)
  
  // ローカルタイムゾーンでDateオブジェクトを作成
  return new Date(year, month - 1, day, hour, minute, second || 0)
}

// 同じ店舗のユーザーのみをフィルタリング
const sameStoreUsers = computed(() => {
  if (!Array.isArray(users.value) || !currentUser.value?.store) {
    return []
  }
  
  return users.value.filter(user => user.store === currentUser.value.store)
})

// ユーザー情報を含むシフトデータ（同じ店舗のみ）
const enrichedShifts = computed(() => {
  if (!Array.isArray(shifts.value) || !Array.isArray(sameStoreUsers.value)) {
    return []
  }
  
  // 同じ店舗のユーザーIDのセットを作成
  const sameStoreUserIds = new Set(sameStoreUsers.value.map(user => user.id))
  
  return shifts.value.filter(shift => {
    // 同じ店舗のユーザーのシフトのみを含める
    return sameStoreUserIds.has(shift.user_id) &&
           shift.work_start && shift.work_end && 
           calculateWorkingHoursNumeric(shift.work_start, shift.work_end) >= 1/60
  }).map(shift => {
    // Users.id = Shift_confirms.user_id の関係でユーザーを検索
    const user = sameStoreUsers.value.find(u => u.id === shift.user_id) || {}
    return {
      ...shift,
      userName: user.name || `ユーザー${shift.user_id}`
    }
  })
})

// 統計情報
const totalShifts = computed(() => enrichedShifts.value.length)
const totalEmployees = computed(() => {
  const uniqueUserIds = [...new Set(enrichedShifts.value.map(shift => shift.user_id))]
  return uniqueUserIds.length
})
const averageHoursPerDay = computed(() => {
  if (enrichedShifts.value.length === 0) return 0
  const totalHours = enrichedShifts.value.reduce((sum, shift) => {
    return sum + calculateWorkingHoursNumeric(shift.work_start, shift.work_end)
  }, 0)
  return totalHours / enrichedShifts.value.length
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
  selectable: false,
  // 日付クリック時の処理
  dateClick: function(info) {
    handleDateClick(info.dateStr)
  },
  // イベントクリック時の処理
  eventClick: function(info) {
    const dateStr = info.event.startStr.split('T')[0]
    handleDateClick(dateStr)
  },
  events: []
})

// データ取得
const fetchData = async () => {
  if (!currentUser.value) {
    error.value = 'ユーザー情報が取得できませんでした。ログインし直してください。'
    return
  }

  loading.value = true
  error.value = null
  
  try {
    // 並行してシフトデータとユーザーデータを取得
    const [shiftsResponse, usersResponse] = await Promise.all([
      fetch(`${props.apiBaseUrl}/Shift_confirms`),
      fetch(`${props.apiBaseUrl}/Users`)
    ])
    
    if (!shiftsResponse.ok || !usersResponse.ok) {
      throw new Error(`API Error: ${shiftsResponse.status} / ${usersResponse.status}`)
    }
    
    const shiftsData = await shiftsResponse.json()
    const usersData = await usersResponse.json()
    
    console.log('Shifts API Response:', shiftsData)
    console.log('Users API Response:', usersData)
    console.log('Current User Store:', currentUser.value.store)
    
    // シフトデータの処理
    if (Array.isArray(shiftsData)) {
      shifts.value = shiftsData
    } else if (shiftsData && typeof shiftsData === 'object') {
      shifts.value = [shiftsData]
    } else {
      shifts.value = []
    }
    
    // ユーザーデータの処理
    if (Array.isArray(usersData)) {
      users.value = usersData
    } else if (usersData && typeof usersData === 'object') {
      users.value = [usersData]
    } else {
      users.value = []
    }
    
    console.log('Same Store Users:', sameStoreUsers.value)
    
    updateCalendarEvents()
    
  } catch (err) {
    console.error('データの取得に失敗しました:', err)
    
    if (err.message.includes('fetch')) {
      error.value = 'サーバーに接続できませんでした。'
    } else {
      error.value = 'データの取得に失敗しました。'
    }
    
    shifts.value = []
    users.value = []
  } finally {
    loading.value = false
  }
}

// 日付クリック処理
const handleDateClick = (dateStr) => {
  selectedDate.value = dateStr
  
  // その日のシフトを取得
  selectedShifts.value = enrichedShifts.value.filter(shift => {
    let shiftDate
    if (shift.work_date) {
      // work_dateを使用してローカル日付として解釈
      shiftDate = parseLocalDate(shift.work_date)
    } else {
      // work_startから日付を抽出
      shiftDate = parseLocalDate(shift.work_start)
    }
    
    if (!shiftDate) return false
    
    // 日付文字列として比較
    const shiftDateStr = shiftDate.getFullYear() + '-' + 
                         String(shiftDate.getMonth() + 1).padStart(2, '0') + '-' + 
                         String(shiftDate.getDate()).padStart(2, '0')
    
    return shiftDateStr === dateStr
  }).sort((a, b) => {
    // 開始時間順にソート
    const aStart = parseLocalDateTime(a.work_start)
    const bStart = parseLocalDateTime(b.work_start)
    return aStart - bStart
  })
  
  showModal.value = true
}

// モーダルを閉じる
const closeModal = () => {
  showModal.value = false
  selectedDate.value = ''
  selectedShifts.value = []
}

// カレンダーのイベントを更新
const updateCalendarEvents = () => {
  // 日付ごとにシフトをグループ化
  const eventsByDate = {}
  
  enrichedShifts.value.forEach(shift => {
    let shiftDate
    if (shift.work_date) {
      // work_dateを使用してローカル日付として解釈
      shiftDate = parseLocalDate(shift.work_date)
    } else {
      // work_startから日付を抽出
      shiftDate = parseLocalDate(shift.work_start)
    }
    
    if (!shiftDate) return
    
    // 日付文字列として統一
    const dateStr = shiftDate.getFullYear() + '-' + 
                   String(shiftDate.getMonth() + 1).padStart(2, '0') + '-' + 
                   String(shiftDate.getDate()).padStart(2, '0')
    
    if (!eventsByDate[dateStr]) {
      eventsByDate[dateStr] = []
    }
    eventsByDate[dateStr].push(shift)
  })
  
  // FullCalendar用のイベントを作成
  const events = Object.entries(eventsByDate).map(([dateStr, dayShifts]) => {
    const totalHours = dayShifts.reduce((sum, shift) => {
      return sum + calculateWorkingHoursNumeric(shift.work_start, shift.work_end)
    }, 0)
    
    return {
      id: dateStr,
      title: `${dayShifts.length}名 (${totalHours.toFixed(1)}h)`,
      start: dateStr,
      allDay: true,
      backgroundColor: getEventColor(dayShifts.length),
      borderColor: getEventColor(dayShifts.length),
      textColor: '#fff'
    }
  })
  
  calendarOptions.value.events = events
}

// イベントの色を決定
const getEventColor = (shiftCount) => {
  if (shiftCount >= 5) return '#dc3545' // 赤（多い）
  if (shiftCount >= 3) return '#ffc107' // 黄（普通）
  return '#28a745' // 緑（少ない）
}

// 日付フォーマット（ヘッダー用）
const formatDateHeader = (dateStr) => {
  if (!dateStr) return ''
  
  try {
    const [year, month, day] = dateStr.split('-').map(Number)
    const date = new Date(year, month - 1, day)
    
    return date.toLocaleDateString('ja-JP', {
      year: 'numeric',
      month: '2-digit',
      day: '2-digit',
      weekday: 'long'
    })
  } catch (error) {
    console.error('日付フォーマットエラー:', error)
    return dateStr
  }
}

// 時間フォーマット
const formatTime = (dateTimeString) => {
  if (!dateTimeString) return '-'
  
  try {
    const date = parseLocalDateTime(dateTimeString)
    if (!date || isNaN(date.getTime())) return '-'
    
    return date.toLocaleTimeString('ja-JP', {
      hour: '2-digit',
      minute: '2-digit',
      hour12: false
    })
  } catch (error) {
    console.error('時刻フォーマットエラー:', error)
    return '-'
  }
}

// 勤務時間計算（表示用）
const calculateWorkingHours = (startTime, endTime) => {
  const hours = calculateWorkingHoursNumeric(startTime, endTime)
  if (hours <= 0) return '-'
  if (hours < 1) return `${Math.round(hours * 60)}分`
  return `${hours.toFixed(1)}時間`
}

// 勤務時間計算（数値）
const calculateWorkingHoursNumeric = (startTime, endTime) => {
  if (!startTime || !endTime) return 0
  
  try {
    const start = parseLocalDateTime(startTime)
    const end = parseLocalDateTime(endTime)
    
    if (!start || !end || isNaN(start.getTime()) || isNaN(end.getTime())) return 0
    
    const diffMs = end.getTime() - start.getTime()
    const diffHours = diffMs / (1000 * 60 * 60)
    
    return Math.max(0, diffHours)
  } catch (error) {
    console.error('勤務時間計算エラー:', error)
    return 0
  }
}

// マウント時にデータを取得
onMounted(async () => {
  await fetchData()
})
</script>

<style scoped>
.shift-check-container {
  padding: 20px;
  max-width: 1200px;
  margin: 0 auto;
  transform: translateX(50%);
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
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.store-info {
  color: #007bff;
  font-size: 0.9rem;
}

.loading {
  text-align: center;
  padding: 40px;
  font-size: 1.1rem;
  color: #666;
}

.loading::after {
  content: '';
  display: inline-block;
  width: 20px;
  height: 20px;
  margin-left: 10px;
  border: 3px solid #f3f3f3;
  border-top: 3px solid #007bff;
  border-radius: 50%;
  animation: spin 1s linear infinite;
}

@keyframes spin {
  0% { transform: rotate(0deg); }
  100% { transform: rotate(360deg); }
}

.error {
  background-color: #fee;
  border: 1px solid #fcc;
  border-radius: 8px;
  padding: 20px;
  margin-bottom: 20px;
  text-align: center;
}

.error p {
  color: #c33;
  margin-bottom: 15px;
  font-weight: 500;
}

/* ポップアップスタイル（ShiftSubmitと統一） */
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
  max-width: 600px;
  max-height: 80vh;
  overflow-y: auto;
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

.modal-body {
  padding: 20px;
}

.no-shifts {
  text-align: center;
  color: #666;
  font-size: 1.1rem;
  padding: 40px;
}

.shifts-list {
  display: flex;
  flex-direction: column;
  gap: 15px;
}

.shift-card {
  border: 1px solid #dee2e6;
  border-radius: 8px;
  padding: 15px;
  transition: box-shadow 0.2s;
}

.shift-card:hover {
  box-shadow: 0 2px 8px rgba(0,0,0,0.1);
}

.shift-card-header {
  display: flex;
  align-items: center;
  gap: 10px;
  margin-bottom: 15px;
}

.shift-user-name {
  font-weight: 600;
  color: #333;
  font-size: 1.1rem;
}

.shift-user-id {
  color: #666;
  font-size: 0.9rem;
}

.shift-card-body {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.shift-time-info {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.shift-time, .shift-duration {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.time-label, .duration-label {
  color: #666;
  font-size: 0.9rem;
}

.time-value, .duration-value {
  font-weight: 500;
  color: #333;
}

/* 統計セクション */
.statistics-section {
  margin-top: 40px;
  background-color: #f8f9fa;
  padding: 20px;
  border-radius: 8px;
}

.statistics-section h3 {
  margin-top: 0;
  color: #333;
}

.stats-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: 20px;
  margin-top: 20px;
}

.stat-card {
  background: white;
  padding: 20px;
  border-radius: 8px;
  text-align: center;
  border-left: 4px solid #28a745;
}

.stat-number {
  font-size: 2rem;
  font-weight: bold;
  color: #28a745;
  margin-bottom: 5px;
}

.stat-label {
  color: #666;
  font-size: 0.9rem;
}

/* ボタンスタイル */
.btn {
  padding: 10px 20px;
  border: none;
  border-radius: 4px;
  cursor: pointer;
  font-size: 14px;
  font-weight: bold;
}

.btn-primary {
  background-color: #28a745;
  color: white;
}

.btn-primary:hover {
  background-color: #218838;
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

/* レスポンシブデザイン */
@media (max-width: 768px) {
  .shift-check-container {
    padding: 15px;
    transform: none;
  }
  
  .header {
    flex-direction: column;
    gap: 10px;
    align-items: flex-start;
  }
  
  .stats-grid {
    grid-template-columns: 1fr;
  }
  
  .popup-content {
    width: 95%;
    max-height: 90vh;
  }
  
  .user-info {
    align-self: stretch;
  }
}
</style>
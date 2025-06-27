<template>
  <div class="shift-calendar-container">
    <h1 class="page-title">シフトカレンダー</h1>
    
    <!-- ローディング表示 -->
    <div v-if="loading" class="loading">
      <p>データを読み込み中...</p>
    </div>

    <!-- エラー表示 -->
    <div v-if="error" class="error">
      <p>{{ error }}</p>
      <button @click="fetchData" class="retry-button">再読み込み</button>
    </div>

    <!-- カレンダーコントロール -->
    <div v-if="!loading && !error" class="calendar-controls">
      <button @click="previousMonth" class="nav-button">&lt;</button>
      <h2 class="current-month">{{ formatMonth(currentDate) }}</h2>
      <button @click="nextMonth" class="nav-button">&gt;</button>
    </div>

    <!-- カレンダー表示 -->
    <div v-if="!loading && !error" class="calendar-container">
      <div class="calendar-grid">
        <!-- 曜日ヘッダー -->
        <div class="day-header" v-for="day in dayHeaders" :key="day">{{ day }}</div>
        
        <!-- カレンダー日付セル -->
        <div 
          v-for="(date, index) in calendarDates" 
          :key="index"
          :class="['calendar-cell', { 
            'other-month': !date.isCurrentMonth,
            'today': date.isToday,
            'has-shift': date.shifts.length > 0
          }]"
          @click="openDateModal(date)"
        >
          <div class="date-number">{{ date.day }}</div>
          
          <!-- シフト詳細表示 -->
          <div class="shifts-preview" v-if="date.shifts.length > 0">
            <div 
              v-for="(shift, shiftIndex) in getDisplayShifts(date.shifts)" 
              :key="shift.id"
              class="shift-preview-item"
            >
              <div class="shift-name">{{ shift.userName }}</div>
              <div class="shift-time">{{ formatTimeRange(shift.work_start, shift.work_end) }}</div>
            </div>
            
            <!-- 追加のシフトがある場合 -->
            <div v-if="date.shifts.length > maxShiftsDisplay" class="more-shifts">
              +{{ date.shifts.length - maxShiftsDisplay }}件
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- 日付詳細モーダル -->
    <div v-if="showModal" class="modal-overlay" @click="closeModal">
      <div class="modal-content" @click.stop>
        <div class="modal-header">
          <h3>{{ formatDate(selectedDate.date) }}</h3>
          <button @click="closeModal" class="close-button">&times;</button>
        </div>
        
        <div class="modal-body">
          <div v-if="selectedDate.shifts.length === 0" class="no-shifts">
            この日にシフトはありません
          </div>
          
          <div v-else class="shifts-list">
            <div v-for="shift in selectedDate.shifts" :key="shift.id" class="shift-card">
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
  </div>
</template>

<script>
import axios from 'axios'

export default {
  name: 'ShiftCalendar',
  data() {
    return {
      shifts: [],
      users: [],
      loading: false,
      error: null,
      currentDate: new Date(),
      dayHeaders: ['日', '月', '火', '水', '木', '金', '土'],
      showModal: false,
      selectedDate: null,
      maxShiftsDisplay: 3 // カレンダーセルに表示する最大シフト数
    }
  },
  computed: {
    // ユーザー情報を含むシフトデータ
    enrichedShifts() {
      if (!Array.isArray(this.shifts) || !Array.isArray(this.users)) {
        return []
      }
      
      return this.shifts.filter(shift => {
        return shift.work_start && shift.work_end && 
               this.calculateWorkingHoursNumeric(shift.work_start, shift.work_end) >= 1/60
      }).map(shift => {
        // Users.id = Shift_confirms.user_id の関係でユーザーを検索
        const user = this.users.find(u => u.id === shift.user_id) || {}
        return {
          ...shift,
          userName: user.name || `ユーザー${shift.user_id}`
        }
      })
    },
    
    // カレンダーの日付配列
    calendarDates() {
      const year = this.currentDate.getFullYear()
      const month = this.currentDate.getMonth()
      
      // 月の最初の日と最後の日
      const firstDay = new Date(year, month, 1)
      const lastDay = new Date(year, month + 1, 0)
      
      // カレンダーの開始日（前月の日曜日から）
      const startDate = new Date(firstDay)
      startDate.setDate(startDate.getDate() - firstDay.getDay())
      
      // カレンダーの終了日（次月の土曜日まで）
      const endDate = new Date(lastDay)
      endDate.setDate(endDate.getDate() + (6 - lastDay.getDay()))
      
      const dates = []
      const currentDate = new Date(startDate)
      const today = new Date()
      
      while (currentDate <= endDate) {
        const dateStr = currentDate.toISOString().split('T')[0]
        const shiftsForDate = this.enrichedShifts.filter(shift => {
          // work_dateがある場合はそれを優先、なければwork_startの日付を使用
          let shiftDate
          if (shift.work_date) {
            shiftDate = new Date(shift.work_date)
          } else {
            shiftDate = new Date(shift.work_start)
          }
          return shiftDate.toISOString().split('T')[0] === dateStr
        }).sort((a, b) => {
          // 開始時間順にソート
          return new Date(a.work_start) - new Date(b.work_start)
        })
        
        dates.push({
          date: new Date(currentDate),
          day: currentDate.getDate(),
          isCurrentMonth: currentDate.getMonth() === month,
          isToday: currentDate.toDateString() === today.toDateString(),
          shifts: shiftsForDate
        })
        
        currentDate.setDate(currentDate.getDate() + 1)
      }
      
      return dates
    }
  },
  mounted() {
    this.fetchData()
  },
  methods: {
    async fetchData() {
      this.loading = true
      this.error = null
      
      try {
        // 並行してシフトデータとユーザーデータを取得
        const [shiftsResponse, usersResponse] = await Promise.all([
          axios.get('http://localhost:5174/api/Shift_confirms'),
          axios.get('http://localhost:5174/api/Users')
        ])
        
        console.log('Shifts API Response:', shiftsResponse.data)
        console.log('Users API Response:', usersResponse.data)
        
        // シフトデータの処理
        if (Array.isArray(shiftsResponse.data)) {
          this.shifts = shiftsResponse.data
        } else if (shiftsResponse.data && typeof shiftsResponse.data === 'object') {
          this.shifts = [shiftsResponse.data]
        } else {
          this.shifts = []
        }
        
        // ユーザーデータの処理
        if (Array.isArray(usersResponse.data)) {
          this.users = usersResponse.data
        } else if (usersResponse.data && typeof usersResponse.data === 'object') {
          this.users = [usersResponse.data]
        } else {
          this.users = []
        }
        
        console.log('Processed shifts:', this.shifts)
        console.log('Processed users:', this.users)
        console.log('Enriched shifts:', this.enrichedShifts)
        
      } catch (error) {
        console.error('データの取得に失敗しました:', error)
        
        if (error.response) {
          this.error = `サーバーエラー (${error.response.status}): ${error.response.data?.message || 'データの取得に失敗しました'}`
        } else if (error.request) {
          this.error = 'サーバーに接続できませんでした。'
        } else {
          this.error = 'リクエストの送信中にエラーが発生しました。'
        }
        
        this.shifts = []
        this.users = []
      } finally {
        this.loading = false
      }
    },
    
    previousMonth() {
      this.currentDate = new Date(this.currentDate.getFullYear(), this.currentDate.getMonth() - 1, 1)
    },
    
    nextMonth() {
      this.currentDate = new Date(this.currentDate.getFullYear(), this.currentDate.getMonth() + 1, 1)
    },
    
    openDateModal(date) {
      this.selectedDate = date
      this.showModal = true
    },
    
    closeModal() {
      this.showModal = false
      this.selectedDate = null
    },
    
    // カレンダーセルに表示するシフトを制限
    getDisplayShifts(shifts) {
      return shifts.slice(0, this.maxShiftsDisplay)
    },
    
    formatMonth(date) {
      return date.toLocaleDateString('ja-JP', {
        year: 'numeric',
        month: 'long'
      })
    },
    
    formatDate(date) {
      if (!date) return '-'
      
      try {
        return date.toLocaleDateString('ja-JP', {
          year: 'numeric',
          month: '2-digit',
          day: '2-digit',
          weekday: 'long'
        })
      } catch (error) {
        console.error('日付フォーマットエラー:', error)
        return '-'
      }
    },
    
    formatTime(dateTimeString) {
      if (!dateTimeString) return '-'
      
      try {
        const date = new Date(dateTimeString)
        if (isNaN(date.getTime())) return '-'
        
        return date.toLocaleTimeString('ja-JP', {
          hour: '2-digit',
          minute: '2-digit',
          hour12: false
        })
      } catch (error) {
        console.error('時刻フォーマットエラー:', error)
        return '-'
      }
    },
    
    // 時間範囲を短縮形式で表示
    formatTimeRange(startTime, endTime) {
      const start = this.formatTime(startTime)
      const end = this.formatTime(endTime)
      
      if (start === '-' || end === '-') return '-'
      
      // 短縮表示（例：09:00-17:00）
      return `${start}-${end}`
    },
    
    calculateWorkingHours(startTime, endTime) {
      const hours = this.calculateWorkingHoursNumeric(startTime, endTime)
      if (hours <= 0) return '-'
      if (hours < 1) return `${Math.round(hours * 60)}分`
      return `${hours.toFixed(1)}時間`
    },
    
    calculateWorkingHoursNumeric(startTime, endTime) {
      if (!startTime || !endTime) return 0
      
      try {
        const start = new Date(startTime)
        const end = new Date(endTime)
        
        if (isNaN(start.getTime()) || isNaN(end.getTime())) return 0
        
        const diffMs = end.getTime() - start.getTime()
        const diffHours = diffMs / (1000 * 60 * 60)
        
        return Math.max(0, diffHours)
      } catch (error) {
        console.error('勤務時間計算エラー:', error)
        return 0
      }
    }
  }
}
</script>

<style scoped>
.shift-calendar-container {
  padding: 20px;
  max-width: 1400px;
  margin: 0 auto;
  font-family: 'Helvetica Neue', Arial, 'Hiragino Kaku Gothic ProN', 'Hiragino Sans', Meiryo, sans-serif;
}

.page-title {
  font-size: 2rem;
  color: #333;
  margin-bottom: 30px;
  text-align: center;
  font-weight: 600;
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

.retry-button {
  background-color: #007bff;
  color: white;
  border: none;
  padding: 10px 20px;
  border-radius: 6px;
  cursor: pointer;
  font-size: 0.9rem;
  transition: background-color 0.2s;
}

.retry-button:hover {
  background-color: #0056b3;
}

/* カレンダーコントロール */
.calendar-controls {
  display: flex;
  justify-content: center;
  align-items: center;
  margin-bottom: 20px;
  gap: 20px;
}

.nav-button {
  background-color: #007bff;
  color: white;
  border: none;
  padding: 10px 15px;
  border-radius: 6px;
  cursor: pointer;
  font-size: 1.2rem;
  font-weight: bold;
  transition: background-color 0.2s;
}

.nav-button:hover {
  background-color: #0056b3;
}

.current-month {
  font-size: 1.5rem;
  color: #333;
  margin: 0;
  min-width: 200px;
  text-align: center;
}

/* カレンダーグリッド */
.calendar-container {
  margin-bottom: 30px;
}

.calendar-grid {
  display: grid;
  grid-template-columns: repeat(7, 1fr);
  gap: 1px;
  background-color: #ddd;
  border-radius: 8px;
  overflow: hidden;
  box-shadow: 0 2px 8px rgba(0,0,0,0.1);
}

.day-header {
  background-color: #f8f9fa;
  padding: 15px 5px;
  text-align: center;
  font-weight: 600;
  color: #333;
  border-bottom: 2px solid #dee2e6;
}

.calendar-cell {
  background-color: white;
  min-height: 120px;
  padding: 8px;
  position: relative;
  cursor: pointer;
  transition: background-color 0.2s;
  display: flex;
  flex-direction: column;
}

.calendar-cell:hover {
  background-color: #f8f9fa;
}

.calendar-cell.other-month {
  background-color: #f8f9fa;
  color: #999;
}

.calendar-cell.today {
  background-color: #e3f2fd;
  border: 2px solid #2196f3;
}

.calendar-cell.has-shift {
  background-color: #fff3e0;
}

.calendar-cell.has-shift:hover {
  background-color: #ffe0b2;
}

.date-number {
  font-weight: 600;
  margin-bottom: 8px;
  font-size: 1rem;
  flex-shrink: 0;
}

/* シフトプレビュー表示 */
.shifts-preview {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 4px;
  overflow: hidden;
}

.shift-preview-item {
  background-color: rgba(0, 123, 255, 0.1);
  border-left: 3px solid #007bff;
  padding: 4px 6px;
  border-radius: 3px;
  font-size: 0.75rem;
  line-height: 1.2;
}

.shift-name {
  font-weight: 600;
  color: #333;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  margin-bottom: 2px;
}

.shift-time {
  color: #666;
  font-size: 0.7rem;
}

.more-shifts {
  background-color: rgba(108, 117, 125, 0.1);
  border-left: 3px solid #6c757d;
  padding: 4px 6px;
  border-radius: 3px;
  font-size: 0.7rem;
  color: #6c757d;
  text-align: center;
  font-weight: 500;
}

/* モーダル */
.modal-overlay {
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

.modal-content {
  background-color: white;
  border-radius: 12px;
  max-width: 600px;
  width: 90%;
  max-height: 80vh;
  overflow-y: auto;
  box-shadow: 0 10px 30px rgba(0, 0, 0, 0.3);
}

.modal-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 20px;
  border-bottom: 1px solid #dee2e6;
}

.modal-header h3 {
  margin: 0;
  color: #333;
  font-size: 1.3rem;
  font-weight: 600;
}

.close-button {
  background: none;
  border: none;
  font-size: 1.5rem;
  cursor: pointer;
  color: #666;
  width: 30px;
  height: 30px;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 50%;
  transition: background-color 0.2s;
}

.close-button:hover {
  background-color: #f8f9fa;
  color: #333;
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

/* レスポンシブデザイン */
@media (max-width: 768px) {
  .shift-calendar-container {
    padding: 15px;
  }
  
  .page-title {
    font-size: 1.6rem;
  }
  
  .calendar-grid {
    gap: 0;
  }
  
  .calendar-cell {
    min-height: 100px;
    padding: 4px;
  }
  
  .date-number {
    font-size: 0.9rem;
  }
  
  .shift-preview-item {
    font-size: 0.7rem;
    padding: 3px 4px;
  }
  
  .shift-name {
    font-size: 0.7rem;
  }
  
  .shift-time {
    font-size: 0.65rem;
  }
  
  .modal-content {
    width: 95%;
    max-height: 90vh;
  }
}

@media (max-width: 480px) {
  .shift-calendar-container {
    padding: 10px;
  }
  
  .calendar-cell {
    min-height: 80px;
    padding: 2px;
  }
  
  .date-number {
    font-size: 0.8rem;
    margin-bottom: 4px;
  }
  
  .shift-preview-item {
    font-size: 0.65rem;
    padding: 2px 3px;
  }
  
  .shift-name {
    font-size: 0.65rem;
  }
  
  .shift-time {
    font-size: 0.6rem;
  }
  
  .current-month {
    font-size: 1.2rem;
    min-width: 150px;
  }
  
  .nav-button {
    padding: 8px 12px;
    font-size: 1rem;
  }
  
  .modal-header {
    padding: 15px;
  }
  
  .modal-body {
    padding: 15px;
  }
}
</style>
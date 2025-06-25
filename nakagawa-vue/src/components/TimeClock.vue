<template>
  <div v-if="showPopup" class="popup-overlay" @click="closePopup">
    <div class="popup-content" @click.stop>
      <h3>打刻</h3>
      
      <!-- 打刻種別選択 -->
      <div v-if="!selectedType" class="button-group">
        <button @click="selectType('clock_in')" class="btn btn-primary">
          出勤
        </button>
        <button @click="selectType('clock_out')" class="btn btn-secondary">
          退勤
        </button>
      </div>
      
      <!-- 確認画面 -->
      <div v-if="selectedType && !isConfirmed" class="confirmation">
        <p>{{ selectedType === 'clock_in' ? '出勤' : '退勤' }}で打刻しますか？</p>
        <p class="timestamp">{{ currentTime }}</p>
        <div class="button-group">
          <button @click="confirmClock" class="btn btn-confirm" :disabled="loading">
            {{ loading ? '処理中...' : '確認' }}
          </button>
          <button @click="resetSelection" class="btn btn-cancel">
            戻る
          </button>
        </div>
      </div>
      
      <!-- 完了メッセージ -->
      <div v-if="isConfirmed" class="success-message">
        <p>{{ selectedType === 'clock_in' ? '出勤' : '退勤' }}を記録しました</p>
        <button @click="closePopup" class="btn btn-primary">
          閉じる
        </button>
      </div>
      
      <!-- エラーメッセージ -->
      <div v-if="errorMessage" class="error-message">
        <p>{{ errorMessage }}</p>
        <button @click="resetSelection" class="btn btn-primary">
          戻る
        </button>
      </div>
      
      <!-- 閉じるボタン -->
      <button v-if="!isConfirmed && !errorMessage" @click="closePopup" class="close-btn">
        ×
      </button>
    </div>
  </div>
</template>

<script setup>
import { ref, watch, onUnmounted } from 'vue'

// Props
const props = defineProps({
  show: {
    type: Boolean,
    default: false
  }
})

// Emits
const emit = defineEmits(['close', 'clock-recorded'])

// Reactive data
const showPopup = ref(false)
const selectedType = ref(null)
const isConfirmed = ref(false)
const loading = ref(false)
const errorMessage = ref('')
const currentTime = ref('')
const timeInterval = ref(null)

// Watch props.show
watch(() => props.show, (newVal) => {
  showPopup.value = newVal
  if (newVal) {
    resetAll()
    startTimeUpdate()
  } else {
    stopTimeUpdate()
  }
})

// Methods
const selectType = (type) => {
  selectedType.value = type
  errorMessage.value = ''
}

const resetSelection = () => {
  selectedType.value = null
  isConfirmed.value = false
  errorMessage.value = ''
  loading.value = false
}

const resetAll = () => {
  selectedType.value = null
  isConfirmed.value = false
  errorMessage.value = ''
  loading.value = false
}

const confirmClock = async () => {
  loading.value = true
  errorMessage.value = ''
  
  try {
    // ローカルストレージからユーザー情報を取得
    const userData = JSON.parse(localStorage.getItem('user') || '{}')
    const userId = userData.user_id || userData.id
    
    if (!userId) {
      throw new Error('ユーザー情報が見つかりません')
    }
    
    // 現在時刻を取得
    const now = new Date()
    
    // APIに送信するデータを準備
    const workLogData = {
      user_id: userId,
      work_start: selectedType.value === 'clock_in' ? now.toISOString() : null,
      work_end: selectedType.value === 'clock_out' ? now.toISOString() : null,
      work_date: now.toISOString().split('T')[0] // YYYY-MM-DD形式
    }
    
    // 退勤の場合は、同日の最新の出勤記録を更新
    if (selectedType.value === 'clock_out') {
      await updateLatestWorkLog(userId, now)
    } else {
      // 出勤の場合は新規作成
      await createWorkLog(workLogData)
    }
    
    isConfirmed.value = true
    emit('clock-recorded', {
      type: selectedType.value,
      time: now,
      userId: userId
    })
    
  } catch (error) {
    console.error('打刻エラー:', error)
    errorMessage.value = error.message || '打刻に失敗しました'
  } finally {
    loading.value = false
  }
}

const createWorkLog = async (data) => {
  const response = await fetch('http://localhost:5173/api/work_logs', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(data)
  })
  
  if (!response.ok) {
    const errorData = await response.text()
    throw new Error(`サーバーエラー: ${response.status}`)
  }
  
  return await response.json()
}

const updateLatestWorkLog = async (userId, endTime) => {
  // 同日の最新の出勤記録を取得
  const today = endTime.toISOString().split('T')[0]
  const response = await fetch(`http://localhost:5173/api/work_logs/by-userid/${userId}`)
  
  if (!response.ok) {
    throw new Error('勤務記録の取得に失敗しました')
  }
  
  const workLogs = await response.json()
  
  // 同日の最新の出勤記録を探す（work_endがnullのもの）
  const todayLogs = workLogs.filter(log => {
    const logDate = new Date(log.work_start).toISOString().split('T')[0]
    return logDate === today && log.work_end === null
  })
  
  if (todayLogs.length === 0) {
    throw new Error('対応する出勤記録が見つかりません')
  }
  
  // 最新の記録を更新
  const latestLog = todayLogs[todayLogs.length - 1]
  latestLog.work_end = endTime.toISOString()
  
  const updateResponse = await fetch(`http://localhost:5173/api/work_logs/${latestLog.id}`, {
    method: 'PUT',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(latestLog)
  })
  
  if (!updateResponse.ok) {
    throw new Error('退勤記録の更新に失敗しました')
  }
}

const closePopup = () => {
  showPopup.value = false
  stopTimeUpdate()
  emit('close')
}

const startTimeUpdate = () => {
  updateCurrentTime()
  timeInterval.value = setInterval(updateCurrentTime, 1000)
}

const stopTimeUpdate = () => {
  if (timeInterval.value) {
    clearInterval(timeInterval.value)
    timeInterval.value = null
  }
}

const updateCurrentTime = () => {
  const now = new Date()
  currentTime.value = now.toLocaleString('ja-JP', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit'
  })
}

// Cleanup on unmount
onUnmounted(() => {
  stopTimeUpdate()
})
</script>

<style scoped>
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
  padding: 30px;
  border-radius: 8px;
  min-width: 300px;
  max-width: 400px;
  position: relative;
  box-shadow: 0 4px 20px rgba(0, 0, 0, 0.15);
}

.popup-content h3 {
  margin: 0 0 20px 0;
  text-align: center;
  color: #333;
}

.button-group {
  display: flex;
  gap: 10px;
  justify-content: center;
  margin: 20px 0;
}

.btn {
  padding: 12px 24px;
  border: none;
  border-radius: 4px;
  cursor: pointer;
  font-size: 16px;
  transition: background-color 0.3s;
}

.btn:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.btn-primary {
  background-color: #007bff;
  color: white;
}

.btn-primary:hover:not(:disabled) {
  background-color: #0056b3;
}

.btn-secondary {
  background-color: #6c757d;
  color: white;
}

.btn-secondary:hover:not(:disabled) {
  background-color: #545b62;
}

.btn-confirm {
  background-color: #28a745;
  color: white;
}

.btn-confirm:hover:not(:disabled) {
  background-color: #1e7e34;
}

.btn-cancel {
  background-color: #dc3545;
  color: white;
}

.btn-cancel:hover {
  background-color: #c82333;
}

.confirmation {
  text-align: center;
}

.confirmation p {
  margin: 10px 0;
}

.timestamp {
  font-size: 18px;
  font-weight: bold;
  color: #007bff;
  margin: 15px 0 !important;
}

.success-message {
  text-align: center;
  color: #28a745;
}

.success-message p {
  font-size: 18px;
  margin: 20px 0;
}

.error-message {
  text-align: center;
  color: #dc3545;
}

.error-message p {
  margin: 20px 0;
}

.close-btn {
  position: absolute;
  top: 10px;
  right: 15px;
  background: none;
  border: none;
  font-size: 24px;
  cursor: pointer;
  color: #999;
}

.close-btn:hover {
  color: #333;
}
</style>
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
import { ref, watch, onMounted, onUnmounted } from 'vue'
import { useUser } from './useUser'
 
// Props
const props = defineProps({
  show: {
    type: Boolean,
    default: false
  },
  apiBaseUrl: {
    type: String,
    default: 'http://localhost:5174/api'
  }
})
 
// Emits
const emit = defineEmits(['close', 'clock-recorded'])
 
// ユーザー情報の取得
const { currentUser, userId } = useUser()
const userDbId = ref(null) // DBのUser.idを保持
 
// Reactive data
const showPopup = ref(false)
const selectedType = ref(null)
const isConfirmed = ref(false)
const loading = ref(false)
const errorMessage = ref('')
const currentTime = ref('')
const timeInterval = ref(null)
 
// onMountedでDBのUser.idを取得
onMounted(async () => {
  if (!userId.value) {
    console.error('ユーザー情報が取得できません')
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
  }
})
 
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
  loading.value = true;
  errorMessage.value = '';
  
  try {
    if (!userDbId.value) {
      throw new Error('ユーザー情報が見つかりません');
    }
    
    console.log('userDbId.value:', userDbId.value); // デバッグ用
    
    // 現在時刻を取得
    const now = new Date();
    
    // 退勤の場合は、同日の最新の出勤記録を更新
    if (selectedType.value === 'clock_out') {
      await updateLatestWorkLog(userDbId.value, now);
    } else {
      // 出勤の場合は新規作成
      const workLogData = {
        user_id: parseInt(userDbId.value), // 数値として明示的に変換
        work_start: now.toISOString(),
        work_end: null,
        work_date: now.toISOString().split('T')[0] + 'T00:00:00.000Z'
      };
      
      console.log('送信前のworkLogData:', workLogData); // デバッグ用
      
      // データの妥当性チェック
      if (!workLogData.user_id || isNaN(workLogData.user_id)) {
        throw new Error('ユーザーIDが無効です');
      }
      
      await createWorkLog(workLogData);
    }
    
    isConfirmed.value = true;
    emit('clock-recorded', {
      type: selectedType.value,
      time: now,
      userId: userDbId.value
    });
    
  } catch (error) {
    console.error('打刻エラー:', error);
    errorMessage.value = error.message || '打刻に失敗しました';
  } finally {
    loading.value = false;
  }
};
 
const createWorkLog = async (data) => {
  try {
    // データの検証
    if (!data || !data.user_id) {
      throw new Error('必要なデータが不足しています');
    }
    
    console.log('送信するデータ:', JSON.stringify(data, null, 2)); // デバッグ用
    
    const response = await fetch(`${props.apiBaseUrl}/Work_logs`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Accept': 'application/json'
      },
      body: JSON.stringify(data)
    });
    
    console.log('レスポンスステータス:', response.status); // デバッグ用
    
    if (!response.ok) {
      const errorText = await response.text();
      console.error('サーバーエラー詳細:', errorText);
      throw new Error(`サーバーエラー: ${response.status} - ${errorText}`);
    }
    
    return await response.json();
  } catch (error) {
    console.error('createWorkLog エラー:', error);
    throw error;
  }
};
 
const updateLatestWorkLog = async (userId, endTime) => {
  try {
    console.log('updateLatestWorkLog開始 - userId:', userId, 'endTime:', endTime);
    
    // ユーザーの勤務記録を取得
    const response = await fetch(`${props.apiBaseUrl}/Work_logs/by-userid/${userId}`);
    
    if (!response.ok) {
      throw new Error('勤務記録の取得に失敗しました');
    }
    
    const workLogs = await response.json();
    console.log('取得した勤務記録:', workLogs);
    
    // 日本時間での今日の日付を取得
    const today = new Date(endTime.getTime() + (9 * 60 * 60 * 1000))
      .toISOString().split('T')[0];
    console.log('今日の日付:', today);
    
    // 同日の未完了な出勤記録を探す（work_endがnullまたは未定義のもの）
    const incompleteTodayLogs = workLogs.filter(log => {
      if (!log.work_start) return false;
      
      // work_startの日付部分を取得（タイムゾーンを考慮）
      const logDate = new Date(log.work_start + (log.work_start.includes('Z') ? '' : 'Z'));
      const logDateStr = new Date(logDate.getTime() + (9 * 60 * 60 * 1000))
        .toISOString().split('T')[0];
      
      console.log(`ログID ${log.id}: 日付=${logDateStr}, work_end=${log.work_end}`);
      
      return logDateStr === today && (log.work_end === null || log.work_end === undefined);
    });
    
    console.log('未完了の今日の記録:', incompleteTodayLogs);
    
    if (incompleteTodayLogs.length === 0) {
      // より詳細なエラーメッセージを提供
      const todayLogs = workLogs.filter(log => {
        if (!log.work_start) return false;
        const logDate = new Date(log.work_start + (log.work_start.includes('Z') ? '' : 'Z'));
        const logDateStr = new Date(logDate.getTime() + (9 * 60 * 60 * 1000))
          .toISOString().split('T')[0];
        return logDateStr === today;
      });
      
      if (todayLogs.length === 0) {
        throw new Error('本日の出勤記録がありません。先に出勤打刻を行ってください。');
      } else {
        throw new Error('本日の出勤記録は既に退勤済みです。');
      }
    }
    
    // 最新の未完了記録を取得（work_startが最も新しいもの）
    const latestLog = incompleteTodayLogs.reduce((latest, current) => {
      return new Date(current.work_start) > new Date(latest.work_start) ? current : latest;
    });
    
    console.log('更新対象のログ:', latestLog);
    
    // 退勤時刻を設定（元のlatestLogをコピーして更新）
    const updatedLog = {
      ...latestLog,
      work_end: endTime.toISOString()
    };
    
    console.log('更新データ:', updatedLog);
    
    // 更新APIを呼び出し
    const updateResponse = await fetch(`${props.apiBaseUrl}/Work_logs/${latestLog.id}`, {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(updatedLog)
    });
    
    if (!updateResponse.ok) {
      const errorText = await updateResponse.text();
      console.error('更新エラー:', errorText);
      throw new Error(`退勤記録の更新に失敗しました: ${updateResponse.status} - ${errorText}`);
    }
    
    console.log('退勤記録が正常に更新されました');
    
  } catch (error) {
    console.error('updateLatestWorkLog エラー:', error);
    throw error;
  }
};
 
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
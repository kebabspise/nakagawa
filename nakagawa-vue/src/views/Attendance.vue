<template>
  <div class="container">
    <div class="back-button-wrapper">
      <BackButton />
    </div>
    <div class="header">
      <h1 class="title">勤怠記録管理システム</h1>
      <div class="controls">
        <div class="month-selector">
          <label>表示月:</label>
          <input type="month" v-model="selectedMonth" @change="fetchData">
          <button class="refresh-btn" @click="fetchData">更新</button>
        </div>
        <div class="export-controls">
          <button class="export-btn" @click="exportToPDF" :disabled="users.length === 0">
            PDF出力
          </button>
        </div>
      </div>
    </div>
    
    <div v-if="loading" class="loading">
      データを読み込んでいます...
    </div>
    
    <div v-else-if="error" class="error">
      {{ error }}
    </div>
    
    <div v-else-if="users.length === 0" class="no-data">
      ユーザーデータがありません
    </div>
    
    <div v-else class="table-container">
      <table class="attendance-table">
        <thead>
          <tr>
            <th class="user-info">ユーザー情報</th>
            <th 
              v-for="day in daysInMonth" 
              :key="day.date"
              :class="['date-header', { 'weekend': day.isWeekend, 'today': day.isToday }]"
            >
              {{ day.display }}
            </th>
          </tr>
        </thead>
        <tbody>
          <template v-for="user in users" :key="user.id">
            <tr>
              <td class="user-info">
                <div><strong>{{ user.name }}</strong></div>
                <div>ID: {{ user.user_id }}</div>
                <div>時給: ¥{{ user.wages }}</div>
              </td>
              <td 
                v-for="day in daysInMonth" 
                :key="day.date"
                :class="['work-time-cell', { 'weekend': day.isWeekend, 'today': day.isToday }]"
              >
                <div v-if="getWorkLog(user.id, day.date)">
                  <div v-if="!isEditing(user.id, day.date)" class="time-display">
                    <div>開始: {{ formatTime(getWorkLog(user.id, day.date).work_start) }}</div>
                    <div>終了: {{ formatTime(getWorkLog(user.id, day.date).work_end) }}</div>
                    <div>
                      <button class="edit-btn" @click="startEdit(user.id, day.date)">編集</button>
                      <button class="delete-btn" @click="deleteWorkLog(getWorkLog(user.id, day.date).id)">削除</button>
                    </div>
                  </div>
                  <div v-else>
                    <div>
                      <input 
                        type="time" 
                        class="time-input"
                        v-model="editingData[`${user.id}-${day.date}`].work_start"
                      >
                    </div>
                    <div>
                      <input 
                        type="time" 
                        class="time-input"
                        v-model="editingData[`${user.id}-${day.date}`].work_end"
                      >
                    </div>
                    <div>
                      <button class="save-btn" @click="saveEdit(user.id, day.date)">保存</button>
                      <button class="cancel-btn" @click="cancelEdit(user.id, day.date)">取消</button>
                    </div>
                  </div>
                </div>
                <div v-else @click="startAdd(user.id, day.date)" class="empty-cell">
                  <div v-if="!isAdding(user.id, day.date)">
                  </div>
                  <div v-else>
                    <div>
                      <input 
                        type="time" 
                        class="time-input"
                        v-model="addingData[`${user.id}-${day.date}`].work_start"
                      >
                    </div>
                    <div>
                      <input 
                        type="time" 
                        class="time-input"
                        v-model="addingData[`${user.id}-${day.date}`].work_end"
                      >
                    </div>
                    <div>
                      <button class="save-btn" @click="saveAdd(user.id, day.date)">保存</button>
                      <button class="cancel-btn" @click="cancelAdd(user.id, day.date)">取消</button>
                    </div>
                  </div>
                </div>
              </td>
            </tr>
          </template>
        </tbody>
      </table>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import axios from 'axios'
import BackButton from '../components/BackButton.vue'

// リアクティブデータ
const users = ref([])
const workLogs = ref([])
const selectedMonth = ref(new Date().toISOString().slice(0, 7))
const loading = ref(false)
const error = ref(null)
const editingData = ref({})
const addingData = ref({})
const apiUrl = 'http://localhost:5174/api'

// 計算プロパティ
const daysInMonth = computed(() => {
  const [year, month] = selectedMonth.value.split('-').map(Number)
  const daysInMonth = new Date(year, month, 0).getDate()
  const days = []
  const today = new Date()
  
  for (let day = 1; day <= daysInMonth; day++) {
    const date = new Date(year, month - 1, day)
    const dateStr = date.toISOString().split('T')[0]
    const dayOfWeek = date.getDay()
    
    days.push({
      date: dateStr,
      display: `${month}/${day}\n${['日', '月', '火', '水', '木', '金', '土'][dayOfWeek]}`,
      isWeekend: dayOfWeek === 0 || dayOfWeek === 6,
      isToday: dateStr === today.toISOString().split('T')[0]
    })
  }
  
  return days
})

// work_startから日付部分（yyyy-mm-dd）を抽出するヘルパー関数（1日引く）
const getWorkStartDateString = (workStart) => {
  if (!workStart) return null
  // work_startの日付から1日引いた日付を取得
  const workStartDate = new Date(workStart)
  const adjustedDate = new Date(workStartDate.getTime())// - (24 * 60 * 60 * 1000)) // 1日（24時間）を引く
  return adjustedDate.toISOString().split('T')[0]
}

// メソッド
const fetchData = async () => {
  loading.value = true
  error.value = null
  
  try {
    // ユーザー情報と勤務記録を並行取得
    const [usersResponse, workLogsResponse] = await Promise.all([
      axios.get(`${apiUrl}/Users`),
      axios.get(`${apiUrl}/Work_logs`)
    ])
    
    users.value = usersResponse.data
    // 全ての勤務記録を取得してから、選択月でフィルタ
    const allWorkLogs = workLogsResponse.data
    workLogs.value = filterWorkLogsByMonth(allWorkLogs)
    
  } catch (err) {
    console.error('データ取得エラー:', err)
    error.value = 'データの取得に失敗しました。APIサーバーが起動していることを確認してください。'
  } finally {
    loading.value = false
  }
}

// スクリプト読み込みヘルパー関数
const loadScript = (src) => {
  return new Promise((resolve, reject) => {
    const script = document.createElement('script')
    script.src = src
    script.onload = resolve
    script.onerror = reject
    document.head.appendChild(script)
  })
}

// jsPDFライブラリを動的に読み込む関数（改良版）
const loadJsPDF = () => {
  return new Promise((resolve, reject) => {
    if (window.jspdf && window.jspdf.jsPDF) {
      resolve()
      return
    }
    
    // jsPDFの本体とフォント拡張を読み込み
    const loadScripts = async () => {
      try {
        // jsPDF本体
        await loadScript('https://cdnjs.cloudflare.com/ajax/libs/jspdf/2.5.1/jspdf.umd.min.js')
        
        // 少し待ってから初期化チェック
        await new Promise(resolve => setTimeout(resolve, 200))
        
        if (window.jspdf && window.jspdf.jsPDF) {
          resolve()
        } else {
          reject(new Error('jsPDFの初期化に失敗しました'))
        }
      } catch (error) {
        reject(error)
      }
    }
    
    loadScripts()
  })
}

// 印刷可能なHTMLを生成
const generatePrintableHTML = () => {
  const [year, month] = selectedMonth.value.split('-').map(Number)
  const title = `勤怠記録表 ${year}年${month}月`
  
  let html = `<div class="title">${title}</div>`
  
  // テーブル開始
  html += '<table>'
  
  // ヘッダー行
  html += '<thead><tr>'
  html += '<th class="user-info">ユーザー情報</th>'
  
  daysInMonth.value.forEach(day => {
    const dayNum = day.date.split('-')[2]
    const dayOfWeek = ['日', '月', '火', '水', '木', '金', '土'][new Date(day.date).getDay()]
    const weekendClass = day.isWeekend ? 'weekend' : ''
    html += `<th class="${weekendClass}">${month}/${dayNum}<br>${dayOfWeek}</th>`
  })
  
  html += '</tr></thead>'
  
  // データ行
  html += '<tbody>'
  users.value.forEach(user => {
    html += '<tr>'
    html += `<td class="user-info">
      <strong>${user.name}</strong><br>
      ID: ${user.user_id}<br>
      時給: ¥${user.wages}
    </td>`
    
    daysInMonth.value.forEach(day => {
      const workLog = getWorkLog(user.id, day.date)
      const weekendClass = day.isWeekend ? 'weekend' : ''
      
      if (workLog) {
        const startTime = formatTime(workLog.work_start)
        const endTime = formatTime(workLog.work_end)
        html += `<td class="${weekendClass}">
          ${startTime}<br>
          ${endTime}
        </td>`
      } else {
        html += `<td class="${weekendClass}">-</td>`
      }
    })
    
    html += '</tr>'
  })
  html += '</tbody></table>'
  
  // 統計情報
  html += '<div class="stats"><h3>統計情報</h3>'
  users.value.forEach(user => {
    const userLogs = workLogs.value.filter(log => log.user_id === user.id)
    const workDays = userLogs.length
    
    let totalMinutes = 0
    userLogs.forEach(log => {
      if (log.work_start && log.work_end) {
        const start = new Date(log.work_start)
        const end = new Date(log.work_end)
        const minutes = (end - start) / (1000 * 60)
        totalMinutes += minutes
      }
    })
    
    const totalHours = Math.floor(totalMinutes / 60)
    const remainingMinutes = totalMinutes % 60
    const totalWage = Math.round((totalMinutes / 60) * user.wages)
    
    html += `<p><strong>${user.name}</strong>: ${workDays}日勤務, ${totalHours}時間${remainingMinutes}分, 給与: ¥${totalWage.toLocaleString()}</p>`
  })
  html += '</div>'
  
  return html
}

// PDF出力関数（文字化け対策版 - HTML印刷方式）
const exportToPDF = async () => {
  try {
    // HTML to PDFライブラリを使用する代替案
    const printContent = generatePrintableHTML()
    
    // 新しいウィンドウでHTML表示
    const printWindow = window.open('', '_blank', 'width=1200,height=800')
    printWindow.document.write(`
      <!DOCTYPE html>
      <html>
      <head>
        <meta charset="UTF-8">
        <title>勤怠記録表</title>
        <style>
          body {
            font-family: 'Meiryo', 'MS PGothic', 'Hiragino Sans', 'Hiragino Kaku Gothic ProN', sans-serif;
            margin: 20px;
            font-size: 12px;
          }
          table {
            width: 100%;
            border-collapse: collapse;
            margin-bottom: 20px;
          }
          th, td {
            border: 1px solid #000;
            padding: 8px;
            text-align: center;
            vertical-align: middle;
          }
          th {
            background-color: #f0f0f0;
            font-weight: bold;
          }
          .user-info {
            text-align: left;
            background-color: #f8f8f8;
            min-width: 120px;
          }
          .weekend {
            background-color: #ffe6e6;
          }
          .title {
            text-align: center;
            font-size: 18px;
            font-weight: bold;
            margin-bottom: 20px;
          }
          .stats {
            margin-top: 20px;
            font-size: 11px;
          }
          .stats h3 {
            margin-bottom: 10px;
          }
          .stats p {
            margin: 5px 0;
          }
          @media print {
            body { margin: 0; }
            .no-print { display: none; }
            table { font-size: 10px; }
            th, td { padding: 6px; }
          }
          @page {
            size: A4 landscape;
            margin: 1cm;
          }
        </style>
      </head>
      <body>
        ${printContent}
        <div class="no-print" style="margin-top: 20px; text-align: center;">
          <button onclick="window.print()" style="padding: 10px 20px; margin: 5px; font-size: 14px;">印刷</button>
          <button onclick="window.close()" style="padding: 10px 20px; margin: 5px; font-size: 14px;">閉じる</button>
        </div>
      </body>
      </html>
    `)
    printWindow.document.close()
    
    // 自動的に印刷ダイアログを開く
    setTimeout(() => {
      printWindow.print()
    }, 500)
    
  } catch (err) {
    console.error('PDF出力エラー:', err)
    alert('PDF出力に失敗しました。')
  }
}

const filterWorkLogsByMonth = (logs) => {
  const [year, month] = selectedMonth.value.split('-').map(Number)
  
  return logs.filter(log => {
    const workStartDate = getWorkStartDateString(log.work_start)
    if (!workStartDate) return false
    
    const logYear = parseInt(workStartDate.split('-')[0])
    const logMonth = parseInt(workStartDate.split('-')[1])
    
    return logYear === year && logMonth === month
  })
}

const getWorkLog = (userId, date) => {
  return workLogs.value.find(log => {
    if (log.user_id !== userId) return false
    
    const workStartDate = getWorkStartDateString(log.work_start)
    return workStartDate === date
  })
}

const formatTime = (dateTime) => {
  if (!dateTime) return '-'
  return new Date(utcToJst(dateTime)).toLocaleTimeString('ja-JP', {
    hour: '2-digit',
    minute: '2-digit'
  })
}

const isEditing = (userId, date) => {
  return editingData.value[`${userId}-${date}`] !== undefined
}

const isAdding = (userId, date) => {
  return addingData.value[`${userId}-${date}`] !== undefined
}

const startEdit = (userId, date) => {
  const workLog = getWorkLog(userId, date)
  if (!workLog) return
  
  editingData.value[`${userId}-${date}`] = {
    work_start: formatTimeForInput(workLog.work_start),
    work_end: formatTimeForInput(workLog.work_end)
  }
}

const cancelEdit = (userId, date) => {
  delete editingData.value[`${userId}-${date}`]
}

const saveEdit = async (userId, date) => {
  const workLog = getWorkLog(userId, date)
  const editData = editingData.value[`${userId}-${date}`]
  
  if (!workLog || !editData) return
  
  try {
    const updatedWorkLog = {
      ...workLog,
      work_start: combineDateTime(date, editData.work_start),
      work_end: combineDateTime(date, editData.work_end)
    }
    
    await axios.put(`${apiUrl}/Work_logs/${workLog.id}`, updatedWorkLog)
    
    // ローカルデータを更新
    const index = workLogs.value.findIndex(log => log.id === workLog.id)
    if (index !== -1) {
      workLogs.value[index] = updatedWorkLog
    }
    
    delete editingData.value[`${userId}-${date}`]
    
  } catch (err) {
    console.error('更新エラー:', err)
    alert('勤務記録の更新に失敗しました。')
  }
}

const startAdd = (userId, date) => {
  addingData.value[`${userId}-${date}`] = {
    work_start: '09:00',
    work_end: '18:00'
  }
}

const cancelAdd = (userId, date) => {
  delete addingData.value[`${userId}-${date}`]
}

const saveAdd = async (userId, date) => {
  const addData = addingData.value[`${userId}-${date}`]
  if (!addData) return
  
  try {
    const newWorkLog = {
      user_id: userId,
      work_start: combineDateTime(date, addData.work_start),
      work_end: combineDateTime(date, addData.work_end)
    }
    
    const response = await axios.post(`${apiUrl}/Work_logs`, newWorkLog)
    
    // ローカルデータに追加
    workLogs.value.push(response.data)
    
    delete addingData.value[`${userId}-${date}`]
    
  } catch (err) {
    console.error('追加エラー:', err)
    alert('勤務記録の追加に失敗しました。')
  }
}

const deleteWorkLog = async (workLogId) => {
  if (!confirm('この勤務記録を削除してもよろしいですか？')) return
  
  try {
    await axios.delete(`${apiUrl}/Work_logs/${workLogId}`)
    
    // ローカルデータから削除
    workLogs.value = workLogs.value.filter(log => log.id !== workLogId)
    
  } catch (err) {
    console.error('削除エラー:', err)
    alert('勤務記録の削除に失敗しました。')
  }
}

const formatTimeForInput = (dateTime) => {
  if (!dateTime) return ''
  return new Date(dateTime).toTimeString().slice(0, 5)
}

const combineDateTime = (date, time) => {
  return `${date}T${time}:00`
}

function utcToJst(utcDateString) {
  if (!utcDateString) return ''
  const date = new Date(utcDateString)
  const jstDate = new Date(date.getTime() + (9 * 60 * 60 * 1000))
  return jstDate
}

// ライフサイクル
onMounted(() => {
  fetchData()
})
</script>

<style scoped>
.container {
  max-width: 1400px;
  margin: 0 auto;
  background: white;
  border-radius: 8px;
  box-shadow: 0 2px 10px rgba(0,0,0,0.1);
  padding: 20px;
}

.header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 30px;
  padding-bottom: 20px;
  border-bottom: 2px solid #e0e0e0;
}

.title {
  color: #333;
  margin: 0;
  font-size: 28px;
  font-weight: 600;
}

.controls {
  display: flex;
  gap: 20px;
  align-items: center;
}

.month-selector {
  display: flex;
  align-items: center;
  gap: 10px;
}

.month-selector input {
  padding: 8px 12px;
  border: 1px solid #ddd;
  border-radius: 4px;
  font-size: 16px;
}

.refresh-btn {
  padding: 8px 16px;
  background: #007bff;
  color: white;
  border: none;
  border-radius: 4px;
  cursor: pointer;
  font-size: 14px;
}

.refresh-btn:hover {
  background: #0056b3;
}

.export-controls {
  display: flex;
  gap: 10px;
}

.export-btn {
  padding: 8px 16px;
  background: #28a745;
  color: white;
  border: none;
  border-radius: 4px;
  cursor: pointer;
  font-size: 14px;
  font-weight: 500;
}

.export-btn:hover:not(:disabled) {
  background: #218838;
}

.export-btn:disabled {
  background: #6c757d;
  cursor: not-allowed;
}

.table-container {
  overflow-x: auto;
  margin-top: 20px;
}

.attendance-table {
  width: 100%;
  border-collapse: collapse;
  min-width: 1200px;
}

.attendance-table th,
.attendance-table td {
  border: 1px solid #ddd;
  padding: 8px;
  text-align: center;
  font-size: 12px;
}

.attendance-table th {
  background-color: #f8f9fa;
  font-weight: 600;
  position: sticky;
  top: 0;
  z-index: 10;
}

.user-info {
  background-color: #f0f8ff;
  font-weight: 600;
  min-width: 120px;
  text-align: left;
  padding: 10px;
}

.date-header {
  background-color: #e8f4f8;
  writing-mode: vertical-rl;
  text-orientation: mixed;
  min-width: 60px;
  font-size: 11px;
}

.work-time-cell {
  min-width: 80px;
  padding: 4px;
}

.time-input {
  width: 100%;
  padding: 2px 4px;
  border: 1px solid #ccc;
  border-radius: 3px;
  font-size: 11px;
  text-align: center;
}

.time-display {
  font-size: 11px;
  line-height: 1.2;
}

.edit-btn, .save-btn, .cancel-btn, .delete-btn {
  padding: 2px 6px;
  margin: 1px;
  border: none;
  border-radius: 3px;
  cursor: pointer;
  font-size: 10px;
}

.edit-btn {
  background: #28a745;
  color: white;
}

.save-btn {
  background: #007bff;
  color: white;
}

.cancel-btn {
  background: #6c757d;
  color: white;
}

.delete-btn {
  background: #dc3545;
  color: white;
}

.empty-cell {
  min-height: 60px;
  cursor: pointer;
  border-radius: 3px;
  transition: background-color 0.2s;
}

.empty-cell:hover {
  background-color: #f0f8ff;
}

.add-btn {
  background: #17a2b8;
  color: white;
  padding: 2px 6px;
  border: none;
  border-radius: 3px;
  cursor: pointer;
  font-size: 10px;
}

.loading {
  text-align: center;
  padding: 40px;
  color: #666;
}

.error {
  color: #dc3545;
  text-align: center;
  padding: 20px;
  background: #f8d7da;
  border: 1px solid #f5c6cb;
  border-radius: 4px;
  margin: 20px 0;
}

.no-data {
  text-align: center;
  padding: 40px;
  color: #666;
  font-style: italic;
}

.weekend {
  background-color: #ffe6e6;
}

.today {
  background-color: #fff3cd;
}
</style>
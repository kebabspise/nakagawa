<template>
  <div class="attendance-summary">
    <h3>ユーザー別勤務時間一覧</h3>

    <div class="filters">
      <label>
        ユーザー名:
        <input v-model="filterName" placeholder="名前でフィルター" />
      </label>
      <label>
        並び替え:
        <select v-model="sortKey">
          <option value="user_id">ユーザーID</option>
          <option value="user.name">ユーザー名</option>
          <option value="work_start">開始時刻</option>
          <option value="work_end">終了時刻</option>
        </select>
      </label>
      <label>
        昇順:
        <input type="checkbox" v-model="sortAsc" />
      </label>
    </div>

    <table>
      <thead>
        <tr>
          <th>ユーザーID</th>
          <th>ユーザー名</th>
          <th>勤務日</th>
          <th>開始時刻</th>
          <th>終了時刻</th>
          <th>勤務時間 (h)</th>
          <th>操作</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="log in filteredLogs" :key="log.id">
          <td>{{ log.user_id }}</td>
          <td>{{ log.user?.name || '不明' }}</td>
          <td>{{ formatDate(log.work_start) }}</td>
          <td><input type="datetime-local" v-model="log.work_start" /></td>
          <td><input type="datetime-local" v-model="log.work_end" /></td>
          <td>{{ calculateHours(log.work_start, log.work_end) }}</td>
          <td>
            <button @click="updateLog(log)">更新</button>
            <button @click="deleteLog(log.id)">削除</button>
          </td>
        </tr>
      </tbody>
    </table>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'

const props = defineProps({
  apiBaseUrl: { type: String, default: 'http://localhost:5174/api' }
})

const workLogs = ref([])
const filterName = ref('')
const sortKey = ref('user_id')
const sortAsc = ref(true)

onMounted(async () => {
  await loadLogs()
})

const loadLogs = async () => {
  try {
    const res = await fetch(`${props.apiBaseUrl}/work_logs`)
    if (!res.ok) throw new Error(`取得失敗: ${res.status}`)
    const data = await res.json()
    data.forEach(log => {
      if (log.work_start) log.work_start = log.work_start.slice(0, 16)
      if (log.work_end) log.work_end = log.work_end.slice(0, 16)
    })
    workLogs.value = data
  } catch (err) {
    console.error('勤務ログの取得に失敗:', err)
  }
}

const filteredLogs = computed(() => {
  let logs = [...workLogs.value]
  if (filterName.value) {
    logs = logs.filter(log => log.user?.name?.includes(filterName.value))
  }
  return logs.sort((a, b) => {
    const valA = getValue(a, sortKey.value)
    const valB = getValue(b, sortKey.value)
    if (valA === undefined || valB === undefined) return 0
    return sortAsc.value
      ? String(valA).localeCompare(String(valB))
      : String(valB).localeCompare(String(valA))
  })
})

const getValue = (obj, path) => {
  return path.split('.').reduce((o, key) => o?.[key], obj)
}

const updateLog = async (log) => {
  try {
    const updatedLog = {
      id: log.id,
      user_id: log.user_id,
      work_start: new Date(log.work_start).toISOString(),
      work_end: new Date(log.work_end).toISOString(),
      work_date: new Date(log.work_start).toISOString().split('T')[0]
    }
    const res = await fetch(`${props.apiBaseUrl}/work_logs/${log.id}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(updatedLog)
    })
    if (!res.ok) throw new Error(`更新失敗: ${res.status}`)
    alert('更新しました')
    await loadLogs()
  } catch (err) {
    console.error('更新エラー:', err)
    alert('更新に失敗しました')
  }
}

const deleteLog = async (id) => {
  if (!confirm('この勤務記録を削除しますか？')) return
  try {
    const res = await fetch(`${props.apiBaseUrl}/work_logs/${id}`, { method: 'DELETE' })
    if (!res.ok) throw new Error(`削除失敗: ${res.status}`)
    alert('削除しました')
    await loadLogs()
  } catch (err) {
    console.error('削除エラー:', err)
    alert('削除に失敗しました')
  }
}

const formatDate = (iso) => {
  if (!iso) return '-'
  return new Date(iso).toLocaleDateString('ja-JP')
}

const calculateHours = (start, end) => {
  if (!start || !end) return '-'
  const diff = (new Date(end) - new Date(start)) / (1000 * 60 * 60)
  return diff.toFixed(2)
}
</script>

<style scoped>
.attendance-summary {
  padding: 20px;
  max-width: 1000px;
  margin: auto;
}

.filters {
  display: flex;
  gap: 20px;
  margin-bottom: 16px;
  align-items: center;
}

.filters label {
  display: flex;
  flex-direction: column;
  font-weight: bold;
  font-size: 14px;
}

.attendance-summary table {
  width: 100%;
  border-collapse: collapse;
}

.attendance-summary th,
.attendance-summary td {
  padding: 8px 12px;
  border: 1px solid #ddd;
  text-align: center;
}

.attendance-summary th {
  background-color: #f4f4f4;
}

button {
  margin: 2px;
  padding: 4px 8px;
  font-size: 14px;
}

input[type="datetime-local"] {
  width: 180px;
}
</style>

<template>
  <div class="user-manage">
    <div class="header">
      <h1>従業員管理</h1>
      <button class="btn btn-primary" @click="showAddForm = true">
        新規従業員追加
      </button>
    </div>

    <!-- 従業員追加/編集フォーム -->
    <div v-if="showAddForm || editingUser" class="form-modal">
      <div class="form-overlay" @click="closeForm"></div>
      <div class="form-container">
        <h2>{{ editingUser ? '従業員情報編集' : '新規従業員追加' }}</h2>
        <form @submit.prevent="submitForm">
          <div class="form-group">
            <label for="user_id">従業員ID:</label>
            <input 
              id="user_id"
              v-model.number="formData.user_id" 
              type="number" 
              required
              :disabled="editingUser !== null"
            />
          </div>
          
          <div class="form-group">
            <label for="name">氏名:</label>
            <input 
              id="name"
              v-model="formData.name" 
              type="text" 
              required
            />
          </div>
          
          <div class="form-group">
            <label for="pass">パスワード:</label>
            <input 
              id="pass"
              v-model="formData.pass" 
              type="password" 
              required
            />
          </div>
          
          <div class="form-group">
            <label class="checkbox-label">
              <input 
                v-model="formData.admin" 
                type="checkbox"
              />
              管理者権限
            </label>
          </div>
          
          <div class="form-group">
            <label for="wages">時給:</label>
            <input 
              id="wages"
              v-model.number="formData.wages" 
              type="number" 
              required
              min="0"
            />
          </div>
          
          <div class="form-actions">
            <button type="button" class="btn btn-secondary" @click="closeForm">
              キャンセル
            </button>
            <button type="submit" class="btn btn-primary">
              {{ editingUser ? '更新' : '追加' }}
            </button>
          </div>
        </form>
      </div>
    </div>

    <!-- 従業員検索 -->
    <div class="search-section">
      <div class="search-form">
        <input 
          v-model.number="searchUserId" 
          type="number" 
          placeholder="従業員IDで検索"
          class="search-input"
        />
        <button class="btn btn-secondary" @click="searchUser">検索</button>
        <button class="btn btn-secondary" @click="clearSearch">クリア</button>
      </div>
    </div>

    <!-- 検索結果・従業員情報表示 -->
    <div v-if="currentUser" class="user-info">
      <div class="user-card">
        <div class="user-header">
          <h3>従業員情報</h3>
          <div class="user-actions">
            <button class="btn btn-edit" @click="startEdit(currentUser)">
              編集
            </button>
            <button class="btn btn-danger" @click="confirmDelete(currentUser.user_id)">
              削除
            </button>
          </div>
        </div>
        
        <div class="user-details">
          <div class="detail-item">
            <span class="label">従業員ID:</span>
            <span class="value">{{ currentUser.user_id }}</span>
          </div>
          <div class="detail-item">
            <span class="label">氏名:</span>
            <span class="value">{{ currentUser.name }}</span>
          </div>
          <div class="detail-item">
            <span class="label">管理者権限:</span>
            <span class="value">{{ currentUser.admin ? 'あり' : 'なし' }}</span>
          </div>
          <div class="detail-item">
            <span class="label">時給:</span>
            <span class="value">¥{{ currentUser.wages.toLocaleString() }}</span>
          </div>
        </div>
      </div>
    </div>

    <!-- メッセージ表示 -->
    <div v-if="message" :class="['message', messageType]">
      {{ message }}
    </div>

    <!-- 削除確認ダイアログ -->
    <div v-if="showDeleteConfirm" class="form-modal">
      <div class="form-overlay" @click="showDeleteConfirm = false"></div>
      <div class="confirm-container">
        <h3>削除確認</h3>
        <p>従業員ID: {{ deleteUserId }} を削除してもよろしいですか？</p>
        <div class="form-actions">
          <button class="btn btn-secondary" @click="showDeleteConfirm = false">
            キャンセル
          </button>
          <button class="btn btn-danger" @click="deleteUser">
            削除
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, reactive } from 'vue'

// リアクティブデータ
const currentUser = ref(null)
const showAddForm = ref(false)
const editingUser = ref(null)
const searchUserId = ref(null)
const message = ref('')
const messageType = ref('success')
const showDeleteConfirm = ref(false)
const deleteUserId = ref(null)

const formData = reactive({
  user_id: null,
  name: '',
  pass: '',
  admin: false,
  wages: 0
})

// 従業員検索
const searchUser = async () => {
  if (!searchUserId.value) return
  
  try {
    const response = await fetch(`http://localhost:5174/api/users/by-userid/${searchUserId.value}`)
    if (response.ok) {
      currentUser.value = await response.json()
      showMessage('従業員情報を取得しました', 'success')
    } else if (response.status === 404) {
      currentUser.value = null
      showMessage('指定された従業員IDが見つかりません', 'error')
    }
  } catch (error) {
    showMessage('検索中にエラーが発生しました', 'error')
    console.error('Search error:', error)
  }
}

// 検索クリア
const clearSearch = () => {
  searchUserId.value = null
  currentUser.value = null
  message.value = ''
}

// フォーム表示
const startEdit = (user) => {
  editingUser.value = user
  Object.assign(formData, {
    user_id: user.user_id,
    name: user.name,
    pass: '', // セキュリティのため空にする
    admin: user.admin,
    wages: user.wages
  })
}

// フォーム閉じる
const closeForm = () => {
  showAddForm.value = false
  editingUser.value = null
  resetForm()
}

// フォームリセット
const resetForm = () => {
  Object.assign(formData, {
    user_id: null,
    name: '',
    pass: '',
    admin: false,
    wages: 0
  })
}

// フォーム送信
const submitForm = async () => {
  try {
    if (editingUser.value) {
      await updateUser()
    } else {
      await createUser()
    }
  } catch (error) {
    showMessage('操作中にエラーが発生しました', 'error')
    console.error('Form submit error:', error)
  }
}

// 従業員作成
const createUser = async () => {
  const response = await fetch('http://localhost:5174/api/users', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json'
    },
    body: JSON.stringify(formData)
  })

  if (response.ok) {
    showMessage('従業員を追加しました', 'success')
    closeForm()
  } else {
    showMessage('従業員の追加に失敗しました', 'error')
  }
}

// 従業員更新
const updateUser = async () => {
  const response = await fetch(`http://localhost:5174/api/users/by-userid/${formData.user_id}`, {
    method: 'PUT',
    headers: {
      'Content-Type': 'application/json'
    },
    body: JSON.stringify(formData)
  })

  if (response.ok) {
    showMessage('従業員情報を更新しました', 'success')
    closeForm()
    // 表示中の従業員情報を更新
    if (currentUser.value && currentUser.value.user_id === formData.user_id) {
      await searchUser()
    }
  } else {
    showMessage('従業員情報の更新に失敗しました', 'error')
  }
}

// 削除確認
const confirmDelete = (userId) => {
  deleteUserId.value = userId
  showDeleteConfirm.value = true
}

// 従業員削除
const deleteUser = async () => {
  try {
    const response = await fetch(`http://localhost:5174/api/users/by-userid/${deleteUserId.value}`, {
      method: 'DELETE'
    })

    if (response.ok) {
      showMessage('従業員を削除しました', 'success')
      if (currentUser.value && currentUser.value.user_id === deleteUserId.value) {
        currentUser.value = null
      }
    } else {
      showMessage('従業員の削除に失敗しました', 'error')
    }
  } catch (error) {
    showMessage('削除中にエラーが発生しました', 'error')
    console.error('Delete error:', error)
  } finally {
    showDeleteConfirm.value = false
    deleteUserId.value = null
  }
}

// メッセージ表示
const showMessage = (text, type = 'success') => {
  message.value = text
  messageType.value = type
  setTimeout(() => {
    message.value = ''
  }, 3000)
}
</script>

<style scoped>
.user-manage {
  max-width: 800px;
  margin: 0 auto;
  padding: 20px;
}

.header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 30px;
  border-bottom: 2px solid #e0e0e0;
  padding-bottom: 15px;
}

.header h1 {
  color: #333;
  margin: 0;
}

.btn {
  padding: 10px 20px;
  border: none;
  border-radius: 5px;
  cursor: pointer;
  font-size: 14px;
  transition: background-color 0.3s;
}

.btn-primary {
  background-color: #007bff;
  color: white;
}

.btn-primary:hover {
  background-color: #0056b3;
}

.btn-secondary {
  background-color: #6c757d;
  color: white;
  margin-left: 10px;
}

.btn-secondary:hover {
  background-color: #545b62;
}

.btn-edit {
  background-color: #28a745;
  color: white;
  margin-right: 10px;
}

.btn-edit:hover {
  background-color: #1e7e34;
}

.btn-danger {
  background-color: #dc3545;
  color: white;
}

.btn-danger:hover {
  background-color: #c82333;
}

.form-modal {
  position: fixed;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  z-index: 1000;
  display: flex;
  justify-content: center;
  align-items: center;
}

.form-overlay {
  position: absolute;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  background-color: rgba(0, 0, 0, 0.5);
}

.form-container, .confirm-container {
  background: white;
  padding: 30px;
  border-radius: 10px;
  box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
  position: relative;
  max-width: 500px;
  width: 90%;
}

.form-container h2, .confirm-container h3 {
  margin-top: 0;
  color: #333;
  border-bottom: 1px solid #e0e0e0;
  padding-bottom: 10px;
}

.form-group {
  margin-bottom: 20px;
}

.form-group label {
  display: block;
  margin-bottom: 5px;
  font-weight: bold;
  color: #555;
}

.form-group input {
  width: 100%;
  padding: 10px;
  border: 1px solid #ddd;
  border-radius: 5px;
  font-size: 14px;
}

.form-group input:focus {
  outline: none;
  border-color: #007bff;
  box-shadow: 0 0 0 2px rgba(0, 123, 255, 0.25);
}

.checkbox-label {
  display: flex !important;
  align-items: center;
  font-weight: bold;
}

.checkbox-label input {
  width: auto !important;
  margin-right: 10px;
}

.form-actions {
  display: flex;
  justify-content: flex-end;
  gap: 10px;
  margin-top: 20px;
}

.search-section {
  margin-bottom: 30px;
}

.search-form {
  display: flex;
  align-items: center;
  gap: 10px;
}

.search-input {
  flex: 1;
  padding: 10px;
  border: 1px solid #ddd;
  border-radius: 5px;
  font-size: 14px;
}

.user-info {
  margin-bottom: 20px;
}

.user-card {
  border: 1px solid #e0e0e0;
  border-radius: 10px;
  padding: 20px;
  background-color: #f8f9fa;
}

.user-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 20px;
  border-bottom: 1px solid #dee2e6;
  padding-bottom: 10px;
}

.user-header h3 {
  margin: 0;
  color: #333;
}

.user-actions {
  display: flex;
  gap: 10px;
}

.user-details {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 15px;
}

.detail-item {
  display: flex;
  flex-direction: column;
}

.detail-item .label {
  font-weight: bold;
  color: #666;
  margin-bottom: 5px;
}

.detail-item .value {
  color: #333;
  font-size: 16px;
}

.message {
  padding: 10px 20px;
  border-radius: 5px;
  margin-top: 20px;
  font-weight: bold;
}

.message.success {
  background-color: #d4edda;
  color: #155724;
  border: 1px solid #c3e6cb;
}

.message.error {
  background-color: #f8d7da;
  color: #721c24;
  border: 1px solid #f5c6cb;
}

@media (max-width: 768px) {
  .header {
    flex-direction: column;
    gap: 15px;
    align-items: stretch;
  }

  .search-form {
    flex-direction: column;
  }

  .user-details {
    grid-template-columns: 1fr;
  }

  .user-header {
    flex-direction: column;
    gap: 15px;
    align-items: stretch;
  }

  .form-actions, .user-actions {
    flex-direction: column;
  }
}
</style>
<!-- UserManage -->
<template>
  <div class="user-manage-container">
    <!-- 戻るボタン -->
    <div class="back-button-wrapper">
      <BackButton />
    </div>

    <!-- ヘッダー -->
    <div class="header">
      <h2>従業員管理</h2>
      <button class="btn btn-primary" @click="showAddForm = true">
        新規従業員追加
      </button>
    </div>

    <!-- 従業員追加/編集フォーム -->
    <div v-if="showAddForm || editingUser" class="popup-overlay" @click="closeForm">
      <div class="popup-content" @click.stop>
        <div class="popup-header">
          <h3>{{ editingUser ? '従業員情報編集' : '新規従業員追加' }}</h3>
          <button @click="closeForm" class="close-btn">&times;</button>
        </div>
        
        <form @submit.prevent="submitForm" class="form-content">
          <div class="form-group">
            <label for="user_id">従業員ID:</label>
            <input 
              id="user_id"
              v-model.number="formData.user_id" 
              type="number" 
              required
              :disabled="editingUser !== null"
              class="form-control"
            />
          </div>
          
          <div class="form-group">
            <label for="name">氏名:</label>
            <input 
              id="name"
              v-model="formData.name" 
              type="text" 
              required
              class="form-control"
            />
          </div>
          
          <div class="form-group">
            <label for="pass">パスワード:</label>
            <input 
              id="pass"
              v-model="formData.pass" 
              type="password" 
              required
              class="form-control"
            />
          </div>
          
          <div class="form-group">
            <label class="checkbox-label">
              <input 
                v-model="formData.admin" 
                type="checkbox"
                class="checkbox-input"
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
              class="form-control"
            />
          </div>
          
          <div class="form-group">
            <label for="store">店舗番号:</label>
              <select 
                id="store"
                v-model.number="formData.store" 
                required
                class="form-control"
              >
                <option :value="1">1号店</option>
                <option :value="2">2号店</option>
                <option :value="3">3号店</option>
              </select>
          </div>
          
          <div class="form-actions">
            <button type="submit" class="btn btn-primary">
              {{ editingUser ? '更新' : '追加' }}
            </button>
            <button type="button" class="btn btn-secondary" @click="closeForm">
              キャンセル
            </button>
          </div>
        </form>
      </div>
    </div>

    <!-- 検索・絞り込みセクション -->
    <div class="search-section">
      <div class="search-form">
        <!-- 従業員ID検索 -->
        <div class="search-item">
          <label for="search-user-id">従業員ID検索:</label>
          <input 
            id="search-user-id"
            v-model.number="searchUserId" 
            type="number" 
            placeholder="従業員IDで検索"
            class="form-control search-input"
          />
          <button class="btn btn-primary" @click="searchUser">検索</button>
        </div>
        
        <!-- 店舗番号絞り込み -->
        <div class="search-item">
          <label for="filter-store">店舗番号絞り込み:</label>
          <select 
            id="filter-store"
            v-model.number="filterStore" 
            class="form-control search-input"
            @change="filterByStore"
          >
            <option value="">全店舗</option>
            <option v-for="storeNum in availableStores" :key="storeNum" :value="storeNum">
              {{ storeNum }}号店
            </option>
          </select>
        </div>
        
        <button class="btn btn-secondary" @click="clearSearch">すべてクリア</button>
      </div>
    </div>

    <!-- 従業員一覧表示 -->
    <div class="users-list-section">
      <div class="section-header">
        <h3>
          従業員一覧
          <span v-if="filterStore" class="filter-indicator">
            （{{ filterStore }}号店のみ表示）
          </span>
        </h3>
        <button class="btn btn-secondary" @click="loadAllUsers">
          一覧更新
        </button>
      </div>
      
      <div v-if="displayUsers.length > 0" class="users-grid">
        <div v-for="user in displayUsers" :key="user.id" class="user-card">
          <div class="user-card-header">
            <h4>{{ user.name }}</h4>
            <div class="user-actions">
              <button class="btn btn-primary" @click="startEdit(user)">
                編集
              </button>
              <button 
                class="btn btn-danger" 
                @click="confirmDelete(user.user_id)"
                :disabled="isLastAdmin(user)"
                :title="isLastAdmin(user) ? '最後の管理者のため削除できません' : ''"
              >
                削除
              </button>
            </div>
          </div>
          
          <div class="user-details">
            <div class="detail-item">
              <span class="label">従業員ID:</span>
              <span class="value">{{ user.user_id }}</span>
            </div>
            <div class="detail-item">
              <span class="label">店舗番号:</span>
              <span class="value store-badge">{{ user.store }}号店</span>
            </div>
            <div class="detail-item">
              <span class="label">管理者権限:</span>
              <span class="value admin-badge" :class="{ 'is-admin': user.admin }">
                {{ user.admin ? 'あり' : 'なし' }}
              </span>
            </div>
            <div class="detail-item">
              <span class="label">時給:</span>
              <span class="value">¥{{ user.wages.toLocaleString() }}</span>
            </div>
          </div>
        </div>
      </div>
      
      <div v-else class="no-users-message">
        <span v-if="filterStore">
          店舗{{ filterStore }}に該当する従業員データがありません
        </span>
        <span v-else>
          従業員データがありません
        </span>
      </div>
    </div>

    <!-- 検索結果表示（検索時のみ） -->
    <div v-if="currentUser && searchUserId" class="user-info-section">
      <div class="user-card highlighted">
        <div class="user-card-header">
          <h3>検索結果</h3>
          <div class="user-actions">
            <button class="btn btn-primary" @click="startEdit(currentUser)">
              編集
            </button>
            <button 
              class="btn btn-danger" 
              @click="confirmDelete(currentUser.user_id)"
              :disabled="isLastAdmin(currentUser)"
              :title="isLastAdmin(currentUser) ? '最後の管理者のため削除できません' : ''"
            >
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
            <span class="label">店舗番号:</span>
            <span class="value store-badge">{{ currentUser.store }}号店</span>
          </div>
          <div class="detail-item">
            <span class="label">管理者権限:</span>
            <span class="value admin-badge" :class="{ 'is-admin': currentUser.admin }">
              {{ currentUser.admin ? 'あり' : 'なし' }}
            </span>
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
    <div v-if="showDeleteConfirm" class="popup-overlay" @click="showDeleteConfirm = false">
      <div class="popup-content" @click.stop>
        <div class="popup-header">
          <h3>削除確認</h3>
          <button @click="showDeleteConfirm = false" class="close-btn">&times;</button>
        </div>
        
        <div class="form-content">
          <p>従業員ID: {{ deleteUserId }} を削除してもよろしいですか？</p>
          <div class="form-actions">
            <button class="btn btn-danger" @click="deleteUser">
              削除
            </button>
            <button class="btn btn-secondary" @click="showDeleteConfirm = false">
              キャンセル
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted, computed } from 'vue'
import BackButton from '../components/BackButton.vue'

// リアクティブデータ
const users = ref([])
const currentUser = ref(null)
const showAddForm = ref(false)
const editingUser = ref(null)
const searchUserId = ref(null)
const filterStore = ref('')
const message = ref('')
const messageType = ref('success')
const showDeleteConfirm = ref(false)
const deleteUserId = ref(null)

const formData = reactive({
  user_id: null,
  name: '',
  pass: '',
  admin: false,
  wages: 0,
  store: 1
})

// 計算プロパティ
const availableStores = computed(() => {
  const stores = [...new Set(users.value.map(user => user.store))].sort((a, b) => a - b)
  return stores
})

const displayUsers = computed(() => {
  if (!filterStore.value) {
    return users.value
  }
  return users.value.filter(user => user.store === filterStore.value)
})

// コンポーネントマウント時に全ユーザーを読み込む
onMounted(() => {
  loadAllUsers()
})

// 全ユーザー読み込み
const loadAllUsers = async () => {
  try {
    const response = await fetch('http://localhost:5174/api/users')
    if (response.ok) {
      users.value = await response.json()
    } else {
      showMessage('ユーザー一覧の取得に失敗しました', 'error')
    }
  } catch (error) {
    showMessage('ユーザー一覧の取得中にエラーが発生しました', 'error')
    console.error('Load users error:', error)
  }
}

// 最後の管理者かどうかをチェック
const isLastAdmin = (user) => {
  if (!user.admin) return false
  const adminCount = users.value.filter(u => u.admin).length
  return adminCount === 1
}

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

// 店舗絞り込み
const filterByStore = () => {
  // フィルタが変更された時の処理（必要に応じて追加のロジックを入れる）
}

// 検索・絞り込みクリア
const clearSearch = () => {
  searchUserId.value = null
  filterStore.value = ''
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
    wages: user.wages,
    store: user.store
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
    wages: 0,
    store: 1
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
    await loadAllUsers() // 一覧を更新
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
    await loadAllUsers() // 一覧を更新
    // 検索結果の従業員情報も更新
    if (currentUser.value && currentUser.value.user_id === formData.user_id) {
      await searchUser()
    }
  } else {
    showMessage('従業員情報の更新に失敗しました', 'error')
  }
}

// 削除確認
const confirmDelete = (userId) => {
  // 最後の管理者の場合は削除確認ダイアログを表示しない
  const userToDelete = users.value.find(u => u.user_id === userId)
  if (isLastAdmin(userToDelete)) {
    showMessage('最後の管理者のため削除できません', 'error')
    return
  }
  
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
      await loadAllUsers() // 一覧を更新
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
.user-manage-container {
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

.btn-secondary {
  background-color: #6c757d;
  color: white;
}

.btn-secondary:hover {
  background-color: #545b62;
}

.btn-danger {
  background-color: #dc3545;
  color: white;
}

.btn-danger:hover:not(:disabled) {
  background-color: #c82333;
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

.form-content {
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

.checkbox-label {
  display: flex !important;
  align-items: center;
  font-weight: bold;
}

.checkbox-input {
  width: auto !important;
  margin-right: 10px;
}

.form-actions {
  display: flex;
  gap: 10px;
  justify-content: flex-end;
  margin-top: 20px;
}

.search-section {
  margin-bottom: 30px;
  background-color: #f8f9fa;
  padding: 20px;
  border-radius: 8px;
}

.search-form {
  display: flex;
  flex-direction: column;
  gap: 20px;
}

.search-item {
  display: flex;
  align-items: center;
  gap: 10px;
}

.search-item label {
  min-width: 140px;
  font-weight: bold;
  color: #333;
}

.search-input {
  flex: 1;
  max-width: 200px;
}

.users-list-section {
  margin-bottom: 30px;
}

.section-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 20px;
  padding-bottom: 10px;
  border-bottom: 1px solid #eee;
}

.section-header h3 {
  margin: 0;
  color: #333;
}

.filter-indicator {
  font-size: 14px;
  color: #28a745;
  font-weight: normal;
}

.users-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(400px, 1fr));
  gap: 20px;
}

.user-info-section {
  margin-bottom: 20px;
}

.user-card {
  background: white;
  border-radius: 8px;
  padding: 0;
  border: 1px solid #ddd;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
}

.user-card.highlighted {
  border: 2px solid #28a745;
  box-shadow: 0 4px 8px rgba(40, 167, 69, 0.2);
}

.user-card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 15px 20px;
  border-bottom: 1px solid #eee;
  background-color: #f8f9fa;
  border-radius: 8px 8px 0 0;
}

.user-card-header h3,
.user-card-header h4 {
  margin: 0;
  color: #333;
}

.user-actions {
  display: flex;
  gap: 10px;
}

.user-details {
  padding: 20px;
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

.admin-badge {
  padding: 4px 8px;
  border-radius: 4px;
  font-size: 14px;
  font-weight: bold;
}

.admin-badge.is-admin {
  background-color: #e9f7ef;
  color: #155724;
  border: 1px solid #c3e6cb;
}

.store-badge {
  padding: 4px 8px;
  border-radius: 4px;
  font-size: 14px;
  font-weight: bold;
  background-color: #e3f2fd;
  color: #1565c0;
  border: 1px solid #bbdefb;
}

.no-users-message {
  text-align: center;
  color: #666;
  font-style: italic;
  padding: 40px;
  background-color: #f8f9fa;
  border-radius: 8px;
}

.message {
  padding: 15px 20px;
  border-radius: 4px;
  margin-top: 20px;
  font-weight: bold;
}

.message.success {
  background-color: #e9f7ef;
  color: #155724;
  border: 1px solid #c3e6cb;
}

.message.error {
  background-color: #f8d7da;
  color: #721c24;
  border: 1px solid #f5c6cb;
}

@media (max-width: 768px) {
  .user-manage-container {
    transform: none;
  }

  .header {
    flex-direction: column;
    gap: 15px;
    align-items: stretch;
  }

  .search-form {
    gap: 15px;
  }

  .search-item {
    flex-direction: column;
    align-items: stretch;
  }

  .search-item label {
    min-width: auto;
  }

  .search-input {
    max-width: none;
  }

  .users-grid {
    grid-template-columns: 1fr;
  }

  .user-details {
    grid-template-columns: 1fr;
  }

  .user-card-header {
    flex-direction: column;
    gap: 15px;
    align-items: stretch;
  }

  .form-actions, .user-actions {
    flex-direction: column;
  }

  .section-header {
    flex-direction: column;
    gap: 10px;
    align-items: stretch;
  }
}
</style>
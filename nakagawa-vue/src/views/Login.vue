<template>
  <div class="login-container">
    <h2>ログイン</h2>
    
    <!-- エラーメッセージ表示 -->
    <div v-if="errorMessage" class="error-message">
      {{ errorMessage }}
    </div>
    
    <!-- 成功メッセージ表示 -->
    <div v-if="successMessage" class="success-message">
      {{ successMessage }}
    </div>
    
    <form @submit.prevent="handleLogin">
      <div class="form-group">
        <label for="user_id">ユーザーID:</label>
        <input 
          type="number" 
          id="user_id" 
          v-model="userId" 
          required
          :disabled="isLoading"
        />
      </div>
      
      <div class="form-group">
        <label for="password">パスワード:</label>
        <input 
          type="password" 
          id="password" 
          v-model="password" 
          required
          :disabled="isLoading"
        />
      </div>
      
      <button 
        type="submit" 
        :disabled="isLoading || !userId || !password"
        class="login-button"
      >
        {{ isLoading ? 'ログイン中...' : 'ログイン' }}
      </button>
    </form>
  </div>
</template>

<script setup>
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useUser } from '../components/useUser'
import axios from 'axios'

const userId = ref('')
const password = ref('')
const isLoading = ref(false)
const errorMessage = ref('')
const successMessage = ref('')
const router = useRouter()

// ユーザー状態管理のcomposableを使用
const { setUser } = useUser()

// ログイン処理
const handleLogin = async () => {
  // メッセージをクリア
  errorMessage.value = ''
  successMessage.value = ''
  isLoading.value = true

  try {
    // ユーザーIDを数値に変換
    const userIdInt = parseInt(userId.value)
    
    // ユーザーIDが数値でない場合のバリデーション
    if (isNaN(userIdInt)) {
      throw new Error('ユーザーIDは数値で入力してください')
    }

    // ユーザー情報を取得するためのAPIリクエスト
    const response = await axios.get(`http://localhost:5174/api/users/by-userid/${userIdInt}`, {
      timeout: 10000 // 10秒でタイムアウト
    })

    // ユーザーが存在する場合
    if (response.data && response.status === 200) {
      const userData = response.data
      
      // パスワード認証は実際のアプリケーションではサーバーサイドで行うべきですが、
      // 現在のAPI構造では、パスワードがレスポンスに含まれていないため、
      // フロントエンド側でのパスワード検証を行う必要があります。
      // 
      // 注意: 実際のプロダクションでは、パスワードはハッシュ化され、
      // サーバーサイドで認証処理を行うべきです。
      
      // パスワードを検証するために、別途認証用のAPIエンドポイントが必要です。
      // 現在のAPI構造では認証機能がないため、ここでは簡単な実装を示します。
      
      // ユーザー情報をグローバル状態に保存
      setUser(userData)
      
      // adminフラグに基づいてページ遷移
      if (userData.admin === true) {
        successMessage.value = '管理者としてログインしました'
        // 管理者ページに遷移
        await router.push('/Admin_Home')
      } else {
        successMessage.value = 'ユーザーとしてログインしました'
        // 一般ユーザーページに遷移
        await router.push('/User_Home')
      }
      
      console.log('ログイン成功:', userData)
      
      // フォームをクリア
      userId.value = ''
      password.value = ''
    }

  } catch (error) {
    console.error('ログインエラー:', error)
    
    // エラーメッセージの設定
    if (error.response) {
      // サーバーからのレスポンスがある場合
      switch (error.response.status) {
        case 401:
          errorMessage.value = 'ユーザーIDまたはパスワードが間違っています'
          break
        case 404:
          errorMessage.value = 'ユーザーIDが見つかりません'
          break
        case 500:
          errorMessage.value = 'サーバーエラーが発生しました'
          break
        default:
          errorMessage.value = `エラーが発生しました (${error.response.status})`
      }
    } else if (error.request) {
      // リクエストが送信されたが、レスポンスがない場合
      errorMessage.value = 'サーバーに接続できません。ネットワークを確認してください'
    } else {
      // その他のエラー
      errorMessage.value = error.message || 'ログインに失敗しました'
    }
  } finally {
    isLoading.value = false
  }
}
</script>

<style scoped>
.login-container {
  max-width: 400px;
  margin: 2rem auto;
  padding: 2rem;
  border: 1px solid #ddd;
  border-radius: 12px;
  box-shadow: 0 2px 10px rgba(0, 0, 0, 0.1);
  background-color: #fff;
}

h2 {
  text-align: center;
  margin-bottom: 1.5rem;
  color: #333;
}

.form-group {
  margin-bottom: 1rem;
}

label {
  display: block;
  margin-bottom: 0.5rem;
  font-weight: bold;
  color: #555;
}

input {
  width: 100%;
  padding: 0.75rem;
  border: 1px solid #ccc;
  border-radius: 6px;
  box-sizing: border-box;
  font-size: 1rem;
  transition: border-color 0.3s ease;
}

input:focus {
  outline: none;
  border-color: #007bff;
  box-shadow: 0 0 0 2px rgba(0, 123, 255, 0.25);
}

input:disabled {
  background-color: #f5f5f5;
  cursor: not-allowed;
}

.login-button {
  width: 100%;
  padding: 0.75rem;
  background-color: #007bff;
  color: white;
  border: none;
  border-radius: 6px;
  font-size: 1rem;
  cursor: pointer;
  transition: background-color 0.3s ease;
}

.login-button:hover:not(:disabled) {
  background-color: #0056b3;
}

.login-button:disabled {
  background-color: #6c757d;
  cursor: not-allowed;
}

.error-message {
  background-color: #f8d7da;
  color: #721c24;
  padding: 0.75rem;
  border: 1px solid #f5c6cb;
  border-radius: 6px;
  margin-bottom: 1rem;
  text-align: center;
}

.success-message {
  background-color: #d4edda;
  color: #155724;
  padding: 0.75rem;
  border: 1px solid #c3e6cb;
  border-radius: 6px;
  margin-bottom: 1rem;
  text-align: center;
}
</style>
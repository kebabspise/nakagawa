// src/composables/useUser.js
import { ref, computed } from 'vue'

// グローバルな状態を管理
const userData = ref(null)

export function useUser() {
  // ユーザー情報を設定
  const setUser = (user) => {
    userData.value = user
    // ローカルストレージにも保存
    localStorage.setItem('user', JSON.stringify(user))
  }

  // ユーザー情報を取得
  const getUser = () => {
    if (!userData.value) {
      // ローカルストレージから復元を試みる
      const stored = localStorage.getItem('user')
      if (stored) {
        userData.value = JSON.parse(stored)
      }
    }
    return userData.value
  }

  // ユーザーをクリア（ログアウト用）
  const clearUser = () => {
    userData.value = null
    localStorage.removeItem('user')
  }

  // 現在のユーザー情報を取得
  const currentUser = computed(() => getUser())

  // ユーザーIDを取得
  const userId = computed(() => {
    const user = getUser()
    return user?.user_id || user?.id
  })

  // ユーザー名を取得
  const userName = computed(() => {
    const user = getUser()
    return user?.name || ''
  })

  // 管理者かどうかを判定
  const isAdmin = computed(() => {
    const user = getUser()
    return user?.admin === true
  })

  return {
    setUser,
    getUser,
    clearUser,
    currentUser,
    userId,
    userName,
    isAdmin
  }
}
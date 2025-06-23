<template>
  <div class="login-container">
    <form>
      <div>
        <label for="user_id">ユーザーID:</label>
        <input type="text" id="user_id" v-model="userId" />
      </div>
      <div>
        <label for="password">パスワード:</label>
        <input type="password" id="password" v-model="password" />
      </div>
      <button type="submit">ログイン</button>
    </form>
  </div>
</template>

<script setup>
import { ref } from 'vue'

const userId = ref('')
const password = ref('')
</script>

<style scoped>
.login-container {
  max-width: 300px;
  margin: 2rem auto;
  padding: 1rem;
  border: 1px solid #ccc;
  border-radius: 8px;
}

input {
  width: 100%;
  padding: 0.5rem;
  margin-top: 0.25rem;
  margin-bottom: 1rem;
  box-sizing: border-box;
}

button {
  width: 100%;
  padding: 0.5rem;
}
</style>

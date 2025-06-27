<template>
  <button 
    @click="goBack" 
    class="back-button"
    :disabled="!canGoBack && !fallbackRoute"
  >
    <span class="back-icon">←</span>
    <span class="back-text">{{ buttonText }}</span>
  </button>
</template>

<script setup>
import { computed } from 'vue'
import { useRouter } from 'vue-router'

const props = defineProps({
  buttonText: {
    type: String,
    default: '戻る'
  },
  fallbackRoute: {
    type: String,
    default: '/'
  },
  showOnlyIfCanGoBack: {
    type: Boolean,
    default: false
  }
})

const router = useRouter()

const canGoBack = computed(() => {
  return window.history.length > 1
})

const goBack = () => {
  if (canGoBack.value) {
    router.go(-1)
  } else if (props.fallbackRoute) {
    router.push(props.fallbackRoute)
  }
}
</script>

<style scoped>
.back-button {
  /* 幅・見た目・余白を main-btn と完全統一 */
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  width: 100%;
  margin: 0;
  padding: 16px;
  font-size: 17px;
  font-weight: bold;
  border: none;
  border-radius: 12px;
  background-color: #6c757d;
  color: white;
  cursor: pointer;
  transition: all 0.2s ease;
  box-shadow: 0 2px 5px rgba(0, 0, 0, 0.1);
}

.back-button:hover:not(:disabled) {
  background-color: #5a6268;
  transform: translateY(-1px);
  box-shadow: 0 4px 8px rgba(0, 0, 0, 0.15);
}

.back-button:disabled {
  background-color: #dee2e6;
  color: #6c757d;
  cursor: not-allowed;
  box-shadow: none;
  transform: none;
}

.back-icon {
  font-size: 1.1rem;
  font-weight: bold;
}

.back-text {
  font-size: 0.95rem;
}
</style>

<script setup>
import { ref } from 'vue';
import { useRouter } from 'vue-router';
import TimeClockPopup from '../components/TimeClock.vue';

const router = useRouter();
const title = ref('一般ユーザーのホーム画面です');
const title2 = ref('サブタイトルです。');
const showTimeClockPopup = ref(false);

function onClockRecorded(data) {
  console.log('打刻完了:', data);
}
</script>

<template>
  <div style="display: flex; flex-direction: column; align-items: center; justify-content: center; min-height: 60vh;">
    <h1>{{ title }}</h1>
    <h2>{{ title2 }}</h2>

    <button @click="goToAbout">Aboutページへ移動</button>
    <button @click="showTimeClockPopup = true">打刻</button>

    <TimeClockPopup 
      :show="showTimeClockPopup"
      @close="showTimeClockPopup = false"
      @clock-recorded="onClockRecorded"
    />

    <button @click="goToShiftSubmit">シフト提出</button>
    <button @click="goToShiftCheck">シフト確認</button>
  </div>
</template>
<template>
  <div>
    <h2>Frappe Gantt サンプル（1日単位の時間表示）</h2>
    <div ref="gantt" style="width:100%; min-width:1200px;"></div>
  </div>
</template>

<script setup>
import { onMounted, ref } from 'vue'
import Gantt from 'frappe-gantt'

const gantt = ref(null)
const tasks = [
  {
    id: 'salary',
    name: '給料を受け取る',
    start: '2025-06-24 09:00',
    end: '2025-06-24 10:00',
    progress: 0,
  },
  {
    id: 'meeting',
    name: 'ミーティング',
    start: '2025-06-24 13:00',
    end: '2025-06-24 14:30',
    progress: 100,
  },
  {
    id: 'report',
    name: 'レポート提出',
    start: '2025-06-24 15:00',
    end: '2025-06-24 16:00',
    progress: 50,
  },
  {
    id: 'training',
    name: '研修',
    start: '2025-06-24 17:00',
    end: '2025-06-24 18:00',
    progress: 80,
  }
]

onMounted(() => {
  const ganttChart = new Gantt(gantt.value, tasks, {
    view_mode: 'Hour', // 時間単位で表示
    custom_popup_html: null
  })
  ganttChart.change_view_mode('Hour')
  // 6月24日のみの範囲で初期表示
  setTimeout(() => {
    const svg = gantt.value.querySelector('svg');
    if (svg) {
      // 6月24日の日付ラベルを探してスクロール
      const labels = svg.querySelectorAll('.grid .tick text');
      for (const label of labels) {
        if (label.textContent && label.textContent.includes('06-24')) {
          label.scrollIntoView({ behavior: 'smooth', block: 'center', inline: 'center' });
          break;
        }
      }
    }
  }, 500);
})
</script>
<script setup>
import { RouterLink, RouterView } from 'vue-router';
</script>

<template>
  <ul id="nav">
    <li><RouterLink to="/">Home</RouterLink></li>
    <li><RouterLink to="/gantt">GanttSample</RouterLink></li> <!-- 追加 -->
  </ul>
  <RouterView />
</template>


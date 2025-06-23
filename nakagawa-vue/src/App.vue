<script setup>
import { RouterLink, RouterView } from 'vue-router';
</script>

<template>
  <ul id="nav">
    <li><RouterLink to="/">Home</RouterLink></li>
  </ul>
  <RouterView />
</template>


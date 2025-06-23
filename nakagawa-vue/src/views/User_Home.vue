<script setup>
import axios from 'axios';
import {ref, onMounted} from 'vue';
import { useRouter } from 'vue-router';
const router = useRouter();

// titleとtitle2の定義
const title = ref('一般ユーザーのホーム画面です');
const title2 = ref('サブタイトルです。')


function goToAbout()
    {
    router.push('/about')
    }//Aboutページへ移動する記述

// function goToAbout()
    // {
    // router.push('/about')
    // }//出退勤記録ページへ移動する記述

// function goToAbout()
    // {
    // router.push('/about')
    // }//シフト提出ページへ移動する記述

// function goToAbout()
    // {
    // router.push('/about')
    // }//シフト確認ページへ移動する記述

</script>



<template>
  <div style="display: flex; flex-direction: column; align-items: center; justify-content: center; min-height: 60vh;">
    <h1>{{ title }}</h1>
    <h2>{{ title2 }}</h2>

    <button @click="goToAbout" style="margin-top: 32px; padding: 12px 32px; font-size: 1.2rem;">Aboutページへ移動</button>
    <button @click="goToTimeCard" style="margin-top: 32px; padding: 12px 32px; font-size: 1.2rem;">入店・退店</button>
    <button @click="goToShiftSubmit" style="margin-top: 32px; padding: 12px 32px; font-size: 1.2rem;">シフト提出</button>
    <button @click="goToShiftCheck" style="margin-top: 32px; padding: 12px 32px; font-size: 1.2rem;">シフト確認</button>
  </div>
</template>








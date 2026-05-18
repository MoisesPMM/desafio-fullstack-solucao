<template>
  <div v-if="cidadesRegistradas.length || carregandoCidades" class="saved-cities" aria-label="Cidades registradas">
    <span>{{ carregandoCidades ? 'Carregando cidades salvas...' : 'Cidades salvas' }}</span>
    <div v-if="cidadesRegistradas.length" class="city-chip-list">
      <button
        v-for="cidade in cidadesRegistradas"
        :key="cidade"
        class="city-chip"
        type="button"
        @click="emit('selecionar-cidade', cidade)"
      >
        {{ cidade }}
      </button>
    </div>
  </div>
</template>

<script setup lang="ts">
defineProps<{
  cidadesRegistradas: string[]
  carregandoCidades: boolean
}>()

const emit = defineEmits<{
  'selecionar-cidade': [cidade: string]
}>()
</script>

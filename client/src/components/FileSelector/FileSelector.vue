<script setup lang="ts">
import { ref, computed } from 'vue'
import { Upload, FileText, Loader2, AlertCircle, X, FolderOpen } from 'lucide-vue-next'
import { useLogStore, useTabsStore } from '@/stores'
import { usePhotinoBridge } from '@/composables'
import { Button } from '@/components/ui/button'

const logStore = useLogStore()
const tabsStore = useTabsStore()
const { isDesktop, showOpenFileDialog, showOpenFolderDialog } = usePhotinoBridge()
const fileInput = ref<HTMLInputElement | null>(null)
const isDragOver = ref(false)

const ALLOWED_EXTENSIONS = ['.log', '.txt']
const MAX_FILE_SIZE_MB = 100

// Validate file
function validateFile(file: File): string | null {
  const ext = '.' + file.name.split('.').pop()?.toLowerCase()
  if (!ALLOWED_EXTENSIONS.includes(ext)) {
    return `Неверный формат файла. Разрешены: ${ALLOWED_EXTENSIONS.join(', ')}`
  }
  if (file.size > MAX_FILE_SIZE_MB * 1024 * 1024) {
    return `Файл слишком большой. Максимум: ${MAX_FILE_SIZE_MB}MB`
  }
  return null
}

// Handle file selection
async function handleFileSelect(file: File) {
  const error = validateFile(file)
  if (error) {
    tabsStore.setError(error)
    return
  }

  try {
    await logStore.uploadFile(file)
  } catch {
    // Error is already set in store
  }
}

// Input change handler
function onInputChange(event: Event) {
  const input = event.target as HTMLInputElement
  const file = input.files?.[0]
  if (file) {
    handleFileSelect(file)
  }
  // Reset input to allow selecting the same file again
  input.value = ''
}

// Click handler
function onButtonClick() {
  fileInput.value?.click()
}

// Drag & Drop handlers
function onDragEnter(event: DragEvent) {
  event.preventDefault()
  isDragOver.value = true
}

function onDragLeave(event: DragEvent) {
  event.preventDefault()
  isDragOver.value = false
}

function onDragOver(event: DragEvent) {
  event.preventDefault()
}

function onDrop(event: DragEvent) {
  event.preventDefault()
  isDragOver.value = false

  const file = event.dataTransfer?.files?.[0]
  if (file) {
    handleFileSelect(file)
  }
}

// Clear error
function clearError() {
  logStore.clearError()
}

// Desktop: Handle native file dialog
async function handleDesktopOpenFile() {
  try {
    const filePath = await showOpenFileDialog()
    if (filePath) {
      await logStore.openFile(filePath)
    }
  } catch (error) {
    tabsStore.setError(error instanceof Error ? error.message : 'Failed to open file')
  }
}

// Desktop: Handle native folder dialog
async function handleDesktopOpenFolder() {
  try {
    const folderPath = await showOpenFolderDialog()
    if (folderPath) {
      await logStore.openDirectory(folderPath)
    }
  } catch (error) {
    tabsStore.setError(error instanceof Error ? error.message : 'Failed to open folder')
  }
}

const isDisabled = computed(() => tabsStore.isLoading)
</script>

<template>
  <div class="file-selector w-full">
    <!-- Hidden file input -->
    <input
      ref="fileInput"
      type="file"
      accept=".log,.txt"
      class="hidden"
      :disabled="isDisabled"
      @change="onInputChange"
    />

    <!-- Drop zone -->
    <div
      class="drop-zone group relative overflow-hidden rounded-xl border-2 border-dashed transition-all duration-300"
      :class="{
        'border-zinc-700 bg-zinc-900/50 hover:border-zinc-600 hover:bg-zinc-900': !isDragOver && !tabsStore.hasError,
        'border-emerald-500 bg-emerald-950/20': isDragOver,
        'border-red-700 bg-red-950/20': tabsStore.hasError
      }"
      @dragenter="onDragEnter"
      @dragleave="onDragLeave"
      @dragover="onDragOver"
      @drop="onDrop"
    >
      <!-- Gradient glow on hover -->
      <div
        class="absolute inset-0 opacity-0 transition-opacity duration-300 group-hover:opacity-100"
        :class="{ 'opacity-100': isDragOver }"
      >
        <div class="absolute inset-0 bg-gradient-to-br from-emerald-500/5 via-transparent to-sky-500/5" />
      </div>

      <!-- Content -->
      <div class="relative flex flex-col items-center gap-4 px-8 py-10">
        <!-- Icon -->
        <div
          class="relative flex h-16 w-16 items-center justify-center rounded-2xl transition-all duration-300"
          :class="{
            'bg-zinc-800 text-zinc-400 group-hover:bg-zinc-700 group-hover:text-zinc-300': !isDragOver && !tabsStore.isLoading,
            'bg-emerald-900/50 text-emerald-400': isDragOver,
            'bg-zinc-800 text-emerald-500': tabsStore.isLoading
          }"
        >
          <Loader2 v-if="tabsStore.isLoading" class="h-8 w-8 animate-spin" />
          <Upload v-else-if="isDragOver" class="h-8 w-8" />
          <FileText v-else class="h-8 w-8" />

          <!-- Pulse ring on drag -->
          <div
            v-if="isDragOver"
            class="absolute inset-0 animate-ping rounded-2xl bg-emerald-500/30"
          />
        </div>

        <!-- Text -->
        <div class="text-center">
          <h3 class="font-mono text-base font-medium text-zinc-300">
            <template v-if="tabsStore.isLoading">Загрузка файла...</template>
            <template v-else-if="isDragOver">Отпустите для загрузки</template>
            <template v-else>Перетащите файл сюда</template>
          </h3>
          <p class="mt-1 font-mono text-sm text-zinc-500">
            <template v-if="!tabsStore.isLoading && !isDragOver">
              или нажмите кнопку ниже
            </template>
            <template v-else-if="tabsStore.isLoading">
              Пожалуйста, подождите...
            </template>
          </p>
        </div>

        <!-- Upload button(s) -->
        <!-- Web mode: single upload button -->
        <Button
          v-if="!isDesktop"
          variant="outline"
          :disabled="isDisabled"
          class="group/btn relative mt-2 overflow-hidden border-zinc-700 bg-zinc-800/50 font-mono text-sm transition-all hover:border-emerald-700 hover:bg-emerald-950/30"
          @click="onButtonClick"
        >
          <span class="relative z-10 flex items-center gap-2">
            <Upload class="h-4 w-4 transition-transform group-hover/btn:-translate-y-0.5" />
            <span>Выбрать файл</span>
          </span>
        </Button>

        <!-- Desktop mode: open file and open folder buttons -->
        <div v-else class="mt-2 flex gap-2">
          <Button
            variant="outline"
            :disabled="isDisabled"
            class="group/btn relative overflow-hidden border-zinc-700 bg-zinc-800/50 font-mono text-sm transition-all hover:border-emerald-700 hover:bg-emerald-950/30"
            @click="handleDesktopOpenFile"
          >
            <span class="relative z-10 flex items-center gap-2">
              <FileText class="h-4 w-4" />
              <span>Открыть файл</span>
            </span>
          </Button>
          <Button
            variant="outline"
            :disabled="isDisabled"
            class="group/btn relative overflow-hidden border-zinc-700 bg-zinc-800/50 font-mono text-sm transition-all hover:border-sky-700 hover:bg-sky-950/30"
            @click="handleDesktopOpenFolder"
          >
            <span class="relative z-10 flex items-center gap-2">
              <FolderOpen class="h-4 w-4" />
              <span>Открыть директорию</span>
            </span>
          </Button>
        </div>

        <!-- Allowed formats hint -->
        <p class="font-mono text-xs text-zinc-600">
          <template v-if="!isDesktop">
            Поддерживаемые форматы: .log, .txt • Максимум {{ MAX_FILE_SIZE_MB }}MB
          </template>
          <template v-else>
            Поддерживаемые форматы: .log, .txt
          </template>
        </p>
      </div>
    </div>

    <!-- Error message -->
    <Transition
      enter-active-class="transition-all duration-200"
      enter-from-class="opacity-0 -translate-y-2"
      enter-to-class="opacity-100 translate-y-0"
      leave-active-class="transition-all duration-150"
      leave-from-class="opacity-100"
      leave-to-class="opacity-0"
    >
      <div
        v-if="tabsStore.hasError"
        class="mt-3 flex items-center justify-between gap-3 rounded-lg border border-red-900/50 bg-red-950/30 px-4 py-3"
      >
        <div class="flex items-center gap-2">
          <AlertCircle class="h-4 w-4 flex-shrink-0 text-red-500" />
          <span class="font-mono text-sm text-red-400">{{ tabsStore.error }}</span>
        </div>
        <button
          type="button"
          class="rounded-md p-1 text-red-500/70 transition-colors hover:bg-red-500/10 hover:text-red-400"
          @click="clearError"
        >
          <X class="h-4 w-4" />
        </button>
      </div>
    </Transition>
  </div>
</template>

<style scoped>
.file-selector {
  font-family: 'JetBrains Mono', 'Fira Code', 'SF Mono', Consolas, monospace;
}
</style>

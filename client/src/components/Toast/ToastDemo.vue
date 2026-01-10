<script setup lang="ts">
import { useToast } from '@/composables/useToast'

const { showToast } = useToast()

const showErrorToast = () => {
  showToast('Failed to upload file. Please try again.', 'error')
}

const showSuccessToast = () => {
  showToast('File loaded successfully!', 'success')
}

const showInfoToast = () => {
  showToast('Processing your request...', 'info')
}

const showMultipleToasts = () => {
  showToast('First notification', 'info')
  setTimeout(() => showToast('Second notification', 'success'), 500)
  setTimeout(() => showToast('Third notification', 'error'), 1000)
}

const showLongMessage = () => {
  showToast(
    'This is a very long error message that demonstrates how the toast component handles text wrapping and maintains proper formatting even with extensive content.',
    'error'
  )
}
</script>

<template>
  <div class="min-h-screen bg-zinc-950 text-zinc-100 p-8">
    <div class="max-w-2xl mx-auto space-y-6">
      <h1 class="text-3xl font-mono font-bold mb-8">Toast Component Demo</h1>

      <div class="space-y-4">
        <button
          @click="showErrorToast"
          class="w-full border-2 border-red-500 bg-red-500/10 px-6 py-3 font-mono text-sm hover:bg-red-500/20 transition-colors"
        >
          Show Error Toast
        </button>

        <button
          @click="showSuccessToast"
          class="w-full border-2 border-green-500 bg-green-500/10 px-6 py-3 font-mono text-sm hover:bg-green-500/20 transition-colors"
        >
          Show Success Toast
        </button>

        <button
          @click="showInfoToast"
          class="w-full border-2 border-blue-500 bg-blue-500/10 px-6 py-3 font-mono text-sm hover:bg-blue-500/20 transition-colors"
        >
          Show Info Toast
        </button>

        <button
          @click="showMultipleToasts"
          class="w-full border-2 border-purple-500 bg-purple-500/10 px-6 py-3 font-mono text-sm hover:bg-purple-500/20 transition-colors"
        >
          Show Multiple Toasts
        </button>

        <button
          @click="showLongMessage"
          class="w-full border-2 border-yellow-500 bg-yellow-500/10 px-6 py-3 font-mono text-sm hover:bg-yellow-500/20 transition-colors"
        >
          Show Long Message
        </button>
      </div>

      <div class="mt-12 p-6 border-2 border-zinc-700 bg-zinc-900">
        <h2 class="text-xl font-mono font-semibold mb-4">Features:</h2>
        <ul class="space-y-2 font-mono text-sm text-zinc-400">
          <li>• Auto-dismiss after 5 seconds</li>
          <li>• Manual close with X button</li>
          <li>• Smooth transitions (slide from right)</li>
          <li>• Terminal/brutalist design</li>
          <li>• Type-based icons (AlertCircle, CheckCircle, Info)</li>
          <li>• Color-coded left border</li>
          <li>• Stacked notifications</li>
        </ul>
      </div>
    </div>
  </div>
</template>

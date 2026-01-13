<script setup lang="ts">
import { useToast } from '@/composables/useToast'

const { showToast } = useToast()

const showErrorToast = () => {
  showToast('Failed to upload file. Connection timeout occurred.', 'error')
}

const showSuccessToast = () => {
  showToast('File "2025-12-25.log" loaded successfully', 'success')
}

const showWarningToast = () => {
  showToast('Large file detected. Processing may take longer.', 'warning')
}

const showInfoToast = () => {
  showToast('Processing your request...', 'info')
}

const showMultipleToasts = () => {
  showToast('Starting file analysis', 'info', 6000)
  setTimeout(() => showToast('Parsing log entries...', 'info', 5000), 400)
  setTimeout(() => showToast('Found 15,000 entries', 'success', 4000), 800)
  setTimeout(() => showToast('3 warnings detected', 'warning', 4000), 1200)
}

const showLongMessage = () => {
  showToast(
    'This is a very long error message that demonstrates how the toast component handles text wrapping and maintains proper formatting even with extensive content that spans multiple lines.',
    'error',
    8000
  )
}

const showQuickToast = () => {
  showToast('Quick notification!', 'success', 2000)
}

const showPersistentToast = () => {
  showToast('This notification will stay for 10 seconds. Hover to pause!', 'info', 10000)
}
</script>

<template>
  <div class="min-h-screen bg-zinc-950 text-zinc-100 p-8">
    <div class="max-w-3xl mx-auto space-y-8">
      <!-- Header -->
      <div class="border-b-2 border-zinc-800 pb-6">
        <h1 class="text-4xl font-mono font-bold bg-gradient-to-r from-emerald-400 to-cyan-400 bg-clip-text text-transparent mb-2">
          Toast Notifications Demo
        </h1>
        <p class="font-mono text-sm text-zinc-500">
          Modern glassmorphism design with progress bars and hover interactions
        </p>
      </div>

      <!-- Basic Toasts -->
      <div class="space-y-3">
        <h2 class="text-xl font-mono font-semibold text-zinc-300 mb-4">Basic Notifications</h2>

        <button
          @click="showSuccessToast"
          class="w-full rounded-lg border-2 border-emerald-600/50 bg-emerald-950/30 px-6 py-3 font-mono text-sm text-emerald-400 hover:bg-emerald-900/30 hover:border-emerald-500 transition-all hover:shadow-lg hover:shadow-emerald-900/50"
        >
          <span class="flex items-center justify-center gap-2">
            <span class="text-lg">✓</span>
            Show Success Toast
          </span>
        </button>

        <button
          @click="showErrorToast"
          class="w-full rounded-lg border-2 border-rose-600/50 bg-rose-950/30 px-6 py-3 font-mono text-sm text-rose-400 hover:bg-rose-900/30 hover:border-rose-500 transition-all hover:shadow-lg hover:shadow-rose-900/50"
        >
          <span class="flex items-center justify-center gap-2">
            <span class="text-lg">✕</span>
            Show Error Toast
          </span>
        </button>

        <button
          @click="showWarningToast"
          class="w-full rounded-lg border-2 border-amber-600/50 bg-amber-950/30 px-6 py-3 font-mono text-sm text-amber-400 hover:bg-amber-900/30 hover:border-amber-500 transition-all hover:shadow-lg hover:shadow-amber-900/50"
        >
          <span class="flex items-center justify-center gap-2">
            <span class="text-lg">⚠</span>
            Show Warning Toast
          </span>
        </button>

        <button
          @click="showInfoToast"
          class="w-full rounded-lg border-2 border-cyan-600/50 bg-cyan-950/30 px-6 py-3 font-mono text-sm text-cyan-400 hover:bg-cyan-900/30 hover:border-cyan-500 transition-all hover:shadow-lg hover:shadow-cyan-900/50"
        >
          <span class="flex items-center justify-center gap-2">
            <span class="text-lg">ℹ</span>
            Show Info Toast
          </span>
        </button>
      </div>

      <!-- Advanced -->
      <div class="space-y-3">
        <h2 class="text-xl font-mono font-semibold text-zinc-300 mb-4">Advanced Features</h2>

        <button
          @click="showMultipleToasts"
          class="w-full rounded-lg border-2 border-purple-600/50 bg-purple-950/30 px-6 py-3 font-mono text-sm text-purple-400 hover:bg-purple-900/30 hover:border-purple-500 transition-all hover:shadow-lg hover:shadow-purple-900/50"
        >
          <span class="flex items-center justify-center gap-2">
            <span class="text-lg">⋮</span>
            Show Multiple Toasts (Staggered)
          </span>
        </button>

        <button
          @click="showLongMessage"
          class="w-full rounded-lg border-2 border-orange-600/50 bg-orange-950/30 px-6 py-3 font-mono text-sm text-orange-400 hover:bg-orange-900/30 hover:border-orange-500 transition-all hover:shadow-lg hover:shadow-orange-900/50"
        >
          <span class="flex items-center justify-center gap-2">
            <span class="text-lg">📝</span>
            Show Long Message (8s)
          </span>
        </button>

        <button
          @click="showQuickToast"
          class="w-full rounded-lg border-2 border-blue-600/50 bg-blue-950/30 px-6 py-3 font-mono text-sm text-blue-400 hover:bg-blue-900/30 hover:border-blue-500 transition-all hover:shadow-lg hover:shadow-blue-900/50"
        >
          <span class="flex items-center justify-center gap-2">
            <span class="text-lg">⚡</span>
            Show Quick Toast (2s)
          </span>
        </button>

        <button
          @click="showPersistentToast"
          class="w-full rounded-lg border-2 border-indigo-600/50 bg-indigo-950/30 px-6 py-3 font-mono text-sm text-indigo-400 hover:bg-indigo-900/30 hover:border-indigo-500 transition-all hover:shadow-lg hover:shadow-indigo-900/50"
        >
          <span class="flex items-center justify-center gap-2">
            <span class="text-lg">⏱</span>
            Show Persistent Toast (10s)
          </span>
        </button>
      </div>

      <!-- Features -->
      <div class="rounded-xl border-2 border-zinc-800 bg-zinc-900/50 p-6 backdrop-blur-sm">
        <h2 class="text-xl font-mono font-semibold mb-4 text-emerald-400">✨ Features</h2>
        <ul class="space-y-2.5 font-mono text-sm text-zinc-400">
          <li class="flex items-start gap-2">
            <span class="text-emerald-500 mt-0.5">▸</span>
            <span><strong class="text-zinc-300">Glassmorphism design</strong> with backdrop blur and transparency</span>
          </li>
          <li class="flex items-start gap-2">
            <span class="text-emerald-500 mt-0.5">▸</span>
            <span><strong class="text-zinc-300">Animated progress bar</strong> shows remaining time</span>
          </li>
          <li class="flex items-start gap-2">
            <span class="text-emerald-500 mt-0.5">▸</span>
            <span><strong class="text-zinc-300">Pause on hover</strong> - stops auto-dismiss while hovering</span>
          </li>
          <li class="flex items-start gap-2">
            <span class="text-emerald-500 mt-0.5">▸</span>
            <span><strong class="text-zinc-300">Icon glow effects</strong> appear on hover</span>
          </li>
          <li class="flex items-start gap-2">
            <span class="text-emerald-500 mt-0.5">▸</span>
            <span><strong class="text-zinc-300">Smooth animations</strong> - slide, fade, blur, and scale transitions</span>
          </li>
          <li class="flex items-start gap-2">
            <span class="text-emerald-500 mt-0.5">▸</span>
            <span><strong class="text-zinc-300">Staggered entrance</strong> for multiple notifications</span>
          </li>
          <li class="flex items-start gap-2">
            <span class="text-emerald-500 mt-0.5">▸</span>
            <span><strong class="text-zinc-300">Custom durations</strong> - configure auto-dismiss time</span>
          </li>
          <li class="flex items-start gap-2">
            <span class="text-emerald-500 mt-0.5">▸</span>
            <span><strong class="text-zinc-300">4 notification types</strong> - success, error, warning, info</span>
          </li>
          <li class="flex items-start gap-2">
            <span class="text-emerald-500 mt-0.5">▸</span>
            <span><strong class="text-zinc-300">Responsive design</strong> - works on all screen sizes</span>
          </li>
        </ul>
      </div>

      <!-- Instructions -->
      <div class="rounded-xl border border-cyan-800/50 bg-cyan-950/20 p-5 backdrop-blur-sm">
        <h3 class="font-mono text-sm font-semibold text-cyan-400 mb-2">💡 Interaction Tips</h3>
        <p class="font-mono text-xs text-cyan-300/80 leading-relaxed">
          Try hovering over a notification to pause the auto-dismiss timer. The progress bar will freeze,
          and you can read the message at your own pace. Move your mouse away to resume the countdown.
        </p>
      </div>
    </div>
  </div>
</template>

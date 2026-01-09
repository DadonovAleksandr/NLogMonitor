// Log Level (соответствует серверному LogLevel)
export const LogLevel = {
  Trace: 0,
  Debug: 1,
  Info: 2,
  Warn: 3,
  Error: 4,
  Fatal: 5
} as const

export type LogLevel = (typeof LogLevel)[keyof typeof LogLevel]

// Запись лога
export interface LogEntry {
  id: number
  timestamp: string // ISO date string
  level: string // строковое представление уровня (Trace, Debug, Info, Warn, Error, Fatal)
  message: string
  logger: string
  processId: number
  threadId: number
  exception?: string
}

// DTO для клиентских логов (отправка на сервер)
export interface ClientLogDto {
  level: LogLevel
  message: string
  timestamp?: string
}

// Результат открытия файла
export interface OpenFileResult {
  sessionId: string
  fileName: string
  filePath: string
  totalEntries: number
  levelCounts: LevelCounts
}

// Счётчики по уровням логов (ключ - строка уровня: "Trace", "Debug", и т.д.)
export type LevelCounts = Record<string, number>

// Параметры фильтрации
export interface FilterOptions {
  searchText?: string
  minLevel?: LogLevel
  maxLevel?: LogLevel
  fromDate?: string
  toDate?: string
  logger?: string
  page?: number
  pageSize?: number
}

// Результат с пагинацией
export interface PagedResult<T> {
  items: T[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
}

// Недавний файл
export interface RecentLog {
  path: string
  isDirectory: boolean
  openedAt: string
  displayName: string
}

// Ответ об ошибке API
export interface ApiError {
  message: string
  details?: string
  traceId?: string
}

// Health check response
export interface HealthResponse {
  status: string
  timestamp: string
}

// Форматы экспорта
export type ExportFormat = 'json' | 'csv'

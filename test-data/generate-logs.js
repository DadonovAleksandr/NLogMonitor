// Generate test log file with many entries for pagination testing
const fs = require('fs');
const path = require('path');

const levels = ['TRACE', 'DEBUG', 'INFO', 'WARN', 'ERROR', 'FATAL'];
const loggers = [
  'MyApp.Services.UserService',
  'MyApp.Controllers.ApiController',
  'MyApp.Data.Repository',
  'MyApp.Infrastructure.Database',
  'MyApp.Security.AuthService'
];

const messages = [
  'Processing user request',
  'Database query executed successfully',
  'Cache miss, fetching from database',
  'User authentication successful',
  'Invalid input parameter detected',
  'API rate limit exceeded',
  'Background job completed',
  'Configuration loaded',
  'Network connection established',
  'Transaction committed'
];

const outputPath = path.join(__dirname, 'test-logs.log');
const entryCount = 250; // Enough for 5 pages with pageSize=50
const lines = [];

// Start date
const startDate = new Date('2026-01-10T10:00:00.000');

for (let i = 0; i < entryCount; i++) {
  const timestamp = new Date(startDate.getTime() + i * 1000);
  const year = timestamp.getFullYear();
  const month = String(timestamp.getMonth() + 1).padStart(2, '0');
  const day = String(timestamp.getDate()).padStart(2, '0');
  const hours = String(timestamp.getHours()).padStart(2, '0');
  const minutes = String(timestamp.getMinutes()).padStart(2, '0');
  const seconds = String(timestamp.getSeconds()).padStart(2, '0');
  const ms = String(timestamp.getMilliseconds()).padStart(4, '0');

  const dateStr = `${year}-${month}-${day} ${hours}:${minutes}:${seconds}.${ms}`;
  const level = levels[Math.floor(Math.random() * levels.length)];
  const logger = loggers[Math.floor(Math.random() * loggers.length)];
  const message = `${messages[Math.floor(Math.random() * messages.length)]} (entry #${i + 1})`;
  const processId = 1234;
  const threadId = Math.floor(Math.random() * 10) + 1;

  const line = `${dateStr}|${level}|${message}|${logger}|${processId}|${threadId}`;
  lines.push(line);
}

fs.writeFileSync(outputPath, lines.join('\n'), 'utf8');
console.log(`Generated ${entryCount} log entries in ${outputPath}`);

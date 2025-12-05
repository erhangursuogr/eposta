// Development Environment Configuration
export const environment = {
  production: false,
  apiUrl: 'http://localhost:5118',
  apiVersion: 'v1',

  // Feature flags
  enableDebugMode: true,
  enableMockData: false,

  // Logging
  logLevel: 'debug', // 'debug' | 'info' | 'warn' | 'error'

  // Session
  tokenExpiryWarningMinutes: 10,
  autoLogoutMinutes: 720, // 12 hours

  // File upload
  maxFileSizeMB: 50,
  allowedFileTypes: [
    '.jpg', '.jpeg', '.png', '.gif',
    '.pdf', '.doc', '.docx',
    '.xls', '.xlsx',
    '.ppt', '.pptx',
    '.txt'
  ],

  // Pagination
  defaultPageSize: 20,
  pageSizeOptions: [10, 20, 50, 100],

  // Date format
  dateFormat: 'dd.MM.yyyy',
  dateTimeFormat: 'dd.MM.yyyy HH:mm',
  timeFormat: 'HH:mm',

  // Admin Dashboards
  hangfireUrl: 'http://localhost:5118/hangfire',
  seqUrl: 'http://localhost:5341',

  // Application info
  appName: 'DEÜ E-Posta Yönetim Sistemi',
  appVersion: '1.0.0',
  copyrightYear: new Date().getFullYear(),
  institutionName: 'Dokuz Eylül Üniversitesi'
};

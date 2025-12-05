// Production Environment Configuration
export const environment = {
  production: true,
  apiUrl: 'https://kurumsalduyuru.deu.edu.tr',
  apiVersion: 'v1',

  // Feature flags
  enableDebugMode: false,
  enableMockData: false,

  // Logging
  logLevel: 'error', // 'debug' | 'info' | 'warn' | 'error'

  // Session
  tokenExpiryWarningMinutes: 10,
  autoLogoutMinutes: 1440, // 24 hours

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
  hangfireUrl: 'http://localhost:5118/hangfire',//'https://kurumsalduyuru.deu.edu.tr/hangfire',
  seqUrl: 'http://localhost:5341/',//'https://seq.deu.edu.tr', // Production Seq URL (sunucu kurulunca güncelle)

  // Application info
  appName: 'DEÜ Duyuru Yönetim Sistemi',
  appVersion: '1.0.0',
  copyrightYear: new Date().getFullYear(),
  institutionName: 'Dokuz Eylül Üniversitesi'
};

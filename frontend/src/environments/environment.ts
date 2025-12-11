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

  // SSO Keycloak (AUTH_MODE=1 ise kullanılır)
  keycloakAuthUrl: 'https://sso.deu.edu.tr:8443/realms/dokuzeylul/protocol/openid-connect/auth?client_id=kurumsal-duyuru-client&redirect_uri=http://localhost:4200/auth/callback&response_type=code&scope=openid email profile' as string,

  keycloakLogoutUrl: 'https://sso.deu.edu.tr:8443/realms/dokuzeylul/protocol/openid-connect/logout?post_logout_redirect_uri=http://localhost:4200/login' as string,

  // Application info
  appName: 'DEÜ Kurumsal Duyuru Yönetim Sistemi',
  appVersion: '1.0.0',
  copyrightYear: new Date().getFullYear(),
  institutionName: 'Dokuz Eylül Üniversitesi'
};

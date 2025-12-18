// Dashboard API Response Models

export interface DashboardStats {
  totalAnnouncements: number;
  draftAnnouncements: number;
  pendingAnnouncements: number;
  approvedAnnouncements: number;
  sentAnnouncements: number;
  totalGroups: number;
  activeGroups: number;
  totalUsers: number;
  todayAnnouncements: number;
  todayLogins: number;
  weekAnnouncements: number;
  weekSentAnnouncements: number;
}

export interface RecentActivity {
  type: string; // 'DUYURU', 'LOGIN', etc.
  description: string;
  date: string; // ISO date string
  userId: number;
  userName: string;
  relatedId?: number;
  ipAddress?: string;
}

export interface AnnouncementChart {
  date: string; // ISO date string
  count: number;
  dateString: string; // formatted: "dd/MM"
}

export interface GroupStats {
  groupId: number;
  groupName: string;
  groupType: string; // 'MANUEL', 'DOSYA', 'DINAMIK', 'DEBIS'
  memberCount: number;
  announcementCount: number;
}

export interface SystemHealth {
  overallStatus: 'HEALTHY' | 'WARNING' | 'ERROR';
  databaseStatus: string;
  diskSpaceStatus: string;
  errorStatus: string;
  freeDiskSpaceGB: number;
  recentErrorCount: number;
  lastCheckTime: string; // ISO date string
  issues: string[];
}

export interface TopUser {
  userId: number;
  userName: string;
  email: string;
  roleName: string;
  announcementCount: number;
  loginCount: number;
  lastLoginDate?: string; // ISO date string
}

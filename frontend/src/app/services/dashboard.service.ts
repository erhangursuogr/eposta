import { Injectable, inject, signal } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../common/models/api-response.model';
import {
  DashboardStats,
  RecentActivity,
  AnnouncementChart,
  GroupStats,
  SystemHealth,
  TopUser
} from '../common/models/dashboard.model';

@Injectable({
  providedIn: 'root'
})
export class DashboardService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/api/dashboard`;

  // Shared stats signal for navbar and home
  stats = signal<DashboardStats | null>(null);

  /**
   * Get dashboard statistics
   * @param onlyMine - If true, returns stats for current user only
   */
  getDashboardStats(onlyMine: boolean = false): Observable<ApiResponse<DashboardStats>> {
    const params = new HttpParams().set('onlyMine', onlyMine.toString());
    return this.http.get<ApiResponse<DashboardStats>>(`${this.apiUrl}/stats`, { params })
      .pipe(
        tap(response => {
          if (response.success && response.data) {
            this.stats.set(response.data);
          }
        })
      );
  }

  /**
   * Get recent activities (announcements, logins)
   * @param count - Number of activities to retrieve (default: 10)
   */
  getRecentActivities(count: number = 10): Observable<ApiResponse<RecentActivity[]>> {
    const params = new HttpParams().set('count', count.toString());
    return this.http.get<ApiResponse<RecentActivity[]>>(`${this.apiUrl}/recent-activities`, { params });
  }

  /**
   * Get announcement chart data for trend visualization
   * @param days - Number of days to include (default: 30)
   */
  getAnnouncementChartData(days: number = 30): Observable<ApiResponse<AnnouncementChart[]>> {
    const params = new HttpParams().set('days', days.toString());
    return this.http.get<ApiResponse<AnnouncementChart[]>>(`${this.apiUrl}/announcement-chart`, { params });
  }

  /**
   * Get group statistics (top groups by member count)
   */
  getGroupStats(): Observable<ApiResponse<GroupStats[]>> {
    return this.http.get<ApiResponse<GroupStats[]>>(`${this.apiUrl}/group-stats`);
  }

  /**
   * Get system health status (ADMIN only)
   */
  getSystemHealth(): Observable<ApiResponse<SystemHealth>> {
    return this.http.get<ApiResponse<SystemHealth>>(`${this.apiUrl}/system-health`);
  }

  /**
   * Get top active users
   * @param count - Number of users to retrieve (default: 5)
   */
  getTopUsers(count: number = 5): Observable<ApiResponse<TopUser[]>> {
    const params = new HttpParams().set('count', count.toString());
    return this.http.get<ApiResponse<TopUser[]>>(`${this.apiUrl}/top-users`, { params });
  }
}

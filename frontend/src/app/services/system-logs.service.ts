import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../common/models/api-response.model';
import {
  LoginLog,
  SystemLog,
  EmailLog,
  PagedLogResponse,
  LoginLogFilterRequest,
  SystemLogFilterRequest,
  EmailLogFilterRequest
} from '../common/models/system-logs.model';

@Injectable({
  providedIn: 'root'
})
export class SystemLogsService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/api/admin/logs`;

  getLoginLogs(filter: LoginLogFilterRequest): Observable<ApiResponse<PagedLogResponse<LoginLog>>> {
    let params = new HttpParams();

    if (filter.baslangicTarihi) params = params.set('baslangicTarihi', filter.baslangicTarihi);
    if (filter.bitisTarihi) params = params.set('bitisTarihi', filter.bitisTarihi);
    if (filter.arama) params = params.set('arama', filter.arama);
    if (filter.girisTuru) params = params.set('girisTuru', filter.girisTuru);
    if (filter.sadeceBasarisiz !== undefined) params = params.set('sadeceBasarisiz', filter.sadeceBasarisiz.toString());
    params = params.set('sayfa', filter.sayfa.toString());
    params = params.set('sayfaBoyutu', filter.sayfaBoyutu.toString());

    return this.http.get<ApiResponse<PagedLogResponse<LoginLog>>>(`${this.apiUrl}/login`, { params });
  }

  getSystemLogs(filter: SystemLogFilterRequest): Observable<ApiResponse<PagedLogResponse<SystemLog>>> {
    let params = new HttpParams();

    if (filter.baslangicTarihi) params = params.set('baslangicTarihi', filter.baslangicTarihi);
    if (filter.bitisTarihi) params = params.set('bitisTarihi', filter.bitisTarihi);
    if (filter.arama) params = params.set('arama', filter.arama);
    if (filter.logSeviye) params = params.set('logSeviye', filter.logSeviye);
    if (filter.kategori) params = params.set('kategori', filter.kategori);
    if (filter.sadeceHata !== undefined) params = params.set('sadeceHata', filter.sadeceHata.toString());
    params = params.set('sayfa', filter.sayfa.toString());
    params = params.set('sayfaBoyutu', filter.sayfaBoyutu.toString());

    return this.http.get<ApiResponse<PagedLogResponse<SystemLog>>>(`${this.apiUrl}/system`, { params });
  }

  getEmailLogs(filter: EmailLogFilterRequest): Observable<ApiResponse<PagedLogResponse<EmailLog>>> {
    let params = new HttpParams();

    if (filter.baslangicTarihi) params = params.set('baslangicTarihi', filter.baslangicTarihi);
    if (filter.bitisTarihi) params = params.set('bitisTarihi', filter.bitisTarihi);
    if (filter.arama) params = params.set('arama', filter.arama);
    if (filter.duyuruId) params = params.set('duyuruId', filter.duyuruId.toString());
    if (filter.sadeceBasarisiz !== undefined) params = params.set('sadeceBasarisiz', filter.sadeceBasarisiz.toString());
    params = params.set('sayfa', filter.sayfa.toString());
    params = params.set('sayfaBoyutu', filter.sayfaBoyutu.toString());

    return this.http.get<ApiResponse<PagedLogResponse<EmailLog>>>(`${this.apiUrl}/email`, { params });
  }
}

import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse, LoginData, LoginRequest } from '../common/models/api-response.model';

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private _http = inject(HttpClient);
  private baseUrl = environment.apiUrl;

  login(request: LoginRequest): Observable<ApiResponse<LoginData>> {
    return this._http.post<ApiResponse<LoginData>>(`${this.baseUrl}/api/auth/login`, request);
  }

  logout(): Observable<ApiResponse> {
    return this._http.post<ApiResponse>(`${this.baseUrl}/api/auth/logout`, {});
  }

  getCurrentUser(): Observable<ApiResponse<any>> {
    return this._http.get<ApiResponse<any>>(`${this.baseUrl}/api/auth/me`);
  }
}

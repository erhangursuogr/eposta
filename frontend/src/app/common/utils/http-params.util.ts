import { HttpParams } from '@angular/common/http';

/**
 * HTTP query parametrelerini dinamik olarak oluşturan utility fonksiyonu
 * Kod tekrarını önler ve tutarlılık sağlar
 *
 * @example
 * const params = buildHttpParams({
 *   page: 1,
 *   pageSize: 20,
 *   searchQuery: 'test',
 *   durum: null // null değerler eklenmez
 * });
 */
export function buildHttpParams(filters: Record<string, any>): HttpParams {
  let params = new HttpParams();

  Object.keys(filters).forEach(key => {
    const value = filters[key];

    // null, undefined veya empty string değerleri atla
    if (value === null || value === undefined || value === '') {
      return;
    }

    // Array'leri string'e çevir
    if (Array.isArray(value)) {
      if (value.length > 0) {
        params = params.set(key, value.join(','));
      }
      return;
    }

    // Boolean değerleri string'e çevir
    if (typeof value === 'boolean') {
      params = params.set(key, value.toString());
      return;
    }

    // Date objelerini ISO string'e çevir
    if (value instanceof Date) {
      params = params.set(key, value.toISOString());
      return;
    }

    // Diğer tüm değerleri string'e çevir
    params = params.set(key, value.toString());
  });

  return params;
}

/**
 * Sayfalama parametrelerini standart formatta oluşturur
 *
 * @example
 * const params = buildPaginationParams(1, 20, 'olusturmaTarihi', 'desc');
 */
export function buildPaginationParams(
  page: number = 1,
  pageSize: number = 20,
  sortBy?: string,
  sortOrder?: 'asc' | 'desc'
): HttpParams {
  return buildHttpParams({
    page,
    pageSize,
    sortBy,
    sortOrder
  });
}

export interface FilterableField {
  field: string;
  label: string;
  type: "text" | "number" | "select" | "date" | string;
  options?: string[];
}

export interface FilterItem {
  field: string;
  operator: string;
  value: string;
}

export interface RelatedFilterGroup {
  entity: string;
  quantifier: "any" | "none";
  filters: FilterItem[];
}

export interface RelationshipDescriptor {
  entity: string;
  label: string;
}

export interface SearchRequest {
  filters: FilterItem[];
  relatedFilters?: RelatedFilterGroup[];
  sortBy?: string | null;
  sortDirection: "asc" | "desc";
  page: number;
  pageSize: number;
}

export interface SearchResponse<T = Record<string, unknown>> {
  total: number;
  items: T[];
  page: number;
  pageSize: number;
}

export interface SavedFilter {
  id: string;
  name: string;
  entity: string;
  filters: string;
  createdAt: string;
}

interface RawSearchResponse<T> {
  total?: number;
  items?: T[];
  data?: T[];
  page?: number;
  pageSize?: number;
}

function getAuthHeaders(): HeadersInit {
  if (typeof window === "undefined") return {};

  const token =
    localStorage.getItem("auth-token") ||
    localStorage.getItem("token") ||
    localStorage.getItem("jwt");

  return token ? { Authorization: `Bearer ${token}` } : {};
}

async function apiRequest<T>(
  path: string,
  options: RequestInit = {},
): Promise<T> {
  const response = await fetch(`/api/proxy${path}`, {
    ...options,
    headers: {
      "Content-Type": "application/json",
      ...getAuthHeaders(),
      ...(options.headers || {}),
    },
  });

  if (response.status === 401) {
    window.location.href = "/login";
    throw new Error("Unauthorised");
  }

  if (!response.ok) {
    throw new Error(`Request failed (HTTP ${response.status})`);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return response.json() as Promise<T>;
}

export async function fetchFilterableFields(
  entity: string,
): Promise<FilterableField[]> {
  return apiRequest<FilterableField[]>(`/search/fields/${entity}`);
}

export async function fetchRelationships(
  entity: string,
): Promise<RelationshipDescriptor[]> {
  return apiRequest<RelationshipDescriptor[]>(
    `/search/relationships/${entity}`,
  );
}

export async function searchEntity<T = Record<string, unknown>>(
  entity: string,
  request: SearchRequest,
): Promise<SearchResponse<T>> {
  const raw = await apiRequest<RawSearchResponse<T>>(`/search/${entity}`, {
    method: "POST",
    body: JSON.stringify(request),
  });

  return {
    total: raw.total ?? 0,
    items: raw.items ?? raw.data ?? [],
    page: raw.page ?? request.page,
    pageSize: raw.pageSize ?? request.pageSize,
  };
}

export async function fetchSavedFilters(
  entity: string,
): Promise<SavedFilter[]> {
  return apiRequest<SavedFilter[]>(`/search/saved-filters/${entity}`);
}

export async function createSavedFilter(
  entity: string,
  name: string,
  filters: FilterItem[],
  relatedFilters?: RelatedFilterGroup[],
): Promise<SavedFilter> {
  return apiRequest<SavedFilter>("/search/saved-filters", {
    method: "POST",
    body: JSON.stringify({ entity, name, filters, relatedFilters }),
  });
}

export async function deleteSavedFilter(id: string): Promise<void> {
  await apiRequest<void>(`/search/saved-filters/${id}`, {
    method: "DELETE",
  });
}

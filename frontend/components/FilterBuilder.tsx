"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import {
  createSavedFilter,
  deleteSavedFilter,
  fetchFilterableFields,
  fetchSavedFilters,
  searchEntity,
  type FilterableField,
  type FilterItem,
  type SavedFilter,
  type SearchRequest,
  type SearchResponse,
} from "@/lib/api/search";
import styles from "./FilterBuilder.module.css";

interface FilterBuilderProps<T = Record<string, unknown>> {
  entity: string;
  onResults: (data: SearchResponse<T>) => void;
  onRequestChange?: (request: SearchRequest) => void;
  pageSize?: number;
  sortBy?: string | null;
  sortDirection?: "asc" | "desc";
}

interface FilterRow {
  id: string;
  field: string;
  operator: string;
  value: string;
}

const OPERATORS_BY_TYPE: Record<string, { value: string; label: string }[]> = {
  text: [
    { value: "equals", label: "equals" },
    { value: "contains", label: "contains" },
  ],
  number: [
    { value: "equals", label: "equals" },
    { value: "greaterThan", label: "greater than" },
    { value: "lessThan", label: "less than" },
  ],
  select: [{ value: "equals", label: "equals" }],
  date: [
    { value: "equals", label: "on" },
    { value: "greaterThan", label: "after" },
    { value: "lessThan", label: "before" },
  ],
};

function createEmptyRow(): FilterRow {
  return {
    id: crypto.randomUUID(),
    field: "",
    operator: "equals",
    value: "",
  };
}

function toRows(filters: FilterItem[]): FilterRow[] {
  return filters.map((filter) => ({
    id: crypto.randomUUID(),
    field: filter.field,
    operator: filter.operator,
    value: filter.value,
  }));
}

function toValidFilters(rows: FilterRow[]): FilterItem[] {
  return rows
    .filter((row) => row.field.trim() && row.value.trim())
    .map(({ field, operator, value }) => ({ field, operator, value }));
}

export default function FilterBuilder<T = Record<string, unknown>>({
  entity,
  onResults,
  onRequestChange,
  pageSize = 25,
  sortBy = null,
  sortDirection = "asc",
}: FilterBuilderProps<T>) {
  const [fields, setFields] = useState<FilterableField[]>([]);
  const [savedFilters, setSavedFilters] = useState<SavedFilter[]>([]);
  const [rows, setRows] = useState<FilterRow[]>([]);
  const [selectedSavedFilterId, setSelectedSavedFilterId] = useState("");
  const [open, setOpen] = useState(false);
  const [loading, setLoading] = useState(false);
  const [initialising, setInitialising] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fieldMap = useMemo(
    () => new Map(fields.map((field) => [field.field, field])),
    [fields],
  );

  const activeFilters = useMemo(() => toValidFilters(rows), [rows]);
  const selectedSavedFilter = savedFilters.find(
    (filter) => filter.id === selectedSavedFilterId,
  );

  const runSearch = useCallback(
    async (filters: FilterItem[], page = 1) => {
      const request: SearchRequest = {
        filters,
        sortBy,
        sortDirection,
        page,
        pageSize,
      };

      setLoading(true);
      setError(null);
      onRequestChange?.(request);

      try {
        const results = await searchEntity<T>(entity, request);
        onResults(results);
      } catch (err) {
        setError(err instanceof Error ? err.message : "Search failed.");
      } finally {
        setLoading(false);
      }
    },
    [entity, onRequestChange, onResults, pageSize, sortBy, sortDirection],
  );

  const refreshSavedFilters = useCallback(async () => {
    const saved = await fetchSavedFilters(entity);
    setSavedFilters(saved);
  }, [entity]);

  useEffect(() => {
    let active = true;

    async function initialise() {
      setInitialising(true);
      setError(null);

      try {
        const [fieldResults, savedResults] = await Promise.all([
          fetchFilterableFields(entity),
          fetchSavedFilters(entity),
        ]);

        if (!active) return;
        setFields(fieldResults);
        setSavedFilters(savedResults);
        await runSearch([], 1);
      } catch (err) {
        if (active) {
          setError(err instanceof Error ? err.message : "Failed to load filters.");
        }
      } finally {
        if (active) setInitialising(false);
      }
    }

    initialise();

    return () => {
      active = false;
    };
  }, [entity, runSearch]);

  function updateRow(id: string, patch: Partial<FilterRow>) {
    setRows((current) =>
      current.map((row) => (row.id === id ? { ...row, ...patch } : row)),
    );
  }

  function handleFieldChange(row: FilterRow, fieldName: string) {
    updateRow(row.id, {
      field: fieldName,
      operator: "equals",
      value: "",
    });
  }

  async function handleApply() {
    await runSearch(activeFilters, 1);
  }

  async function handleClear() {
    setRows([]);
    setSelectedSavedFilterId("");
    await runSearch([], 1);
  }

  async function handleSave() {
    const filters = activeFilters;
    if (filters.length === 0) return;

    const name = window.prompt("Name this filter set");
    if (!name?.trim()) return;

    setLoading(true);
    setError(null);

    try {
      const saved = await createSavedFilter(entity, name.trim(), filters);
      await refreshSavedFilters();
      setSelectedSavedFilterId(saved.id);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to save filter.");
    } finally {
      setLoading(false);
    }
  }

  async function handleSavedFilterSelect(id: string) {
    setSelectedSavedFilterId(id);
    if (!id) return;

    const saved = savedFilters.find((filter) => filter.id === id);
    if (!saved) return;

    try {
      const parsed = JSON.parse(saved.filters) as FilterItem[];
      setRows(toRows(parsed));
      await runSearch(parsed, 1);
    } catch {
      setError("Saved filter could not be parsed.");
    }
  }

  async function handleDeleteSavedFilter(id: string) {
    setLoading(true);
    setError(null);

    try {
      await deleteSavedFilter(id);
      if (selectedSavedFilterId === id) setSelectedSavedFilterId("");
      await refreshSavedFilters();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to delete filter.");
    } finally {
      setLoading(false);
    }
  }

  return (
    <section>
      <div className={styles.toolbar}>
        <button
          type="button"
          className={`${styles.toggle} ${
            activeFilters.length > 0 ? styles.toggleActive : ""
          }`}
          onClick={() => setOpen((value) => !value)}
        >
          Filters {open ? "▲" : "▼"}
        </button>

        {savedFilters.length > 0 && (
          <div className={styles.savedActions}>
            <select
              className={styles.select}
              value={selectedSavedFilterId}
              onChange={(event) => handleSavedFilterSelect(event.target.value)}
              disabled={loading}
            >
              <option value="">Saved filters</option>
              {savedFilters.map((filter) => (
                <option key={filter.id} value={filter.id}>
                  {filter.name}
                </option>
              ))}
            </select>
            {selectedSavedFilter && (
              <button
                type="button"
                className={styles.dangerButton}
                onClick={() => handleDeleteSavedFilter(selectedSavedFilter.id)}
                disabled={loading}
                aria-label={`Delete ${selectedSavedFilter.name}`}
              >
                ×
              </button>
            )}
          </div>
        )}

        <span className={styles.activeCount}>
          {activeFilters.length} active filter(s)
        </span>
      </div>

      {open && (
        <div className={styles.panel}>
          {rows.map((row) => {
            const field = fieldMap.get(row.field);
            const operators = OPERATORS_BY_TYPE[field?.type ?? "text"] ?? [
              { value: "equals", label: "equals" },
            ];

            return (
              <div className={styles.row} key={row.id}>
                <select
                  className={styles.select}
                  value={row.field}
                  onChange={(event) => handleFieldChange(row, event.target.value)}
                >
                  <option value="">Select field</option>
                  {fields.map((fieldOption) => (
                    <option key={fieldOption.field} value={fieldOption.field}>
                      {fieldOption.label}
                    </option>
                  ))}
                </select>

                <select
                  className={styles.select}
                  value={row.operator}
                  onChange={(event) =>
                    updateRow(row.id, { operator: event.target.value })
                  }
                  disabled={!row.field}
                >
                  {operators.map((operator) => (
                    <option key={operator.value} value={operator.value}>
                      {operator.label}
                    </option>
                  ))}
                </select>

                {field?.type === "select" ? (
                  <select
                    className={styles.select}
                    value={row.value}
                    onChange={(event) =>
                      updateRow(row.id, { value: event.target.value })
                    }
                    disabled={!field}
                  >
                    <option value="">Select value</option>
                    {(field.options ?? []).map((option) => (
                      <option key={option} value={option}>
                        {option}
                      </option>
                    ))}
                  </select>
                ) : (
                  <input
                    className={styles.input}
                    type={
                      field?.type === "number"
                        ? "number"
                        : field?.type === "date"
                          ? "date"
                          : "text"
                    }
                    value={row.value}
                    onChange={(event) =>
                      updateRow(row.id, { value: event.target.value })
                    }
                    placeholder="Value"
                    disabled={!field}
                  />
                )}

                <button
                  type="button"
                  className={styles.removeButton}
                  onClick={() =>
                    setRows((current) =>
                      current.filter((candidate) => candidate.id !== row.id),
                    )
                  }
                  aria-label="Remove filter"
                >
                  ×
                </button>
              </div>
            );
          })}

          <div className={styles.actions}>
            <button
              type="button"
              className={styles.button}
              onClick={() => setRows((current) => [...current, createEmptyRow()])}
            >
              + Add filter
            </button>
            <button
              type="button"
              className={`${styles.button} ${styles.primaryButton}`}
              onClick={handleApply}
              disabled={loading || initialising}
            >
              {loading ? "Searching..." : "Apply"}
            </button>
            <button
              type="button"
              className={styles.button}
              onClick={handleClear}
              disabled={loading}
            >
              Clear
            </button>
            {rows.length > 0 && (
              <button
                type="button"
                className={styles.button}
                onClick={handleSave}
                disabled={loading || activeFilters.length === 0}
              >
                Save filter
              </button>
            )}
          </div>

          {savedFilters.length > 0 && (
            <div className={styles.savedList}>
              {savedFilters.map((filter) => (
                <div className={styles.savedItem} key={filter.id}>
                  <button
                    type="button"
                    className={styles.button}
                    onClick={() => handleSavedFilterSelect(filter.id)}
                    disabled={loading}
                  >
                    {filter.name}
                  </button>
                  <button
                    type="button"
                    className={styles.dangerButton}
                    onClick={() => handleDeleteSavedFilter(filter.id)}
                    disabled={loading}
                    aria-label={`Delete ${filter.name}`}
                  >
                    Delete
                  </button>
                </div>
              ))}
            </div>
          )}

          {error && <div className={styles.error}>{error}</div>}
        </div>
      )}

      {!open && error && <div className={styles.error}>{error}</div>}
    </section>
  );
}

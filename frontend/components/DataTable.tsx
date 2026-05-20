"use client";

import type { ReactNode } from "react";

export interface Column<T> {
  label: string;
  render: (row: T) => ReactNode;
}

interface DataTableProps<T> {
  columns: Column<T>[];
  rows: T[];
  emptyMessage?: string;
}

export default function DataTable<T>({
  columns,
  rows,
  emptyMessage = "No records to display.",
}: DataTableProps<T>) {
  if (rows.length === 0) {
    return <div className="empty">{emptyMessage}</div>;
  }

  return (
    <div className="table-wrap">
      <table>
        <thead>
          <tr>
            {columns.map((col) => (
              <th key={col.label}>{col.label}</th>
            ))}
          </tr>
        </thead>
        <tbody>
          {rows.map((row, i) => (
            <tr key={i}>
              {columns.map((col) => (
                <td key={col.label}>{col.render(row)}</td>
              ))}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

export function StatusBadge({ value }: { value: string | null | undefined }) {
  if (!value) return null;
  const cls = String(value)
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "-");
  return <span className={`status status-${cls}`}>{value}</span>;
}

export function formatDate(value: string | null | undefined): string {
  if (!value) return "";
  return new Intl.DateTimeFormat("en-AU", {
    year: "numeric",
    month: "short",
    day: "numeric",
  }).format(new Date(value));
}

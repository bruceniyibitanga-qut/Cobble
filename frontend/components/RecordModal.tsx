"use client";

import { type FormEvent } from "react";

export type FieldDef = [
  name: string,
  label: string,
  inputType: string,
  required: boolean,
  options?: string[],
];

interface Organisation {
  id: string;
  name: string;
}

interface RecordModalProps {
  open: boolean;
  title: string;
  fields: FieldDef[];
  record: Record<string, unknown>;
  partners: Organisation[];
  isNew: boolean;
  onClose: () => void;
  onSubmit: (data: Record<string, unknown>) => void;
}

function defaultValue(name: string): unknown {
  const defaults: Record<string, unknown> = {
    partnershipStatus: "prospect",
    projectType: "capstone",
    semester: "S1",
    year: new Date().getFullYear(),
    status: "proposed",
    multipleTeams: "false",
    proposedProjectType: "capstone",
    proposedSemester: "S1",
    proposedYear: new Date().getFullYear(),
    proposedMultipleTeams: "false",
    eventType: "meeting",
    eventDate: new Date().toISOString().slice(0, 10),
    role: "industry_partner",
    isActive: "true",
  };
  return defaults[name];
}

export default function RecordModal({
  open,
  title,
  fields,
  record,
  partners,
  isNew,
  onClose,
  onSubmit,
}: RecordModalProps) {
  if (!open) return null;

  function handleSubmit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const form = new FormData(e.currentTarget);
    const data: Record<string, unknown> = Object.fromEntries(form.entries());

    ["industryId", "facultyId", "year", "proposedYear", "proposedFacultyId"].forEach(
      (key) => {
        data[key] = data[key] ? Number(data[key]) : null;
      },
    );
    ["contactId", "organisationId"].forEach((key) => {
      data[key] = data[key] || null;
    });
    if (data.isActive !== undefined) data.isActive = data.isActive === "true";
    if (data.multipleTeams !== undefined) {
      data.multipleTeams = data.multipleTeams === "true";
    }
    if (data.proposedMultipleTeams !== undefined) {
      data.proposedMultipleTeams = data.proposedMultipleTeams === "true";
    }
    if (data.password === "") data.password = null;

    onSubmit(data);
  }

  return (
    <div className="modal-overlay active" onClick={onClose}>
      <div className="modal" onClick={(e) => e.stopPropagation()}>
        <h2>{title}</h2>
        <form onSubmit={handleSubmit}>
          <div className="form-grid">
            {fields.map(([name, label, inputType, required, options]) => {
              const value = record[name] ?? defaultValue(name) ?? "";
              const isRequired =
                required || (isNew && name === "password");
              const full = inputType === "textarea" ? " full" : "";
              const inputId = `field-${name}`;

              if (inputType === "textarea") {
                return (
                  <div key={name} className={`form-group${full}`}>
                    <label htmlFor={inputId}>{label}</label>
                    <textarea
                      id={inputId}
                      name={name}
                      defaultValue={String(value)}
                      required={isRequired}
                    />
                  </div>
                );
              }

              if (inputType === "select" && options) {
                return (
                  <div key={name} className="form-group">
                    <label htmlFor={inputId}>{label}</label>
                    <select
                      id={inputId}
                      name={name}
                      defaultValue={String(value)}
                      required={isRequired}
                    >
                      {options.map((opt) => (
                        <option key={opt} value={opt}>
                          {opt}
                        </option>
                      ))}
                    </select>
                  </div>
                );
              }

              if (inputType === "select-org") {
                return (
                  <div key={name} className="form-group">
                    <label htmlFor={inputId}>{label}</label>
                    <select
                      id={inputId}
                      name={name}
                      defaultValue={String(value)}
                      required={isRequired}
                    >
                      {partners.map((p) => (
                        <option key={p.id} value={p.id}>
                          {p.name}
                        </option>
                      ))}
                    </select>
                  </div>
                );
              }

              if (inputType === "select-org-empty") {
                return (
                  <div key={name} className="form-group">
                    <label htmlFor={inputId}>{label}</label>
                    <select
                      id={inputId}
                      name={name}
                      defaultValue={String(value)}
                      required={isRequired}
                    >
                      <option value="">No organisation</option>
                      {partners.map((p) => (
                        <option key={p.id} value={p.id}>
                          {p.name}
                        </option>
                      ))}
                    </select>
                  </div>
                );
              }

              return (
                <div key={name} className={`form-group${full}`}>
                  <label htmlFor={inputId}>{label}</label>
                  <input
                    id={inputId}
                    name={name}
                    type={inputType}
                    defaultValue={String(value)}
                    required={isRequired}
                  />
                </div>
              );
            })}
          </div>
          <div className="modal-actions">
            <button type="button" className="secondary-btn" onClick={onClose}>
              Cancel
            </button>
            <button type="submit" className="primary-btn">
              Save
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

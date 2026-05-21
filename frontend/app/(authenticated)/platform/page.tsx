"use client";

import { useEffect, useState, useCallback, Suspense } from "react";
import { useSearchParams } from "next/navigation";
import { useAuth } from "@/lib/auth-context";
import { request } from "@/lib/api";
import {
  canCreate,
  canEdit,
  canDelete,
  prettyRole,
  type Role,
  type EntityType,
} from "@/lib/permissions";
import DataTable, {
  StatusBadge,
  formatDate,
  type Column,
} from "@/components/DataTable";
import FilterBuilder from "@/components/FilterBuilder";
import RecordModal, { type FieldDef } from "@/components/RecordModal";
import "./platform.css";

/* eslint-disable @typescript-eslint/no-explicit-any */

const VIEW_COPY: Record<string, { title: string; context: string }> = {
  partners: {
    title: "Industry Partners",
    context:
      "Partners can have multiple approved projects and pending project applications.",
  },
  projects: {
    title: "Projects",
    context: "Approved project records linked to industry partners.",
  },
  applications: {
    title: "Project Applications",
    context:
      "Pending partner-submitted applications waiting for review or approval.",
  },
  events: {
    title: "Events",
    context:
      "Showcases, expos, meetings, and other industry engagement records.",
  },
  users: {
    title: "User Management",
    context: "Admin-only account, role, and access management.",
  },
};

const FIELD_SETS: Record<string, FieldDef[]> = {
  partner: [
    ["name", "Name", "text", true],
    ["industryId", "Industry", "select-industry", false],
    ["email", "Email", "email", false],
    ["website", "Website", "text", false],
    ["phone", "Phone", "text", false],
    ["addressLine1", "Address line 1", "text", false],
    ["addressLine2", "Address line 2", "text", false],
    ["city", "City", "text", false],
    ["state", "State", "text", false],
    ["postcode", "Postcode", "text", false],
    ["country", "Country", "text", false],
    [
      "partnershipStatus",
      "Partnership status",
      "select",
      true,
      ["prospect", "active", "completed", "inactive"],
    ],
    ["notes", "Notes", "textarea", false],
  ],
  project: [
    ["title", "Title", "text", true],
    ["description", "Description", "textarea", false],
    ["organisationId", "Organisation", "select-org", true],
    ["facultyId", "Faculty ID", "number", false],
    [
      "projectType",
      "Project type",
      "select",
      true,
      ["capstone", "undergrad", "postgrad"],
    ],
    ["semester", "Semester", "select", true, ["S1", "S2", "SS"]],
    ["year", "Year", "number", true],
    [
      "status",
      "Status",
      "select",
      true,
      ["proposed", "ongoing", "completed", "cancelled"],
    ],
  ],
  application: [
    ["proposedTitle", "Proposed title", "text", true],
    ["proposedDescription", "Description", "textarea", false],
    ["organisationId", "Organisation", "select-org", true],
    ["contactId", "Contact ID", "text", false],
    [
      "proposedProjectType",
      "Project type",
      "select",
      false,
      ["capstone", "undergrad", "postgrad"],
    ],
    ["proposedSemester", "Semester", "select", false, ["S1", "S2", "SS"]],
    ["proposedYear", "Year", "number", false],
    ["proposedFacultyId", "Faculty ID", "number", false],
  ],
  event: [
    ["name", "Name", "text", true],
    ["description", "Description", "textarea", false],
    [
      "eventType",
      "Event type",
      "select",
      false,
      ["showcase", "expo", "meeting", "scholar_program"],
    ],
    ["eventDate", "Date", "date", true],
    ["location", "Location", "text", false],
    ["facultyId", "Faculty ID", "number", false],
    ["notes", "Notes", "textarea", false],
  ],
  user: [
    ["email", "Email", "email", true],
    ["password", "Password", "password", false],
    ["fullName", "Full name", "text", true],
    [
      "role",
      "Role",
      "select",
      true,
      ["admin", "course_organiser", "industry_partner"],
    ],
    ["facultyId", "Faculty ID", "number", false],
    ["organisationId", "Organisation", "select-org-empty", false],
    ["isActive", "Active", "select", true, ["true", "false"]],
  ],
};

const API_PATHS: Record<string, string> = {
  partner: "/organisations",
  project: "/projects",
  application: "/projectapplications",
  event: "/events",
  user: "/users",
};

function filterRows(rows: any[], filters: Record<string, string>): any[] {
  return rows.filter((row) =>
    Object.entries(filters).every(([key, expected]) => {
      if (!expected) return true;
      return String(row[key] ?? "")
        .toLowerCase()
        .includes(expected.toLowerCase());
    }),
  );
}

function PlatformContent() {
  const searchParams = useSearchParams();
  const { user } = useAuth();
  const role = (user?.role || "industry_partner") as Role;

  const [partners, setPartners] = useState<any[]>([]);
  const [projects, setProjects] = useState<any[]>([]);
  const [applications, setApplications] = useState<any[]>([]);
  const [events, setEvents] = useState<any[]>([]);
  const [users, setUsers] = useState<any[]>([]);
  const [industries, setIndustries] = useState<any[]>([]);
  const [loadError, setLoadError] = useState<string | null>(null);

  // Filters
  const [applicationsSearch, setApplicationsSearch] = useState("");
  const [applicationsStatus, setApplicationsStatus] = useState("pending");
  const [applicationsSemester, setApplicationsSemester] = useState("");
  const [applicationsYear, setApplicationsYear] = useState("");
  const [usersSearch, setUsersSearch] = useState("");
  const [usersRole, setUsersRole] = useState("");
  const [usersActive, setUsersActive] = useState("");

  // Modal
  const [modalType, setModalType] = useState("");
  const [modalId, setModalId] = useState<string | null>(null);
  const [modalRecord, setModalRecord] = useState<Record<string, unknown>>({});

  const view = searchParams.get("view") || "all";
  const effectiveView =
    view === "users" && role !== "admin" ? "all" : VIEW_COPY[view] ? view : "all";

  const loadData = useCallback(async () => {
    try {
      const reqs: Promise<any>[] = [
        request("/organisations"),
        request("/projects"),
        request("/projectapplications"),
        request("/events"),
        request("/organisations/industries"),
      ];
      if (role === "admin") reqs.push(request("/users"));

      const results = await Promise.all(reqs);
      setPartners(results[0] || []);
      setProjects(results[1] || []);
      setApplications(results[2] || []);
      setEvents(results[3] || []);
      setIndustries(results[4] || []);
      setUsers(results[5] || []);
      setLoadError(null);
    } catch (err) {
      setLoadError(err instanceof Error ? err.message : "Failed to load data");
    }
  }, [role]);

  useEffect(() => {
    // Platform records are loaded from the API after auth state is known.
    // eslint-disable-next-line react-hooks/set-state-in-effect
    loadData();
  }, [loadData]);

  const handlePartnerResults = useCallback((data: { items: any[] }) => {
    setPartners(data.items);
  }, []);

  const handleProjectResults = useCallback((data: { items: any[] }) => {
    setProjects(data.items);
  }, []);

  const handleEventResults = useCallback((data: { items: any[] }) => {
    setEvents(data.items);
  }, []);

  // Filtered rows
  const filteredApplications = filterRows(applications, {
    proposedTitle: applicationsSearch,
    applicationStatus: applicationsStatus,
    proposedSemester: applicationsSemester,
    proposedYear: applicationsYear,
  });
  const filteredUsers =
    role === "admin"
      ? filterRows(users, {
          fullName: usersSearch,
          role: usersRole,
          isActive: usersActive,
        })
      : [];

  const copy = VIEW_COPY[effectiveView];
  const pageTitle = copy ? copy.title : "Partner and Projects View";
  const pageContext = copy
    ? copy.context
    : `${user?.fullName || "User"} (${prettyRole(role) || "signed in"}) can see records scoped by the API.`;

  function isVisible(section: string) {
    return effectiveView === "all" || effectiveView === section;
  }

  function openModal(type: string, record: any = {}) {
    setModalType(type);
    setModalId(record.id || null);
    setModalRecord(record);
  }

  function closeModal() {
    setModalType("");
    setModalId(null);
    setModalRecord({});
  }

  async function handleSave(data: Record<string, unknown>) {
    const path = API_PATHS[modalType];
    await request(`${path}${modalId ? `/${modalId}` : ""}`, {
      method: modalId ? "PUT" : "POST",
      body: JSON.stringify(data),
    });
    closeModal();
    await loadData();
  }

  async function handleDelete(type: EntityType, id: string) {
    if (!confirm("Move this record to deleted records?")) return;
    const path = API_PATHS[type];
    await request(`${path}/${id}`, { method: "DELETE" });
    await loadData();
  }

  function ActionButtons({ type, id }: { type: EntityType; id: string }) {
    const collections: Record<string, any[]> = {
      partner: partners,
      project: projects,
      application: applications,
      event: events,
      user: users,
    };
    return (
      <>
        {canEdit(role, type) && (
          <button
            className="action-btn edit-btn"
            onClick={() => {
              const record = collections[type]?.find((r: any) => r.id === id);
              if (record) openModal(type, record);
            }}
          >
            Edit
          </button>
        )}
        {canDelete(role, type) && (
          <button
            className="action-btn delete-btn"
            onClick={() => handleDelete(type, id)}
          >
            Delete
          </button>
        )}
      </>
    );
  }

  const partnerColumns: Column<any>[] = [
    { label: "Name", render: (r) => r.name },
    { label: "Industry", render: (r) => r.industry || "Unspecified" },
    { label: "Partnership", render: (r) => <StatusBadge value={r.partnershipStatus} /> },
    { label: "Submission", render: (r) => <StatusBadge value={r.submissionStatus} /> },
    { label: "Email", render: (r) => r.email || r.primaryContactEmail || "" },
    { label: "Projects", render: (r) => r.projectCount },
    { label: "Pending apps", render: (r) => r.pendingApplicationCount },
    { label: "Actions", render: (r) => <ActionButtons type="partner" id={r.id} /> },
  ];

  const projectColumns: Column<any>[] = [
    { label: "Title", render: (r) => r.title },
    { label: "Partner", render: (r) => r.organisationName },
    { label: "Type", render: (r) => r.projectType },
    { label: "Intake", render: (r) => `${r.semester} ${r.year}` },
    { label: "Status", render: (r) => <StatusBadge value={r.status} /> },
    { label: "Updated", render: (r) => formatDate(r.updatedAt) },
    { label: "Actions", render: (r) => <ActionButtons type="project" id={r.id} /> },
  ];

  const applicationColumns: Column<any>[] = [
    { label: "Proposed title", render: (r) => r.proposedTitle },
    { label: "Partner", render: (r) => r.organisationName },
    { label: "Type", render: (r) => r.proposedProjectType || "" },
    {
      label: "Intake",
      render: (r) =>
        [r.proposedSemester, r.proposedYear].filter(Boolean).join(" "),
    },
    { label: "Status", render: (r) => <StatusBadge value={r.applicationStatus} /> },
    { label: "Submitted", render: (r) => formatDate(r.submittedAt) },
    {
      label: "Actions",
      render: (r) => <ActionButtons type="application" id={r.id} />,
    },
  ];

  const eventColumns: Column<any>[] = [
    { label: "Name", render: (r) => r.name },
    { label: "Type", render: (r) => r.eventType || "" },
    { label: "Date", render: (r) => formatDate(r.eventDate) },
    { label: "Location", render: (r) => r.location || "" },
    { label: "Faculty", render: (r) => r.faculty || "All faculties" },
    { label: "Attendees", render: (r) => r.attendanceCount },
    { label: "Actions", render: (r) => <ActionButtons type="event" id={r.id} /> },
  ];

  const userColumns: Column<any>[] = [
    { label: "Name", render: (r) => r.fullName },
    { label: "Email", render: (r) => r.email },
    { label: "Role", render: (r) => <StatusBadge value={r.role} /> },
    { label: "Faculty", render: (r) => r.faculty || "" },
    { label: "Organisation", render: (r) => r.organisationName || "" },
    {
      label: "Active",
      render: (r) => <StatusBadge value={r.isActive ? "active" : "inactive"} />,
    },
    { label: "Last login", render: (r) => formatDate(r.lastLoginAt) },
    { label: "Actions", render: (r) => <ActionButtons type="user" id={r.id} /> },
  ];

  return (
    <div className="platform-page">
      <div className="container">
        <div className="page-title">
          <h1>{pageTitle}</h1>
          <p>{pageContext}</p>
        </div>

        <div className="summary-grid">
          {isVisible("partners") && (
            <div className="summary-card">
              <div className="summary-label">Partners</div>
              <div className="summary-value">{partners.length}</div>
            </div>
          )}
          {isVisible("projects") && (
            <div className="summary-card">
              <div className="summary-label">Projects</div>
              <div className="summary-value">{projects.length}</div>
            </div>
          )}
          {isVisible("applications") && (
            <div className="summary-card">
              <div className="summary-label">Applications</div>
              <div className="summary-value">
                {filteredApplications.length}
              </div>
            </div>
          )}
          {isVisible("events") && (
            <div className="summary-card">
              <div className="summary-label">Events</div>
              <div className="summary-value">{events.length}</div>
            </div>
          )}
          {isVisible("users") && role === "admin" && (
            <div className="summary-card">
              <div className="summary-label">Users</div>
              <div className="summary-value">{filteredUsers.length}</div>
            </div>
          )}
        </div>

        {/* Partners Section */}
        {isVisible("partners") && (
          <section className="section">
            <div className="section-header">
              <h2>Industry Partners</h2>
              <span>Organisations visible to your role</span>
            </div>
            <div className="section-tools">
              {canCreate(role, "partner") && (
                <button
                  className="primary-btn"
                  onClick={() => openModal("partner")}
                >
                  Add Partner
                </button>
              )}
            </div>
            <FilterBuilder
              entity="organisations"
              sortBy="name"
              onResults={handlePartnerResults}
            />
            {loadError ? (
              <div className="error">Could not load data ({loadError}).</div>
            ) : (
              <DataTable columns={partnerColumns} rows={partners} />
            )}
          </section>
        )}

        {/* Projects Section */}
        {isVisible("projects") && (
          <section className="section">
            <div className="section-header">
              <h2>Projects</h2>
              <span>Approved and active project records</span>
            </div>
            <div className="section-tools">
              {canCreate(role, "project") && (
                <button
                  className="primary-btn"
                  onClick={() => openModal("project")}
                >
                  Add Project
                </button>
              )}
            </div>
            <FilterBuilder
              entity="projects"
              sortBy="title"
              onResults={handleProjectResults}
            />
            {loadError ? (
              <div className="error">Could not load data ({loadError}).</div>
            ) : (
              <DataTable columns={projectColumns} rows={projects} />
            )}
          </section>
        )}

        {/* Applications Section */}
        {isVisible("applications") && (
          <section className="section">
            <div className="section-header">
              <h2>Project Applications</h2>
              <span>Partner-facing project submissions</span>
            </div>
            <div className="section-tools">
              <input
                type="search"
                placeholder="Search applications..."
                value={applicationsSearch}
                onChange={(e) => setApplicationsSearch(e.target.value)}
              />
              <select
                value={applicationsStatus}
                onChange={(e) => setApplicationsStatus(e.target.value)}
              >
                <option value="pending">Pending</option>
                <option value="rejected">Rejected</option>
                <option value="archived">Archived</option>
              </select>
              <select
                value={applicationsSemester}
                onChange={(e) => setApplicationsSemester(e.target.value)}
              >
                <option value="">All semesters</option>
                <option value="S1">S1</option>
                <option value="S2">S2</option>
                <option value="SS">SS</option>
              </select>
              <input
                type="number"
                min={2019}
                max={2100}
                placeholder="Year"
                value={applicationsYear}
                onChange={(e) => setApplicationsYear(e.target.value)}
              />
              {canCreate(role, "application") && (
                <button
                  className="primary-btn"
                  onClick={() => openModal("application")}
                >
                  Add Application
                </button>
              )}
            </div>
            {loadError ? (
              <div className="error">Could not load data ({loadError}).</div>
            ) : (
              <DataTable
                columns={applicationColumns}
                rows={filteredApplications}
              />
            )}
          </section>
        )}

        {/* Events Section */}
        {isVisible("events") && (
          <section className="section">
            <div className="section-header">
              <h2>Events</h2>
              <span>
                Showcases, expos, meetings, and partner engagement activity
              </span>
            </div>
            <div className="section-tools">
              {canCreate(role, "event") && (
                <button
                  className="primary-btn"
                  onClick={() => openModal("event")}
                >
                  Add Event
                </button>
              )}
            </div>
            <FilterBuilder
              entity="events"
              sortBy="event_date"
              onResults={handleEventResults}
            />
            {loadError ? (
              <div className="error">Could not load data ({loadError}).</div>
            ) : (
              <DataTable columns={eventColumns} rows={events} />
            )}
          </section>
        )}

        {/* Users Section (admin only) */}
        {isVisible("users") && role === "admin" && (
          <section className="section">
            <div className="section-header">
              <h2>User Management</h2>
              <span>Admin-only account and role management</span>
            </div>
            <div className="section-tools">
              <input
                type="search"
                placeholder="Search users..."
                value={usersSearch}
                onChange={(e) => setUsersSearch(e.target.value)}
              />
              <select
                value={usersRole}
                onChange={(e) => setUsersRole(e.target.value)}
              >
                <option value="">All roles</option>
                <option value="admin">Admin</option>
                <option value="course_organiser">Course organiser</option>
                <option value="industry_partner">Industry partner</option>
              </select>
              <select
                value={usersActive}
                onChange={(e) => setUsersActive(e.target.value)}
              >
                <option value="">All account states</option>
                <option value="true">Active</option>
                <option value="false">Inactive</option>
              </select>
              {canCreate(role, "user") && (
                <button
                  className="primary-btn"
                  onClick={() => openModal("user")}
                >
                  Add User
                </button>
              )}
            </div>
            {loadError ? (
              <div className="error">Could not load data ({loadError}).</div>
            ) : (
              <DataTable columns={userColumns} rows={filteredUsers} />
            )}
          </section>
        )}
      </div>

      {modalType && FIELD_SETS[modalType] && (
        <RecordModal
          open={!!modalType}
          title={`${modalId ? "Edit" : "Add"} ${modalType}`}
          fields={FIELD_SETS[modalType]}
          record={modalRecord}
          partners={partners}
          industries={industries}
          isNew={!modalId}
          onClose={closeModal}
          onSubmit={handleSave}
        />
      )}
    </div>
  );
}

export default function PlatformPage() {
  return (
    <Suspense>
      <PlatformContent />
    </Suspense>
  );
}

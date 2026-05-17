-- =============================================================================
-- QUT Professional Services CRM — Database Schema
-- Target: PostgreSQL 15+
-- Conventions:
--   * UUID primary keys (gen_random_uuid via pgcrypto)
--   * TIMESTAMPTZ for all time fields
--   * Soft deletes via `deleted_at` (NULL = live record)
--   * Audit columns: created_at, updated_at, created_by, updated_by
--   * Filtered indexes (WHERE deleted_at IS NULL) for hot paths
-- =============================================================================

CREATE EXTENSION IF NOT EXISTS "pgcrypto";   -- gen_random_uuid()
CREATE EXTENSION IF NOT EXISTS "pg_trgm";    -- trigram search on names/titles


-- =============================================================================
-- 1. LOOKUPS / RBAC
-- =============================================================================

CREATE TABLE roles (
    id              SERIAL PRIMARY KEY,
    name            VARCHAR(50) UNIQUE NOT NULL,
    description     TEXT,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

INSERT INTO roles (name, description) VALUES
    ('admin',            'Full access. Approves submissions, manages users.'),
    ('course_organiser', 'PS staff. Manages partners, projects, events.'),
    ('industry_partner', 'External user. Registers org and submits project EOIs.');

CREATE TABLE permissions (
    id              SERIAL PRIMARY KEY,
    resource        VARCHAR(50)  NOT NULL,   -- e.g. 'organisations', 'projects'
    action          VARCHAR(50)  NOT NULL,   -- e.g. 'create','read','update','delete','approve'
    description     TEXT,
    UNIQUE (resource, action)
);

CREATE TABLE role_permissions (
    role_id         INT NOT NULL REFERENCES roles(id)       ON DELETE CASCADE,
    permission_id   INT NOT NULL REFERENCES permissions(id) ON DELETE CASCADE,
    PRIMARY KEY (role_id, permission_id)
);

CREATE TABLE faculties (
    id              SERIAL PRIMARY KEY,
    name            VARCHAR(255) UNIQUE NOT NULL,
    code            VARCHAR(50)  UNIQUE,
    created_at      TIMESTAMPTZ  NOT NULL DEFAULT NOW()
);

INSERT INTO faculties (name, code) VALUES
    ('Faculty of Science',                    'SCI'),
    ('Faculty of Engineering',                'ENG'),
    ('Faculty of Business and Law',           'BUS'),
    ('Faculty of Health',                     'HLTH'),
    ('Faculty of Creative Industries',        'CI'),
    ('Faculty of Information Technology',     'IT');

CREATE TABLE industries (
    id              SERIAL PRIMARY KEY,
    name            VARCHAR(100) UNIQUE NOT NULL,
    description     TEXT,
    created_at      TIMESTAMPTZ  NOT NULL DEFAULT NOW()
);

INSERT INTO industries (name) VALUES
    ('IT / Software'),
    ('Finance / Banking'),
    ('Health / Medical'),
    ('Government'),
    ('Education'),
    ('Retail / SME'),
    ('Manufacturing'),
    ('Energy / Utilities'),
    ('Telecommunications'),
    ('Other');


-- =============================================================================
-- 2. USERS
--   Note: organisation_id FK is added AFTER organisations is created (circular).
-- =============================================================================

CREATE TABLE users (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    email               VARCHAR(255) UNIQUE NOT NULL,
    password_hash       VARCHAR(255) NOT NULL,           -- bcrypt/argon2 hash
    full_name           VARCHAR(255) NOT NULL,
    role_id             INT  NOT NULL REFERENCES roles(id),
    faculty_id          INT  REFERENCES faculties(id),   -- nullable; for course_organisers
    organisation_id     UUID,                            -- for industry_partner users; FK below
    is_active           BOOLEAN NOT NULL DEFAULT TRUE,
    email_verified_at   TIMESTAMPTZ,
    last_login_at       TIMESTAMPTZ,
    created_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by          UUID REFERENCES users(id),
    updated_by          UUID REFERENCES users(id),
    deleted_at          TIMESTAMPTZ
);

CREATE INDEX idx_users_role          ON users (role_id)    WHERE deleted_at IS NULL;
CREATE INDEX idx_users_faculty       ON users (faculty_id) WHERE deleted_at IS NULL;
CREATE INDEX idx_users_organisation  ON users (organisation_id) WHERE deleted_at IS NULL;


-- =============================================================================
-- 3. ORGANISATIONS (Industry Partners)
-- =============================================================================

CREATE TABLE organisations (
    id                    UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name                  VARCHAR(255) NOT NULL,
    legal_name            VARCHAR(255),
    industry_id           INT REFERENCES industries(id),
    website               VARCHAR(255),
    domain                VARCHAR(255),                -- e.g. example.com (parsed from email/website)
    phone                 VARCHAR(50),
    email                 VARCHAR(255),
    address_line1         VARCHAR(255),
    address_line2         VARCHAR(255),
    city                  VARCHAR(100),
    state                 VARCHAR(100),
    postcode              VARCHAR(20),
    country               VARCHAR(100) DEFAULT 'Australia',
    notes                 TEXT,

    -- CRM lifecycle (CO-6)
    partnership_status    VARCHAR(20) NOT NULL DEFAULT 'prospect'
                          CHECK (partnership_status IN ('prospect','active','completed','inactive')),

    -- Submission/approval workflow (IP-1, AD-2)
    submission_status     VARCHAR(20) NOT NULL DEFAULT 'approved'
                          CHECK (submission_status IN ('pending','approved','rejected','archived')),
    submitted_by          UUID REFERENCES users(id),
    reviewed_by           UUID REFERENCES users(id),
    reviewed_at           TIMESTAMPTZ,
    review_notes          TEXT,

    created_at            TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at            TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by            UUID REFERENCES users(id),
    updated_by            UUID REFERENCES users(id),
    deleted_at            TIMESTAMPTZ
);

CREATE INDEX idx_org_name                ON organisations (name)               WHERE deleted_at IS NULL;
CREATE INDEX idx_org_industry            ON organisations (industry_id)        WHERE deleted_at IS NULL;
CREATE INDEX idx_org_partnership_status  ON organisations (partnership_status) WHERE deleted_at IS NULL;
CREATE INDEX idx_org_submission_status   ON organisations (submission_status)  WHERE deleted_at IS NULL;
CREATE INDEX idx_org_name_trgm           ON organisations USING gin (name gin_trgm_ops);

-- Close the circular FK from users -> organisations
ALTER TABLE users
    ADD CONSTRAINT fk_users_organisation
    FOREIGN KEY (organisation_id) REFERENCES organisations(id);


-- =============================================================================
-- 4. CONTACTS  (multiple per organisation)
-- =============================================================================

CREATE TABLE contacts (
    id                UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    organisation_id   UUID NOT NULL REFERENCES organisations(id),
    first_name        VARCHAR(100) NOT NULL,
    last_name         VARCHAR(100) NOT NULL,
    job_title         VARCHAR(255),
    email             VARCHAR(255),
    phone             VARCHAR(50),
    is_primary        BOOLEAN NOT NULL DEFAULT FALSE,
    notes             TEXT,
    created_at        TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at        TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by        UUID REFERENCES users(id),
    updated_by        UUID REFERENCES users(id),
    deleted_at        TIMESTAMPTZ
);

CREATE INDEX idx_contacts_org    ON contacts (organisation_id) WHERE deleted_at IS NULL;
CREATE INDEX idx_contacts_email  ON contacts (email)           WHERE deleted_at IS NULL;
-- Enforce: at most one primary contact per organisation (among live rows).
CREATE UNIQUE INDEX idx_contacts_one_primary
    ON contacts (organisation_id)
    WHERE is_primary = TRUE AND deleted_at IS NULL;


-- =============================================================================
-- 5. PROJECTS  (one organisation per project — see schema_design.md re: CO-5)
-- =============================================================================

CREATE TABLE projects (
    id                UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    title             VARCHAR(500) NOT NULL,
    description       TEXT,
    organisation_id   UUID NOT NULL REFERENCES organisations(id),
    faculty_id        INT  REFERENCES faculties(id),

    project_type      VARCHAR(20) NOT NULL
                      CHECK (project_type IN ('capstone','undergrad','postgrad')),

    semester          VARCHAR(10) NOT NULL CHECK (semester IN ('S1','S2','SS')), -- SS = summer
    year              INT NOT NULL CHECK (year BETWEEN 2019 AND 2100),

    status            VARCHAR(20) NOT NULL DEFAULT 'ongoing'
                      CHECK (status IN ('proposed','ongoing','completed','cancelled')),

    start_date        DATE,
    end_date          DATE,
    notes             TEXT,

    created_at        TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at        TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by        UUID REFERENCES users(id),
    updated_by        UUID REFERENCES users(id),
    deleted_at        TIMESTAMPTZ
);

CREATE INDEX idx_projects_org        ON projects (organisation_id)  WHERE deleted_at IS NULL;
CREATE INDEX idx_projects_faculty    ON projects (faculty_id)       WHERE deleted_at IS NULL;
CREATE INDEX idx_projects_intake     ON projects (year, semester)   WHERE deleted_at IS NULL;
CREATE INDEX idx_projects_type       ON projects (project_type)     WHERE deleted_at IS NULL;
CREATE INDEX idx_projects_status     ON projects (status)           WHERE deleted_at IS NULL;
CREATE INDEX idx_projects_title_trgm ON projects USING gin (title gin_trgm_ops);


-- =============================================================================
-- 6. PROJECT APPLICATIONS  (Industry partner EOIs — IP-2, AD-2)
-- =============================================================================

CREATE TABLE project_applications (
    id                          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    organisation_id             UUID NOT NULL REFERENCES organisations(id),
    contact_id                  UUID REFERENCES contacts(id),

    proposed_title              VARCHAR(500) NOT NULL,
    proposed_description        TEXT,
    proposed_project_type       VARCHAR(20)
                                CHECK (proposed_project_type IN ('capstone','undergrad','postgrad')),
    proposed_semester           VARCHAR(10) CHECK (proposed_semester IN ('S1','S2','SS')),
    proposed_year               INT,
    proposed_faculty_id         INT REFERENCES faculties(id),

    application_status          VARCHAR(20) NOT NULL DEFAULT 'pending'
                                CHECK (application_status IN ('pending','approved','rejected','archived')),

    submitted_by                UUID REFERENCES users(id),
    submitted_at                TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    reviewed_by                 UUID REFERENCES users(id),
    reviewed_at                 TIMESTAMPTZ,
    review_notes                TEXT,

    -- Set when an application is approved and converted to a project.
    resulting_project_id        UUID REFERENCES projects(id),

    created_at                  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at                  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by                  UUID REFERENCES users(id),
    updated_by                  UUID REFERENCES users(id),
    deleted_at                  TIMESTAMPTZ
);

CREATE INDEX idx_apps_org      ON project_applications (organisation_id)              WHERE deleted_at IS NULL;
CREATE INDEX idx_apps_status   ON project_applications (application_status)           WHERE deleted_at IS NULL;
CREATE INDEX idx_apps_intake   ON project_applications (proposed_year, proposed_semester) WHERE deleted_at IS NULL;


-- =============================================================================
-- 7. EVENTS + ATTENDANCE  (organisation-level; contact-level deferred)
-- =============================================================================

CREATE TABLE events (
    id                UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name              VARCHAR(255) NOT NULL,
    description       TEXT,
    event_type        VARCHAR(50),                 -- 'showcase' | 'expo' | 'meeting' | 'scholar_program' | ...
    event_date        DATE NOT NULL,
    location          VARCHAR(255),
    faculty_id        INT REFERENCES faculties(id),
    notes             TEXT,
    created_at        TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at        TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by        UUID REFERENCES users(id),
    updated_by        UUID REFERENCES users(id),
    deleted_at        TIMESTAMPTZ
);

CREATE INDEX idx_events_date     ON events (event_date)  WHERE deleted_at IS NULL;
CREATE INDEX idx_events_faculty  ON events (faculty_id)  WHERE deleted_at IS NULL;
CREATE INDEX idx_events_type     ON events (event_type)  WHERE deleted_at IS NULL;

CREATE TABLE event_attendances (
    id                UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    event_id          UUID NOT NULL REFERENCES events(id),
    organisation_id   UUID NOT NULL REFERENCES organisations(id),
    notes             TEXT,
    created_at        TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at        TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by        UUID REFERENCES users(id),
    updated_by        UUID REFERENCES users(id),
    deleted_at        TIMESTAMPTZ
);

CREATE UNIQUE INDEX idx_attendance_unique
    ON event_attendances (event_id, organisation_id)
    WHERE deleted_at IS NULL;


-- =============================================================================
-- 8. AUDIT LOG
-- =============================================================================

CREATE TABLE audit_log (
    id            BIGSERIAL PRIMARY KEY,
    table_name    VARCHAR(100) NOT NULL,
    record_id     UUID NOT NULL,
    action        VARCHAR(20)  NOT NULL CHECK (action IN ('INSERT','UPDATE','DELETE','SOFT_DELETE','RESTORE')),
    changed_by    UUID REFERENCES users(id),
    changed_at    TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    old_values    JSONB,
    new_values    JSONB,
    ip_address    INET,
    user_agent    TEXT
);

CREATE INDEX idx_audit_record   ON audit_log (table_name, record_id);
CREATE INDEX idx_audit_changed  ON audit_log (changed_at);
CREATE INDEX idx_audit_user     ON audit_log (changed_by);


-- =============================================================================
-- 9. TRIGGERS — auto-update updated_at on every row update
-- =============================================================================

CREATE OR REPLACE FUNCTION set_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DO $$
DECLARE t TEXT;
BEGIN
    FOR t IN
        SELECT c.table_name
        FROM information_schema.columns c
        WHERE c.column_name = 'updated_at'
          AND c.table_schema = current_schema()
    LOOP
        EXECUTE format(
          'CREATE TRIGGER trg_%I_updated_at BEFORE UPDATE ON %I
             FOR EACH ROW EXECUTE FUNCTION set_updated_at()', t, t);
    END LOOP;
END $$;


-- =============================================================================
-- 10. ANALYTICS VIEWS
-- =============================================================================

-- Per-organisation rollup: total projects, first/last engagement, events attended.
CREATE OR REPLACE VIEW v_organisation_stats AS
SELECT
    o.id,
    o.name,
    o.industry_id,
    i.name                                              AS industry_name,
    o.partnership_status,
    COUNT(DISTINCT p.id)                                AS total_projects,
    MIN(make_date(p.year,
        CASE p.semester WHEN 'S1' THEN 2 WHEN 'S2' THEN 7 ELSE 11 END, 1)) AS first_project_date,
    MAX(make_date(p.year,
        CASE p.semester WHEN 'S1' THEN 2 WHEN 'S2' THEN 7 ELSE 11 END, 1)) AS last_project_date,
    COUNT(DISTINCT ea.event_id)                         AS events_attended
FROM organisations o
LEFT JOIN industries        i  ON i.id = o.industry_id
LEFT JOIN projects          p  ON p.organisation_id = o.id  AND p.deleted_at IS NULL
LEFT JOIN event_attendances ea ON ea.organisation_id = o.id AND ea.deleted_at IS NULL
WHERE o.deleted_at IS NULL
GROUP BY o.id, o.name, o.industry_id, i.name, o.partnership_status;

-- Per-intake rollup: total projects, unique partners, new vs returning.
-- Intake order: S1 < SS < S2 within a year, encoded as a sortable integer.
CREATE OR REPLACE VIEW v_intake_summary AS
WITH semester_order AS (
    SELECT unnest(ARRAY['S1','SS','S2']) AS semester,
           generate_series(1, 3)         AS sem_ord
),
partner_first AS (
    SELECT p.organisation_id,
           MIN(p.year * 10 + s.sem_ord) AS first_intake_key
    FROM projects p
    JOIN semester_order s ON s.semester = p.semester
    WHERE p.deleted_at IS NULL
    GROUP BY p.organisation_id
),
projects_keyed AS (
    SELECT p.*, p.year * 10 + s.sem_ord AS intake_key
    FROM projects p
    JOIN semester_order s ON s.semester = p.semester
    WHERE p.deleted_at IS NULL
)
SELECT
    pk.year,
    pk.semester,
    COUNT(*)                                                        AS total_projects,
    COUNT(DISTINCT pk.organisation_id)                              AS unique_partners,
    COUNT(DISTINCT pk.organisation_id) FILTER (
        WHERE pf.first_intake_key = pk.intake_key
    )                                                               AS new_partners,
    COUNT(DISTINCT pk.organisation_id) FILTER (
        WHERE pf.first_intake_key <> pk.intake_key
    )                                                               AS returning_partners
FROM projects_keyed pk
LEFT JOIN partner_first pf ON pf.organisation_id = pk.organisation_id
GROUP BY pk.year, pk.semester
ORDER BY pk.year DESC;

-- Most active partners over the last 3 years.
CREATE OR REPLACE VIEW v_most_active_partners_3yr AS
SELECT
    o.id,
    o.name,
    i.name             AS industry_name,
    COUNT(p.id)        AS project_count
FROM organisations o
JOIN projects p   ON p.organisation_id = o.id AND p.deleted_at IS NULL
LEFT JOIN industries i ON i.id = o.industry_id
WHERE o.deleted_at IS NULL
  AND p.year >= EXTRACT(YEAR FROM CURRENT_DATE)::INT - 3
GROUP BY o.id, o.name, i.name
ORDER BY project_count DESC;

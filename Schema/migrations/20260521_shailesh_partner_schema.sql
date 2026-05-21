-- Adds Shailesh partner/project fields to an existing development database.
-- schema.sql already contains these fields for fresh database creation.

ALTER TABLE organisations
    ADD COLUMN registration_id VARCHAR(100) NULL AFTER id,
    ADD COLUMN abn VARCHAR(20) NULL AFTER legal_name,
    ADD COLUMN organisation_information TEXT NULL AFTER country;

CREATE INDEX idx_org_registration_id ON organisations (registration_id);
CREATE INDEX idx_org_abn ON organisations (abn);

ALTER TABLE projects
    ADD COLUMN multiple_teams BOOLEAN NOT NULL DEFAULT FALSE AFTER end_date,
    ADD COLUMN discipline_area VARCHAR(255) NULL AFTER multiple_teams,
    ADD COLUMN secondary_it_discipline VARCHAR(255) NULL AFTER discipline_area,
    ADD COLUMN project_deliverables TEXT NULL AFTER secondary_it_discipline,
    ADD COLUMN project_partner_agreement TEXT NULL AFTER project_deliverables,
    ADD COLUMN student_project_agreement TEXT NULL AFTER project_partner_agreement,
    ADD COLUMN ip_assignment_rationale TEXT NULL AFTER student_project_agreement;

ALTER TABLE project_applications
    ADD COLUMN proposed_multiple_teams BOOLEAN NOT NULL DEFAULT FALSE AFTER proposed_faculty_id,
    ADD COLUMN proposed_discipline_area VARCHAR(255) NULL AFTER proposed_multiple_teams,
    ADD COLUMN proposed_secondary_it_discipline VARCHAR(255) NULL AFTER proposed_discipline_area,
    ADD COLUMN proposed_deliverables TEXT NULL AFTER proposed_secondary_it_discipline,
    ADD COLUMN student_project_agreement TEXT NULL AFTER proposed_deliverables,
    ADD COLUMN ip_assignment_rationale TEXT NULL AFTER student_project_agreement;

ALTER TABLE event_attendances
    ADD COLUMN first_name VARCHAR(100) NULL AFTER organisation_id,
    ADD COLUMN best_contact_name VARCHAR(255) NULL AFTER first_name,
    ADD COLUMN position_title VARCHAR(255) NULL AFTER best_contact_name,
    ADD COLUMN email VARCHAR(255) NULL AFTER position_title,
    ADD COLUMN list_name VARCHAR(255) NULL AFTER email,
    ADD COLUMN source VARCHAR(255) NULL AFTER list_name,
    ADD COLUMN events_invited_to TEXT NULL AFTER source,
    ADD COLUMN response VARCHAR(255) NULL AFTER events_invited_to;

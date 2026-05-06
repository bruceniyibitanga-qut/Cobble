# QUT PS CRM — How to view the schema and the design doc

You have three files in this folder:

- `schema.sql` — the PostgreSQL DDL
- `schema_design.md` — the design document (with a Mermaid ER diagram)
- `docker-compose.yml` — runs Postgres + pgAdmin locally with the schema auto-loaded

---

## A. Run the database (schema.sql)

### Prerequisites
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) installed and running

### Start it up
From this folder:

```bash
docker compose up -d
```

That spins up two containers:
- **Postgres 16** on `localhost:5432` (db: `qut_crm`, user: `qut_crm`, password: `changeme_local_only`)
- **pgAdmin 4** on http://localhost:5050 (login: `admin@local.dev` / `changeme_local_only`)

The schema runs automatically the **first time** the database initialises. If you change `schema.sql` later, wipe and restart:

```bash
docker compose down -v   # -v removes the volume, forcing a re-init
docker compose up -d
```

### Verify the schema loaded

```bash
docker exec -it qut-crm-db psql -U qut_crm -d qut_crm -c "\dt"
```

You should see all 11 tables: `roles`, `permissions`, `role_permissions`, `faculties`, `industries`, `users`, `organisations`, `contacts`, `projects`, `project_applications`, `events`, `event_attendances`, `audit_log`. Run `\dv` to list the analytics views.

### Connect from pgAdmin (visual exploration + live ER diagram)

1. Open http://localhost:5050 and log in.
2. Right-click **Servers → Register → Server**.
3. **General tab** → Name: `QUT CRM`.
4. **Connection tab**:
   - Host: `postgres`  *(the service name, not `localhost` — they share a Docker network)*
   - Port: `5432`
   - Database: `qut_crm`
   - Username: `qut_crm`
   - Password: `changeme_local_only`
5. Save. You can now browse tables, run queries, and — importantly — **right-click the database → ERD Tool → "ERD for Database"** to get a live ER diagram drawn from the actual schema.

### Connect from a SQL client on your host

Any client (DBeaver, TablePlus, DataGrip, `psql`) can connect with:

```
host=localhost port=5432 dbname=qut_crm user=qut_crm password=changeme_local_only
```

### Stop everything

```bash
docker compose down       # keeps data
docker compose down -v    # also wipes the database volume
```

---

## B. View the design doc (schema_design.md)

The doc contains a Mermaid ER diagram that needs a renderer. Pick whichever is easiest:

| Tool | What to do |
|---|---|
| **GitHub** | Push the file to a repo — GitHub renders Mermaid natively. |
| **VS Code** | Open the file, hit `Cmd+Shift+V` for the markdown preview. Mermaid renders out of the box in recent versions; if not, install the *Markdown Preview Mermaid Support* extension. |
| **Obsidian / Typora / Notion** | All render Mermaid natively — paste or import the file. |
| **Online quick view** | Paste the contents into https://markdown.dev/ or https://stackedit.io/ for a one-shot render. |

---

## C. The "complete view" workflow

Together, the cleanest experience is:

1. `docker compose up -d` — schema is live in Postgres.
2. Open pgAdmin → ERD Tool — see the **actual** ER diagram drawn from the running database.
3. Open `schema_design.md` in VS Code preview — read the rationale and user-story mapping alongside the diagram.

That gives you the schema (live, queryable) and the design context (rendered) side by side.

---

## Notes

- `changeme_local_only` is fine for dev, **never** for any deployed environment. Real deployment will use secrets (env vars from your secret manager, or a `.env` file outside source control).
- The `docker-entrypoint-initdb.d` mechanism only runs on a fresh data volume. To re-apply schema changes during development, either `docker compose down -v` (wipes everything) or re-run the SQL manually:
  ```bash
  docker exec -i qut-crm-db psql -U qut_crm -d qut_crm < schema.sql
  ```
  (Note: most statements in `schema.sql` aren't idempotent — re-running will error on existing objects. For iterative dev, prefer the `down -v` cycle or migrate to a tool like Flyway / Alembic / Prisma Migrate.)

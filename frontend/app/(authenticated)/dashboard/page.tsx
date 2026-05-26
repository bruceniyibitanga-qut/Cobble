"use client";

import Link from "next/link";
import {
  Building2,
  ClipboardList,
  Calendar,
  FileText,
  BarChart3,
  Users,
  ShieldAlert,
  type LucideIcon,
} from "lucide-react";
import { useAuth } from "@/lib/auth-context";
import {
  PERM_LABEL,
  getPermissions,
  type Role,
  type PermLevel,
} from "@/lib/permissions";
import "./dashboard.css";

const WELCOME_MESSAGES: Record<string, string> = {
  admin:
    "You have full access to all features, including user and organisation management.",
  course_organiser:
    "You can read and edit Industry Partners, Projects, and Events. User management is restricted to administrators.",
  industry_partner:
    "You have read-only access to projects and events relevant to your organisation.",
};

interface TabCard {
  key: string;
  Icon: LucideIcon;
  title: string;
  desc: string;
  href: string;
}

const TAB_CARDS: TabCard[] = [
  {
    key: "partners",
    Icon: Building2,
    title: "Industry Partners",
    desc: "View and manage QUT's industry partner organisations.",
    href: "/platform?view=partners",
  },
  {
    key: "projects",
    Icon: ClipboardList,
    title: "Projects",
    desc: "Browse capstone, undergrad, and postgrad project history.",
    href: "/platform?view=projects",
  },
  {
    key: "events",
    Icon: Calendar,
    title: "Events",
    desc: "Track showcases, expos, and industry meetings.",
    href: "/platform?view=events",
  },
  {
    key: "applications",
    Icon: FileText,
    title: "Project Applications",
    desc: "Review partner-facing project proposals and submission status.",
    href: "/platform?view=applications",
  },
  {
    key: "analytics",
    Icon: BarChart3,
    title: "Analytics",
    desc: "Reports on partner activity, intake trends, and engagement.",
    href: "#",
  },
  {
    key: "users",
    Icon: Users,
    title: "User Management",
    desc: "Add staff accounts, assign roles, manage permissions.",
    href: "/platform?view=users",
  },
];

export default function DashboardPage() {
  const { user } = useAuth();

  if (!user) return null;

  const role = user.role as Role;
  const perms = getPermissions(role);
  const firstName = user.fullName.split(" ")[0];
  const welcomeMsg =
    WELCOME_MESSAGES[role] || WELCOME_MESSAGES.industry_partner;

  return (
    <div className="dashboard-page">
      <div className="dashboard-container">
        <div className="welcome">
          <h2>Welcome back, {firstName}</h2>
          <p>{welcomeMsg}</p>
        </div>

        {role === "admin" && (
          <div className="admin-only">
            <div className="admin-only-header">
              <ShieldAlert size={18} />
              <h3>Administrator tools</h3>
            </div>
            <p>
              You have full access. From here you can create new organisations,
              manage users, and configure system-wide settings.
            </p>
          </div>
        )}

        <div className="section-title">Application tabs</div>

        <div className="grid">
          {TAB_CARDS.map((tab) => {
            const level = (perms[tab.key] || "no") as PermLevel;
            const label = PERM_LABEL[level];
            const disabled = level === "no";
            const { Icon } = tab;

            const cardContent = (
              <>
                <div className="tab-icon">
                  <Icon size={20} strokeWidth={1.8} />
                </div>
                <div className="tab-title">{tab.title}</div>
                <div className="tab-desc">{tab.desc}</div>
                <div className={`tab-permission ${label.cls}`}>
                  {label.text}
                </div>
              </>
            );

            if (disabled) {
              return (
                <div key={tab.key} className="tab-card disabled">
                  {cardContent}
                </div>
              );
            }

            return (
              <Link key={tab.key} href={tab.href} className="tab-card">
                {cardContent}
              </Link>
            );
          })}
        </div>

        <div className="info-row">
          <strong>Authenticated session.</strong> You are signed in with a
          secure httpOnly cookie. Every request your tabs make to the API is
          proxied through the server with your credentials attached
          automatically.
        </div>
      </div>
    </div>
  );
}

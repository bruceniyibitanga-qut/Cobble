"use client";

import Link from "next/link";
import Image from "next/image";
import { usePathname, useSearchParams } from "next/navigation";
import { useAuth } from "@/lib/auth-context";
import { prettyRole } from "@/lib/permissions";
import "./header.css";

interface HeaderProps {
  subtitle?: string;
}

export default function Header({ subtitle }: HeaderProps) {
  const { user, logout } = useAuth();
  const pathname = usePathname();
  const searchParams = useSearchParams();

  if (!user) return null;

  const currentView = searchParams.get("view") || "";
  const isOnDashboard = pathname === "/dashboard";
  const isOnPlatform = pathname === "/platform";

  function navActive(target: string): string {
    if (target === "/dashboard") return isOnDashboard ? "active" : "";
    if (isOnPlatform && currentView === target) return "active";
    return "";
  }

  const rolePretty = prettyRole(user.role);

  return (
    <header className="site-header">
      <div className="header-left">
        <Link href="/dashboard">
          <Image src="/qut-logo.svg" alt="QUT logo" width={36} height={36} />
        </Link>
        <div>
          <div className="header-title">Industry Relations Database</div>
          <div className="header-subtitle">
            {subtitle || "QUT Professional Services"}
          </div>
        </div>
      </div>
      <div className="header-right">
        <nav className="top-nav" aria-label="Primary">
          <Link href="/dashboard" className={navActive("/dashboard")}>
            Dashboard
          </Link>
          <Link
            href="/platform?view=partners"
            className={navActive("partners")}
          >
            Organisations
          </Link>
          <Link
            href="/platform?view=projects"
            className={navActive("projects")}
          >
            Projects
          </Link>
          <Link href="/platform?view=events" className={navActive("events")}>
            Events
          </Link>
          <Link
            href="/platform?view=applications"
            className={navActive("applications")}
          >
            Applications
          </Link>
          {user.role === "admin" && (
            <Link href="/platform?view=users" className={navActive("users")}>
              User Management
            </Link>
          )}
        </nav>
        <div className="user-block">
          <div className="user-name">{user.fullName}</div>
          <div className="user-meta">
            <span>{user.email}</span>
            <span className={`role-badge role-${user.role}`}>
              {rolePretty}
            </span>
          </div>
        </div>
        <button className="logout-btn" onClick={logout} type="button">
          Sign out
        </button>
      </div>
    </header>
  );
}

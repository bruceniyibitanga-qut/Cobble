"use client";

import { useState, type FormEvent } from "react";
import Image from "next/image";
import { useRouter } from "next/navigation";
import "./login.css";

const TEST_ACCOUNTS = [
  { label: "Admin", email: "admin@system.com", password: "Admin123!" },
  { label: "Staff", email: "staff@system.com", password: "Staff123!" },
  { label: "Faculty", email: "faculty@system.com", password: "Faculty123!" },
];

export default function LoginPage() {
  const router = useRouter();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [message, setMessage] = useState<{
    type: "error" | "success";
    text: string;
  } | null>(null);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setMessage(null);
    setSubmitting(true);

    try {
      const res = await fetch("/api/auth/login", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ email, password }),
      });

      const data = await res.json();

      if (!res.ok) {
        throw new Error(data.error || `Login failed (HTTP ${res.status}).`);
      }

      setMessage({
        type: "success",
        text: `Welcome, ${data.fullName}. Redirecting...`,
      });

      setTimeout(() => router.push("/dashboard"), 600);
    } catch (err) {
      const errorMessage =
        err instanceof Error ? err.message : "An unknown error occurred.";
      setMessage({
        type: "error",
        text: errorMessage + " Is the API running on port 8080?",
      });
    } finally {
      setSubmitting(false);
    }
  }

  function autofill(account: (typeof TEST_ACCOUNTS)[number]) {
    setEmail(account.email);
    setPassword(account.password);
  }

  return (
    <div className="login-page">
      <div className="login-card">
        <div className="brand">
          <Image
            src="/qut-logo.svg"
            alt="QUT logo"
            className="brand-logo"
            width={48}
            height={48}
          />
          <h1>Industry Relations Database</h1>
          <p className="subtitle">Sign in to access the dashboard</p>
        </div>

        <form onSubmit={handleSubmit}>
          <div className="form-group">
            <label htmlFor="email">Email</label>
            <input
              type="email"
              id="email"
              placeholder="staff@qut.edu.au"
              required
              autoComplete="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
            />
          </div>

          <div className="form-group">
            <label htmlFor="password">Password</label>
            <input
              type="password"
              id="password"
              placeholder="Enter your password"
              required
              autoComplete="current-password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
            />
          </div>

          <div className="options-row">
            <label className="checkbox-label">
              <input type="checkbox" />
              <span>Remember me</span>
            </label>
            <a href="#" className="forgot-link">
              Forgot password?
            </a>
          </div>

          <button type="submit" disabled={submitting}>
            {submitting ? "Signing in..." : "Sign In"}
          </button>

          {message && (
            <div className={`message ${message.type}`}>{message.text}</div>
          )}
        </form>

        <div className="test-accounts">
          <strong>Test accounts (click to autofill)</strong>
          {TEST_ACCOUNTS.map((account) => (
            <div
              key={account.email}
              className="test-account-row"
              onClick={() => autofill(account)}
            >
              <span>{account.label}</span>
              <code>{account.email}</code>
            </div>
          ))}
        </div>

        <p className="footer">
          QUT Professional Services &middot; IFB398 Capstone
        </p>
      </div>
    </div>
  );
}

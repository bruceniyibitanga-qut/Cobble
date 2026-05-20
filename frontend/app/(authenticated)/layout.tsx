"use client";

import { Suspense, type ReactNode } from "react";
import { AuthProvider, useAuth } from "@/lib/auth-context";
import Header from "@/components/Header";

function LoadingSkeleton() {
  return (
    <div
      style={{
        background: "#f3f4f6",
        minHeight: "100vh",
      }}
    >
      <div
        style={{
          background: "#002C5F",
          height: 64,
          display: "flex",
          alignItems: "center",
          padding: "0 28px",
        }}
      >
        <div
          style={{
            width: 36,
            height: 36,
            borderRadius: 6,
            background: "rgba(255,255,255,0.15)",
          }}
        />
        <div style={{ marginLeft: 14 }}>
          <div
            style={{
              width: 180,
              height: 14,
              borderRadius: 4,
              background: "rgba(255,255,255,0.15)",
              marginBottom: 6,
            }}
          />
          <div
            style={{
              width: 120,
              height: 10,
              borderRadius: 4,
              background: "rgba(255,255,255,0.1)",
            }}
          />
        </div>
      </div>
      <div
        style={{
          maxWidth: 1100,
          margin: "0 auto",
          padding: "32px 28px",
        }}
      >
        <div
          style={{
            background: "white",
            borderRadius: 10,
            padding: 28,
            marginBottom: 24,
            border: "1px solid #e5e7eb",
          }}
        >
          <div
            style={{
              width: 260,
              height: 22,
              borderRadius: 4,
              background: "#e5e7eb",
              marginBottom: 10,
            }}
          />
          <div
            style={{
              width: 400,
              height: 14,
              borderRadius: 4,
              background: "#f3f4f6",
            }}
          />
        </div>
        <div
          style={{
            display: "grid",
            gridTemplateColumns: "repeat(auto-fit, minmax(240px, 1fr))",
            gap: 16,
          }}
        >
          {[1, 2, 3, 4, 5, 6].map((i) => (
            <div
              key={i}
              style={{
                background: "white",
                borderRadius: 10,
                padding: 22,
                border: "1px solid #e5e7eb",
                height: 140,
              }}
            >
              <div
                style={{
                  width: 40,
                  height: 40,
                  borderRadius: 8,
                  background: "#f3f4f6",
                  marginBottom: 14,
                }}
              />
              <div
                style={{
                  width: "70%",
                  height: 14,
                  borderRadius: 4,
                  background: "#e5e7eb",
                  marginBottom: 8,
                }}
              />
              <div
                style={{
                  width: "90%",
                  height: 10,
                  borderRadius: 4,
                  background: "#f3f4f6",
                }}
              />
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}

function AuthGate({ children }: { children: ReactNode }) {
  const { loading } = useAuth();

  if (loading) {
    return <LoadingSkeleton />;
  }

  return <>{children}</>;
}

export default function AuthenticatedLayout({
  children,
}: {
  children: ReactNode;
}) {
  return (
    <AuthProvider>
      <AuthGate>
        <Suspense>
          <Header />
        </Suspense>
        {children}
      </AuthGate>
    </AuthProvider>
  );
}

import { NextRequest, NextResponse } from "next/server";
import { cookies } from "next/headers";

const BACKEND_URL = process.env.BACKEND_URL || "http://localhost:8080";
const COOKIE_NAME = "auth-token";
const MAX_AGE = 3600; // 1 hour, matching JWT expiry

export async function POST(req: NextRequest) {
  try {
    const { email, password } = await req.json();

    const backendRes = await fetch(`${BACKEND_URL}/api/Auth/login`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ Email: email, Password: password }),
    });

    if (!backendRes.ok) {
      const status = backendRes.status;
      const message =
        status === 401
          ? "Invalid email or password."
          : `Login failed (HTTP ${status}).`;
      return NextResponse.json({ error: message }, { status });
    }

    const data = await backendRes.json();

    const cookieStore = await cookies();
    cookieStore.set(COOKIE_NAME, data.token, {
      httpOnly: true,
      secure: process.env.NODE_ENV === "production",
      sameSite: "lax",
      path: "/",
      maxAge: MAX_AGE,
    });

    return NextResponse.json({
      fullName: data.fullName,
      email: data.email,
      role: data.role,
      facultyId: data.facultyId || null,
      organisationId: data.organisationId || null,
    });
  } catch {
    return NextResponse.json(
      { error: "Unable to reach the authentication server." },
      { status: 502 },
    );
  }
}

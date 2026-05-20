import { NextResponse } from "next/server";
import { cookies } from "next/headers";

const COOKIE_NAME = "auth-token";

interface JwtPayload {
  [key: string]: unknown;
}

function decodeJwtPayload(token: string): JwtPayload | null {
  try {
    const parts = token.split(".");
    if (parts.length !== 3) return null;
    const payload = parts[1];
    const json = Buffer.from(payload, "base64url").toString("utf-8");
    return JSON.parse(json);
  } catch {
    return null;
  }
}

export async function GET() {
  const cookieStore = await cookies();
  const token = cookieStore.get(COOKIE_NAME)?.value;

  if (!token) {
    return NextResponse.json({ error: "Not authenticated" }, { status: 401 });
  }

  const payload = decodeJwtPayload(token);
  if (!payload) {
    return NextResponse.json({ error: "Invalid token" }, { status: 401 });
  }

  const exp = payload.exp as number | undefined;
  if (exp && exp * 1000 < Date.now()) {
    return NextResponse.json({ error: "Token expired" }, { status: 401 });
  }

  const role =
    (payload[
      "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
    ] as string) ||
    (payload.role as string) ||
    "industry_partner";

  const email =
    (payload[
      "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress"
    ] as string) ||
    (payload.email as string) ||
    "";

  const fullName =
    (payload[
      "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name"
    ] as string) ||
    (payload.fullName as string) ||
    (payload.name as string) ||
    "User";

  return NextResponse.json({
    fullName,
    email,
    role: role.toLowerCase(),
    facultyId: (payload.facultyId as string) || null,
    organisationId: (payload.organisationId as string) || null,
  });
}

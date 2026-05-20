import { NextRequest, NextResponse } from "next/server";
import { cookies } from "next/headers";

const BACKEND_URL = process.env.BACKEND_URL || "http://localhost:8080";
const COOKIE_NAME = "auth-token";

async function proxyRequest(
  req: NextRequest,
  { params }: { params: Promise<{ path: string[] }> },
) {
  const { path } = await params;
  const cookieStore = await cookies();
  const token = cookieStore.get(COOKIE_NAME)?.value;

  if (!token) {
    return NextResponse.json({ error: "Not authenticated" }, { status: 401 });
  }

  const backendPath = `/api/${path.join("/")}`;
  const url = new URL(backendPath, BACKEND_URL);
  req.nextUrl.searchParams.forEach((value, key) => {
    url.searchParams.set(key, value);
  });

  const headers: HeadersInit = {
    Authorization: `Bearer ${token}`,
  };

  let body: string | null = null;
  if (req.method !== "GET" && req.method !== "HEAD") {
    const contentType = req.headers.get("content-type");
    if (contentType) headers["Content-Type"] = contentType;
    body = await req.text();
  }

  try {
    const backendRes = await fetch(url.toString(), {
      method: req.method,
      headers,
      body,
    });

    if (backendRes.status === 401) {
      cookieStore.set(COOKIE_NAME, "", {
        httpOnly: true,
        secure: process.env.NODE_ENV === "production",
        sameSite: "lax",
        path: "/",
        maxAge: 0,
      });
      return NextResponse.json({ error: "Session expired" }, { status: 401 });
    }

    if (backendRes.status === 204) {
      return new NextResponse(null, { status: 204 });
    }

    const data = await backendRes.text();
    return new NextResponse(data, {
      status: backendRes.status,
      headers: { "Content-Type": backendRes.headers.get("content-type") || "application/json" },
    });
  } catch {
    return NextResponse.json(
      { error: "Unable to reach the backend server." },
      { status: 502 },
    );
  }
}

export const GET = proxyRequest;
export const POST = proxyRequest;
export const PUT = proxyRequest;
export const DELETE = proxyRequest;
export const PATCH = proxyRequest;

export function registerServiceWorker() {
  if (typeof window === "undefined") return;
  if (!("serviceWorker" in navigator)) return;

  window.addEventListener("load", () => {
    navigator.serviceWorker.register("/sw.js").catch(() => {
      // Service worker registration failed
    });
  });
}

export function requestNotificationPermission(): Promise<NotificationPermission> {
  if (!("Notification" in window)) return Promise.resolve("denied" as NotificationPermission);
  return Notification.requestPermission();
}

export function isPWAInstalled(): boolean {
  if (typeof window === "undefined") return false;
  return window.matchMedia("(display-mode: standalone)").matches;
}

const API_URL = process.env.NEXT_PUBLIC_API_URL || "http://127.0.0.1:5100";

export async function subscribeToPush(token: string, tenantSlug: string): Promise<boolean> {
  if (!("serviceWorker" in navigator) || !("PushManager" in window)) return false;

  try {
    const permission = await Notification.requestPermission();
    if (permission !== "granted") return false;

    const res = await fetch(`${API_URL}/api/push/vapid-key`, {
      headers: { "X-Tenant-Id": tenantSlug },
    });
    if (!res.ok) return false;
    const { publicKey } = await res.json();

    const registration = await navigator.serviceWorker.ready;
    const subscription = await registration.pushManager.subscribe({
      userVisibleOnly: true,
      applicationServerKey: urlBase64ToUint8Array(publicKey).buffer as ArrayBuffer,
    });

    const subJson = subscription.toJSON();
    await fetch(`${API_URL}/api/push/subscribe`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        Authorization: `Bearer ${token}`,
        "X-Tenant-Id": tenantSlug,
      },
      body: JSON.stringify({
        endpoint: subJson.endpoint,
        p256dh: subJson.keys?.p256dh,
        auth: subJson.keys?.auth,
      }),
    });

    return true;
  } catch {
    return false;
  }
}

export async function unsubscribeFromPush(token: string, tenantSlug: string): Promise<boolean> {
  if (!("serviceWorker" in navigator)) return false;

  try {
    const registration = await navigator.serviceWorker.ready;
    const subscription = await registration.pushManager.getSubscription();
    if (!subscription) return true;

    await fetch(`${API_URL}/api/push/unsubscribe`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        Authorization: `Bearer ${token}`,
        "X-Tenant-Id": tenantSlug,
      },
      body: JSON.stringify({ endpoint: subscription.endpoint }),
    });

    await subscription.unsubscribe();
    return true;
  } catch {
    return false;
  }
}

function urlBase64ToUint8Array(base64String: string): Uint8Array {
  const padding = "=".repeat((4 - (base64String.length % 4)) % 4);
  const base64 = (base64String + padding).replace(/-/g, "+").replace(/_/g, "/");
  const raw = atob(base64);
  const output = new Uint8Array(raw.length);
  for (let i = 0; i < raw.length; i++) {
    output[i] = raw.charCodeAt(i);
  }
  return output;
}

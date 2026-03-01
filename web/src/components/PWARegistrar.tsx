"use client";

import { useEffect } from "react";
import { registerServiceWorker } from "@/lib/pwa";
import { PWAInstallPrompt } from "./PWAInstallPrompt";

export function PWARegistrar() {
  useEffect(() => {
    registerServiceWorker();
  }, []);

  return <PWAInstallPrompt />;
}

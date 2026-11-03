import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

// In development, /api requests go to the local ASP.NET Core API (see backend/AsistentePersonal/Properties/launchSettings.json).
// The https profile listens on 7264. The dev certificate is self-signed, so secure is false.
// GitHub Pages serves the app under /<repo>/, so the build takes its base path from VITE_BASE_PATH.
export default defineConfig({
  base: process.env.VITE_BASE_PATH ?? "/",
  plugins: [react()],
  server: {
    proxy: {
      "/api": {
        target: "https://localhost:7264",
        changeOrigin: true,
        secure: false,
      },
    },
  },
});

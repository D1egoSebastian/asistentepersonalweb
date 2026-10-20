import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

// In development, /api requests go to the local ASP.NET Core API (see backend/AsistentePersonal/Properties/launchSettings.json).
// The https profile listens on 7264. The dev certificate is self-signed, so secure is false.
export default defineConfig({
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

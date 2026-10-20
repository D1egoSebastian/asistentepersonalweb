import { useCallback, useEffect, useState } from "react";
import { getStatus } from "./api/authApi.js";
import { setUnauthorizedHandler } from "./api/habitsApi.js";
import { clearToken, readToken } from "./api/session.js";
import ErrorBanner from "./components/ErrorBanner.jsx";
import HabitsPage from "./pages/HabitsPage.jsx";
import LoginPage from "./pages/LoginPage.jsx";
import SetupPage from "./pages/SetupPage.jsx";

const SESSION_EXPIRED = "Tu sesión expiró. Vuelve a iniciar sesión.";

// Decides which screen to show: first use, sign in, or the habits (spec 002, plan §4).
export default function App() {
  const [screen, setScreen] = useState("loading");
  const [notice, setNotice] = useState(null);

  const decideScreen = useCallback(async () => {
    try {
      const { hasOwner } = await getStatus();
      if (!hasOwner) {
        setScreen("setup");
      } else {
        setScreen(readToken() ? "habits" : "login");
      }
    } catch (err) {
      setNotice(err.message);
      setScreen("error");
    }
  }, []);

  useEffect(() => {
    // A 401 from any habits call means the session is gone: back to sign in, with the reason (CA-3.3).
    setUnauthorizedHandler(() => {
      setNotice(SESSION_EXPIRED);
      setScreen("login");
    });
    decideScreen();
    return () => setUnauthorizedHandler(null);
  }, [decideScreen]);

  // The first-use screen was answered 409: the account exists, so the only way in is to sign in.
  function handleAccountExists(message) {
    setNotice(message);
    setScreen("login");
  }

  function handleSessionStarted() {
    setNotice(null);
    setScreen("habits");
  }

  // "Salir" only clears this browser's session (spec 002, D-2).
  function handleLogout() {
    clearToken();
    setNotice(null);
    setScreen("login");
  }

  return (
    <main className="app">
      {screen === "loading" && <p>Cargando…</p>}
      {screen === "error" && (
        <>
          <ErrorBanner message={notice} />
          <button type="button" onClick={decideScreen}>
            Reintentar
          </button>
        </>
      )}
      {screen === "setup" && (
        <SetupPage onSessionStarted={handleSessionStarted} onAccountExists={handleAccountExists} />
      )}
      {screen === "login" && <LoginPage notice={notice} onSessionStarted={handleSessionStarted} />}
      {screen === "habits" && <HabitsPage onLogout={handleLogout} />}
    </main>
  );
}

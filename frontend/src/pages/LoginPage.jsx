import { useState } from "react";
import { login } from "../api/authApi.js";
import ErrorBanner from "../components/ErrorBanner.jsx";

// Sign-in screen (spec 002, RF-2). "notice" carries a message from the app, such as an expired session.
export default function LoginPage({ notice, onSessionStarted }) {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState(null);
  const [sending, setSending] = useState(false);

  async function handleSubmit(event) {
    event.preventDefault();
    setError(null);
    setSending(true);
    try {
      await login(email, password);
      onSessionStarted();
    } catch (err) {
      setError(err.message);
    } finally {
      setSending(false);
    }
  }

  return (
    <section className="auth-page">
      <h1>Iniciar sesión</h1>
      <ErrorBanner message={error ?? notice} />
      <form className="habit-form" onSubmit={handleSubmit}>
        <label>
          Email
          <input type="email" autoComplete="email" value={email} onChange={(e) => setEmail(e.target.value)} />
        </label>
        <label>
          Contraseña
          <input
            type="password"
            autoComplete="current-password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
          />
        </label>
        <div className="habit-form__actions">
          <button type="submit" disabled={sending}>
            Entrar
          </button>
        </div>
      </form>
    </section>
  );
}

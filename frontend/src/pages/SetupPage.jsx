import { useState } from "react";
import { setup } from "../api/authApi.js";
import ErrorBanner from "../components/ErrorBanner.jsx";

// First-use screen: only shown while no account exists (spec 002, RF-1).
// "onAccountExists" runs when someone created the account first (409): the app moves to sign in.
export default function SetupPage({ onSessionStarted, onAccountExists }) {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [confirmation, setConfirmation] = useState("");
  const [error, setError] = useState(null);
  const [sending, setSending] = useState(false);

  async function handleSubmit(event) {
    event.preventDefault();
    setError(null);
    setSending(true);
    try {
      await setup(email, password, confirmation);
      onSessionStarted();
    } catch (err) {
      if (err.status === 409) {
        onAccountExists(err.message);
        return;
      }
      setError(err.message);
    } finally {
      setSending(false);
    }
  }

  return (
    <section className="auth-page">
      <h1>Crea tu cuenta</h1>
      <p>Es la primera vez que abres la app. Esta cuenta será la única.</p>
      <ErrorBanner message={error} />
      <form className="habit-form" onSubmit={handleSubmit}>
        <label>
          Email
          <input type="email" autoComplete="email" value={email} onChange={(e) => setEmail(e.target.value)} />
        </label>
        <label>
          Contraseña (al menos 12 caracteres)
          <input
            type="password"
            autoComplete="new-password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
          />
        </label>
        <label>
          Repite la contraseña
          <input
            type="password"
            autoComplete="new-password"
            value={confirmation}
            onChange={(e) => setConfirmation(e.target.value)}
          />
        </label>
        <div className="habit-form__actions">
          <button type="submit" disabled={sending}>
            Crear cuenta
          </button>
        </div>
      </form>
    </section>
  );
}

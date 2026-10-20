import { useState } from "react";

// Creates a habit (mode "create") or edits its name and description (mode "edit").
// In edit mode, frequency and goal are shown as fixed text: the API does not change them (spec RF-8).
export default function HabitForm({ mode, habit, onSubmit, onCancel }) {
  const [name, setName] = useState(habit?.name ?? "");
  const [description, setDescription] = useState(habit?.description ?? "");
  const [frequency, setFrequency] = useState("Daily");
  const [weeklyGoal, setWeeklyGoal] = useState("");
  const [sending, setSending] = useState(false);

  function formValues() {
    if (mode === "edit") {
      return { name, description };
    }
    return {
      name,
      description,
      frequency,
      weeklyGoal: frequency === "Weekly" && weeklyGoal !== "" ? Number(weeklyGoal) : null,
    };
  }

  // "Guardar" stays disabled until the save finishes, so a double click sends a single request.
  async function handleSubmit(event) {
    event.preventDefault();
    setSending(true);
    try {
      await onSubmit(formValues());
    } finally {
      setSending(false);
    }
  }

  return (
    <form className="habit-form" onSubmit={handleSubmit}>
      <h2>{mode === "edit" ? "Editar hábito" : "Nuevo hábito"}</h2>

      <label>
        Nombre
        <input value={name} onChange={(event) => setName(event.target.value)} />
      </label>

      <label>
        Descripción (opcional)
        <textarea value={description} onChange={(event) => setDescription(event.target.value)} />
      </label>

      {mode === "create" && (
        <>
          <fieldset>
            <legend>Frecuencia</legend>
            <label>
              <input
                type="radio"
                name="frequency"
                value="Daily"
                checked={frequency === "Daily"}
                onChange={() => setFrequency("Daily")}
              />
              Diario
            </label>
            <label>
              <input
                type="radio"
                name="frequency"
                value="Weekly"
                checked={frequency === "Weekly"}
                onChange={() => setFrequency("Weekly")}
              />
              Semanal
            </label>
          </fieldset>

          {frequency === "Weekly" && (
            <label>
              Veces por semana (1 a 7)
              <input
                type="number"
                min="1"
                max="7"
                value={weeklyGoal}
                onChange={(event) => setWeeklyGoal(event.target.value)}
              />
            </label>
          )}
        </>
      )}

      {mode === "edit" && habit && (
        <div className="habit-form__fixed">
          <p>Frecuencia: {habit.frequency === "Daily" ? "Diaria" : "Semanal"}</p>
          {habit.frequency === "Weekly" && <p>Meta: {habit.weeklyGoal} veces por semana</p>}
        </div>
      )}

      <div className="habit-form__actions">
        <button type="submit" disabled={sending}>
          Guardar
        </button>
        <button type="button" onClick={onCancel}>
          Cancelar
        </button>
      </div>
    </form>
  );
}

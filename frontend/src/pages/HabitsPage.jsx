import ErrorBanner from "../components/ErrorBanner.jsx";
import HabitForm from "../components/HabitForm.jsx";
import HabitList from "../components/HabitList.jsx";
import { useHabits } from "../hooks/useHabits.js";

// Shows the habits screen. All the logic lives in useHabits (constitution, principle 3).
export default function HabitsPage() {
  const habits = useHabits();

  return (
    <section className="habits-page">
      <header className="habits-page__header">
        <h1>Mis hábitos</h1>
        <div className="habits-page__header-actions">
          {!habits.creating && (
            <button type="button" onClick={() => habits.setCreating(true)}>
              Nuevo hábito
            </button>
          )}
        </div>
      </header>

      <ErrorBanner message={habits.error} />

      {habits.creating && (
        <HabitForm mode="create" onSubmit={habits.create} onCancel={() => habits.setCreating(false)} />
      )}

      {habits.today === null && !habits.error && <p>Cargando hábitos…</p>}

      {habits.today !== null && (
        <HabitList
          items={habits.items}
          busyIds={habits.busyIds}
          editingId={habits.editingId}
          onToggleDay={habits.toggleDay}
          onEdit={habits.setEditingId}
          onDelete={habits.remove}
          onSaveEdit={habits.saveEdit}
          onCancelEdit={() => habits.setEditingId(null)}
        />
      )}
    </section>
  );
}

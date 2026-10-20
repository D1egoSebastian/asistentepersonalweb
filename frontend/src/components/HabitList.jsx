import HabitCard from "./HabitCard.jsx";
import HabitForm from "./HabitForm.jsx";

// Lists habits in the order the API sends them. One habit at a time can show its edit form instead of its card.
export default function HabitList({
  items,
  busyIds,
  editingId,
  onToggleDay,
  onEdit,
  onDelete,
  onSaveEdit,
  onCancelEdit,
}) {
  if (items.length === 0) {
    return <p className="habit-list__empty">Todavía no tienes hábitos. Crea el primero para empezar.</p>;
  }

  return (
    <ul className="habit-list">
      {items.map(({ habit, view }) => (
        <li key={view.id}>
          {editingId === view.id ? (
            <HabitForm
              mode="edit"
              habit={habit}
              onSubmit={(values) => onSaveEdit(view.id, values)}
              onCancel={onCancelEdit}
            />
          ) : (
            <HabitCard
              view={view}
              busy={busyIds.has(view.id)}
              onToggleDay={onToggleDay}
              onEdit={() => onEdit(view.id)}
              onDelete={() => onDelete(habit)}
            />
          )}
        </li>
      ))}
    </ul>
  );
}

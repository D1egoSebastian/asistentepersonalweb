// "1 vez por semana", "3 veces por semana".
export function goalLabel(goal) {
  return goal === 1 ? "1 vez por semana" : `${goal} veces por semana`;
}

// The unit of a count: day(s) for daily habits, week(s) for weekly ones.
export function unitLabel(count, isDaily) {
  if (isDaily) {
    return count === 1 ? "día" : "días";
  }
  return count === 1 ? "semana" : "semanas";
}

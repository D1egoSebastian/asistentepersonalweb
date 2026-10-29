import { formatShortDate } from "./dates.js";

// Builds the Chart.js configuration for the weekly trend as a plain object.
// It does not import Chart.js, so it can be tested without it (plan 004, D-T9).
// `points` come from toMetricsView (`label`, rounded `value` or null); `colors` are { line, grid, text }.

const PERCENT_MIN = 0;
const PERCENT_MAX = 100;

function percentTick(value) {
  return `${value} %`;
}

function lineDataset(points, colors) {
  return {
    label: "Cumplimiento general",
    data: points.map((point) => point.value),
    borderColor: colors.line,
    backgroundColor: colors.line,
    pointBackgroundColor: colors.line,
    spanGaps: false,
  };
}

// The X axis shows the short date of each Monday so the 12 weeks fit on a phone; the tooltip keeps the full label.
function scales(points, colors) {
  const grid = { color: colors.grid };
  const shortDate = (_value, index) => formatShortDate(points[index].monday);
  return {
    x: { grid, ticks: { color: colors.text, callback: shortDate } },
    y: {
      min: PERCENT_MIN,
      max: PERCENT_MAX,
      grid,
      ticks: { color: colors.text, callback: percentTick },
    },
  };
}

export function trendChartConfig(points, colors) {
  return {
    type: "line",
    data: {
      labels: points.map((point) => point.label),
      datasets: [lineDataset(points, colors)],
    },
    options: {
      animation: false,
      maintainAspectRatio: false,
      spanGaps: false,
      scales: scales(points, colors),
    },
  };
}

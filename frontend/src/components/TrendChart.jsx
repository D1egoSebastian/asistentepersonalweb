import { CategoryScale, Chart, LineController, LineElement, LinearScale, PointElement, Tooltip } from "chart.js";
import { useEffect, useRef } from "react";
import { useColorScheme } from "../hooks/useColorScheme.js";
import { trendChartConfig } from "../logic/trendChart.js";

// Only what a line chart needs is registered, so the rest of Chart.js stays out of the bundle (plan 004, D-T9).
Chart.register(LineController, LineElement, PointElement, LinearScale, CategoryScale, Tooltip);

// Reads the chart colors from the CSS variables, so light and dark live in one place (D-T11).
function readColors() {
  const style = getComputedStyle(document.documentElement);
  const read = (name) => style.getPropertyValue(name).trim();
  return { line: read("--color-chart-line"), grid: read("--color-chart-grid"), text: read("--color-text") };
}

// Draws the weekly trend. It only draws: the points come ready from toMetricsView.
// The chart is destroyed on unmount and rebuilt when the data or the color scheme change.
export default function TrendChart({ trend }) {
  const canvasRef = useRef(null);
  const scheme = useColorScheme();

  useEffect(() => {
    const chart = new Chart(canvasRef.current, trendChartConfig(trend, readColors()));
    return () => chart.destroy();
  }, [trend, scheme]);

  return (
    <div className="trend-chart">
      <canvas
        ref={canvasRef}
        role="img"
        aria-label="Gráfico de líneas con el cumplimiento general de las últimas 12 semanas. Los mismos datos están en la tabla que sigue."
      />
    </div>
  );
}

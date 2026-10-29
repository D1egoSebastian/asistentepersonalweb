import { describe, it } from "node:test";
import assert from "node:assert/strict";
import { trendChartConfig } from "./trendChart.js";

const COLORS = { line: "#112233", grid: "#445566", text: "#778899" };

// Twelve points shaped like the ones toMetricsView builds: the value is already rounded, or null for "sin datos".
function makePoints(values) {
  return values.map((value, index) => ({
    monday: `2026-07-${String(20 + index).padStart(2, "0")}`,
    label: `semana ${index + 1}`,
    value,
    text: value === null ? "sin datos" : `${value} %`,
  }));
}

const TWELVE = makePoints([100, 50, 67, 0, 80, 90, 100, 33, 20, 75, 60, 40]);

describe("trendChartConfig: data", () => {
  it("is a line chart with one label and one value per point, in order", () => {
    const config = trendChartConfig(TWELVE, COLORS);
    assert.equal(config.type, "line");
    assert.equal(config.data.labels.length, 12);
    assert.equal(config.data.datasets.length, 1);
    assert.deepEqual(config.data.datasets[0].data, [100, 50, 67, 0, 80, 90, 100, 33, 20, 75, 60, 40]);
    assert.equal(config.data.labels[0], "semana 1");
    assert.equal(config.data.labels[11], "semana 12");
  });

  it("keeps null as a gap and does not draw a line across it (CA-7.4)", () => {
    const config = trendChartConfig(makePoints([100, null, 50, 0, null, null, 20, 30, 40, 50, 60, 70]), COLORS);
    const [dataset] = config.data.datasets;
    assert.equal(dataset.data[1], null);
    assert.equal(dataset.data[4], null);
    assert.equal(dataset.data[3], 0, "a real 0 % stays 0, it is not a gap");
    assert.equal(dataset.spanGaps, false);
    assert.equal(config.options.spanGaps, false);
  });

  it("follows the points it receives, whatever their count", () => {
    const config = trendChartConfig(makePoints([10, null, 30]), COLORS);
    assert.deepEqual(config.data.datasets[0].data, [10, null, 30]);
    assert.deepEqual(config.data.labels, ["semana 1", "semana 2", "semana 3"]);
  });
});

describe("trendChartConfig: options", () => {
  it("fixes the Y axis from 0 to 100 %", () => {
    const { y } = trendChartConfig(TWELVE, COLORS).options.scales;
    assert.equal(y.min, 0);
    assert.equal(y.max, 100);
    assert.equal(y.ticks.callback(50), "50 %");
  });

  it("turns animation off", () => {
    assert.equal(trendChartConfig(TWELVE, COLORS).options.animation, false);
  });
});

describe("trendChartConfig: colors (CA-7.6)", () => {
  it("applies the received colors to the line, the grid and the text", () => {
    const { data, options } = trendChartConfig(TWELVE, COLORS);
    const [dataset] = data.datasets;
    assert.equal(dataset.borderColor, "#112233");
    assert.equal(dataset.pointBackgroundColor, "#112233");
    assert.equal(options.scales.y.grid.color, "#445566");
    assert.equal(options.scales.x.grid.color, "#445566");
    assert.equal(options.scales.y.ticks.color, "#778899");
    assert.equal(options.scales.x.ticks.color, "#778899");
  });

  it("uses other colors when it receives other colors", () => {
    const other = { line: "#aaaaaa", grid: "#bbbbbb", text: "#cccccc" };
    const { data, options } = trendChartConfig(TWELVE, other);
    assert.equal(data.datasets[0].borderColor, "#aaaaaa");
    assert.equal(options.scales.y.grid.color, "#bbbbbb");
    assert.equal(options.scales.x.ticks.color, "#cccccc");
  });
});

describe("trendChartConfig: small screens", () => {
  it("labels the X axis with the short date of each Monday", () => {
    const { x } = trendChartConfig(TWELVE, COLORS).options.scales;
    assert.equal(x.ticks.callback(0, 0), "20 jul");
    assert.equal(x.ticks.callback(11, 11), "31 jul");
  });

  it("keeps the full week label for the tooltip", () => {
    assert.equal(trendChartConfig(TWELVE, COLORS).data.labels[0], "semana 1");
  });

  it("lets the container set the height instead of a 2:1 ratio", () => {
    assert.equal(trendChartConfig(TWELVE, COLORS).options.maintainAspectRatio, false);
  });
});

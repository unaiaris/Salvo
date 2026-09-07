import type { DashboardRiskBucket, Language } from "@/lib/api/contract";
import { formatting, type Formatting } from "@/lib/format";

/**
 * Risk over time, drawn as SVG on the server.
 *
 * This is decision 43 and it is not a stylistic preference. A charting library is a client
 * component, and every prop of a client component is serialised whole into the RSC payload inside
 * the HTML — so handing one the weekly buckets would ship the dashboard's data to the browser and
 * reopen exactly the surface decision 42 closes. Markup the server writes has no such payload. What
 * it costs is hover tooltips, which is why every bar carries its own `<title>` and why the same
 * numbers are also printed in a real table underneath: nothing here is available only on hover.
 *
 * The bucket is a week and the week is `occurredAt` in business time, the same zone
 * `RuleConfig.BusinessTimeZone` uses to decide what day an order belongs to. Grouping by the date an
 * evaluation was computed would draw the corpus by the day somebody happened to run the scoring.
 */

const WIDTH = 720;
const HEIGHT = 260;
const PLOT_LEFT = 46;
const PLOT_RIGHT = 712;
const PLOT_TOP = 16;
const BASELINE = 206;

const ORDER_FILL = "#cbd5e1";
const FLAGGED_FILL = "#be123c";

/** A round ceiling for the axis, so the top gridline is a number worth reading. */
function niceMaximum(value: number): number {
  if (value <= 5) {
    return 5;
  }

  const magnitude = 10 ** Math.floor(Math.log10(value));
  const step = magnitude / 2;

  return Math.ceil(value / step) * step;
}

export function RiskOverTimeChart({
  buckets,
  language,
}: {
  readonly buckets: readonly DashboardRiskBucket[];
  readonly language: Language;
}) {
  const f = formatting(language);
  const { t } = f;
  const maximum = niceMaximum(Math.max(...buckets.map((bucket) => bucket.orderCount), 1));
  const plotWidth = PLOT_RIGHT - PLOT_LEFT;
  const plotHeight = BASELINE - PLOT_TOP;
  const slot = plotWidth / buckets.length;
  const barWidth = Math.max(2, slot * 0.68);
  const totalOrders = buckets.reduce((sum, bucket) => sum + bucket.orderCount, 0);
  const totalFlagged = buckets.reduce((sum, bucket) => sum + bucket.flaggedCount, 0);
  const first = buckets.at(0);
  const last = buckets.at(-1);
  // The caller does not render this component for an empty corpus. Deriving the range from what is
  // actually there — instead of asserting it is — keeps the description from inventing a date if
  // that ever stops being true.
  const range = first === undefined || last === undefined
    ? ""
    : t.dashboard.chartRange(
        f.formatCalendarDate(first.weekStart),
        f.formatCalendarDate(last.weekStart),
      );

  const scale = (value: number): number => (value / maximum) * plotHeight;
  const ticks = [0, maximum / 2, maximum];
  // With seventeen weeks every label would collide, so one in every few is drawn and the table
  // below carries the rest.
  const labelEvery = Math.ceil(buckets.length / 6);

  return (
    <div className="flex flex-col gap-4">
      <svg
        viewBox={`0 0 ${String(WIDTH)} ${String(HEIGHT)}`}
        role="img"
        aria-labelledby="risk-chart-title risk-chart-desc"
        className="w-full"
      >
        <title id="risk-chart-title">{t.dashboard.chartTitle}</title>
        <desc id="risk-chart-desc">
          {t.dashboard.chartDescription(
            String(buckets.length),
            range,
            f.formatCount(totalOrders),
            f.formatCount(totalFlagged),
          )}
        </desc>

        {ticks.map((tick) => {
          const y = BASELINE - scale(tick);

          return (
            <g key={tick}>
              <line
                x1={PLOT_LEFT}
                x2={PLOT_RIGHT}
                y1={y}
                y2={y}
                stroke="#e2e8f0"
                strokeWidth={1}
              />
              <text x={PLOT_LEFT - 8} y={y + 4} textAnchor="end" fontSize={11} fill="#475569">
                {f.formatCount(Math.round(tick))}
              </text>
            </g>
          );
        })}

        {buckets.map((bucket, index) => {
          const x = PLOT_LEFT + index * slot + (slot - barWidth) / 2;
          const orderHeight = scale(bucket.orderCount);
          const flaggedHeight = scale(bucket.flaggedCount);
          const week = f.formatCalendarDate(bucket.weekStart);

          return (
            <g key={bucket.weekStart}>
              <title>
                {t.dashboard.chartBar(
                  week,
                  f.formatCount(bucket.orderCount),
                  f.formatCount(bucket.flaggedCount),
                )}
              </title>
              <rect
                x={x}
                y={BASELINE - orderHeight}
                width={barWidth}
                height={orderHeight}
                fill={ORDER_FILL}
              />
              {bucket.flaggedCount > 0 && (
                <rect
                  x={x}
                  y={BASELINE - flaggedHeight}
                  width={barWidth}
                  height={flaggedHeight}
                  fill={FLAGGED_FILL}
                />
              )}
              {index % labelEvery === 0 && (
                <text
                  x={x + barWidth / 2}
                  y={BASELINE + 16}
                  textAnchor="middle"
                  fontSize={10}
                  fill="#475569"
                >
                  {bucket.weekStart.slice(5)}
                </text>
              )}
            </g>
          );
        })}

        <line
          x1={PLOT_LEFT}
          x2={PLOT_RIGHT}
          y1={BASELINE}
          y2={BASELINE}
          stroke="#94a3b8"
          strokeWidth={1}
        />

        <g fontSize={11} fill="#334155">
          <rect x={PLOT_LEFT} y={HEIGHT - 22} width={10} height={10} fill={ORDER_FILL} />
          <text x={PLOT_LEFT + 16} y={HEIGHT - 13}>
            {t.dashboard.chartLegendOrders}
          </text>
          <rect x={PLOT_LEFT + 150} y={HEIGHT - 22} width={10} height={10} fill={FLAGGED_FILL} />
          <text x={PLOT_LEFT + 166} y={HEIGHT - 13}>
            {t.dashboard.chartLegendFlagged}
          </text>
        </g>
      </svg>

      <RiskOverTimeTable buckets={buckets} f={f} />
    </div>
  );
}

/**
 * The chart's numbers, exactly. It is not a fallback: it is the version of this figure that can be
 * read aloud, sorted by eye and copied, and it is always on screen rather than folded away.
 */
function RiskOverTimeTable({
  buckets,
  f,
}: {
  readonly buckets: readonly DashboardRiskBucket[];
  readonly f: Formatting;
}) {
  return (
    <div className="overflow-x-auto">
      <table className="w-full min-w-[32rem] border-collapse text-sm">
        <caption className="pb-2 text-left text-xs text-slate-600">
          {f.t.dashboard.chartTableCaption}
        </caption>
        <thead>
          <tr className="border-b border-slate-200 text-left text-xs uppercase tracking-wide text-slate-600">
            <th scope="col" className="py-2 pr-4 font-semibold">
              {f.t.dashboard.chartColumnWeek}
            </th>
            <th scope="col" className="py-2 pr-4 text-right font-semibold">
              {f.t.dashboard.chartColumnOrders}
            </th>
            <th scope="col" className="py-2 pr-4 text-right font-semibold">
              {f.t.dashboard.chartColumnFlagged}
            </th>
            <th scope="col" className="py-2 text-right font-semibold">
              {f.t.dashboard.chartColumnShare}
            </th>
          </tr>
        </thead>
        <tbody>
          {buckets.map((bucket) => (
            <tr key={bucket.weekStart} className="border-b border-slate-100">
              <th scope="row" className="py-1.5 pr-4 text-left font-normal text-slate-700">
                {f.formatCalendarDate(bucket.weekStart)}
              </th>
              <td className="py-1.5 pr-4 text-right tabular-nums">
                {f.formatCount(bucket.orderCount)}
              </td>
              <td className="py-1.5 pr-4 text-right tabular-nums">
                {f.formatCount(bucket.flaggedCount)}
              </td>
              <td className="py-1.5 text-right tabular-nums text-slate-600">
                {bucket.orderCount === 0
                  ? "—"
                  : f.formatPercent(bucket.flaggedCount / bucket.orderCount)}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

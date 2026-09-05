import "server-only";

import {
  ALERT_FEED_PAGE_SIZE,
  ALERT_SORT,
  ALERT_STATUS,
  type AlertDetail,
  type AlertList,
  type AlertReviewOutcome,
} from "./contract";
import { type ApiResult, fail, succeed } from "./failures";
import {
  projectAlertDetail,
  projectAlertList,
  projectOrderCount,
  projectReviewOutcome,
} from "./guards";
import { requestJson } from "./server-client";

function project<T>(result: ApiResult<unknown>, guard: (value: unknown) => T | null): ApiResult<T> {
  if (!result.ok) {
    return result;
  }

  const projected = guard(result.value);

  return projected === null ? fail({ kind: "malformed" }) : succeed(projected);
}

/**
 * The review queue: one page of open alerts, highest current local score first.
 *
 * A single page of `MaximumPageSize` is asked for on purpose. No ordering of this feed survives a
 * scoring run that lands between two page reads — a run inserts new alerts at the front and can move
 * the score an alert is ordered by — and for the size of the MVP corpus not paging is a better answer
 * than reconciling pages. The `sort` parameter stays in the API for when it is not.
 */
export async function fetchOpenAlerts(): Promise<ApiResult<AlertList>> {
  const result = await requestJson({
    path: "/api/alerts",
    query: {
      status: ALERT_STATUS.open,
      sort: ALERT_SORT.scoreDesc,
      pageSize: ALERT_FEED_PAGE_SIZE,
    },
  });

  return project(result, projectAlertList);
}

export async function fetchAlert(id: string): Promise<ApiResult<AlertDetail>> {
  const result = await requestJson({ path: `/api/alerts/${encodeURIComponent(id)}` });

  return project(result, projectAlertDetail);
}

export interface ReviewSubmission {
  readonly newStatus: string;
  readonly note: string | null;
  readonly acknowledgedDivergence: boolean;
  /**
   * The explanation the reviewer had in front of them, or `null` when there was none.
   *
   * Never a requirement: a verdict is emitted with or without an explanation, and the field only
   * records which one was on screen. Without it, a review formed while the explanation was still
   * being written and one formed after reading it are indistinguishable forever.
   */
  readonly explanationId: string | null;
}

export async function submitAlertReview(
  id: string,
  submission: ReviewSubmission,
): Promise<ApiResult<AlertReviewOutcome>> {
  const result = await requestJson({
    path: `/api/alerts/${encodeURIComponent(id)}/review`,
    method: "POST",
    body: {
      newStatus: submission.newStatus,
      note: submission.note,
      acknowledgedDivergence: submission.acknowledgedDivergence,
      explanationId: submission.explanationId,
    },
  });

  return project(result, projectReviewOutcome);
}

/**
 * How many orders exist at all.
 *
 * The feed needs it on the empty path only, to tell an empty corpus from a corpus that was imported
 * and never scored: both leave the alert list empty and `scoringRunSequence` null, and they call for
 * opposite instructions on screen. One order is enough to answer the question, so the smallest page
 * is requested.
 */
export async function fetchOrderCount(): Promise<ApiResult<number>> {
  const result = await requestJson({ path: "/api/orders", query: { pageSize: 1 } });
  const projected = project(result, projectOrderCount);

  return projected.ok ? succeed(projected.value.totalCount) : projected;
}

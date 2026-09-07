using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Salvo.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// The language joins the identity of an explanation, and the tenth failure code joins the
    /// enumeration the database enforces.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The backfill is <c>es</c> and not the empty string EF proposes, because it is not a
    /// placeholder: every row that exists when this runs was written by a template that only ever
    /// wrote Spanish, so <c>es</c> is what those rows say rather than what we are willing to settle
    /// for. The empty string would also fail <c>ck_alert_explanations_language</c> on the spot,
    /// which is the check doing its job on the very first row it sees.
    /// </para>
    /// <para>
    /// SQLite cannot alter a check constraint in place, so EF rebuilds the table around these
    /// operations. Nothing here rewrites a row beyond filling the new column.
    /// </para>
    /// </remarks>
    public partial class ExplanationLanguage : Migration
    {
        private static readonly string[] IdentityColumns =
        [
            "risk_evaluation_id",
            "provider",
            "template_version",
            "alert_policy_version",
            "language",
        ];

        private static readonly string[] PendingEvaluationColumns =
        [
            "risk_evaluation_id",
            "provider",
            "language",
        ];

        private static readonly string[] PreviousIdentityColumns =
        [
            "risk_evaluation_id",
            "provider",
            "template_version",
            "alert_policy_version",
        ];

        private static readonly string[] PreviousPendingEvaluationColumns =
        [
            "risk_evaluation_id",
            "provider",
        ];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_alert_explanations_identity",
                table: "alert_explanations");

            migrationBuilder.DropIndex(
                name: "ux_alert_explanations_pending_evaluation",
                table: "alert_explanations");

            migrationBuilder.DropCheckConstraint(
                name: "ck_alert_explanations_failure_code",
                table: "alert_explanations");

            migrationBuilder.AddColumn<string>(
                name: "language",
                table: "alert_explanations",
                type: "TEXT",
                maxLength: 2,
                nullable: false,
                defaultValue: "es");

            migrationBuilder.CreateIndex(
                name: "ux_alert_explanations_identity",
                table: "alert_explanations",
                columns: IdentityColumns,
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_alert_explanations_pending_evaluation",
                table: "alert_explanations",
                columns: PendingEvaluationColumns,
                unique: true,
                filter: "status = 'PENDING'");

            migrationBuilder.AddCheckConstraint(
                name: "ck_alert_explanations_failure_code",
                table: "alert_explanations",
                sql: "failure_code IS NULL OR failure_code IN ('PROVIDER_UNAVAILABLE', 'PROVIDER_TIMEOUT', 'PROVIDER_REFUSED', 'MALFORMED_OUTPUT', 'NOT_GROUNDED_NUMBER', 'NOT_GROUNDED_RULE', 'TOO_LONG', 'CANCELLED', 'ATTEMPT_LIMIT_REACHED', 'LEGACY_SIGNAL_FORMAT')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_alert_explanations_language",
                table: "alert_explanations",
                sql: "language IN ('es', 'pt')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_alert_explanations_identity",
                table: "alert_explanations");

            migrationBuilder.DropIndex(
                name: "ux_alert_explanations_pending_evaluation",
                table: "alert_explanations");

            migrationBuilder.DropCheckConstraint(
                name: "ck_alert_explanations_failure_code",
                table: "alert_explanations");

            migrationBuilder.DropCheckConstraint(
                name: "ck_alert_explanations_language",
                table: "alert_explanations");

            migrationBuilder.DropColumn(
                name: "language",
                table: "alert_explanations");

            migrationBuilder.CreateIndex(
                name: "ux_alert_explanations_identity",
                table: "alert_explanations",
                columns: PreviousIdentityColumns,
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_alert_explanations_pending_evaluation",
                table: "alert_explanations",
                columns: PreviousPendingEvaluationColumns,
                unique: true,
                filter: "status = 'PENDING'");

            migrationBuilder.AddCheckConstraint(
                name: "ck_alert_explanations_failure_code",
                table: "alert_explanations",
                sql: "failure_code IS NULL OR failure_code IN ('PROVIDER_UNAVAILABLE', 'PROVIDER_TIMEOUT', 'PROVIDER_REFUSED', 'MALFORMED_OUTPUT', 'NOT_GROUNDED_NUMBER', 'NOT_GROUNDED_RULE', 'TOO_LONG', 'CANCELLED', 'ATTEMPT_LIMIT_REACHED')");
        }
    }
}

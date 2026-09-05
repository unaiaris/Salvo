using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Salvo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Explanations : Migration
    {
        private static readonly string[] IdentityColumns =
        [
            "risk_evaluation_id",
            "provider",
            "template_version",
            "alert_policy_version",
        ];

        private static readonly string[] PendingEvaluationColumns = ["risk_evaluation_id", "provider"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "explanation_id",
                table: "alert_reviews",
                type: "TEXT",
                maxLength: 36,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "alert_explanations",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    risk_evaluation_id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    provider = table.Column<string>(type: "TEXT", maxLength: 9, nullable: false),
                    template_version = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    alert_policy_version = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    provider_version = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    requested_from_alert_id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    status = table.Column<string>(type: "TEXT", maxLength: 7, nullable: false),
                    summary = table.Column<string>(type: "TEXT", maxLength: 1200, nullable: true),
                    referenced_rules_json = table.Column<string>(type: "TEXT", nullable: true),
                    failure_code = table.Column<string>(type: "TEXT", maxLength: 21, nullable: true),
                    failure_detail = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    input_tokens = table.Column<int>(type: "INTEGER", nullable: true),
                    output_tokens = table.Column<int>(type: "INTEGER", nullable: true),
                    attempt_count = table.Column<int>(type: "INTEGER", nullable: false),
                    requested_at_utc = table.Column<string>(type: "TEXT", maxLength: 24, nullable: false),
                    settled_at_utc = table.Column<string>(type: "TEXT", maxLength: 24, nullable: true),
                    row_version = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_alert_explanations", x => x.id);
                    table.CheckConstraint("ck_alert_explanations_attempts", "attempt_count BETWEEN 0 AND 3");
                    table.CheckConstraint("ck_alert_explanations_failure", "failure_code IS NULL OR status = 'FAILED'");
                    table.CheckConstraint("ck_alert_explanations_failure_code", "failure_code IS NULL OR failure_code IN ('PROVIDER_UNAVAILABLE', 'PROVIDER_TIMEOUT', 'PROVIDER_REFUSED', 'MALFORMED_OUTPUT', 'NOT_GROUNDED_NUMBER', 'NOT_GROUNDED_RULE', 'TOO_LONG', 'CANCELLED', 'ATTEMPT_LIMIT_REACHED')");
                    table.CheckConstraint("ck_alert_explanations_provider", "provider IN ('MOCK', 'ANTHROPIC')");
                    table.CheckConstraint("ck_alert_explanations_ready", "(status = 'READY' AND summary IS NOT NULL AND referenced_rules_json IS NOT NULL) OR (status <> 'READY' AND summary IS NULL AND referenced_rules_json IS NULL)");
                    table.CheckConstraint("ck_alert_explanations_requested_at_utc", "length(requested_at_utc) = 24 AND substr(requested_at_utc, 24, 1) = 'Z'");
                    table.CheckConstraint("ck_alert_explanations_settled", "(status = 'PENDING' AND settled_at_utc IS NULL) OR (status <> 'PENDING' AND settled_at_utc IS NOT NULL)");
                    table.CheckConstraint("ck_alert_explanations_settled_at_utc", "settled_at_utc IS NULL OR (length(settled_at_utc) = 24 AND substr(settled_at_utc, 24, 1) = 'Z' AND settled_at_utc >= requested_at_utc)");
                    table.CheckConstraint("ck_alert_explanations_status", "status IN ('PENDING', 'READY', 'FAILED')");
                    table.CheckConstraint("ck_alert_explanations_summary_length", "summary IS NULL OR length(summary) <= 1200");
                    table.CheckConstraint("ck_alert_explanations_tokens", "(input_tokens IS NULL OR input_tokens >= 0) AND (output_tokens IS NULL OR output_tokens >= 0)");
                    table.ForeignKey(
                        name: "FK_alert_explanations_alerts_requested_from_alert_id",
                        column: x => x.requested_from_alert_id,
                        principalTable: "alerts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_alert_explanations_risk_evaluations_risk_evaluation_id",
                        column: x => x.risk_evaluation_id,
                        principalTable: "risk_evaluations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_alert_reviews_explanation_id",
                table: "alert_reviews",
                column: "explanation_id");

            migrationBuilder.CreateIndex(
                name: "ix_alert_explanations_requested_from_alert",
                table: "alert_explanations",
                column: "requested_from_alert_id");

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

            migrationBuilder.AddForeignKey(
                name: "FK_alert_reviews_alert_explanations_explanation_id",
                table: "alert_reviews",
                column: "explanation_id",
                principalTable: "alert_explanations",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_alert_reviews_alert_explanations_explanation_id",
                table: "alert_reviews");

            migrationBuilder.DropTable(
                name: "alert_explanations");

            migrationBuilder.DropIndex(
                name: "IX_alert_reviews_explanation_id",
                table: "alert_reviews");

            migrationBuilder.DropColumn(
                name: "explanation_id",
                table: "alert_reviews");
        }
    }
}

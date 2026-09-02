using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Salvo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RiskEvaluationPersistence : Migration
    {
        private static readonly string[] EvaluationOrderColumns =
        [
            "order_id",
            "created_at_utc"
        ];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "risk_evaluations",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    order_id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    source = table.Column<string>(type: "TEXT", maxLength: 13, nullable: false),
                    rule_config_version = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    score = table.Column<int>(type: "INTEGER", nullable: true),
                    status = table.Column<string>(type: "TEXT", maxLength: 8, nullable: false),
                    signals_json = table.Column<string>(type: "TEXT", nullable: true),
                    evaluation_fingerprint = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    external_evaluation_id = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    error_code = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    created_at_utc = table.Column<string>(type: "TEXT", maxLength: 24, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_risk_evaluations", x => x.id);
                    table.CheckConstraint("ck_risk_evaluations_created_at_utc", "length(created_at_utc) = 24 AND substr(created_at_utc, 24, 1) = 'Z'");
                    table.CheckConstraint("ck_risk_evaluations_fingerprint", "evaluation_fingerprint IS NULL OR (length(evaluation_fingerprint) = 64 AND evaluation_fingerprint NOT GLOB '*[^0-9a-f]*')");
                    table.CheckConstraint("ck_risk_evaluations_local_completeness", "source <> 'LOCAL' OR (rule_config_version IS NOT NULL AND score IS NOT NULL AND signals_json IS NOT NULL AND evaluation_fingerprint IS NOT NULL)");
                    table.CheckConstraint("ck_risk_evaluations_score", "score IS NULL OR (score >= 0 AND score <= 100)");
                    table.CheckConstraint("ck_risk_evaluations_source", "source IN ('LOCAL', 'EXTERNAL_MOCK', 'KOIN_SANDBOX')");
                    table.CheckConstraint("ck_risk_evaluations_status", "status IN ('PENDING', 'APPROVED', 'DENIED', 'ERROR')");
                    table.ForeignKey(
                        name: "FK_risk_evaluations_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "scoring_runs",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    run_sequence = table.Column<long>(type: "INTEGER", nullable: false),
                    rule_config_version = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    started_at_utc = table.Column<string>(type: "TEXT", maxLength: 24, nullable: false),
                    completed_at_utc = table.Column<string>(type: "TEXT", maxLength: 24, nullable: false),
                    order_count = table.Column<int>(type: "INTEGER", nullable: false),
                    evaluations_created = table.Column<int>(type: "INTEGER", nullable: false),
                    evaluations_reused = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scoring_runs", x => x.id);
                    table.CheckConstraint("ck_scoring_runs_completed_at_utc", "length(completed_at_utc) = 24 AND substr(completed_at_utc, 24, 1) = 'Z' AND completed_at_utc >= started_at_utc");
                    table.CheckConstraint("ck_scoring_runs_counts", "order_count >= 0 AND evaluations_created >= 0 AND evaluations_reused >= 0 AND evaluations_created + evaluations_reused = order_count");
                    table.CheckConstraint("ck_scoring_runs_run_sequence", "run_sequence >= 1");
                    table.CheckConstraint("ck_scoring_runs_started_at_utc", "length(started_at_utc) = 24 AND substr(started_at_utc, 24, 1) = 'Z'");
                });

            migrationBuilder.CreateTable(
                name: "run_evaluations",
                columns: table => new
                {
                    run_id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    order_id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    evaluation_id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_run_evaluations", x => new { x.run_id, x.order_id });
                    table.ForeignKey(
                        name: "FK_run_evaluations_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_run_evaluations_risk_evaluations_evaluation_id",
                        column: x => x.evaluation_id,
                        principalTable: "risk_evaluations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_run_evaluations_scoring_runs_run_id",
                        column: x => x.run_id,
                        principalTable: "scoring_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_risk_evaluations_order",
                table: "risk_evaluations",
                columns: EvaluationOrderColumns);

            migrationBuilder.CreateIndex(
                name: "ux_risk_evaluations_local_fingerprint",
                table: "risk_evaluations",
                column: "evaluation_fingerprint",
                unique: true,
                filter: "source = 'LOCAL'");

            migrationBuilder.CreateIndex(
                name: "ix_run_evaluations_evaluation",
                table: "run_evaluations",
                column: "evaluation_id");

            migrationBuilder.CreateIndex(
                name: "ix_run_evaluations_order",
                table: "run_evaluations",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "ux_scoring_runs_run_sequence",
                table: "scoring_runs",
                column: "run_sequence",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "run_evaluations");

            migrationBuilder.DropTable(
                name: "risk_evaluations");

            migrationBuilder.DropTable(
                name: "scoring_runs");
        }
    }
}

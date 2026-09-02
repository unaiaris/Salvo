using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Salvo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AlertsAndReview : Migration
    {
        private static readonly string[] OrderCreatedColumns =
        [
            "order_id",
            "created_at_utc"
        ];

        private static readonly string[] StatusCreatedColumns =
        [
            "status",
            "created_at_utc"
        ];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "alerts_created",
                table: "scoring_runs",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "alerts_skipped_open",
                table: "scoring_runs",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "alerts_skipped_reviewed",
                table: "scoring_runs",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "alerts",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    order_id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    risk_evaluation_id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    risk_score_snapshot = table.Column<int>(type: "INTEGER", nullable: false),
                    signals_snapshot_json = table.Column<string>(type: "TEXT", nullable: false),
                    alert_policy_version = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    status = table.Column<string>(type: "TEXT", maxLength: 14, nullable: false),
                    supersedes_alert_id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: true),
                    created_at_utc = table.Column<string>(type: "TEXT", maxLength: 24, nullable: false),
                    reviewed_at_utc = table.Column<string>(type: "TEXT", maxLength: 24, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_alerts", x => x.id);
                    table.CheckConstraint("ck_alerts_created_at_utc", "length(created_at_utc) = 24 AND substr(created_at_utc, 24, 1) = 'Z'");
                    table.CheckConstraint("ck_alerts_review_consistency", "(status = 'OPEN' AND reviewed_at_utc IS NULL) OR (status <> 'OPEN' AND reviewed_at_utc IS NOT NULL)");
                    table.CheckConstraint("ck_alerts_reviewed_at_utc", "reviewed_at_utc IS NULL OR (length(reviewed_at_utc) = 24 AND substr(reviewed_at_utc, 24, 1) = 'Z' AND reviewed_at_utc >= created_at_utc)");
                    table.CheckConstraint("ck_alerts_risk_score_snapshot", "risk_score_snapshot >= 0 AND risk_score_snapshot <= 100");
                    table.CheckConstraint("ck_alerts_status", "status IN ('OPEN', 'CONFIRMED_SAFE', 'REPORTED_FRAUD')");
                    table.CheckConstraint("ck_alerts_supersedes_other", "supersedes_alert_id IS NULL OR supersedes_alert_id <> id");
                    table.ForeignKey(
                        name: "FK_alerts_alerts_supersedes_alert_id",
                        column: x => x.supersedes_alert_id,
                        principalTable: "alerts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_alerts_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_alerts_risk_evaluations_risk_evaluation_id",
                        column: x => x.risk_evaluation_id,
                        principalTable: "risk_evaluations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "alert_reviews",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    alert_id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    previous_status = table.Column<string>(type: "TEXT", maxLength: 14, nullable: false),
                    new_status = table.Column<string>(type: "TEXT", maxLength: 14, nullable: false),
                    note = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    reviewed_at_utc = table.Column<string>(type: "TEXT", maxLength: 24, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_alert_reviews", x => x.id);
                    table.CheckConstraint("ck_alert_reviews_new_status", "new_status IN ('CONFIRMED_SAFE', 'REPORTED_FRAUD')");
                    table.CheckConstraint("ck_alert_reviews_previous_status", "previous_status IN ('OPEN', 'CONFIRMED_SAFE', 'REPORTED_FRAUD')");
                    table.CheckConstraint("ck_alert_reviews_reviewed_at_utc", "length(reviewed_at_utc) = 24 AND substr(reviewed_at_utc, 24, 1) = 'Z'");
                    table.CheckConstraint("ck_alert_reviews_transition", "new_status <> previous_status");
                    table.ForeignKey(
                        name: "FK_alert_reviews_alerts_alert_id",
                        column: x => x.alert_id,
                        principalTable: "alerts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_scoring_runs_alert_counts",
                table: "scoring_runs",
                sql: "alerts_created >= 0 AND alerts_skipped_open >= 0 AND alerts_skipped_reviewed >= 0 AND alerts_created + alerts_skipped_open + alerts_skipped_reviewed <= order_count");

            migrationBuilder.CreateIndex(
                name: "ux_alert_reviews_alert",
                table: "alert_reviews",
                column: "alert_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_alerts_order_created",
                table: "alerts",
                columns: OrderCreatedColumns);

            migrationBuilder.CreateIndex(
                name: "ix_alerts_status_created",
                table: "alerts",
                columns: StatusCreatedColumns);

            migrationBuilder.CreateIndex(
                name: "IX_alerts_supersedes_alert_id",
                table: "alerts",
                column: "supersedes_alert_id");

            migrationBuilder.CreateIndex(
                name: "ux_alerts_open_order",
                table: "alerts",
                column: "order_id",
                unique: true,
                filter: "status = 'OPEN'");

            migrationBuilder.CreateIndex(
                name: "ux_alerts_risk_evaluation",
                table: "alerts",
                column: "risk_evaluation_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "alert_reviews");

            migrationBuilder.DropTable(
                name: "alerts");

            migrationBuilder.DropCheckConstraint(
                name: "ck_scoring_runs_alert_counts",
                table: "scoring_runs");

            migrationBuilder.DropColumn(
                name: "alerts_created",
                table: "scoring_runs");

            migrationBuilder.DropColumn(
                name: "alerts_skipped_open",
                table: "scoring_runs");

            migrationBuilder.DropColumn(
                name: "alerts_skipped_reviewed",
                table: "scoring_runs");
        }
    }
}

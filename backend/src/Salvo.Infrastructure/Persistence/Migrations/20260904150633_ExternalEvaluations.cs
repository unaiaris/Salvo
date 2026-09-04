using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Salvo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ExternalEvaluations : Migration
    {
        private static readonly string[] OrderRequestedColumns = ["order_id", "requested_at_utc"];

        private static readonly string[] ProviderReferenceColumns = ["provider", "reference_id"];

        private static readonly string[] StatusRequestedColumns = ["status", "requested_at_utc"];

        private static readonly string[] PendingOrderColumns = ["order_id", "provider"];

        private static readonly string[] ProviderIdentifierColumns = ["provider", "external_evaluation_id"];
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_risk_evaluations_source",
                table: "risk_evaluations");

            migrationBuilder.DropCheckConstraint(
                name: "ck_risk_evaluations_status",
                table: "risk_evaluations");

            migrationBuilder.DropColumn(
                name: "error_code",
                table: "risk_evaluations");

            migrationBuilder.DropColumn(
                name: "external_evaluation_id",
                table: "risk_evaluations");

            migrationBuilder.CreateTable(
                name: "external_evaluations",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    order_id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    provider = table.Column<string>(type: "TEXT", maxLength: 13, nullable: false),
                    reference_id = table.Column<string>(type: "TEXT", maxLength: 129, nullable: false),
                    external_evaluation_id = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    status = table.Column<string>(type: "TEXT", maxLength: 8, nullable: false),
                    score = table.Column<int>(type: "INTEGER", nullable: true),
                    error_code = table.Column<string>(type: "TEXT", maxLength: 17, nullable: true),
                    last_error_code = table.Column<string>(type: "TEXT", maxLength: 17, nullable: true),
                    attempt_count = table.Column<int>(type: "INTEGER", nullable: false),
                    settled_by = table.Column<string>(type: "TEXT", maxLength: 14, nullable: true),
                    requested_at_utc = table.Column<string>(type: "TEXT", maxLength: 24, nullable: false),
                    updated_at_utc = table.Column<string>(type: "TEXT", maxLength: 24, nullable: false),
                    settled_at_utc = table.Column<string>(type: "TEXT", maxLength: 24, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_external_evaluations", x => x.id);
                    table.CheckConstraint("ck_external_evaluations_attempt_count", "attempt_count >= 0");
                    table.CheckConstraint("ck_external_evaluations_error_code", "error_code IS NULL OR error_code IN ('UNREACHABLE', 'PROVIDER_REJECTED', 'TIMEOUT', 'PROVIDER_ERROR', 'INVALID_RESPONSE')");
                    table.CheckConstraint("ck_external_evaluations_error_status", "error_code IS NULL OR status = 'ERROR'");
                    table.CheckConstraint("ck_external_evaluations_last_error_code", "last_error_code IS NULL OR last_error_code IN ('UNREACHABLE', 'PROVIDER_REJECTED', 'TIMEOUT', 'PROVIDER_ERROR', 'INVALID_RESPONSE')");
                    table.CheckConstraint("ck_external_evaluations_provider", "provider IN ('EXTERNAL_MOCK', 'KOIN_SANDBOX')");
                    table.CheckConstraint("ck_external_evaluations_requested_at_utc", "length(requested_at_utc) = 24 AND substr(requested_at_utc, 24, 1) = 'Z'");
                    table.CheckConstraint("ck_external_evaluations_score", "score IS NULL OR score >= 0");
                    table.CheckConstraint("ck_external_evaluations_settled_at_utc", "settled_at_utc IS NULL OR (length(settled_at_utc) = 24 AND substr(settled_at_utc, 24, 1) = 'Z' AND settled_at_utc >= requested_at_utc)");
                    table.CheckConstraint("ck_external_evaluations_settled_by", "settled_by IS NULL OR settled_by IN ('SYNC', 'CALLBACK', 'RECONCILIATION')");
                    table.CheckConstraint("ck_external_evaluations_settled_by_consistency", "(settled_by IS NULL AND settled_at_utc IS NULL) OR (settled_by IS NOT NULL AND settled_at_utc IS NOT NULL)");
                    table.CheckConstraint("ck_external_evaluations_settlement", "(status = 'PENDING' AND settled_at_utc IS NULL) OR (status <> 'PENDING' AND settled_at_utc IS NOT NULL)");
                    table.CheckConstraint("ck_external_evaluations_status", "status IN ('PENDING', 'APPROVED', 'DENIED', 'ERROR')");
                    table.CheckConstraint("ck_external_evaluations_updated_at_utc", "length(updated_at_utc) = 24 AND substr(updated_at_utc, 24, 1) = 'Z' AND updated_at_utc >= requested_at_utc");
                    table.ForeignKey(
                        name: "FK_external_evaluations_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_risk_evaluations_source",
                table: "risk_evaluations",
                sql: "source = 'LOCAL'");

            migrationBuilder.AddCheckConstraint(
                name: "ck_risk_evaluations_status",
                table: "risk_evaluations",
                sql: "status IN ('APPROVED', 'DENIED')");

            migrationBuilder.CreateIndex(
                name: "ix_external_evaluations_order_requested",
                table: "external_evaluations",
                columns: OrderRequestedColumns);

            migrationBuilder.CreateIndex(
                name: "ix_external_evaluations_provider_reference",
                table: "external_evaluations",
                columns: ProviderReferenceColumns);

            migrationBuilder.CreateIndex(
                name: "ix_external_evaluations_status_requested",
                table: "external_evaluations",
                columns: StatusRequestedColumns);

            migrationBuilder.CreateIndex(
                name: "ux_external_evaluations_pending_order",
                table: "external_evaluations",
                columns: PendingOrderColumns,
                unique: true,
                filter: "status = 'PENDING'");

            migrationBuilder.CreateIndex(
                name: "ux_external_evaluations_provider_identifier",
                table: "external_evaluations",
                columns: ProviderIdentifierColumns,
                unique: true,
                filter: "external_evaluation_id IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "external_evaluations");

            migrationBuilder.DropCheckConstraint(
                name: "ck_risk_evaluations_source",
                table: "risk_evaluations");

            migrationBuilder.DropCheckConstraint(
                name: "ck_risk_evaluations_status",
                table: "risk_evaluations");

            migrationBuilder.AddColumn<string>(
                name: "error_code",
                table: "risk_evaluations",
                type: "TEXT",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "external_evaluation_id",
                table: "risk_evaluations",
                type: "TEXT",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_risk_evaluations_source",
                table: "risk_evaluations",
                sql: "source IN ('LOCAL', 'EXTERNAL_MOCK', 'KOIN_SANDBOX')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_risk_evaluations_status",
                table: "risk_evaluations",
                sql: "status IN ('PENDING', 'APPROVED', 'DENIED', 'ERROR')");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Salvo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CallbackReceipts : Migration
    {
        private static readonly string[] ProviderIdentifierColumns = ["provider", "external_evaluation_id"];

        private static readonly string[] ProviderReferenceColumns = ["provider", "reference_id"];

        private static readonly string[] ProviderStatusColumns = ["provider", "status"];

        private static readonly string[] ProviderKeyColumns = ["provider", "deduplication_key"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "callback_receipts",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    provider = table.Column<string>(type: "TEXT", maxLength: 13, nullable: false),
                    deduplication_key = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    external_evaluation_id = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    reference_id = table.Column<string>(type: "TEXT", maxLength: 129, nullable: true),
                    reported_status = table.Column<string>(type: "TEXT", maxLength: 8, nullable: false),
                    reported_score = table.Column<int>(type: "INTEGER", nullable: true),
                    provider_instant_utc = table.Column<string>(type: "TEXT", maxLength: 24, nullable: true),
                    status = table.Column<string>(type: "TEXT", maxLength: 11, nullable: false),
                    replay_count = table.Column<int>(type: "INTEGER", nullable: false),
                    received_at_utc = table.Column<string>(type: "TEXT", maxLength: 24, nullable: false),
                    last_seen_at_utc = table.Column<string>(type: "TEXT", maxLength: 24, nullable: false),
                    processed_at_utc = table.Column<string>(type: "TEXT", maxLength: 24, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_callback_receipts", x => x.id);
                    table.CheckConstraint("ck_callback_receipts_correlation", "external_evaluation_id IS NOT NULL OR reference_id IS NOT NULL");
                    table.CheckConstraint("ck_callback_receipts_last_seen_at_utc", "length(last_seen_at_utc) = 24 AND substr(last_seen_at_utc, 24, 1) = 'Z' AND last_seen_at_utc >= received_at_utc");
                    table.CheckConstraint("ck_callback_receipts_processed", "(status = 'UNMATCHED' AND processed_at_utc IS NULL) OR (status <> 'UNMATCHED' AND processed_at_utc IS NOT NULL)");
                    table.CheckConstraint("ck_callback_receipts_processed_at_utc", "processed_at_utc IS NULL OR (length(processed_at_utc) = 24 AND substr(processed_at_utc, 24, 1) = 'Z')");
                    table.CheckConstraint("ck_callback_receipts_provider", "provider IN ('EXTERNAL_MOCK', 'KOIN_SANDBOX')");
                    table.CheckConstraint("ck_callback_receipts_provider_instant_utc", "provider_instant_utc IS NULL OR (length(provider_instant_utc) = 24 AND substr(provider_instant_utc, 24, 1) = 'Z')");
                    table.CheckConstraint("ck_callback_receipts_received_at_utc", "length(received_at_utc) = 24 AND substr(received_at_utc, 24, 1) = 'Z'");
                    table.CheckConstraint("ck_callback_receipts_replay_count", "replay_count >= 0");
                    table.CheckConstraint("ck_callback_receipts_reported_score", "reported_score IS NULL OR reported_score >= 0");
                    table.CheckConstraint("ck_callback_receipts_reported_status", "reported_status IN ('PENDING', 'APPROVED', 'DENIED', 'ERROR')");
                    table.CheckConstraint("ck_callback_receipts_status", "status IN ('APPLIED', 'NO_OP', 'SUPERSEDED', 'CONFLICTING', 'UNMATCHED')");
                });

            migrationBuilder.CreateIndex(
                name: "ix_callback_receipts_provider_identifier",
                table: "callback_receipts",
                columns: ProviderIdentifierColumns);

            migrationBuilder.CreateIndex(
                name: "ix_callback_receipts_provider_reference",
                table: "callback_receipts",
                columns: ProviderReferenceColumns);

            migrationBuilder.CreateIndex(
                name: "ix_callback_receipts_provider_status",
                table: "callback_receipts",
                columns: ProviderStatusColumns);

            migrationBuilder.CreateIndex(
                name: "ux_callback_receipts_provider_key",
                table: "callback_receipts",
                columns: ProviderKeyColumns,
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "callback_receipts");
        }
    }
}

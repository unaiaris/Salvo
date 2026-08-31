using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Salvo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialOrderContractData : Migration
    {
        private static readonly string[] BuyerCurrencyHistoryColumns =
        [
            "merchant_id",
            "buyer_reference_id",
            "currency_code",
            "occurred_at_utc",
            "merchant_reference_id"
        ];

        private static readonly string[] ChronologicalColumns =
        [
            "occurred_at_utc",
            "merchant_id",
            "merchant_reference_id"
        ];

        private static readonly string[] MerchantReferenceColumns =
        [
            "merchant_id",
            "merchant_reference_id"
        ];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "orders",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    merchant_id = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    merchant_reference_id = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    buyer_reference_id = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    occurred_at_utc = table.Column<string>(type: "TEXT", maxLength: 24, nullable: false),
                    amount_cents = table.Column<long>(type: "INTEGER", nullable: false),
                    currency_code = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    country_code = table.Column<string>(type: "TEXT", maxLength: 2, nullable: false),
                    city = table.Column<string>(type: "TEXT", maxLength: 80, nullable: true),
                    channel = table.Column<string>(type: "TEXT", maxLength: 11, nullable: true),
                    device_session_id = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    created_at_utc = table.Column<string>(type: "TEXT", maxLength: 24, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_orders", x => x.id);
                    table.CheckConstraint("ck_orders_amount_cents", "amount_cents >= 1 AND amount_cents <= 1000000000000");
                    table.CheckConstraint("ck_orders_buyer_reference_id", "length(buyer_reference_id) BETWEEN 5 AND 64 AND buyer_reference_id GLOB 'BUY_*' AND buyer_reference_id NOT GLOB '*[^A-Z0-9_-]*'");
                    table.CheckConstraint("ck_orders_channel", "channel IS NULL OR channel IN ('WEB', 'MOBILE_APP', 'MARKETPLACE')");
                    table.CheckConstraint("ck_orders_city", "city IS NULL OR length(city) <= 80");
                    table.CheckConstraint("ck_orders_country_code", "length(country_code) = 2 AND country_code NOT GLOB '*[^A-Z]*'");
                    table.CheckConstraint("ck_orders_created_at_utc", "length(created_at_utc) = 24 AND substr(created_at_utc, 24, 1) = 'Z'");
                    table.CheckConstraint("ck_orders_currency_code", "currency_code IN ('UYU', 'BRL', 'USD')");
                    table.CheckConstraint("ck_orders_device_session_id", "device_session_id IS NULL OR (length(device_session_id) BETWEEN 5 AND 64 AND device_session_id GLOB 'DEV_*' AND device_session_id NOT GLOB '*[^A-Z0-9_-]*')");
                    table.CheckConstraint("ck_orders_merchant_id", "length(merchant_id) BETWEEN 5 AND 64 AND merchant_id GLOB 'MER_*' AND merchant_id NOT GLOB '*[^A-Z0-9_-]*'");
                    table.CheckConstraint("ck_orders_merchant_reference_id", "length(merchant_reference_id) BETWEEN 5 AND 64 AND merchant_reference_id GLOB 'ORD_*' AND merchant_reference_id NOT GLOB '*[^A-Z0-9_-]*'");
                    table.CheckConstraint("ck_orders_occurred_at_utc", "length(occurred_at_utc) = 24 AND substr(occurred_at_utc, 24, 1) = 'Z'");
                });

            migrationBuilder.CreateTable(
                name: "order_evaluation_labels",
                columns: table => new
                {
                    order_id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    is_fraud_label = table.Column<bool>(type: "INTEGER", nullable: false),
                    created_at_utc = table.Column<string>(type: "TEXT", maxLength: 24, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_evaluation_labels", x => x.order_id);
                    table.CheckConstraint("ck_order_evaluation_labels_created_at_utc", "length(created_at_utc) = 24 AND substr(created_at_utc, 24, 1) = 'Z'");
                    table.CheckConstraint("ck_order_evaluation_labels_is_fraud_label", "is_fraud_label IN (0, 1)");
                    table.ForeignKey(
                        name: "FK_order_evaluation_labels_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_orders_buyer_currency_history",
                table: "orders",
                columns: BuyerCurrencyHistoryColumns);

            migrationBuilder.CreateIndex(
                name: "ix_orders_chronological",
                table: "orders",
                columns: ChronologicalColumns);

            migrationBuilder.CreateIndex(
                name: "ux_orders_merchant_reference",
                table: "orders",
                columns: MerchantReferenceColumns,
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "order_evaluation_labels");

            migrationBuilder.DropTable(
                name: "orders");
        }
    }
}

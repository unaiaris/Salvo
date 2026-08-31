using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Salvo.Domain.Orders;

namespace Salvo.Infrastructure.Persistence.Configurations;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable(
            "orders",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_orders_amount_cents",
                    "amount_cents >= 1 AND amount_cents <= 1000000000000");
                table.HasCheckConstraint(
                    "ck_orders_currency_code",
                    "currency_code IN ('UYU', 'BRL', 'USD')");
                table.HasCheckConstraint(
                    "ck_orders_country_code",
                    "length(country_code) = 2 AND country_code NOT GLOB '*[^A-Z]*'");
                table.HasCheckConstraint(
                    "ck_orders_channel",
                    "channel IS NULL OR channel IN ('WEB', 'MOBILE_APP', 'MARKETPLACE')");
                table.HasCheckConstraint(
                    "ck_orders_occurred_at_utc",
                    "length(occurred_at_utc) = 24 AND substr(occurred_at_utc, 24, 1) = 'Z'");
                table.HasCheckConstraint(
                    "ck_orders_created_at_utc",
                    "length(created_at_utc) = 24 AND substr(created_at_utc, 24, 1) = 'Z'");
                table.HasCheckConstraint(
                    "ck_orders_merchant_id",
                    "length(merchant_id) BETWEEN 5 AND 64 AND merchant_id GLOB 'MER_*' AND merchant_id NOT GLOB '*[^A-Z0-9_-]*'");
                table.HasCheckConstraint(
                    "ck_orders_merchant_reference_id",
                    "length(merchant_reference_id) BETWEEN 5 AND 64 AND merchant_reference_id GLOB 'ORD_*' AND merchant_reference_id NOT GLOB '*[^A-Z0-9_-]*'");
                table.HasCheckConstraint(
                    "ck_orders_buyer_reference_id",
                    "length(buyer_reference_id) BETWEEN 5 AND 64 AND buyer_reference_id GLOB 'BUY_*' AND buyer_reference_id NOT GLOB '*[^A-Z0-9_-]*'");
                table.HasCheckConstraint(
                    "ck_orders_device_session_id",
                    "device_session_id IS NULL OR (length(device_session_id) BETWEEN 5 AND 64 AND device_session_id GLOB 'DEV_*' AND device_session_id NOT GLOB '*[^A-Z0-9_-]*')");
                table.HasCheckConstraint(
                    "ck_orders_city",
                    "city IS NULL OR length(city) <= 80");
            });

        builder.HasKey(order => order.Id);

        builder.Property(order => order.Id)
            .HasColumnName("id")
            .HasConversion<string>()
            .HasMaxLength(36)
            .ValueGeneratedNever();
        builder.Property(order => order.MerchantId)
            .HasColumnName("merchant_id")
            .HasMaxLength(Order.MaximumIdentifierLength)
            .IsRequired();
        builder.Property(order => order.MerchantReferenceId)
            .HasColumnName("merchant_reference_id")
            .HasMaxLength(Order.MaximumIdentifierLength)
            .IsRequired();
        builder.Property(order => order.BuyerReferenceId)
            .HasColumnName("buyer_reference_id")
            .HasMaxLength(Order.MaximumIdentifierLength)
            .IsRequired();
        builder.Property(order => order.OccurredAt)
            .HasColumnName("occurred_at_utc")
            .HasConversion<UtcDateTimeOffsetConverter>()
            .HasMaxLength(24)
            .IsRequired();
        builder.Property(order => order.AmountCents)
            .HasColumnName("amount_cents")
            .IsRequired();
        builder.Property(order => order.CurrencyCode)
            .HasColumnName("currency_code")
            .HasMaxLength(3)
            .IsRequired();
        builder.Property(order => order.CountryCode)
            .HasColumnName("country_code")
            .HasMaxLength(2)
            .IsRequired();
        builder.Property(order => order.City)
            .HasColumnName("city")
            .HasMaxLength(Order.MaximumCityLength);
        builder.Property(order => order.Channel)
            .HasColumnName("channel")
            .HasConversion<OrderChannelConverter>()
            .HasMaxLength(11);
        builder.Property(order => order.DeviceSessionId)
            .HasColumnName("device_session_id")
            .HasMaxLength(Order.MaximumIdentifierLength);
        builder.Property(order => order.CreatedAt)
            .HasColumnName("created_at_utc")
            .HasConversion<UtcDateTimeOffsetConverter>()
            .HasMaxLength(24)
            .IsRequired();

        builder.HasIndex(order => new { order.MerchantId, order.MerchantReferenceId })
            .IsUnique()
            .HasDatabaseName("ux_orders_merchant_reference");
        builder.HasIndex(order => new
            {
                order.OccurredAt,
                order.MerchantId,
                order.MerchantReferenceId,
            })
            .HasDatabaseName("ix_orders_chronological");
        builder.HasIndex(order => new
            {
                order.MerchantId,
                order.BuyerReferenceId,
                order.CurrencyCode,
                order.OccurredAt,
                order.MerchantReferenceId,
            })
            .HasDatabaseName("ix_orders_buyer_currency_history");
    }
}

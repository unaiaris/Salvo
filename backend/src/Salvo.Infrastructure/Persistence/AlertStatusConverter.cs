using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Salvo.Domain.Alerts;

namespace Salvo.Infrastructure.Persistence;

public sealed class AlertStatusConverter()
    : ValueConverter<AlertStatus, string>(
        value => AlertWireNames.ToWire(value),
        value => AlertWireNames.ParseStatus(value));

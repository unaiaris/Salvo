using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Salvo.Domain.External;

namespace Salvo.Infrastructure.Persistence;

public sealed class CallbackReceiptStatusConverter()
    : ValueConverter<CallbackReceiptStatus, string>(
        value => CallbackReceiptWireNames.ToWire(value),
        value => CallbackReceiptWireNames.Parse(value));

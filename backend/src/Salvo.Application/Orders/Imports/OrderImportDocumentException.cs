namespace Salvo.Application.Orders.Importing;

public enum OrderImportDocumentFailure
{
    InvalidDocument,
    TooManyRecords,
    UnsupportedFormat,
}

public sealed class OrderImportDocumentException : Exception
{
    public OrderImportDocumentException(
        string code,
        string message,
        OrderImportDocumentFailure failure = OrderImportDocumentFailure.InvalidDocument,
        Exception? innerException = null)
        : base(message, innerException)
    {
        Code = code;
        Failure = failure;
    }

    public string Code { get; }

    public OrderImportDocumentFailure Failure { get; }
}

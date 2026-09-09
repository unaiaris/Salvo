namespace Salvo.Application.Orders.Importing;

public enum OrderImportDocumentFailure
{
    InvalidDocument,
    TooManyRecords,
    UnsupportedFormat,

    /// <summary>
    /// The deployment holds as many orders as it is willing to. Not a defect of the file: the same
    /// file would be accepted on an instance with room, which is why it answers 409 and not 4xx of
    /// the request.
    /// </summary>
    CapacityReached,
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

using System.Runtime.Serialization;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Kantilever.WarehouseService.Exceptions
{
    [Serializable]
    public class ArticleNotFoundException : Exception
    {
        public override string Message => Error?.Message ?? "";
        public Error? Error { get; }

        public ArticleNotFoundException()
        {
        }

        public ArticleNotFoundException(long articleNumber)
        {
            Error = Errors.ArticleNotFound(articleNumber);
        }

        public ArticleNotFoundException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}
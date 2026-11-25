using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace Kantilever.WarehouseService.Exceptions
{
    public static class Errors
    {
        public static Error ArticleNotFound(long articleNumber)
        {
            return new Error
            (
                Code: "MAG001",
                Message: $"The article with ArticleNumber {articleNumber} could not be found."
            );
        }

        public static Error InsufficientStock(long articleNumber, int availableStock)
        {
            return new Error
            (
                Code: "MAG002",
                Message: $"For article with ArticleNumber {articleNumber} the available stock of {availableStock} is insufficient."
            );
        }

        public static object QuantityCannotBeNegative(int quantity)
        {
            return new Error
            (
                Code: "MAG003",
                Message: $"The quantity ({quantity}) cannot be negative."
            );
        }

        public static Error InsufficientReservedStock(long articleNumber, int reservedStock)
        {
            return new Error
            (
                Code: "MAG004",
                Message: $"For article with ArticleNumber {articleNumber} the reserved stock of {reservedStock} is insufficient."
            );
        }

    }
}

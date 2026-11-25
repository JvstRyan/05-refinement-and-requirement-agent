using System.Runtime.Serialization;

namespace Kantilever.WarehouseService.Exceptions
{
    [Serializable]
    public class FunctionalException : Exception
    {
        public List<Error> Errors { get; private set;  }

        public FunctionalException()
        {
        }

        public FunctionalException(List<Error> errors)
        {
            Errors = errors;
        }

        public FunctionalException(string? message) : base(message)
        {
        }

        public FunctionalException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}
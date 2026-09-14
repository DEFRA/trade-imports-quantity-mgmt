using System;

namespace Infrastructure.Messaging.Exceptions
{
    public sealed class InvalidMessageCompressionException : Exception
    {
        public InvalidMessageCompressionException() { }

        public InvalidMessageCompressionException(string? message)
            : base(message) { }

        public InvalidMessageCompressionException(string? message, Exception? inner)
            : base(message, inner) { }
    }
}

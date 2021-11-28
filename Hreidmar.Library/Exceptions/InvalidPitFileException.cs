using System;

namespace Hreidmar.Library.Exceptions
{
    public class InvalidPitFileException : Exception {
        public InvalidPitFileException(string message) : base(message) { }
    }
}
using System;

namespace Hreidmar.Enigma.Exceptions
{
    public class InvalidPitFileException : Exception {
        public InvalidPitFileException(string message) : base(message) { }
    }
}
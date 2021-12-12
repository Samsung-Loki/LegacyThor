using System;

namespace Hreidmar.Enigma.Exceptions
{
    internal class DeviceConnectionFailedException : Exception {
        public DeviceConnectionFailedException(string message) : base(message) { }
    }
}
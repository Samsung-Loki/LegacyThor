using System;

namespace Hreidmar.Library.Exceptions
{
    internal class DeviceConnectionFailedException : Exception {
        public DeviceConnectionFailedException(string message) : base(message) { }
    }
}
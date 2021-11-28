using System;

namespace Hreidmar.Library.Exceptions
{
    public class DeviceNotFoundException : Exception {
        public DeviceNotFoundException(string message) : base(message) { }
    }
}
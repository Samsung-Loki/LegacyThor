using System;

namespace Hreidmar.Enigma.Exceptions
{
    public class DeviceNotFoundException : Exception {
        public DeviceNotFoundException(string message) : base(message) { }
    }
}
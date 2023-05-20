using System;

namespace RGB.NET.Core;

public class RGBDeviceAddedEventArgs : EventArgs
{
    public IRGBDevice Device { get; }

    public RGBDeviceAddedEventArgs(IRGBDevice device) => this.Device = device;
}

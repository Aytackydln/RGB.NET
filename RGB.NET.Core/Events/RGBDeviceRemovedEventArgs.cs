using System;

namespace RGB.NET.Core;

public class RGBDeviceRemovedEventArgs : EventArgs
{
    public IRGBDevice Device { get; }

    public RGBDeviceRemovedEventArgs(IRGBDevice device) => this.Device = device;
}

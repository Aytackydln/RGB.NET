﻿using System;
using RGB.NET.Core;
using RGB.NET.Devices.Wooting.Native;

namespace RGB.NET.Devices.Wooting.Generic;

/// <inheritdoc />
/// <summary>
/// Represents the update-queue performing updates for cooler master devices.
/// </summary>
public class WootingUpdateQueue : UpdateQueue
{
    #region Properties & Fields
    private readonly byte _deviceid;
    #endregion
    
    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="WootingUpdateQueue"/> class.
    /// </summary>
    /// <param name="updateTrigger">The update trigger used by this queue.</param>
    public WootingUpdateQueue(IDeviceUpdateTrigger updateTrigger, byte deviceId)
        : base(updateTrigger)
    {
        this._deviceid = deviceId;
    }

    #endregion

    #region Methods

    /// <inheritdoc />
    protected override void Update(in ReadOnlySpan<(object key, Color color)> dataSet)
    {
        lock (_WootingSDK.SdkLock)
        {
            _WootingSDK.SelectDevice(_deviceid);
            
            foreach ((object key, Color color) in dataSet)
            {
                (int row, int column) = ((int, int))key;
                _WootingSDK.ArraySetSingle((byte)row, (byte)column, color.GetR(), color.GetG(), color.GetB());
            }

            _WootingSDK.ArrayUpdateKeyboard();
        }
    }

    #endregion
}
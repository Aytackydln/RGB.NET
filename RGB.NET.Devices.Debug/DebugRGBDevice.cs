﻿using System;
using System.Collections.Generic;
using RGB.NET.Core;
using RGB.NET.Core.Layout;

namespace RGB.NET.Devices.Debug
{
    /// <inheritdoc cref="AbstractRGBDevice{TDeviceInfo}" />
    /// <summary>
    /// Represents a debug device.
    /// </summary>
    public class DebugRGBDevice : AbstractRGBDevice<DebugRGBDeviceInfo>, IUnknownDevice
    {
        #region Properties & Fields

        /// <inheritdoc />
        public override DebugRGBDeviceInfo DeviceInfo { get; }

        /// <summary>
        /// Gets the path of the layout used to mock this <see cref="DebugRGBDevice"/>
        /// </summary>
        public string LayoutPath { get; }
        
        private Action<IEnumerable<Led>> _updateLedsAction;

        #endregion

        #region Constructors
        /// <summary>
        /// Internal constructor of <see cref="DebugRGBDeviceInfo"/>.
        /// </summary>
        internal DebugRGBDevice(string layoutPath, Action<IEnumerable<Led>> updateLedsAction = null)
        {
            this.LayoutPath = layoutPath;
            this._updateLedsAction = updateLedsAction;

            DeviceLayout layout = DeviceLayout.Load(layoutPath);
            DeviceInfo = new DebugRGBDeviceInfo(layout.Type, layout.Vendor, layout.Model, layout.Lighting);
        }

        #endregion

        #region Methods

        internal void Initialize(string layoutPath, string imageLayout) => ApplyLayoutFromFile(layoutPath, imageLayout, true);

        /// <inheritdoc />
        protected override void UpdateLeds(IEnumerable<Led> ledsToUpdate) => _updateLedsAction?.Invoke(ledsToUpdate);

        #endregion
    }
}

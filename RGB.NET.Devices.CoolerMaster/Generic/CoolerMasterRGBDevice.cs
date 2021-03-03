﻿using System;
using System.Collections.Generic;
using System.Linq;
using RGB.NET.Core;
using RGB.NET.Devices.CoolerMaster.Native;

namespace RGB.NET.Devices.CoolerMaster
{
    /// <inheritdoc cref="AbstractRGBDevice{TDeviceInfo}" />
    /// <inheritdoc cref="ICoolerMasterRGBDevice" />
    /// <summary>
    /// Represents a generic CoolerMaster-device. (keyboard, mouse, headset, mousepad).
    /// </summary>
    public abstract class CoolerMasterRGBDevice<TDeviceInfo> : AbstractRGBDevice<TDeviceInfo>, ICoolerMasterRGBDevice
        where TDeviceInfo : CoolerMasterRGBDeviceInfo
    {
        #region Properties & Fields

        /// <inheritdoc />
        /// <summary>
        /// Gets information about the <see cref="T:RGB.NET.Devices.CoolerMaster.CoolerMasterRGBDevice" />.
        /// </summary>
        public override TDeviceInfo DeviceInfo { get; }

        /// <summary>
        /// Gets or sets the update queue performing updates for this device.
        /// </summary>
        // ReSharper disable once MemberCanBePrivate.Global
        protected CoolerMasterUpdateQueue? UpdateQueue { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="CoolerMasterRGBDevice{TDeviceInfo}"/> class.
        /// </summary>
        /// <param name="info">The generic information provided by CoolerMaster for the device.</param>
        protected CoolerMasterRGBDevice(TDeviceInfo info)
        {
            this.DeviceInfo = info;
        }

        #endregion

        #region Methods


        /// <summary>
        /// Initializes the device.
        /// </summary>
        public void Initialize(IDeviceUpdateTrigger updateTrigger)
        {
            InitializeLayout();
            
            UpdateQueue = new CoolerMasterUpdateQueue(updateTrigger, DeviceInfo.DeviceIndex);
        }

        /// <summary>
        /// Initializes the <see cref="Led"/> and <see cref="Size"/> of the device.
        /// </summary>
        protected abstract void InitializeLayout();

        /// <inheritdoc />
        protected override void UpdateLeds(IEnumerable<Led> ledsToUpdate) => UpdateQueue?.SetData(GetUpdateData(ledsToUpdate));

        /// <inheritdoc cref="IDisposable.Dispose" />
        /// <inheritdoc cref="AbstractRGBDevice{TDeviceInfo}.Dispose" />
        public override void Dispose()
        {
            try { UpdateQueue?.Dispose(); }
            catch { /* at least we tried */ }

            _CoolerMasterSDK.EnableLedControl(false, DeviceInfo.DeviceIndex);

            base.Dispose();
        }

        #endregion
    }
}

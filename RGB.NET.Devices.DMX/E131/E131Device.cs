﻿using System;
using System.Collections.Generic;
using System.Linq;
using RGB.NET.Core;

namespace RGB.NET.Devices.DMX.E131
{
    /// <summary>
    /// Represents a E1.31-DXM-device.
    /// </summary>
    public class E131Device : AbstractRGBDevice<E131DeviceInfo>, IUnknownDevice
    {
        #region Properties & Fields

        /// <inheritdoc />
        public override E131DeviceInfo DeviceInfo { get; }

        private readonly Dictionary<LedId, List<(int channel, Func<Color, byte> getValueFunc)>> _ledMappings;

        private E131UpdateQueue? _updateQueue;

        #endregion

        #region Constructors

        /// <inheritdoc />
        internal E131Device(E131DeviceInfo deviceInfo, Dictionary<LedId, List<(int channel, Func<Color, byte> getValueFunc)>> ledMappings)
        {
            this.DeviceInfo = deviceInfo;
            this._ledMappings = ledMappings;
        }

        #endregion

        #region Methods

        internal void Initialize(IDeviceUpdateTrigger updateTrigger)
        {
            int count = 0;
            foreach (LedId id in _ledMappings.Keys)
                AddLed(id, new Point((count++) * 10, 0), new Size(10, 10));
            
            _updateQueue = new E131UpdateQueue(updateTrigger, DeviceInfo.Hostname, DeviceInfo.Port);
            _updateQueue.DataPacket.SetCID(DeviceInfo.CID);
            _updateQueue.DataPacket.SetUniverse(DeviceInfo.Universe);
        }

        /// <inheritdoc />
        protected override object GetLedCustomData(LedId ledId) => new LedChannelMapping(_ledMappings[ledId]);


        /// <inheritdoc />
        protected override void UpdateLeds(IEnumerable<Led> ledsToUpdate) => _updateQueue?.SetData(ledsToUpdate.Where(x => x.Color.A > 0));

        /// <inheritdoc />
        public override void Dispose()
        {
            try { _updateQueue?.Dispose(); }
            catch { /* at least we tried */ }

            base.Dispose();
        }

        #endregion
    }
}

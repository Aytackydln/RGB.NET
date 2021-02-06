﻿// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using RGB.NET.Core;
using RGB.NET.Devices.Corsair.Native;

namespace RGB.NET.Devices.Corsair
{
    /// <inheritdoc />
    /// <summary>
    /// Represents a device provider responsible for corsair (CUE) devices.
    /// </summary>
    public class CorsairDeviceProvider : IRGBDeviceProvider
    {
        #region Properties & Fields

        private static CorsairDeviceProvider? _instance;
        /// <summary>
        /// Gets the singleton <see cref="CorsairDeviceProvider"/> instance.
        /// </summary>
        public static CorsairDeviceProvider Instance => _instance ?? new CorsairDeviceProvider();

        /// <summary>
        /// Gets a modifiable list of paths used to find the native SDK-dlls for x86 applications.
        /// The first match will be used.
        /// </summary>
        public static List<string> PossibleX86NativePaths { get; } = new() { "x86/CUESDK.dll", "x86/CUESDK_2015.dll", "x86/CUESDK_2013.dll" };

        /// <summary>
        /// Gets a modifiable list of paths used to find the native SDK-dlls for x64 applications.
        /// The first match will be used.
        /// </summary>
        public static List<string> PossibleX64NativePaths { get; } = new() { "x64/CUESDK.dll", "x64/CUESDK_2015.dll", "x64/CUESDK_2013.dll" };

        /// <inheritdoc />
        /// <summary>
        /// Indicates if the SDK is initialized and ready to use.
        /// </summary>
        public bool IsInitialized { get; private set; }

        /// <summary>
        /// Gets the protocol details for the current SDK-connection.
        /// </summary>
        public CorsairProtocolDetails? ProtocolDetails { get; private set; }

        /// <summary>
        /// Gets the last error documented by CUE.
        /// </summary>
        public CorsairError LastError => _CUESDK.CorsairGetLastError();

        /// <inheritdoc />
        public IEnumerable<IRGBDevice> Devices { get; private set; } = Enumerable.Empty<IRGBDevice>();

        /// <summary>
        /// The <see cref="DeviceUpdateTrigger"/> used to trigger the updates for corsair devices. 
        /// </summary>
        public DeviceUpdateTrigger UpdateTrigger { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="CorsairDeviceProvider"/> class.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if this constructor is called even if there is already an instance of this class.</exception>
        public CorsairDeviceProvider()
        {
            if (_instance != null) throw new InvalidOperationException($"There can be only one instance of type {nameof(CorsairDeviceProvider)}");
            _instance = this;

            UpdateTrigger = new DeviceUpdateTrigger();
        }

        #endregion

        #region Methods

        /// <inheritdoc />
        /// <exception cref="RGBDeviceException">Thrown if the SDK is already initialized or if the SDK is not compatible to CUE.</exception>
        /// <exception cref="CUEException">Thrown if the CUE-SDK provides an error.</exception>
        public bool Initialize(RGBDeviceType loadFilter = RGBDeviceType.All, bool throwExceptions = false)
        {
            IsInitialized = false;

            try
            {
                UpdateTrigger.Stop();

                _CUESDK.Reload();

                ProtocolDetails = new CorsairProtocolDetails(_CUESDK.CorsairPerformProtocolHandshake());

                CorsairError error = LastError;
                if (error != CorsairError.Success)
                    throw new CUEException(error);

                if (ProtocolDetails.BreakingChanges)
                    throw new RGBDeviceException("The SDK currently used isn't compatible with the installed version of CUE.\r\n"
                                              + $"CUE-Version: {ProtocolDetails.ServerVersion} (Protocol {ProtocolDetails.ServerProtocolVersion})\r\n"
                                              + $"SDK-Version: {ProtocolDetails.SdkVersion} (Protocol {ProtocolDetails.SdkProtocolVersion})");

                // DarthAffe 02.02.2021: 127 is iCUE
                if (!_CUESDK.CorsairSetLayerPriority(128))
                    throw new CUEException(LastError);

                Dictionary<string, int> modelCounter = new();
                IList<IRGBDevice> devices = new List<IRGBDevice>();
                int deviceCount = _CUESDK.CorsairGetDeviceCount();
                for (int i = 0; i < deviceCount; i++)
                {
                    try
                    {
                        _CorsairDeviceInfo nativeDeviceInfo = (_CorsairDeviceInfo)Marshal.PtrToStructure(_CUESDK.CorsairGetDeviceInfo(i), typeof(_CorsairDeviceInfo))!;
                        CorsairRGBDeviceInfo info = new(i, RGBDeviceType.Unknown, nativeDeviceInfo, modelCounter);
                        if (!info.CapsMask.HasFlag(CorsairDeviceCaps.Lighting))
                            continue; // Everything that doesn't support lighting control is useless

                        CorsairDeviceUpdateQueue? deviceUpdateQueue = null;
                        foreach (ICorsairRGBDevice device in GetRGBDevice(info, i, nativeDeviceInfo, modelCounter))
                        {
                            if ((device == null) || !loadFilter.HasFlag(device.DeviceInfo.DeviceType)) continue;

                            deviceUpdateQueue ??= new CorsairDeviceUpdateQueue(UpdateTrigger, info.CorsairDeviceIndex);

                            device.Initialize(deviceUpdateQueue);

                            error = LastError;
                            if (error != CorsairError.Success)
                                throw new CUEException(error);

                            devices.Add(device);
                        }
                    }
                    catch { if (throwExceptions) throw; }
                }

                UpdateTrigger.Start();

                Devices = new ReadOnlyCollection<IRGBDevice>(devices);
                IsInitialized = true;
            }
            catch
            {
                Reset();
                if (throwExceptions) throw;
                return false;
            }

            return true;
        }

        private static IEnumerable<ICorsairRGBDevice> GetRGBDevice(CorsairRGBDeviceInfo info, int i, _CorsairDeviceInfo nativeDeviceInfo, Dictionary<string, int> modelCounter)
        {
            switch (info.CorsairDeviceType)
            {
                case CorsairDeviceType.Keyboard:
                    yield return new CorsairKeyboardRGBDevice(new CorsairKeyboardRGBDeviceInfo(i, nativeDeviceInfo, modelCounter));
                    break;

                case CorsairDeviceType.Mouse:
                    yield return new CorsairMouseRGBDevice(new CorsairMouseRGBDeviceInfo(i, nativeDeviceInfo, modelCounter));
                    break;

                case CorsairDeviceType.Headset:
                    yield return new CorsairHeadsetRGBDevice(new CorsairHeadsetRGBDeviceInfo(i, nativeDeviceInfo, modelCounter));
                    break;

                case CorsairDeviceType.Mousepad:
                    yield return new CorsairMousepadRGBDevice(new CorsairMousepadRGBDeviceInfo(i, nativeDeviceInfo, modelCounter));
                    break;

                case CorsairDeviceType.HeadsetStand:
                    yield return new CorsairHeadsetStandRGBDevice(new CorsairHeadsetStandRGBDeviceInfo(i, nativeDeviceInfo, modelCounter));
                    break;

                case CorsairDeviceType.MemoryModule:
                    yield return new CorsairMemoryRGBDevice(new CorsairMemoryRGBDeviceInfo(i, nativeDeviceInfo, modelCounter));
                    break;

                case CorsairDeviceType.Cooler:
                case CorsairDeviceType.CommanderPro:
                case CorsairDeviceType.LightningNodePro:
                    _CorsairChannelsInfo? channelsInfo = nativeDeviceInfo.channels;
                    if (channelsInfo != null)
                    {
                        IntPtr channelInfoPtr = channelsInfo.channels;

                        for (int channel = 0; channel < channelsInfo.channelsCount; channel++)
                        {
                            CorsairLedId referenceLed = GetChannelReferenceId(info.CorsairDeviceType, channel);
                            if (referenceLed == CorsairLedId.Invalid) continue;

                            _CorsairChannelInfo channelInfo = (_CorsairChannelInfo)Marshal.PtrToStructure(channelInfoPtr, typeof(_CorsairChannelInfo))!;

                            int channelDeviceInfoStructSize = Marshal.SizeOf(typeof(_CorsairChannelDeviceInfo));
                            IntPtr channelDeviceInfoPtr = channelInfo.devices;

                            for (int device = 0; device < channelInfo.devicesCount; device++)
                            {
                                _CorsairChannelDeviceInfo channelDeviceInfo = (_CorsairChannelDeviceInfo)Marshal.PtrToStructure(channelDeviceInfoPtr, typeof(_CorsairChannelDeviceInfo))!;

                                yield return new CorsairCustomRGBDevice(new CorsairCustomRGBDeviceInfo(info, nativeDeviceInfo, channelDeviceInfo, referenceLed, modelCounter));
                                referenceLed += channelDeviceInfo.deviceLedCount;

                                channelDeviceInfoPtr = new IntPtr(channelDeviceInfoPtr.ToInt64() + channelDeviceInfoStructSize);
                            }

                            int channelInfoStructSize = Marshal.SizeOf(typeof(_CorsairChannelInfo));
                            channelInfoPtr = new IntPtr(channelInfoPtr.ToInt64() + channelInfoStructSize);
                        }
                    }

                    break;


                // ReSharper disable once RedundantCaseLabel
                case CorsairDeviceType.Unknown:
                default:
                    throw new RGBDeviceException("Unknown Device-Type");
            }
        }

        private static CorsairLedId GetChannelReferenceId(CorsairDeviceType deviceType, int channel)
        {
            if (deviceType == CorsairDeviceType.Cooler)
                return CorsairLedId.CustomLiquidCoolerChannel1Led1;

            return channel switch
            {
                0 => CorsairLedId.CustomDeviceChannel1Led1,
                1 => CorsairLedId.CustomDeviceChannel2Led1,
                2 => CorsairLedId.CustomDeviceChannel3Led1,
                _ => CorsairLedId.Invalid
            };
        }

        private void Reset()
        {
            ProtocolDetails = null;
            Devices = Enumerable.Empty<IRGBDevice>();
            IsInitialized = false;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            try { UpdateTrigger.Dispose(); }
            catch { /* at least we tried */ }

            foreach (IRGBDevice device in Devices)
                try { device.Dispose(); }
                catch { /* at least we tried */ }
            Devices = Enumerable.Empty<IRGBDevice>();

            try { _CUESDK.UnloadCUESDK(); }
            catch { /* at least we tried */ }
        }

        #endregion
    }
}

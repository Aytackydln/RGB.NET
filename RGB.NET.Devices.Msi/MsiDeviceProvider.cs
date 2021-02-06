﻿// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using RGB.NET.Core;
using RGB.NET.Devices.Msi.Exceptions;
using RGB.NET.Devices.Msi.Native;

namespace RGB.NET.Devices.Msi
{
    /// <inheritdoc />
    /// <summary>
    /// Represents a device provider responsible for MSI devices.
    /// </summary>
    public class MsiDeviceProvider : IRGBDeviceProvider
    {
        #region Properties & Fields

        private static MsiDeviceProvider? _instance;
        /// <summary>
        /// Gets the singleton <see cref="MsiDeviceProvider"/> instance.
        /// </summary>
        public static MsiDeviceProvider Instance => _instance ?? new MsiDeviceProvider();

        /// <summary>
        /// Gets a modifiable list of paths used to find the native SDK-dlls for x86 applications.
        /// The first match will be used.
        /// </summary>
        public static List<string> PossibleX86NativePaths { get; } = new() { "x86/MysticLight_SDK.dll" };

        /// <summary>
        /// Gets a modifiable list of paths used to find the native SDK-dlls for x64 applications.
        /// The first match will be used.
        /// </summary>
        public static List<string> PossibleX64NativePaths { get; } = new() { "x64/MysticLight_SDK.dll" };

        /// <inheritdoc />
        /// <summary>
        /// Indicates if the SDK is initialized and ready to use.
        /// </summary>
        public bool IsInitialized { get; private set; }

        /// <inheritdoc />
        public IEnumerable<IRGBDevice> Devices { get; private set; } = Enumerable.Empty<IRGBDevice>();

        /// <summary>
        /// The <see cref="DeviceUpdateTrigger"/> used to trigger the updates for corsair devices. 
        /// </summary>
        public DeviceUpdateTrigger UpdateTrigger { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="MsiDeviceProvider"/> class.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if this constructor is called even if there is already an instance of this class.</exception>
        public MsiDeviceProvider()
        {
            if (_instance != null) throw new InvalidOperationException($"There can be only one instance of type {nameof(MsiDeviceProvider)}");
            _instance = this;

            UpdateTrigger = new DeviceUpdateTrigger();
        }

        #endregion

        #region Methods

        /// <inheritdoc />
        public bool Initialize(RGBDeviceType loadFilter = RGBDeviceType.All, bool throwExceptions = false)
        {
            IsInitialized = false;

            try
            {
                UpdateTrigger.Stop();

                _MsiSDK.Reload();

                IList<IRGBDevice> devices = new List<IRGBDevice>();

                int errorCode;
                if ((errorCode = _MsiSDK.Initialize()) != 0)
                    ThrowMsiError(errorCode);

                if ((errorCode = _MsiSDK.GetDeviceInfo(out string[] deviceTypes, out int[] ledCounts)) != 0)
                    ThrowMsiError(errorCode);

                for (int i = 0; i < deviceTypes.Length; i++)
                {
                    try
                    {
                        string deviceType = deviceTypes[i];
                        int ledCount = ledCounts[i];

                        //Hex3l: MSI_MB provide access to the motherboard "leds" where a led must be intended as a led header (JRGB, JRAINBOW etc..) (Tested on MSI X570 Unify)
                        if (deviceType.Equals("MSI_MB"))
                        {
                            MsiDeviceUpdateQueue updateQueue = new(UpdateTrigger, deviceType);
                            IMsiRGBDevice motherboard = new MsiMainboardRGBDevice(new MsiRGBDeviceInfo(RGBDeviceType.Mainboard, deviceType, "MSI", "Motherboard"));
                            motherboard.Initialize(updateQueue, ledCount);
                            devices.Add(motherboard);
                        }
                        else if (deviceType.Equals("MSI_VGA"))
                        {
                            //Hex3l: Every led under MSI_VGA should be a different graphics card. Handling all the cards together seems a good way to avoid overlapping of leds
                            //Hex3l: The led name is the name of the card (e.g. NVIDIA GeForce RTX 2080 Ti) we could provide it in device info.

                            MsiDeviceUpdateQueue updateQueue = new(UpdateTrigger, deviceType);
                            IMsiRGBDevice graphicscard = new MsiGraphicsCardRGBDevice(new MsiRGBDeviceInfo(RGBDeviceType.GraphicsCard, deviceType, "MSI", "GraphicsCard"));
                            graphicscard.Initialize(updateQueue, ledCount);
                            devices.Add(graphicscard);
                        }
                        else if (deviceType.Equals("MSI_MOUSE"))
                        {
                            //Hex3l: Every led under MSI_MOUSE should be a different mouse. Handling all the mouses together seems a good way to avoid overlapping of leds
                            //Hex3l: The led name is the name of the mouse (e.g. msi CLUTCH GM11) we could provide it in device info.

                            MsiDeviceUpdateQueue updateQueue = new(UpdateTrigger, deviceType);
                            IMsiRGBDevice mouses = new MsiMouseRGBDevice(new MsiRGBDeviceInfo(RGBDeviceType.Mouse, deviceType, "MSI", "Mouse"));
                            mouses.Initialize(updateQueue, ledCount);
                            devices.Add(mouses);
                        }

                        //TODO DarthAffe 22.02.2020: Add other devices
                    }
                    catch { if (throwExceptions) throw; }
                }

                UpdateTrigger.Start();

                Devices = new ReadOnlyCollection<IRGBDevice>(devices);
                IsInitialized = true;
            }
            catch
            {
                if (throwExceptions)
                    throw;
                return false;
            }

            return true;
        }

        private void ThrowMsiError(int errorCode) => throw new MysticLightException(errorCode, _MsiSDK.GetErrorMessage(errorCode));

        /// <inheritdoc />
        public void Dispose()
        {
            try { UpdateTrigger.Dispose(); }
            catch { /* at least we tried */ }

            foreach (IRGBDevice device in Devices)
                try { device.Dispose(); }
                catch { /* at least we tried */ }
            Devices = Enumerable.Empty<IRGBDevice>();

            try { _MsiSDK.UnloadMsiSDK(); }
            catch { /* at least we tried */ }
        }

        #endregion
    }
}

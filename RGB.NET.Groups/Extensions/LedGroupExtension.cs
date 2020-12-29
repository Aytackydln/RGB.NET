﻿// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global

using RGB.NET.Core;

namespace RGB.NET.Groups
{
    /// <summary>
    /// Offers some extensions and helper-methods for <see cref="ILedGroup"/> related things.
    /// </summary>
    public static class LedGroupExtension
    {
        /// <summary>
        /// Converts the given <see cref="ILedGroup" /> to a <see cref="ListLedGroup" />.
        /// </summary>
        /// <param name="ledGroup">The <see cref="ILedGroup" /> to convert.</param>
        /// <returns>The converted <see cref="ListLedGroup" />.</returns>
        public static ListLedGroup ToListLedGroup(this ILedGroup ledGroup)
        {
            // ReSharper disable once InvertIf
            if (!(ledGroup is ListLedGroup listLedGroup))
            {
                if (ledGroup.Surface != null)
                    ledGroup.Detach(ledGroup.Surface);
                listLedGroup = new ListLedGroup(ledGroup.Surface, ledGroup.GetLeds()) { Brush = ledGroup.Brush };
            }
            return listLedGroup;
        }

        /// <summary>
        /// Returns a new <see cref="ListLedGroup" /> which contains all <see cref="Led"/> from the given <see cref="ILedGroup"/> excluding the specified ones.
        /// </summary>
        /// <param name="ledGroup">The base <see cref="ILedGroup"/>.</param>
        /// <param name="ledIds">The <see cref="Led"/> to exclude.</param>
        /// <returns>The new <see cref="ListLedGroup" />.</returns>
        public static ListLedGroup Exclude(this ILedGroup ledGroup, params Led[] ledIds)
        {
            ListLedGroup listLedGroup = ledGroup.ToListLedGroup();
            foreach (Led led in ledIds)
                listLedGroup.RemoveLed(led);
            return listLedGroup;
        }

        // ReSharper disable once UnusedMethodReturnValue.Global
        /// <summary>
        /// Attaches the given <see cref="ILedGroup"/> to the <see cref="RGBSurface"/>.
        /// </summary>
        /// <param name="ledGroup">The <see cref="ILedGroup"/> to attach.</param>
        /// <returns><c>true</c> if the <see cref="ILedGroup"/> could be attached; otherwise, <c>false</c>.</returns>
        public static bool Attach(this ILedGroup ledGroup, RGBSurface surface) => surface.AttachLedGroup(ledGroup);

        /// <summary>
        /// Detaches the given <see cref="ILedGroup"/> from the <see cref="RGBSurface"/>.
        /// </summary>
        /// <param name="ledGroup">The <see cref="ILedGroup"/> to attach.</param>
        /// <returns><c>true</c> if the <see cref="ILedGroup"/> could be detached; otherwise, <c>false</c>.</returns>
        public static bool Detach(this ILedGroup ledGroup, RGBSurface surface) => surface.DetachLedGroup(ledGroup);
    }
}

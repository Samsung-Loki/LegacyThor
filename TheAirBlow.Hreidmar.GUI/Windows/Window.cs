// Copyright © TheAirBlow 2022 <theairblow.help@gmail.com>
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

namespace TheAirBlow.Hreidmar.GUI.Windows;

/// <summary>
/// Hreidmar GUI window
/// </summary>
public abstract class Window
{
    private bool _isOpened;

    /// <summary>
    /// Open the window
    /// </summary>
    public virtual void Open()
        => _isOpened = true;

    /// <summary>
    /// Close the window
    /// </summary>
    public virtual void Close()
        => _isOpened = false;

    /// <summary>
    /// Is the window closed
    /// </summary>
    /// <returns>Value</returns>
    public virtual bool IsOpened()
        => _isOpened;

    /// <summary>
    /// Draw the window
    /// </summary>
    public virtual void Draw() { }
}
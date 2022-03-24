// Copyright © TheAirBlow 2022 <theairblow.help@gmail.com>
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;

namespace Hreidmar.GUI.Windows
{
    /// <summary>
    /// Hreidmar GUI windows manager
    /// </summary>
    public static class WindowsManager
    {
        /// <summary>
        /// All windows
        /// </summary>
        private static Dictionary<string, Window> _windows = new();

        /// <summary>
        /// Add a window
        /// </summary>
        /// <param name="name">Name</param>
        /// <param name="window">Window</param>
        public static void Add(string name, Window window)
            => _windows.Add(name, window);

        /// <summary>
        /// Remove a window
        /// </summary>
        /// <param name="name">Name</param>
        public static void Remove(string name)
            => _windows.Remove(name);
        
        /// <summary>
        /// Open a window
        /// </summary>
        /// <param name="name">Name</param>
        public static void Open(string name)
            => _windows[name].Open();

        /// <summary>
        /// Close a window
        /// </summary>
        /// <param name="name">Name</param>
        public static void Close(string name)
            => _windows[name].Close();
        
        /// <summary>
        /// Is window opened
        /// </summary>
        /// <param name="name">name</param>
        /// <returns>Value</returns>
        public static bool IsOpened(string name)
            => _windows[name].IsOpened();

        /// <summary>
        /// Get a window by name of type
        /// </summary>
        /// <param name="name">Name</param>
        /// <typeparam name="T">Type</typeparam>
        /// <exception cref="Exception">Requested Window type is not an instance of Window</exception>
        /// <returns>Window</returns>
        public static T GetWindow<T>(string name)
            => (T)(object)_windows[name];

        /// <summary>
        /// Draw all windows
        /// </summary>
        public static void Draw()
        {
            foreach (var window in _windows)
                if (window.Value.IsOpened()) window.Value.Draw();
        }
    }
}
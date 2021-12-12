using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Hreidmar.GUI.Windows;
using Hreidmar.Enigma;
using Hreidmar.Enigma.Packets;
using Hreidmar.Enigma.Packets.Inbound;
using Hreidmar.Enigma.PIT;
using ImGuiNET;
using K4os.Compression.LZ4.Streams;
using LibUsbDotNet;
using LibUsbDotNet.DeviceNotify.Info;
using LibUsbDotNet.DeviceNotify.Linux;
using LibUsbDotNet.Main;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nez.ImGuiTools;
using Num = System.Numerics;

#pragma warning disable IDE0052 // Remove unread private members
#pragma warning disable IDE0044 // Add readonly modifier

namespace Hreidmar.GUI
{
    /// <summary>
    /// Simple FNA + ImGui example
    /// </summary>
    public class MonoGameController : Game
    {
        private GraphicsDeviceManager _graphics;
        private ImGuiRenderer _imGuiRenderer;
        private static System.Timers.Timer _timer = new();
        private ImFontPtr _font;

        public MonoGameController()
        {
            // Initialization
            _graphics = new GraphicsDeviceManager(this) {
                PreferredBackBufferWidth = 1024,
                PreferredBackBufferHeight = 768,
                PreferMultiSampling = true
            };

            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            // Rendering
            _imGuiRenderer = new ImGuiRenderer(this);
            _font = ImGui.GetIO().Fonts.AddFontFromFileTTF("Karla-Regular.ttf", 20);
            _imGuiRenderer.RebuildFontAtlas();

            // Styling
            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 3);
            ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.DockingEnable;
            Window.AllowUserResizing = true;

            // Windows
            WindowsManager.Add("about", new AboutWindow());
            WindowsManager.Add("faq", new FaqWindow());
            WindowsManager.Add("help", new HelpWindow());
            WindowsManager.Add("logs", new LogsWindow());
            WindowsManager.Add("options", new OptionsWindow());
            WindowsManager.Add("devices", new DevicesWindow());
            WindowsManager.Add("deviceinfo", new DeviceInfoWindow());
            WindowsManager.Add("flashselect", new FileFlashSelectWindow());
            WindowsManager.Add("loadselect", new PitLoadSelectWindow());
            WindowsManager.Add("pitselect", new PitFlashSelectWindow());
            WindowsManager.Add("file", new FileFlashingWindow());
            WindowsManager.Add("session", new SessionWindow());
            WindowsManager.Add("pit", new PitWindow());
            WindowsManager.Add("69", new PornWindow());
            
            // Automatic refresh
            _timer.Interval = 100;
            _timer.Start();
            _timer.Elapsed += (_, _) => 
                WindowsManager.GetWindow<DevicesWindow>("devices").Refresh();

            base.Initialize();
        }

        protected override void Draw(GameTime gameTime)
        {
            // Begin rendering
            GraphicsDevice.Clear(new Color(114, 144, 154));
            _imGuiRenderer.BeforeLayout(gameTime);
            ImGui.DockSpaceOverViewport(ImGui.GetMainViewport());
            ImGui.PushFont(_font);
            
            // Main menu bar
            if (ImGui.BeginMainMenuBar()) {
                if (ImGui.BeginMenu("Help")) {
                    if (ImGui.MenuItem("About"))
                        WindowsManager.Open("about");
                    if (ImGui.MenuItem("Help"))
                        WindowsManager.Open("help");
                    if (ImGui.MenuItem("FAQ"))
                        WindowsManager.Open("faq");
                    ImGui.EndMenu();
                }
                ImGui.EndMainMenuBar();
            }
            
            // Draw windows
            WindowsManager.Draw();
            
            // End rendering
            _imGuiRenderer.AfterLayout();
            base.Draw(gameTime);
        }
    }
}

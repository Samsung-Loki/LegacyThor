// Copyright © TheAirBlow 2022 <theairblow.help@gmail.com>
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheAirBlow.Hreidmar.GUI.Windows;
using Num = System.Numerics;

#pragma warning disable IDE0052 // Remove unread private members
#pragma warning disable IDE0044 // Add readonly modifier

namespace TheAirBlow.Hreidmar.GUI
{
    /// <summary>
    /// Simple FNA + ImGui example
    /// </summary>
    public class MonoGameController : Game
    {
        private GraphicsDeviceManager _graphics;
        private ImGuiRenderer _imGuiRenderer;
        private SpriteBatch _spriteBatch;
        private static System.Timers.Timer _timer = new();
        private ImFontPtr _font;

        public MonoGameController()
        {
            // Initialization
            Content.RootDirectory = "Content";
            _graphics = new GraphicsDeviceManager(this) {
                PreferredBackBufferWidth = 1024,
                PreferredBackBufferHeight = 768,
                PreferMultiSampling = true
            };

            IsMouseVisible = true;
        }

        private string[] BackgroundImages = { 
            "warning_svb", "download_error", 
            "secure_error", "download" 
        };

        private Texture2D _backgroundTexture;

        protected override void Initialize()
        {
            // Rendering
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _imGuiRenderer = new ImGuiRenderer(this);
            _font = ImGui.GetIO().Fonts.AddFontFromFileTTF(
                "Karla-Regular.ttf", 20);
            _imGuiRenderer.RebuildFontAtlas();

            // Styling
            ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 3);
            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 3);
            ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.DockingEnable;
            Window.AllowUserResizing = true;

            // Windows
            WindowsManager.Add("about", new AboutWindow());
            WindowsManager.Add("faq", new FaqWindow());
            WindowsManager.Add("help", new HelpWindow());
            WindowsManager.Add("logs", new LogsWindow());
            WindowsManager.Add("devices", new DevicesWindow());
            WindowsManager.Add("lol", new PornWindow());
            
            // Background texture
            var random = new Random();
            _backgroundTexture = Content
                .Load<Texture2D>(BackgroundImages[
                random.Next(0, BackgroundImages.Length)
                ]);
            
            // Automatic refresh
            _timer.Interval = 100;
            _timer.Start();
            _timer.Elapsed += (_, _) => 
                WindowsManager.GetWindow<DevicesWindow>("devices").Refresh();

            base.Initialize();
        }
        
        private static Texture2D CreateTexture(GraphicsDevice device, 
            int width, int height, Func<int, Color> paint)
        {
            var texture = new Texture2D(device, width, height);
            var data = new Color[width * height];
            for(var pixel = 0; pixel < data.Length; pixel++)
                data[pixel] = paint( pixel );
            
            texture.SetData( data );
            return texture;
        }

        protected override void Draw(GameTime gameTime)
        {
            // Begin rendering
            GraphicsDevice.Clear(new Color(33, 199, 197));
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

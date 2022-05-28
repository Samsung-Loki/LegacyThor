// Copyright © TheAirBlow 2022 <theairblow.help@gmail.com>
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Serilog;
using TheAirBlow.Hreidmar.GUI.Windows;
using Num = System.Numerics;

#pragma warning disable IDE0052 // Remove unread private members
#pragma warning disable IDE0044 // Add readonly modifier

namespace TheAirBlow.Hreidmar.GUI;

/// <summary>
/// MonoGame + ImGui renderer
/// </summary>
public class MonoGameController : Game
{
    /// <summary>
    /// GraphicsDeviceManager instance
    /// </summary>
    private GraphicsDeviceManager _graphics;
        
    /// <summary>
    /// ImGuiNET renderer
    /// </summary>
    private ImGuiRenderer _imGuiRenderer;
        
    /// <summary>
    /// SpriteBatch instance
    /// </summary>
    private SpriteBatch _spriteBatch;
        
    /// <summary>
    /// Auto-refresh timer
    /// </summary>
    private static readonly System.Timers.Timer _timer = new();
        
    /// <summary>
    /// Font to render text with
    /// </summary>
    private ImFontPtr _font;

    /// <summary>
    /// Initialize GraphicsDeviceManager
    /// </summary>
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

    /// <summary>
    /// Initialize font, styling, windows, etc.
    /// </summary>
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
        WindowsManager.Add("pitedit", new PitEditorWindow());
        WindowsManager.Add("about", new AboutWindow());
        WindowsManager.Add("faq", new FaqWindow());
        WindowsManager.Add("help", new HelpWindow());
        WindowsManager.Add("logs", new LogsWindow());
        WindowsManager.Add("devices", new DevicesWindow());
        WindowsManager.Add("lol", new PornWindow());

        // Automatic refresh
        _timer.Interval = 100;
        _timer.Start();
        _timer.Elapsed += (_, _) => 
            WindowsManager.GetWindow<DevicesWindow>("devices").Refresh();
            
        // Initialize logger
        Program.Logger = new LoggerConfiguration()
            .WriteTo.TextWriter(new SerilogLogWindow(),
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File("main.log")
            .WriteTo.Console()
            .CreateLogger();

        Program.Logger.Information("Logger successfully initialized!");
            
        Window.Title = "Hreidmar GUI";
        base.Initialize();
    }

    /// <summary>
    /// Hreidmar doesn't close if you click the close button, for some reason.
    /// The window disappears, but the process is still running.
    /// This fixes that by forcefully killing the process.
    /// </summary>
    protected override void OnExiting(object sender, EventArgs args) 
    {
        base.OnExiting(sender, args);
        Environment.Exit(0);
    }

    /// <summary>
    /// Draw the windows
    /// </summary>
    protected override void Draw(GameTime gameTime)
    {
        // Begin rendering
        GraphicsDevice.Clear(new Color(33, 0, 0));
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

            if (ImGui.BeginMenu("Tools")) {
                if (ImGui.MenuItem("PIT Viewer"))
                    WindowsManager.Open("pitedit");
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
// Copyright © TheAirBlow 2022 <theairblow.help@gmail.com>
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Runtime.InteropServices;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Serilog;
using TheAirBlow.Thor.GUI.Windows;
using Num = System.Numerics;

#pragma warning disable IDE0052 // Remove unread private members
#pragma warning disable IDE0044 // Add readonly modifier

namespace TheAirBlow.Thor.GUI;

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
    /// Instance of MonoGameController
    /// </summary>
    public static MonoGameController Instance;
        
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
    /// Background Images
    /// </summary>
    private readonly string[] _backgroundImages = { 
        "warning_svb", "download_error", 
        "secure_error", "download" 
    };

    /// <summary>
    /// Background Texture
    /// </summary>
    private Texture2D _backgroundTexture;
    
    /// <summary>
    /// Thor's logo (totally not stolen Samsung icon)
    /// </summary>
    public IntPtr Logo;
    
    /// <summary>
    /// TheAirBlow, the got himself
    /// </summary>
    public IntPtr God;

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
        Instance = this;
    }

    /// <summary>
    /// Initialize font, styling, windows, etc.
    /// </summary>
    protected override unsafe void Initialize()
    {
        // Rendering
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _imGuiRenderer = new ImGuiRenderer(this);
        int[] ranges = {
            0x0020, 0x00FF, // Basic Latin + Latin Supplement
            0x0400, 0x044F  // Cyrillic
        };
        _font = ImGui.GetIO().Fonts.AddFontFromFileTTF(
            "NotoSans-Regular.ttf", 22, 
            ImGuiNative.ImFontConfig_ImFontConfig(), 
            ImGui.GetIO().Fonts.GetGlyphRangesCyrillic());
        _imGuiRenderer.RebuildFontAtlas();

        // Styling
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 10);
        ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 6);
        ImGui.PushStyleVar(ImGuiStyleVar.GrabRounding, 12);
        ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.DockingEnable;
        Window.AllowUserResizing = true;

        // Background texture
        var random = new Random();
        _backgroundTexture = Content
            .Load<Texture2D>(_backgroundImages[
                random.Next(0, _backgroundImages.Length)
            ]);

        Logo = _imGuiRenderer.BindTexture(Content.Load<Texture2D>("icon"));
        God = _imGuiRenderer.BindTexture(Content.Load<Texture2D>("theairblow"));

        // Windows
        WindowsManager.Add("filepicker", new FilePickerWindow());
        WindowsManager.Add("pitedit", new PitEditorWindow());
        WindowsManager.Add("devices", new DevicesWindow());
        WindowsManager.Add("about", new AboutWindow());
        WindowsManager.Add("help", new HelpWindow());
        WindowsManager.Add("logs", new LogsWindow());
        WindowsManager.Add("faq", new FaqWindow());

        // Automatic refresh
        _timer.Interval = 100;
        _timer.Start();
        _timer.Elapsed += (_, _) => 
            WindowsManager.GetWindow<DevicesWindow>("devices").Refresh();
        
        Program.Logger.Information("Welcome to Thor GUI!");
        Window.Title = "Thor GUI";
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
    /// Draw MonoGame & ImGui
    /// </summary>
    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(33, 199, 197));
        BatchDraw(); base.Draw(gameTime);
        _imGuiRenderer.BeforeLayout(gameTime);
        ImGui.DockSpaceOverViewport(ImGui.GetMainViewport(), 
            ImGuiDockNodeFlags.PassthruCentralNode);
        ImGui.PushFont(_font); ImGuiDraw();
        _imGuiRenderer.AfterLayout();
    }

    /// <summary>
    /// ImGui drawing
    /// </summary>
    private void ImGuiDraw()
    {
        // Main menu bar
        if (ImGui.BeginMainMenuBar()) {
            if (ImGui.BeginMenu("Help")) {
                if (ImGui.MenuItem("How to use Thor"))
                    WindowsManager.Open("help");
                if (ImGui.MenuItem("About Thor"))
                    WindowsManager.Open("about");
                if (ImGui.MenuItem("FAQ"))
                    WindowsManager.Open("faq");
                ImGui.Separator();
                if (ImGui.MenuItem("Randomize background")) {
                    var random = new Random();
                    _backgroundTexture = Content
                        .Load<Texture2D>(_backgroundImages[
                            random.Next(0, _backgroundImages.Length)
                        ]);
                }
                
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
    }

    /// <summary>
    /// SpriteBatch (MonoGame) drawing
    /// </summary>
    private void BatchDraw()
    {
        _spriteBatch.Begin();
        var x = GraphicsDevice.Viewport.Width / 2 - _backgroundTexture.Width / 2;
        var y = GraphicsDevice.Viewport.Height / 2 - _backgroundTexture.Height / 2;
        _spriteBatch.Draw(_backgroundTexture, new Vector2(x, y), Color.White);
        _spriteBatch.End();
    }
}
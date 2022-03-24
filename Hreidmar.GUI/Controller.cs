using Hreidmar.GUI.Windows;
using ImGuiNET;
using Microsoft.Xna.Framework;
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
            WindowsManager.Add("devices", new DevicesWindow());
            WindowsManager.Add("lol", new PornWindow());
            
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

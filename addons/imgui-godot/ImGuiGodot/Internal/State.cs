#if GODOT_PC
using Godot;
using ImGuiNET;
using System;
using System.Runtime.InteropServices;

namespace ImGuiGodot.Internal;

internal sealed class State : IDisposable
{
    private enum RendererType
    {
        Dummy,
        Canvas,
        RenderingDevice
    }

    private static readonly IntPtr _backendName = Marshal.StringToCoTaskMemAnsi("godot4_net");
    private static IntPtr _rendererName = IntPtr.Zero;
    private IntPtr _iniFilenameBuffer = IntPtr.Zero;

    internal Viewports Viewports { get; private set; }
    internal Fonts Fonts { get; private set; }
    internal Input Input { get; private set; }
    internal IRenderer Renderer { get; private set; }
    internal float Scale { get; set; } = 1.0f;
    internal static State Instance { get; set; } = null!;

    public State(Window mainWindow, Rid mainSubViewport, IRenderer renderer)
    {
        Renderer = renderer;
        Input = new Input(mainWindow);
        Fonts = new Fonts();

        if (ImGui.GetCurrentContext() != IntPtr.Zero)
        {
            ImGui.DestroyContext();
        }

        var context = ImGui.CreateContext();
        ImGui.SetCurrentContext(context);
        var io = ImGui.GetIO();

        io.BackendFlags =
            ImGuiBackendFlags.HasGamepad |
            ImGuiBackendFlags.HasSetMousePos |
            ImGuiBackendFlags.HasMouseCursors |
            ImGuiBackendFlags.RendererHasVtxOffset |
            ImGuiBackendFlags.RendererHasViewports;

        if (_rendererName == IntPtr.Zero)
        {
            _rendererName = Marshal.StringToCoTaskMemAnsi(Renderer.Name);
        }

        unsafe
        {
            io.NativePtr->BackendPlatformName = (byte*)_backendName;
            io.NativePtr->BackendRendererName = (byte*)_rendererName;
        }

        Viewports = new Viewports(mainWindow, mainSubViewport);
    }

    public void Dispose()
    {
        if (ImGui.GetCurrentContext() != IntPtr.Zero)
            ImGui.DestroyContext();
        Renderer.Dispose();
    }

    public static void Init(Window mainWindow, Rid mainSubViewport, Resource cfg)
    {
        if (IntPtr.Size != sizeof(ulong))
        {
            throw new PlatformNotSupportedException("imgui-godot requires 64-bit pointers");
        }

        RendererType renderer = Enum.Parse<RendererType>((string)cfg.Get("Renderer"));

        if (DisplayServer.GetName() == "headless")
        {
            renderer = RendererType.Dummy;
        }

        // fall back to Canvas in OpenGL compatibility mode
        if (renderer == RendererType.RenderingDevice && RenderingServer.GetRenderingDevice() == null)
        {
            renderer = RendererType.Canvas;
        }

        // there's no way to get the actual current thread model, eg if --render-thread is used
        int threadModel = (int)ProjectSettings.GetSetting("rendering/driver/threads/thread_model");

        IRenderer internalRenderer;
        try
        {
            internalRenderer = renderer switch
            {
                RendererType.Dummy => new DummyRenderer(),
                RendererType.Canvas => new CanvasRenderer(),
                RendererType.RenderingDevice => threadModel == 2 ? new RdRendererThreadSafe() : new RdRenderer(),
                _ => throw new ArgumentException("Invalid renderer", nameof(cfg))
            };
        }
        catch (Exception e)
        {
            if (renderer == RendererType.RenderingDevice)
            {
                GD.PushWarning($"imgui-godot: falling back to Canvas renderer ({e.Message})");
                internalRenderer = new CanvasRenderer();
            }
            else
            {
                GD.PushError("imgui-godot: failed to init renderer");
                internalRenderer = new DummyRenderer();
            }
        }

        Instance = new(mainWindow, mainSubViewport, internalRenderer)
        {
            Scale = (float)cfg.Get("Scale")
        };
        Instance.Renderer.InitViewport(mainSubViewport);

        ImGui.GetIO().SetIniFilename((string)cfg.Get("IniFilename"));

        var fonts = (Godot.Collections.Array)cfg.Get("Fonts");

        for (int i = 0; i < fonts.Count; ++i)
        {
            var fontres = (Resource)fonts[i];
            var font = (FontFile)fontres.Get("FontData");
            int fontSize = (int)fontres.Get("FontSize");
            bool merge = (bool)fontres.Get("Merge");
            if (i == 0)
                ImGuiGD.AddFont(font, fontSize);
            else
                ImGuiGD.AddFont(font, fontSize, merge);
        }
        if ((bool)cfg.Get("AddDefaultFont"))
        {
            ImGuiGD.AddFontDefault();
        }
        ImGuiGD.RebuildFontAtlas();
    }

    public unsafe void SetIniFilename(ImGuiIOPtr io, string fileName)
    {
        io.NativePtr->IniFilename = null;

        if (_iniFilenameBuffer != IntPtr.Zero)
        {
            Marshal.FreeCoTaskMem(_iniFilenameBuffer);
            _iniFilenameBuffer = IntPtr.Zero;
        }

        if (fileName?.Length > 0)
        {
            fileName = ProjectSettings.GlobalizePath(fileName);
            _iniFilenameBuffer = Marshal.StringToCoTaskMemUTF8(fileName);
            io.NativePtr->IniFilename = (byte*)_iniFilenameBuffer;
        }
    }

    public void Update(double delta, Vector2 displaySize)
    {
        var io = ImGui.GetIO();
        io.DisplaySize = new(displaySize.X, displaySize.Y);
        io.DeltaTime = (float)delta;

        Input.Update(io);

        //_inProcessFrame = true;
        ImGui.NewFrame();
    }

    public void Render()
    {
        ImGui.Render();

        ImGui.UpdatePlatformWindows();
        Renderer.Render();
        //_inProcessFrame = false;
    }

    /// <returns>
    /// True if the InputEvent was consumed
    /// </returns>
    public bool ProcessInput(InputEvent evt, Window window)
    {
        return Input.ProcessInput(evt, window);
    }
}
#endif

using PitStopVR.Core.Models;
using PitStopVR.Telemetry.Collectors.Adapters.OpenVR;
using PitStopVR.Telemetry.Models;
using System.Diagnostics;

namespace PitStopVR.Telemetry.Collectors.Adapters;

public sealed class OpenVRMetricsAdapter : IDisposable
{
    private const uint VrApplication_Background = 2;
    private const string SystemInterfaceVersion = "IVRSystem_022";

    private OpenVRContext? _context;
    private Compositor_CumulativeStats _previousStats;
    private bool _disposed;

    public bool IsAvailable(MachineProfile machine)
    {
        if (_disposed)
        {
            return false;
        }

        return OpenVRNative.IsDllAvailable() || IsSteamVRRunning();
    }

    public bool TryInitialize()
    {
        if (_context is not null)
        {
            return true;
        }

        if (!OpenVRNative.IsDllAvailable())
        {
            return false;
        }

        int error = 0;
        var system = OpenVRNative.VR_Init(ref error, VrApplication_Background);
        if (system == IntPtr.Zero || error != 0)
        {
            return false;
        }

        var compositorPtr = OpenVRNative.VR_GetGenericInterface(OpenVRNative.CompositorInterfaceVersion, ref error);
        if (compositorPtr == IntPtr.Zero || error != 0)
        {
            OpenVRNative.VR_Shutdown();
            return false;
        }

        var compositor = new OpenVRCompositor(compositorPtr);
        _context = new OpenVRContext(system, compositor);

        var stats = new Compositor_CumulativeStats();
        compositor.GetCumulativeStats(ref stats, (uint)System.Runtime.InteropServices.Marshal.SizeOf<Compositor_CumulativeStats>());
        _previousStats = stats;

        return true;
    }

    public RawSessionSample? CaptureSample()
    {
        if (_disposed)
        {
            return null;
        }

        if (_context is null && !TryInitialize())
        {
            return null;
        }

        try
        {
            var timing = new Compositor_FrameTiming();
            var compositor = _context!.Compositor;

            if (!compositor.GetFrameTiming(ref timing, 0))
            {
                return null;
            }

            var currentStats = new Compositor_CumulativeStats();
            compositor.GetCumulativeStats(ref currentStats, (uint)System.Runtime.InteropServices.Marshal.SizeOf<Compositor_CumulativeStats>());

            var deltaPresented = currentStats.m_nNumFramePresents - _previousStats.m_nNumFramePresents;
            var deltaDropped = currentStats.m_nNumDroppedFrames - _previousStats.m_nNumDroppedFrames;
            var deltaReprojected = currentStats.m_nNumReprojectedFrames - _previousStats.m_nNumReprojectedFrames;

            _previousStats = currentStats;

            var frameTimeMs = timing.m_flClientFrameIntervalMs > 0
                ? timing.m_flClientFrameIntervalMs
                : (float)(1.0 / 90.0 * 1000.0);

            var fps = 1000.0 / frameTimeMs;

            return new RawSessionSample
            {
                Timestamp = DateTime.Now,
                FpsEstimate = Math.Round(fps, 1),
                DroppedFrames = deltaDropped > int.MaxValue ? 0 : (int)deltaDropped,
                ReprojectedFrames = deltaReprojected > int.MaxValue ? 0 : (int)deltaReprojected,
                Source = SessionSourceType.OpenVR
            };
        }
        catch
        {
            _context?.Dispose();
            _context = null;
            return null;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _context?.Dispose();
        _disposed = true;
    }

    private static bool IsSteamVRRunning()
    {
        return Process.GetProcessesByName("vrmonitor").Any()
            || Process.GetProcessesByName("vrserver").Any();
    }
}

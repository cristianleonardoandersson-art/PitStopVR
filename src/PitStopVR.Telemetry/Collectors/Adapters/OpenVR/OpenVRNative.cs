using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace PitStopVR.Telemetry.Collectors.Adapters.OpenVR;

public static class OpenVRNative
{
    private const string DllName = "openvr_api";
    private const uint VrApplication_Background = 2;

    private static IntPtr _dllHandle;
    private static bool _initialized;

    /// <summary>
    /// Ruta configurable manualmente por el usuario (Configuración) hacia openvr_api.dll
    /// o hacia la carpeta de instalación de SteamVR. Tiene prioridad sobre la detección automática.
    /// </summary>
    public static string? ManualApiPath { get; set; }

    /// <summary>
    /// Versión de la interfaz de IVRCompositor con la que fueron generados los bindings de
    /// <see cref="CompositorFnTable"/>. El prefijo "FnTable:" le indica a SteamVR que devuelva
    /// un puntero directo a una tabla de punteros a función (sin vtable de C++ real detrás),
    /// que es el mecanismo oficial y estable entre compiladores recomendado por Valve.
    /// </summary>
    public const string CompositorInterfaceVersion = "FnTable:IVRCompositor_029";

    public static void ResetForTesting()
    {
        _initialized = false;
        _dllHandle = IntPtr.Zero;
        ManualApiPath = null;
    }

    public static bool IsDllAvailable()
    {
        if (_initialized)
        {
            return _dllHandle != IntPtr.Zero;
        }

        var path = FindOpenVRApiPath();
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            _initialized = true;
            return false;
        }

        _dllHandle = NativeLibrary.Load(path);
        _initialized = true;
        return _dllHandle != IntPtr.Zero;
    }

    public static IntPtr VR_Init(ref int peError, uint eType)
    {
        if (!IsDllAvailable())
        {
            return IntPtr.Zero;
        }

        var symbol = NativeLibrary.GetExport(_dllHandle, "VR_InitInternal");
        var init = Marshal.GetDelegateForFunctionPointer<VR_InitInternalDelegate>(symbol);
        return init(ref peError, eType);
    }

    public static void VR_Shutdown()
    {
        if (!_initialized || _dllHandle == IntPtr.Zero)
        {
            return;
        }

        try
        {
            var symbol = NativeLibrary.GetExport(_dllHandle, "VR_ShutdownInternal");
            var shutdown = Marshal.GetDelegateForFunctionPointer<VR_ShutdownInternalDelegate>(symbol);
            shutdown();
        }
        catch
        {
            // Ignorar si el símbolo no existe.
        }
    }

    public static IntPtr VR_GetGenericInterface(string pchInterfaceVersion, ref int peError)
    {
        if (!IsDllAvailable())
        {
            return IntPtr.Zero;
        }

        var symbol = NativeLibrary.GetExport(_dllHandle, "VR_GetGenericInterface");
        var getInterface = Marshal.GetDelegateForFunctionPointer<VR_GetGenericInterfaceDelegate>(symbol);

        var versionBytes = Encoding.ASCII.GetBytes(pchInterfaceVersion + '\0');
        var versionPtr = Marshal.AllocHGlobal(versionBytes.Length);
        try
        {
            Marshal.Copy(versionBytes, 0, versionPtr, versionBytes.Length);
            return getInterface(versionPtr, ref peError);
        }
        finally
        {
            Marshal.FreeHGlobal(versionPtr);
        }
    }

    public static uint VR_GetInitToken()
    {
        if (!IsDllAvailable())
        {
            return 0;
        }

        var symbol = NativeLibrary.GetExport(_dllHandle, "VR_GetInitToken");
        var token = Marshal.GetDelegateForFunctionPointer<VR_GetInitTokenDelegate>(symbol);
        return token();
    }

    public static string? FindOpenVRApiPath()
    {
        return FindOpenVRApiPath(ManualApiPath);
    }

    internal static string? FindOpenVRApiPath(string? manualApiPath)
    {
        var candidates = new List<string?>
        {
            ResolveManualApiPath(manualApiPath),
            GetEnvironmentPath("STEAMVR_RUNTIME"),
            GetSteamVRPathFromRegistry(),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam", "steamapps", "common", "SteamVR", "bin", "win64", $"{DllName}.dll"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Steam", "steamapps", "common", "SteamVR", "bin", "win64", $"{DllName}.dll"),
            Path.Combine("C:", "Steam", "steamapps", "common", "SteamVR", "bin", "win64", $"{DllName}.dll"),
        };

        foreach (var path in candidates)
        {
            if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
            {
                return path;
            }
        }

        return null;
    }

    private static string? ResolveManualApiPath(string? manualApiPath)
    {
        if (string.IsNullOrWhiteSpace(manualApiPath))
        {
            return null;
        }

        if (File.Exists(manualApiPath))
        {
            return manualApiPath;
        }

        // El usuario puede haber indicado la carpeta de instalación de SteamVR en lugar
        // de la ruta exacta al .dll; probamos la ubicación estándar dentro de esa carpeta.
        var combined = Path.Combine(manualApiPath, "bin", "win64", $"{DllName}.dll");
        return File.Exists(combined) ? combined : null;
    }

    private static string? GetEnvironmentPath(string variable)
    {
        var value = Environment.GetEnvironmentVariable(variable);
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var candidate = Path.Combine(value, "bin", "win64", $"{DllName}.dll");
        return File.Exists(candidate) ? candidate : null;
    }

    private static string? GetSteamVRPathFromRegistry()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Valve\Steam");
            var installPath = key?.GetValue("InstallPath") as string;
            if (string.IsNullOrWhiteSpace(installPath))
            {
                return null;
            }

            var candidate = Path.Combine(installPath, "steamapps", "common", "SteamVR", "bin", "win64", $"{DllName}.dll");
            return File.Exists(candidate) ? candidate : null;
        }
        catch
        {
            return null;
        }
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate IntPtr VR_InitInternalDelegate(ref int peError, uint eType);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void VR_ShutdownInternalDelegate();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate IntPtr VR_GetGenericInterfaceDelegate(IntPtr pchInterfaceVersion, ref int peError);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate uint VR_GetInitTokenDelegate();
}

/// <summary>
/// Subconjunto truncado de vr::Compositor_FrameTiming (openvr_api.h de Valve).
/// Solo se declaran los campos hasta m_flClientFrameIntervalMs porque SteamVR respeta
/// m_nSize (mecanismo oficial de compatibilidad hacia adelante) y únicamente escribe
/// hasta esa cantidad de bytes en el buffer del llamador; los campos posteriores
/// (pose del HMD, contadores de VSync, etc.) no son necesarios para este adaptador.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct Compositor_FrameTiming
{
    public uint m_nSize;
    public uint m_nFrameIndex;
    public uint m_nNumFramePresents;
    public uint m_nNumMisPresented;
    public uint m_nNumDroppedFrames;
    public uint m_nReprojectionFlags;
    public double m_flSystemTimeInSeconds;
    public float m_flPreSubmitGpuMs;
    public float m_flPostSubmitGpuMs;
    public float m_flTotalRenderGpuMs;
    public float m_flCompositorRenderGpuMs;
    public float m_flCompositorRenderCpuMs;
    public float m_flCompositorIdleCpuMs;
    public float m_flClientFrameIntervalMs;
}

/// <summary>
/// Subconjunto truncado de vr::Compositor_CumulativeStats (openvr_api.h de Valve),
/// por el mismo motivo que <see cref="Compositor_FrameTiming"/>: solo necesitamos
/// los contadores acumulados de frames presentados/dropeados/reproyectados.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct Compositor_CumulativeStats
{
    public uint m_nPid;
    public uint m_nNumFramePresents;
    public uint m_nNumDroppedFrames;
    public uint m_nNumReprojectedFrames;
}

[UnmanagedFunctionPointer(CallingConvention.StdCall)]
[return: MarshalAs(UnmanagedType.I1)]
internal delegate bool GetFrameTimingFn(ref Compositor_FrameTiming pTiming, uint unFramesAgo);

[UnmanagedFunctionPointer(CallingConvention.StdCall)]
internal delegate void GetCumulativeStatsFn(ref Compositor_CumulativeStats pStats, uint nStatsSizeInBytes);

/// <summary>
/// Tabla de punteros a función de IVRCompositor_029, generada siguiendo exactamente el
/// mismo patrón que el binding oficial de Valve (openvr_api.cs): los primeros campos
/// (métodos que no usamos) se declaran como IntPtr para preservar el offset correcto
/// de los campos que sí necesitamos, sin adivinar índices de vtable manualmente.
/// Orden de campos verificado contra headers/openvr_api.cs del repositorio
/// ValveSoftware/openvr (struct IVRCompositor, versión IVRCompositor_029).
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct CompositorFnTable
{
    public IntPtr SetTrackingSpace;
    public IntPtr GetTrackingSpace;
    public IntPtr WaitGetPoses;
    public IntPtr GetLastPoses;
    public IntPtr GetLastPoseForTrackedDeviceIndex;
    public IntPtr GetSubmitTexture;
    public IntPtr Submit;
    public IntPtr SubmitWithArrayIndex;
    public IntPtr ClearLastSubmittedFrame;
    public IntPtr PostPresentHandoff;

    [MarshalAs(UnmanagedType.FunctionPtr)]
    public GetFrameTimingFn GetFrameTiming;

    public IntPtr GetFrameTimings;
    public IntPtr GetFrameTimeRemaining;

    [MarshalAs(UnmanagedType.FunctionPtr)]
    public GetCumulativeStatsFn GetCumulativeStats;
}

internal sealed class OpenVRCompositor : IDisposable
{
    private readonly CompositorFnTable _fnTable;
    private bool _disposed;

    public OpenVRCompositor(IntPtr compositorInterfacePtr)
    {
        // Igual que CVRCompositor en el binding oficial de Valve: el puntero devuelto por
        // VR_GetGenericInterface("FnTable:...") ya ES la tabla de funciones, sin
        // indirección adicional de vtable.
        _fnTable = Marshal.PtrToStructure<CompositorFnTable>(compositorInterfacePtr);
    }

    public bool GetFrameTiming(ref Compositor_FrameTiming timing, uint unFramesAgo)
    {
        if (_disposed || _fnTable.GetFrameTiming is null)
        {
            return false;
        }

        timing.m_nSize = (uint)Marshal.SizeOf<Compositor_FrameTiming>();
        return _fnTable.GetFrameTiming(ref timing, unFramesAgo);
    }

    public void GetCumulativeStats(ref Compositor_CumulativeStats stats, uint size)
    {
        if (_disposed || _fnTable.GetCumulativeStats is null)
        {
            return;
        }

        _fnTable.GetCumulativeStats(ref stats, size);
    }

    public void Dispose()
    {
        _disposed = true;
    }
}

internal sealed class OpenVRContext : IDisposable
{
    private readonly IntPtr _system;
    private readonly OpenVRCompositor _compositor;
    private bool _disposed;

    public OpenVRContext(IntPtr system, OpenVRCompositor compositor)
    {
        _system = system;
        _compositor = compositor;
    }

    public OpenVRCompositor Compositor => _compositor;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _compositor.Dispose();
        OpenVRNative.VR_Shutdown();
        _disposed = true;
    }
}

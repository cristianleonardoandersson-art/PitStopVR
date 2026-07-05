using PitStopVR.Core.Models;
using PitStopVR.Telemetry.Models;
using System.Diagnostics;

namespace PitStopVR.Telemetry.Collectors.Adapters;

public sealed class PerformanceCountersAdapter : IDisposable
{
    private PerformanceCounter? _cpuCounter;
    private PerformanceCounter? _gpuCounter;
    private bool _disposed;

    public bool IsAvailable(MachineProfile machine)
    {
        try
        {
            InitializeCpuCounter();
            _ = _cpuCounter?.NextValue();
            return _cpuCounter is not null;
        }
        catch
        {
            return false;
        }
    }

    public RawSessionSample CaptureSample()
    {
        var sample = new RawSessionSample
        {
            Timestamp = DateTime.Now,
            Source = SessionSourceType.PerformanceCounters
        };

        try
        {
            InitializeCpuCounter();
            if (_cpuCounter is not null)
            {
                sample.CpuUsagePercent = Math.Round(_cpuCounter.NextValue(), 1);
            }
        }
        catch
        {
            // Ignorar si el contador falla.
        }

        try
        {
            InitializeGpuCounter();
            if (_gpuCounter is not null)
            {
                sample.GpuUsagePercent = Math.Round(_gpuCounter.NextValue(), 1);
            }
        }
        catch
        {
            // Ignorar si el contador de GPU no está disponible.
        }

        return sample;
    }

    private void InitializeCpuCounter()
    {
        if (_cpuCounter is not null)
        {
            return;
        }

        _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total", true);
        _cpuCounter.NextValue();
    }

    private void InitializeGpuCounter()
    {
        if (_gpuCounter is not null)
        {
            return;
        }

        try
        {
            var category = new PerformanceCounterCategory("GPU Engine");
            var instances = category.GetInstanceNames();
            var instance = instances.FirstOrDefault(i => i.Contains("engtype_3D", StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(instance))
            {
                _gpuCounter = new PerformanceCounter("GPU Engine", "Utilization Percentage", instance, true);
                _gpuCounter.NextValue();
            }
        }
        catch
        {
            _gpuCounter = null;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _cpuCounter?.Dispose();
        _gpuCounter?.Dispose();
        _disposed = true;
    }
}

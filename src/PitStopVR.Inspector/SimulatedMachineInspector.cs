using PitStopVR.Core.Models;
using PitStopVR.Core.Serialization;
using System.Text.Json;

namespace PitStopVR.Inspector;

public sealed class SimulatedMachineInspector
{
    public MachineProfile Generate()
    {
        return new MachineProfile
        {
            Hardware = new HardwareInfo
            {
                Cpu = "AMD Ryzen 7 7800X3D",
                Gpu = "NVIDIA GeForce RTX 4070 Ti",
                RamBytes = 34359738368,
                Motherboard = "MSI MAG X670E TOMAHAWK WIFI",
                OperatingSystem = "Microsoft Windows 11 Pro"
            },
            Software = new SoftwareInfo
            {
                SteamInstalled = true,
                SteamPath = @"C:\Program Files (x86)\Steam",
                SteamVrInstalled = true,
                SteamVrPath = @"C:\Program Files (x86)\Steam\steamapps\common\SteamVR",
                MetaQuestLinkInstalled = true,
                MetaQuestLinkPath = @"C:\Program Files\Oculus\Support\oculus-client",
                OpenXrRuntimeDetected = true,
                OpenXrRuntimePath = @"C:\Program Files (x86)\Steam\steamapps\common\SteamVR\steamxr_win64.json"
            },
            Games =
            [
                new GameInfo { Id = "244210", Name = "Assetto Corsa", InstallDir = @"C:\Program Files (x86)\Steam\steamapps\common\assettocorsa", Source = GameSource.Steam },
                new GameInfo { Id = "805550", Name = "Assetto Corsa Competizione", InstallDir = @"C:\Program Files (x86)\Steam\steamapps\common\Assetto Corsa Competizione", Source = GameSource.Steam },
                new GameInfo { Id = "690640", Name = "iRacing", InstallDir = @"C:\Program Files (x86)\Steam\steamapps\common\iRacing", Source = GameSource.Steam },
                new GameInfo { Id = "1171680", Name = "Automobilista 2", InstallDir = @"C:\Program Files (x86)\Steam\steamapps\common\Automobilista 2", Source = GameSource.Steam },
                new GameInfo { Id = "1061090", Name = "rFactor 2", InstallDir = @"C:\Program Files (x86)\Steam\steamapps\common\rFactor 2", Source = GameSource.Steam },
                new GameInfo { Id = "amph-2f220080cacc", Name = "Forza Horizon 5", InstallDir = @"C:\XboxGames\Forza Horizon 5", Source = GameSource.Xbox }
            ]
        };
    }

    public string GenerateJson()
    {
        return JsonSerializer.Serialize(Generate(), JsonDefaults.Options);
    }
}

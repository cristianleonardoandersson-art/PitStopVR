using PitStopVR.Knowledge.Models;
using System.Text.Json;

namespace PitStopVR.Knowledge;

public sealed class ProfileLoader
{
    private readonly string _knowledgePath;

    public ProfileLoader(string knowledgePath)
    {
        _knowledgePath = knowledgePath;
    }

    public ProfileSet LoadDefaultProfiles()
    {
        var filePath = Path.Combine(_knowledgePath, "profiles", "default.json");
        var content = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<ProfileSet>(content, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }) ?? throw new InvalidOperationException("No se pudieron cargar los perfiles por defecto.");
    }

    public Profile? GetProfile(string id)
    {
        return LoadDefaultProfiles().Profiles.FirstOrDefault(p =>
            p.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MlAgentToolCli.Infrastructure;

public static class JsonGateway
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = false, // Minified single lines are easier for AI to consume without token bloat
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static void RenderSuccess(object data)
    {
        Console.WriteLine(JsonSerializer.Serialize(new { Status = "Success", Payload = data }, Options));
    }

    public static void RenderError(string code, string message)
    {
        Console.WriteLine(JsonSerializer.Serialize(new { Status = "Error", Code = code, Message = message }, Options));
    }
}

using Frida;

if (args.Length < 2)
{
    Console.Error.WriteLine("usage: AgentBuilder <entrypoint> <output> [projectRoot]");
    return 1;
}

var entrypoint = Path.GetFullPath(args[0]);
var output = Path.GetFullPath(args[1]);
var projectRoot = args.Length > 2 ? Path.GetFullPath(args[2]) : Path.GetDirectoryName(entrypoint)!;

using var compiler = new FridaCompiler();
compiler.OnDiagnostics += (_, e) => ReportDiagnostics(e.Diagnostics);

string bundle;
try
{
    bundle = await compiler.Build(entrypoint, new Frida.Data.BuildOptions { ProjectRoot = projectRoot });
}
catch (GLib.GException e)
{
    Console.Error.WriteLine($"agent compilation failed: {e.Message}");
    return 1;
}

Directory.CreateDirectory(Path.GetDirectoryName(output)!);
File.WriteAllText(output, bundle);
Console.WriteLine($"wrote {output} ({bundle.Length} bytes)");
return 0;

void ReportDiagnostics(object? diagnostics)
{
    foreach (var diagnostic in (object?[])diagnostics!)
        Console.Error.WriteLine(FormatDiagnostic((IDictionary<string, object?>)diagnostic!));
}

string FormatDiagnostic(IDictionary<string, object?> diagnostic)
{
    var category = (string)diagnostic["category"]!;
    var code = (long)diagnostic["code"]!;
    var text = (string)diagnostic["text"]!;
    return $"{FormatLocation(diagnostic)}{category} TS{code}: {text}";
}

string FormatLocation(IDictionary<string, object?> diagnostic)
{
    if (diagnostic["file"] is not IDictionary<string, object?> file)
        return "";

    var path = Path.GetRelativePath(projectRoot, (string)file["path"]!);
    var line = (long)file["line"]! + 1;
    var character = (long)file["character"]! + 1;
    return $"{path}:{line}:{character} - ";
}

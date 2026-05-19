using System.Text.Json;
using FRELODYAPP.Services.ChordDraw;
using FRELODYAPP.Services.Seed;
using FRELODYSHRD.Models.ChordDraw;

if (args.Length == 0)
{
    Console.Error.WriteLine("Usage: FRELODYLIB.Tools <command> [args]");
    Console.Error.WriteLine("Commands:");
    Console.Error.WriteLine("  verify-renderer <samples-dir>   Diff ChordSvgRenderer output vs reference SVGs in dir");
    Console.Error.WriteLine("  emit-seeds <source-json> <out-dir>   Run importer + renderer, write JSON+SVG per voicing");
    return 64;
}

return args[0] switch
{
    "verify-renderer" => VerifyRenderer(args.Skip(1).ToArray()),
    "emit-seeds" => EmitSeeds(args.Skip(1).ToArray()),
    _ => Fail($"Unknown command: {args[0]}")
};

static int Fail(string msg)
{
    Console.Error.WriteLine(msg);
    return 64;
}

static int VerifyRenderer(string[] rest)
{
    if (rest.Length < 1) return Fail("verify-renderer needs <samples-dir>");
    var dir = rest[0];
    if (!Directory.Exists(dir)) return Fail($"Samples dir not found: {dir}");

    // Whitespace-normalized comparison: ChordSvgRenderer emits compact SVG (no newlines
    // between elements), reference samples are multi-line. Visually identical.
    static string Normalize(string s) => System.Text.RegularExpressions.Regex
        .Replace(s, @"\s+", " ")
        .Trim();

    var renderer = new ChordSvgRenderer();
    var jsonOpts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    var jsonFiles = Directory.GetFiles(dir, "*.json", SearchOption.TopDirectoryOnly);

    int pass = 0, fail = 0;
    foreach (var jsonPath in jsonFiles)
    {
        var svgPath = Path.ChangeExtension(jsonPath, ".svg");
        if (!File.Exists(svgPath)) { Console.WriteLine($"SKIP {Path.GetFileName(jsonPath)}"); continue; }

        var data = JsonSerializer.Deserialize<ChordDrawData>(File.ReadAllText(jsonPath), jsonOpts);
        if (data is null) { Console.WriteLine($"FAIL {Path.GetFileName(jsonPath)} (parse)"); fail++; continue; }

        var produced = Normalize(renderer.Render(data));
        var expected = Normalize(File.ReadAllText(svgPath));

        if (produced == expected)
        {
            Console.WriteLine($"PASS {Path.GetFileName(jsonPath)}");
            pass++;
        }
        else
        {
            Console.WriteLine($"FAIL {Path.GetFileName(jsonPath)} (structural mismatch)");
            fail++;
        }
    }

    Console.WriteLine($"\n{pass} passed, {fail} failed");
    return fail == 0 ? 0 : 1;
}

static int EmitSeeds(string[] rest)
{
    if (rest.Length < 2) return Fail("emit-seeds needs <source-json> <out-dir> [rootFilter]");
    var sourceJson = rest[0];
    var outDir = rest[1];
    var rootFilter = rest.Length >= 3 ? rest[2] : null;

    if (!File.Exists(sourceJson)) return Fail($"Source not found: {sourceJson}");
    Directory.CreateDirectory(outDir);

    var importer = new ChordsDbImporter();
    var renderer = new ChordSvgRenderer();
    var voicings = importer.Import(sourceJson);

    if (!string.IsNullOrEmpty(rootFilter))
    {
        voicings = voicings.Where(v => ExtractRoot(v.ChordName) == rootFilter).ToList();
    }

    var grouped = voicings.GroupBy(v => (Root: ExtractRoot(v.ChordName), Suffix: v.ChordName[ExtractRoot(v.ChordName).Length..]));

    var jsonOpts = new JsonSerializerOptions
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };
    int written = 0;
    foreach (var group in grouped)
    {
        var qualityFolder = string.IsNullOrEmpty(group.Key.Suffix) ? "major" : SanitizeFolder(group.Key.Suffix);
        var folder = Path.Combine(outDir, group.Key.Root, qualityFolder);
        Directory.CreateDirectory(folder);

        foreach (var v in group)
        {
            var jsonPath = Path.Combine(folder, v.FileNameStem + ".json");
            var svgPath  = Path.Combine(folder, v.FileNameStem + ".svg");
            File.WriteAllText(jsonPath, JsonSerializer.Serialize(v.Data, jsonOpts));
            File.WriteAllText(svgPath, renderer.Render(v.Data));
            written += 2;
        }
    }

    Console.WriteLine($"Wrote {written} files ({voicings.Count} voicings) to {outDir}");

    var byChord = voicings.GroupBy(v => v.ChordName).Count();
    Console.WriteLine($"  ({byChord} unique chord names)");

    return 0;
}

static string ExtractRoot(string chordName)
{
    if (chordName.Length >= 2 && (chordName[1] == '#' || chordName[1] == 'b'))
        return chordName[..2];
    return chordName.Length > 0 ? chordName[..1] : chordName;
}

static string SanitizeFolder(string s)
{
    var chars = s.Select(c =>
        char.IsLetterOrDigit(c) ? c :
        c == '/' ? '-' :
        c == '#' ? 's' :
        c == '(' || c == ')' ? '_' :
        '_').ToArray();
    return new string(chars);
}


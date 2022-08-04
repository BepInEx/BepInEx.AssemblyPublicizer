using System.CommandLine;
using System.Diagnostics;
using Serilog;

namespace BepInEx.AssemblyPublicizer.Cli;

public sealed class PublicizeCommand : RootCommand
{
    public PublicizeCommand()
    {
        Name = "assembly-publicizer";
        Description = "Publicize given assemblies";

        var input = new Argument<FileSystemInfo[]>("input") { Arity = ArgumentArity.OneOrMore }.ExistingOnly();
        Add(input);

        var output = new Option<string?>("--output").LegalFilePathsOnly();
        Add(output);

        var target = new Option<PublicizeTarget>("--target", () => PublicizeTarget.All, "Targets for publicizing");
        Add(target);

        var publicizeCompilerGenerated = new Option<bool>("--publicize-compiler-generated", "Publicize compiler generated types and members");
        Add(publicizeCompilerGenerated);

        var dontAddAttribute = new Option<bool>("--dont-add-attribute", "Skip injecting OriginalAttributes attribute");
        Add(dontAddAttribute);

        var strip = new Option<bool>("--strip", "Strips all method bodies by setting them to `throw null;`");
        Add(strip);

        var overwrite = new Option<bool>("--overwrite", "Overwrite existing files instead appending a postfix");
        Add(overwrite);

        var disableParallel = new Option<bool>("--disable-parallel", "Don't publicize in parallel");
        Add(disableParallel);

        this.SetHandler(Handle, input, output, target, publicizeCompilerGenerated, dontAddAttribute, strip, overwrite, disableParallel);
    }

    private static void Handle(FileSystemInfo[] input, string? output, PublicizeTarget target, bool publicizeCompilerGenerated, bool dontAddAttribute, bool strip, bool overwrite, bool disableParallel)
    {
        var assemblies = new List<FileInfo>();

        foreach (var fileSystemInfo in input)
        {
            switch (fileSystemInfo)
            {
                case DirectoryInfo directoryInfo:
                    assemblies.AddRange(directoryInfo.GetFiles("*.dll"));
                    break;
                case FileInfo fileInfo:
                    assemblies.Add(fileInfo);
                    break;
            }
        }

        Log.Information("Publicizing {Count} assemblies {Assemblies}", assemblies.Count, assemblies.Select(x => x.Name));

        var options = new AssemblyPublicizerOptions
        {
            Target = target,
            PublicizeCompilerGenerated = publicizeCompilerGenerated,
            IncludeOriginalAttributesAttribute = false,
            Strip = strip,
        };

        var stopwatch = Stopwatch.StartNew();

        void Publicize(FileInfo fileInfo)
        {
            var outputPath = output ?? fileInfo.DirectoryName!;

            if (Directory.Exists(outputPath) || string.IsNullOrEmpty(Path.GetExtension(outputPath)))
            {
                Directory.CreateDirectory(outputPath);
                outputPath = Path.Combine(outputPath, overwrite ? fileInfo.Name : Path.GetFileNameWithoutExtension(fileInfo.Name) + "-publicized" + Path.GetExtension(fileInfo.Name));
            }
            else if (Path.GetFullPath(outputPath) == fileInfo.FullName)
            {
                Log.Warning("Can't write to {OutputPath} without --overwrite flag", outputPath);
                return;
            }

            AssemblyPublicizer.Publicize(fileInfo.FullName, outputPath, options);
            Log.Information("Publicized {InputPath} -> {OutputPath}", fileInfo.Name, outputPath);
        }

        if (disableParallel || assemblies.Count <= 1)
        {
            assemblies.ForEach(Publicize);
        }
        else
        {
            Parallel.ForEach(assemblies, Publicize);
        }

        stopwatch.Stop();
        Log.Information("Done in {Time}", stopwatch.Elapsed);
    }
}

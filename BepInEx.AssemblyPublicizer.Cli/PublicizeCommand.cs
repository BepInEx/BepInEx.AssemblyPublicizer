using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.Diagnostics;
using Serilog;

namespace BepInEx.AssemblyPublicizer.Cli;

public sealed class PublicizeCommand : RootCommand
{
    public PublicizeCommand()
    {
        Name = "assembly-publicizer";
        Description = "Publicize given assemblies";

        Add(new Argument<FileSystemInfo[]>("input") { Arity = ArgumentArity.OneOrMore }.ExistingOnly());
        Add(new Option<string?>(new[] { "--output", "-o" }).LegalFilePathsOnly());
        Add(new Option<PublicizeTarget>("--target", () => PublicizeTarget.All, "Targets for publicizing"));
        Add(new Option<bool>("--publicize-compiler-generated", "Publicize compiler generated types and members"));
        Add(new Option<bool>("--dont-add-attribute", "Skip injecting OriginalAttributes attribute"));
        Add(new Option<bool>("--strip", "Strips all method bodies by setting them to `throw null;`"));
        Add(new Option<bool>("--strip-only", "Strips without publicizing, equivalent to `--target None --strip`"));
        Add(new Option<bool>(new[] { "--overwrite", "-f" }, "Overwrite existing files instead appending a postfix"));
        Add(new Option<bool>("--disable-parallel", "Don't publicize in parallel"));

        Handler = HandlerDescriptor.FromDelegate(Handle).GetCommandHandler();
    }

    private static void Handle(FileSystemInfo[] input, string? output, PublicizeTarget target, bool publicizeCompilerGenerated, bool dontAddAttribute, bool strip, bool stripOnly, bool overwrite, bool disableParallel)
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
            Target = stripOnly ? PublicizeTarget.None : target,
            PublicizeCompilerGenerated = publicizeCompilerGenerated,
            IncludeOriginalAttributesAttribute = false,
            Strip = stripOnly || strip,
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

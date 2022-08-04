using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace BepInEx.AssemblyPublicizer.MSBuild;

public class PublicizeTask : Task
{
    [Required]
    public string IntermediateOutputPath { get; set; }

    [Required]
    public ITaskItem[] ReferencePath { get; set; }

    [Required]
    public ITaskItem[] Publicize { get; set; }

    [Output]
    public ITaskItem[] RemovedReferences { get; private set; }

    [Output]
    public ITaskItem[] PublicizedReferences { get; private set; }

    public override bool Execute()
    {
        var outputDirectory = Path.Combine(IntermediateOutputPath, "publicized");
        Directory.CreateDirectory(outputDirectory);

        var assemblyNamesToPublicize = Publicize.Select(x => x.ItemSpec).ToHashSet();

        var removedReferences = new List<ITaskItem>();
        var publicizedReferences = new List<ITaskItem>();

        foreach (var taskItem in ReferencePath)
        {
            var fileName = taskItem.GetMetadata("FileName");
            var shouldPublicize = taskItem.GetMetadata("Publicize") == "true" || assemblyNamesToPublicize.Contains(fileName);
            if (!shouldPublicize) continue;

            var assemblyPath = taskItem.GetMetadata("FullPath");
            var hash = ComputeHash(File.ReadAllBytes(assemblyPath));

            var publicizedAssemblyPath = Path.Combine(outputDirectory, Path.GetFileName(assemblyPath));
            var hashPath = publicizedAssemblyPath + ".md5";

            removedReferences.Add(taskItem);

            var publicizedReference = new TaskItem(publicizedAssemblyPath);
            taskItem.CopyMetadataTo(publicizedReference);
            publicizedReferences.Add(publicizedReference);

            if (File.Exists(hashPath) && File.ReadAllText(hashPath) == hash)
            {
                Log.LogMessage($"{fileName} was already publicized, skipping");
                continue;
            }

            AssemblyPublicizer.Publicize(assemblyPath, publicizedAssemblyPath);

            var originalDocumentationPath = Path.Combine(Path.GetDirectoryName(assemblyPath)!, fileName + ".xml");
            if (File.Exists(originalDocumentationPath))
            {
                File.Copy(originalDocumentationPath, Path.Combine(outputDirectory, fileName + ".xml"));
            }

            File.WriteAllText(hashPath, hash);

            Log.LogMessage($"Publicized {fileName}");
        }

        RemovedReferences = removedReferences.ToArray();
        PublicizedReferences = publicizedReferences.ToArray();

        return true;
    }

    private static string ComputeHash(byte[] bytes)
    {
        using var md5 = MD5.Create();
        return ByteArrayToString(md5.ComputeHash(bytes));
    }

    private static string ByteArrayToString(IReadOnlyCollection<byte> data)
    {
        var builder = new StringBuilder(data.Count * 2);

        foreach (var b in data)
        {
            builder.AppendFormat("{0:x2}", b);
        }

        return builder.ToString();
    }
}

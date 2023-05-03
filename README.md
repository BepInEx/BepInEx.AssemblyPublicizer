# BepInEx.AssemblyPublicizer

[![NuGet](https://img.shields.io/nuget/v/BepInEx.AssemblyPublicizer?label=BepInEx.AssemblyPublicizer&logo=NuGet)](https://www.nuget.org/packages/BepInEx.AssemblyPublicizer)
[![NuGet](https://img.shields.io/nuget/v/BepInEx.AssemblyPublicizer.MSBuild?label=BepInEx.AssemblyPublicizer.MSBuild&logo=NuGet)](https://www.nuget.org/packages/BepInEx.AssemblyPublicizer.MSBuild)
[![NuGet](https://img.shields.io/nuget/v/BepInEx.AssemblyPublicizer.Cli?label=BepInEx.AssemblyPublicizer.Cli&logo=NuGet)](https://www.nuget.org/packages/BepInEx.AssemblyPublicizer.Cli)

Yet another assembly publicizer/stripper

## Using

### from code
```cs
AssemblyPublicizer.Publicize("./Test.dll", "./Test-publicized.dll");
```

### from console
`dotnet tool install -g BepInEx.AssemblyPublicizer.Cli`  
`assembly-publicizer ./Test.dll` - publicizes  
`assembly-publicizer ./Test.dll --strip` - publicizes and strips method bodies  
`assembly-publicizer ./Test.dll --strip-only` - strips without publicizing  

### from msbuild
```xml
<ItemGroup>
    <PackageReference Include="BepInEx.AssemblyPublicizer.MSBuild" Version="0.4.1" />

    <!-- Publicize directly when referencing -->
    <Reference Include=".../TestProject.dll" Publicize="true" />
    <ProjectReference Include="../TestProject/TestProject.csproj" Publicize="true" />
    <PackageReference Include="TestProject" Publicize="true" />

    <!-- Publicize by assembly name -->
    <Publicize Include="TestProject" />
</ItemGroup>
```

works with both .NET (generates IgnoresAccessChecksTo attributes) and Mono (AllowUnsafeBlocks)

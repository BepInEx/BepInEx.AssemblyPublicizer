# BepInEx.AssemblyPublicizer

Yet another assembly publicizer/stripper

## Using

### from code
```cs
AssemblyPublicizer.Publicize("./Test.dll", "./Test-publicized.dll");
```

### from console
`dotnet tool install -g BepInEx.AssemblyPublicizer.Cli`  
`assembly-publicizer ./Test.dll`

### from msbuild
```xml
<ItemGroup>
    <PackageReference Include="BepInEx.AssemblyPublicizer.MSBuild" Version="1.0.0" />

    <!-- Publicize directly when referencing -->
    <Reference Include=".../TestProject.dll" Publicize="true" />
    <ProjectReference Include="../TestProject/TestProject.csproj" Publicize="true" />
    <PackageReference Include="TestProject" Publicize="true" />

    <!-- Publicize by assembly name -->
    <Publicize Include="TestProject" />
</ItemGroup>
```

works with both .NET (generates IgnoresAccessChecksTo attributes) and Mono (AllowUnsafeBlocks)

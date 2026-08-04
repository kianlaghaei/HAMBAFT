using Ink;

if(args.Length!=2)
{
    Console.Error.WriteLine("Usage: dotnet run --project src/Hambaft.Ink.Compiler -- <source.ink> <compiled.ink.json>");
    return 2;
}
var sourcePath=Path.GetFullPath(args[0]);var outputPath=Path.GetFullPath(args[1]);
if(!File.Exists(sourcePath)){Console.Error.WriteLine($"Ink source not found: {sourcePath}");return 3;}
var errors=new List<string>();
var compiler=new Compiler(await File.ReadAllTextAsync(sourcePath),new Compiler.Options
{
    sourceFilename=sourcePath,
    errorHandler=(message,type)=>errors.Add($"{type}: {message}")
});
var story=compiler.Compile();
if(errors.Count>0||story is null)
{
    foreach(var error in errors)Console.Error.WriteLine(error);
    return 4;
}
Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
await File.WriteAllTextAsync(outputPath,story.ToJson());
Console.WriteLine(outputPath);
return 0;

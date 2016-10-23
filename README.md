# Dazinator.AspNet.Extensions.FileProviders

Provides some additonal `IFileProvider` implementations that others may find useful.


## RequestPathFileProvider 

This let's you wrap an existing `IFileProvider` with one that will resolve all of its files with an additional "path" prepended to the oridinary paths. 

For example, let's say your `IFileProvider` exposes a file on `/myfile.txt`, but that you want to serve the file on `/specialfiles/myfile.txt`. 

You can do this:

```csharp
          
            var originalFileProvider = new PhysicalFileProvider(someDir);
            var sut = new RequestPathFileProvider("/specialfiles", originalFileProvider);

```

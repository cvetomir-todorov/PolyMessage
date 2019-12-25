param(
    [string] $path = $PSScriptRoot,
    [string] $configuration = 'Release'
)

write-host
write-host "Working in: " $path
write-host

dotnet publish -c $configuration -o $path\publish\client .\PolyMessage.LoadTesting.Client\PolyMessage.LoadTesting.Client.csproj
dotnet publish -c $configuration -o $path\publish\server .\PolyMessage.LoadTesting.Server\PolyMessage.LoadTesting.Server.csproj

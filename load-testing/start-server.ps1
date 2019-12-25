param(
    [string] $path = $PSScriptRoot,
    [string] $transport = 'Tcp',
    [string] $tls = 'Tls12',
    [string] $listenAddress = 'tcp://192.168.0.101:10678',
    [string] $format = 'MessagePack',
    [string] $logLevel = 'Information'
)

write-host
write-host "Working in: $path"
write-host

dotnet $path\publish\server\PolyMessage.LoadTesting.Server.dll --transport $transport --tls $tls --listenAddress $listenAddress --format $format --logLevel $logLevel

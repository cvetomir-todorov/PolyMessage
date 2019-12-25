param(
    [string] $path = $PSScriptRoot,
    [string] $transport = 'Tcp',
    [string] $tls = 'Tls12',
    [string] $serverAddress = 'tcp://192.168.0.101:10678',
    [string] $format = 'MessagePack',
    [int] $clients = 1,
    [int] $transactions = 100,
    [string] $logLevel = 'Information'
)

write-host
write-host "Working in: " $path
write-host

dotnet $path\publish\client\PolyMessage.LoadTesting.Client.dll --transport $transport --tls $tls --serverAddress $serverAddress --format $format --clients $clients --transactions $transactions --logLevel $logLevel

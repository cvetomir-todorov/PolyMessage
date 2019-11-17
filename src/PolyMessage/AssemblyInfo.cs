using System.Runtime.CompilerServices;
using PolyMessage.CodeGeneration;

[assembly: InternalsVisibleTo("PolyMessage.IntegrationTests")]
// the emitted assembly requires some internal methods
[assembly: InternalsVisibleTo(ILEmitter.AssemblyName)]

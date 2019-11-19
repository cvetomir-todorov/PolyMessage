using System.Runtime.CompilerServices;
using PolyMessage.CodeGeneration;

[assembly: InternalsVisibleTo("PolyMessage.Tests.Micro")]
[assembly: InternalsVisibleTo("PolyMessage.Tests.Integration")]
// the emitted assembly requires some internal methods
[assembly: InternalsVisibleTo(ILEmitter.AssemblyName)]

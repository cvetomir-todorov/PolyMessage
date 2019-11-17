using System.Collections.Generic;
using PolyMessage.Metadata;

namespace PolyMessage.CodeGeneration
{
    internal interface ICodeGenerator
    {
        void GenerateCode(List<Operation> operations);

        CastTaskOfObjectToTaskOfResponse GetCastTaskOfObjectToTaskOfResponseDelegate();
    }
}

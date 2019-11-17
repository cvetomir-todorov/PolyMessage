using System.Collections.Generic;
using System.Threading.Tasks;
using PolyMessage.Metadata;

namespace PolyMessage.CodeGeneration
{
    internal delegate Task CastTaskOfObjectToTaskOfResponse(int messageID, Task<object> taskOfObject);

    internal delegate Task<object> CastTaskOfResponseToTaskOfObject(int messageID, Task taskOfResponse);

    internal interface ICodeGenerator
    {
        void GenerateCode(List<Operation> operations);

        CastTaskOfObjectToTaskOfResponse GetCastTaskOfObjectToTaskOfResponseDelegate();

        CastTaskOfResponseToTaskOfObject GetCastTaskOfResponseToTaskOfObjectDelegate();
    }
}

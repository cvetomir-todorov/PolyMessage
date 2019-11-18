﻿using System.Collections.Generic;
using System.Threading.Tasks;
using PolyMessage.Metadata;

namespace PolyMessage.CodeGeneration
{
    internal delegate Task CastToTaskOfResponse(int responseID, Task<object> taskOfObject);

    internal delegate Task<object> DispatchRequest(int responseID, object request, object implementor);

    internal interface ICodeGenerator
    {
        void GenerateCode(List<Operation> operations);

        CastToTaskOfResponse GetCastToTaskOfResponse();

        DispatchRequest GetDispatchRequest();
    }
}

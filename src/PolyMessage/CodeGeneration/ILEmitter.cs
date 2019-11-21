using System;
using System.Collections.Generic;
using System.Configuration.Assemblies;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using PolyMessage.Metadata;

namespace PolyMessage.CodeGeneration
{
    // TODO: optimize checks based on the message ID - currently there are a lot of comparisons and binary search may be viable
    internal sealed class ILEmitter : ICodeGenerator
    {
        public const string AssemblyName = "PolyMessage.Emitted";
        private bool _isCodeGenerated;
        private CastToTaskOfResponse _castToTaskOfResponse;
        private DispatchRequest _dispatchRequest;

        public void GenerateCode(List<Operation> operations)
        {
            AssemblyName assemblyName = new AssemblyName(AssemblyName);
            assemblyName.Version = new Version(1, 0);
            assemblyName.VersionCompatibility = AssemblyVersionCompatibility.SameDomain;

            AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndCollect);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule($"{AssemblyName}.Module");

            TypeBuilder staticTypeBuilder = moduleBuilder.DefineType($"{AssemblyName}.StaticType", TypeAttributes.Public);
            MethodBuilder castToTaskOfResponseBuilder = EmitCastToTaskOfResponse(staticTypeBuilder, operations);
            MethodBuilder dispatchRequestBuilder = EmitDispatchRequest(staticTypeBuilder, operations);

            TypeInfo staticType = staticTypeBuilder.CreateTypeInfo();
            MethodInfo castToTaskOfResponseMethod = staticType.GetDeclaredMethod(castToTaskOfResponseBuilder.Name);
            _castToTaskOfResponse = (CastToTaskOfResponse) castToTaskOfResponseMethod.CreateDelegate(typeof(CastToTaskOfResponse));
            MethodInfo dispatchRequestMethod = staticType.GetDeclaredMethod(dispatchRequestBuilder.Name);
            _dispatchRequest = (DispatchRequest) dispatchRequestMethod.CreateDelegate(typeof(DispatchRequest));

            _isCodeGenerated = true;
        }

        public CastToTaskOfResponse GetCastToTaskOfResponse()
        {
            EnsureCodeGenerated();
            return _castToTaskOfResponse;
        }

        public DispatchRequest GetDispatchRequest()
        {
            EnsureCodeGenerated();
            return _dispatchRequest;
        }

        private void EnsureCodeGenerated()
        {
            if (!_isCodeGenerated)
                throw new InvalidOperationException($"The method {nameof(GenerateCode)} should be called first.");
        }

        /// <summary>
        /// The emitted method should look just like the one below.
        /// It should switch the responseID and choose a specific cast.
        /// </summary>
        // public static Task CastToTaskOfResponse(int responseID, Task<object> taskOfObject)
        // {
        //     switch (responseID)
        //     {
        //         case 123: return ILEmitter.GenericCastToTaskOfResponse<Response1>(taskOfObject);
        //         case 456: return ILEmitter.GenericCastToTaskOfResponse<Response2>(taskOfObject);
        //         default:  throw new InvalidOperationException($"Unknown message ID {responseID}.");
        //     }
        // }
        private MethodBuilder EmitCastToTaskOfResponse(TypeBuilder typeBuilder, List<Operation> operations)
        {
            MethodBuilder methodBuilder = typeBuilder.DefineMethod(
                "CastToTaskOfResponse", MethodAttributes.Public | MethodAttributes.Static,
                // the method returns Task (which is actually Task<Response>) and accepts a message ID and the Task<object> to be cast
                typeof(Task), new Type[] {typeof(int), typeof(Task<object>)});
            ILGenerator il = methodBuilder.GetILGenerator();

            Label defaultCase = il.DefineLabel();
            Label[] labels = new Label[operations.Count];
            for (int i = 0; i < operations.Count; ++i)
            {
                labels[i] = il.DefineLabel();
            }

            for (int i = 0; i < operations.Count; ++i)
            {
                // branch if message ID (arg0) is equal to the respective operation response ID
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldc_I4, operations[i].ResponseID);
                il.Emit(OpCodes.Beq_S, labels[i]);
            }

            // branch to default case
            il.Emit(OpCodes.Br_S, defaultCase);

            MethodInfo genericCastMethod = GetType().GetMethod(nameof(GenericCastToTaskOfResponse));
            if (genericCastMethod == null)
                throw new InvalidOperationException($"Could not find generic cast method {nameof(GenericCastToTaskOfResponse)}.");

            for (int i = 0; i < operations.Count; ++i)
            {
                MethodInfo specificCast = genericCastMethod.MakeGenericMethod(operations[i].ResponseType);

                il.MarkLabel(labels[i]);
                // call the specific cast with the Task<object> and return the resulting Task<ResponseX> as Task
                il.Emit(OpCodes.Ldarg_1);
                il.EmitCall(OpCodes.Call, specificCast, null);
                il.Emit(OpCodes.Ret);
            }

            MethodInfo stringFormat = typeof(string).GetMethod(nameof(string.Format), new Type[] {typeof(string), typeof(int)});
            Type exceptionType = typeof(InvalidOperationException);
            ConstructorInfo exceptionConstructor = exceptionType.GetConstructor(new Type[] {typeof(string)});

            il.MarkLabel(defaultCase);
            // format the exception message
            il.Emit(OpCodes.Ldstr, "Unknown response ID {0}.");
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Box, typeof(int));
            il.EmitCall(OpCodes.Call, stringFormat, null);
            // throw an exception
            il.Emit(OpCodes.Newobj, exceptionConstructor);
            il.Emit(OpCodes.Throw);

            return methodBuilder;
        }

        /// <summary>
        /// We declare the cast method here because we have to await and it's hard to emit all that code.
        /// </summary>
        public static async Task<TResponse> GenericCastToTaskOfResponse<TResponse>(Task<object> taskOfObject)
        {
            return (TResponse) await taskOfObject.ConfigureAwait(false);
        }

        /// <summary>
        /// The emitted method should look just like the one below.
        /// It should switch the responseID and choose a specific cast.
        /// </summary>
        // public static Task<object> DispatchRequest(int responseID, object request, object implementor)
        // {
        //     switch (responseID)
        //     {
        //         case 123:
        //             Task<Response1> responseTask1 = ((IContract1)implementor).Operation1((Request1)request);
        //             return CastPlaceHolder.GenericCast(responseTask1);
        //         case 456:
        //             Task<Response5> responseTask5 = ((IContract2)implementor).Operation5((Request5)request);
        //             return CastPlaceHolder.GenericCast(responseTask5);
        //         default: throw new InvalidOperationException($"Unknown request ID {requestID}.");
        //     }
        // }
        private MethodBuilder EmitDispatchRequest(TypeBuilder typeBuilder, List<Operation> operations)
        {
            MethodBuilder methodBuilder = typeBuilder.DefineMethod(
                "DispatchRequest", MethodAttributes.Public | MethodAttributes.Static,
                // the method returns Task<object> and accepts requestID, request and contract implementor
                typeof(Task<object>), new Type[] {typeof(int), typeof(object), typeof(object)});
            ILGenerator il = methodBuilder.GetILGenerator();

            Label defaultCase = il.DefineLabel();
            Label[] labels = new Label[operations.Count];
            for (int i = 0; i < operations.Count; ++i)
            {
                labels[i] = il.DefineLabel();
            }

            for (int i = 0; i < operations.Count; ++i)
            {
                // branch if message ID (arg0) is equal to the respective operation response ID
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldc_I4, operations[i].ResponseID);
                il.Emit(OpCodes.Beq_S, labels[i]);
            }

            // branch to default case
            il.Emit(OpCodes.Br_S, defaultCase);

            MethodInfo genericCastMethod = GetType().GetMethod(nameof(GenericCastToTaskOfObject));
            if (genericCastMethod == null)
                throw new InvalidOperationException($"Could not find generic cast method {nameof(GenericCastToTaskOfObject)}.");

            for (int i = 0; i < operations.Count; ++i)
            {
                Operation operation = operations[i];
                MethodInfo specificCast = genericCastMethod.MakeGenericMethod(operation.ResponseType);

                il.MarkLabel(labels[i]);
                // cast the implementor and the request to the operation's contract and request types
                il.Emit(OpCodes.Ldarg_2);
                il.Emit(OpCodes.Castclass, operation.ContractType);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Castclass, operation.RequestType);
                // call the operation method, cast the resulting Task<Response> to Task<object> and return it
                il.EmitCall(OpCodes.Callvirt, operation.Method, null);
                il.EmitCall(OpCodes.Call, specificCast, null);
                il.Emit(OpCodes.Ret);
            }

            MethodInfo stringFormat = typeof(string).GetMethod(nameof(string.Format), new Type[] { typeof(string), typeof(int) });
            Type exceptionType = typeof(InvalidOperationException);
            ConstructorInfo exceptionConstructor = exceptionType.GetConstructor(new Type[] { typeof(string) });

            il.MarkLabel(defaultCase);
            // format the exception message
            il.Emit(OpCodes.Ldstr, "Unknown responseID ID {0}.");
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Box, typeof(int));
            il.EmitCall(OpCodes.Call, stringFormat, null);
            // throw an exception
            il.Emit(OpCodes.Newobj, exceptionConstructor);
            il.Emit(OpCodes.Throw);

            return methodBuilder;
        }

        /// <summary>
        /// We declare the cast method here because we have to await and it's hard to emit all that code.
        /// </summary>
        public static async Task<object> GenericCastToTaskOfObject<TResponse>(Task<TResponse> taskOfResponse)
        {
            return await taskOfResponse.ConfigureAwait(false);
        }
    }
}

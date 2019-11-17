using System;
using System.Collections.Generic;
using System.Configuration.Assemblies;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using PolyMessage.Metadata;

namespace PolyMessage.CodeGeneration
{
    internal sealed class ILEmitter : ICodeGenerator
    {
        public const string AssemblyName = "PolyMessage.Emitted";
        private CastTaskOfObjectToTaskOfResponse _toResponseTaskDelegate;
        private CastTaskOfResponseToTaskOfObject _toObjectTaskDelegate;

        public void GenerateCode(List<Operation> operations)
        {
            AssemblyName assemblyName = new AssemblyName(AssemblyName);
            assemblyName.Version = new Version(1, 0);
            assemblyName.VersionCompatibility = AssemblyVersionCompatibility.SameDomain;

            AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndCollect);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule($"{AssemblyName}.Module");

            TypeBuilder taskCasterBuilder = moduleBuilder.DefineType($"{AssemblyName}.TaskCaster", TypeAttributes.Public);
            MethodBuilder toResponseTaskBuilder = EmitToResponseTaskCast(taskCasterBuilder, operations);
            MethodBuilder toObjectTaskBuilder = EmitToObjectTaskCast(taskCasterBuilder, operations);

            TypeInfo taskCasterType = taskCasterBuilder.CreateTypeInfo();
            MethodInfo toResponseTaskMethod = taskCasterType.GetDeclaredMethod(toResponseTaskBuilder.Name);
            _toResponseTaskDelegate = (CastTaskOfObjectToTaskOfResponse) toResponseTaskMethod.CreateDelegate(typeof(CastTaskOfObjectToTaskOfResponse));
            MethodInfo toObjectTaskMethod = taskCasterType.GetDeclaredMethod(toObjectTaskBuilder.Name);
            _toObjectTaskDelegate = (CastTaskOfResponseToTaskOfObject) toObjectTaskMethod.CreateDelegate(typeof(CastTaskOfResponseToTaskOfObject));
        }

        public CastTaskOfObjectToTaskOfResponse GetCastTaskOfObjectToTaskOfResponseDelegate()
        {
            if (_toResponseTaskDelegate == null)
                throw new InvalidOperationException($"The method {nameof(GenerateCode)} should be called first.");

            return _toResponseTaskDelegate;
        }

        public CastTaskOfResponseToTaskOfObject GetCastTaskOfResponseToTaskOfObjectDelegate()
        {
            if (_toObjectTaskDelegate == null)
                throw new InvalidOperationException($"The method {nameof(GenerateCode)} should be called first.");

            return _toObjectTaskDelegate;
        }

        /// <summary>
        /// The emitted method should look just like the one below.
        /// It should switch the messageID and choose a specific cast.
        /// </summary>
        // public static Task CastTaskOfObjectToTaskOfResponse(int messageID, Task<object> taskOfObject)
        // {
        //     switch (messageID)
        //     {
        //         case 123: return ILEmitter.GenericToResponseTaskCast<ResponseAlpha>(taskOfObject);
        //         case 456: return ILEmitter.GenericToResponseTaskCast<ResponseBeta>(taskOfObject);
        //         default:  throw new InvalidOperationException($"Unknown message ID {messageID}.");
        //     }
        // }
        private MethodBuilder EmitToResponseTaskCast(TypeBuilder typeBuilder, List<Operation> operations)
        {
            MethodBuilder methodBuilder = typeBuilder.DefineMethod(
                "CastTaskOfObjectToTaskOfResponse", MethodAttributes.Public | MethodAttributes.Static,
                // the method returns Task (which is actually Task<Response>) and accepts a message ID and the Task<object> to be cast
                typeof(Task), new Type[] {typeof(int), typeof(Task<object>)});
            ILGenerator il = methodBuilder.GetILGenerator();

            Label defaultCase = il.DefineLabel();
            Label[] labels = new Label[operations.Count];
            for (int i = 0; i < operations.Count; ++i)
            {
                labels[i] = il.DefineLabel();
            }

            // TODO: consider optimizing these checks - currently there are a lot of comparisons
            for (int i = 0; i < operations.Count; ++i)
            {
                // branch if message ID (arg0) is equal to the respective operation response ID
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldc_I4, operations[i].ResponseID);
                il.Emit(OpCodes.Beq_S, labels[i]);
            }

            // branch to default case
            il.Emit(OpCodes.Br_S, defaultCase);

            MethodInfo genericCastMethod = GetType().GetMethod(nameof(GenericToResponseTaskCast));
            if (genericCastMethod == null)
                throw new InvalidOperationException($"Could not find generic cast method {nameof(GenericToResponseTaskCast)}.");

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
            il.Emit(OpCodes.Ldstr, "Unknown message ID {0}.");
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
        public static async Task<TResponse> GenericToResponseTaskCast<TResponse>(Task<object> taskOfObject)
        {
            return (TResponse) await taskOfObject.ConfigureAwait(false);
        }

        /// <summary>
        /// The emitted method should look just like the one below.
        /// It should switch the messageID and choose a specific cast.
        /// </summary>
        // public static Task<object> CastTaskOfObjectToTaskOfResponse(int messageID, Task taskOfResponse)
        // {
        //     switch (messageID)
        //     {
        //         case 123: return ILEmitter.GenericToObjectTaskCast<ResponseAlpha>((Task<ResponseAlpha>) taskOfResponse);
        //         case 456: return ILEmitter.GenericToObjectTaskCast<ResponseBeta>(Task<ResponseBeta> taskOfResponse);
        //         default:  throw new InvalidOperationException($"Unknown message ID {messageID}.");
        //     }
        // }
        private MethodBuilder EmitToObjectTaskCast(TypeBuilder typeBuilder, List<Operation> operations)
        {
            MethodBuilder methodBuilder = typeBuilder.DefineMethod(
                "CastTaskOfResponseToTaskOfObject", MethodAttributes.Public | MethodAttributes.Static,
                // the method returns Task (which is actually Task<Response>) and accepts a message ID and the Task<object> to be cast
                typeof(Task<object>), new Type[] {typeof(int), typeof(Task)});
            ILGenerator il = methodBuilder.GetILGenerator();

            Label defaultCase = il.DefineLabel();
            Label[] labels = new Label[operations.Count];
            for (int i = 0; i < operations.Count; ++i)
            {
                labels[i] = il.DefineLabel();
            }

            // TODO: consider optimizing these checks - currently there are a lot of comparisons
            for (int i = 0; i < operations.Count; ++i)
            {
                // branch if message ID (arg0) is equal to the respective operation response ID
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldc_I4, operations[i].ResponseID);
                il.Emit(OpCodes.Beq_S, labels[i]);
            }

            // branch to default case
            il.Emit(OpCodes.Br_S, defaultCase);

            MethodInfo genericCastMethod = GetType().GetMethod(nameof(GenericToResponseTaskCast));
            if (genericCastMethod == null)
                throw new InvalidOperationException($"Could not find generic cast method {nameof(GenericToResponseTaskCast)}.");

            for (int i = 0; i < operations.Count; ++i)
            {
                //Type specificTaskType = typeof(Task<>).MakeGenericType(operations[i].ResponseType);
                //MethodInfo specificCast = genericCastMethod.MakeGenericMethod(operations[i].ResponseType);

                il.MarkLabel(labels[i]);
                // call the specific cast with the Task cast to Task<ResponseX> and return the resulting Task<object>
                il.Emit(OpCodes.Ldarg_1);
                // Looks like the actual casting is not needed to make things work which is very strange
                //il.Emit(OpCodes.Castclass, specificTaskType);
                //il.EmitCall(OpCodes.Call, specificCast, null);
                il.Emit(OpCodes.Ret);
            }

            MethodInfo stringFormat = typeof(string).GetMethod(nameof(string.Format), new Type[] { typeof(string), typeof(int) });
            Type exceptionType = typeof(InvalidOperationException);
            ConstructorInfo exceptionConstructor = exceptionType.GetConstructor(new Type[] { typeof(string) });

            il.MarkLabel(defaultCase);
            // format the exception message
            il.Emit(OpCodes.Ldstr, "Unknown message ID {0}.");
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
        public static async Task<object> GenericToObjectTaskCast<TResponse>(Task<TResponse> taskOfResponse)
        {
            return await taskOfResponse.ConfigureAwait(false);
        }
    }
}

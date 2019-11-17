using System;
using System.Collections.Generic;
using System.Configuration.Assemblies;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using PolyMessage.Metadata;

namespace PolyMessage.CodeGeneration
{
    internal delegate Task CastTaskOfObjectToTaskOfResponse(int messageID, Task<object> taskOfObject);

    internal sealed class ILEmitter : ICodeGenerator
    {
        public const string AssemblyName = "PolyMessage.Emitted";
        private CastTaskOfObjectToTaskOfResponse _castDelegate;

        public void GenerateCode(List<Operation> operations)
        {
            AssemblyName assemblyName = new AssemblyName(AssemblyName);
            assemblyName.Version = new Version(1, 0);
            assemblyName.VersionCompatibility = AssemblyVersionCompatibility.SameDomain;

            AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndCollect);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule($"{AssemblyName}.Module");

            TypeBuilder taskCasterBuilder = moduleBuilder.DefineType($"{AssemblyName}.TaskCaster", TypeAttributes.Public);
            MethodBuilder methodBuilder = EmitMethod(taskCasterBuilder, operations);
            TypeInfo emittedType = taskCasterBuilder.CreateTypeInfo();
            MethodInfo emittedMethod = emittedType.GetDeclaredMethod(methodBuilder.Name);
            _castDelegate = (CastTaskOfObjectToTaskOfResponse) emittedMethod.CreateDelegate(typeof(CastTaskOfObjectToTaskOfResponse));
        }

        public CastTaskOfObjectToTaskOfResponse GetCastTaskOfObjectToTaskOfResponseDelegate()
        {
            if (_castDelegate == null)
                throw new InvalidOperationException($"The method {nameof(GenerateCode)} should be called first.");

            return _castDelegate;
        }

        /// <summary>
        /// The emitted method should look just like the one below.
        /// It should switch the messageID and choose a specific cast.
        /// </summary>
        // public static Task Cast(int messageID, Task<object> taskObject)
        // {
        //     switch (messageID)
        //     {
        //         case 1234: return ILEmitter.GenericCast<ResponseAlpha>(taskObject);
        //         case 5678: return ILEmitter.GenericCast<ResponseBeta>(taskObject);
        //         default:  throw new InvalidOperationException($"Unknown message ID {messageID}.");
        //     }
        // }
        private MethodBuilder EmitMethod(TypeBuilder typeBuilder, List<Operation> operations)
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

            MethodInfo genericCastMethod = GetType().GetMethod(nameof(GenericCast));
            if (genericCastMethod == null)
                throw new InvalidOperationException($"Could not find generic cast method {nameof(GenericCast)}.");

            for (int i = 0; i < operations.Count; ++i)
            {
                MethodInfo specificCast = genericCastMethod.MakeGenericMethod(operations[i].ResponseType);

                il.MarkLabel(labels[i]);
                // call the specific cast with the Task<object> and return the resulting Task<ResponseX> as Task
                il.Emit(OpCodes.Ldarg_1);
                il.EmitCall(OpCodes.Call, specificCast, null);
                il.Emit(OpCodes.Ret);
            }

            MethodInfo stringFormat = typeof(string).GetMethod("Format", new Type[] {typeof(string), typeof(int)});
            Type exceptionType = typeof(InvalidOperationException);
            ConstructorInfo exceptionConstructor = exceptionType.GetConstructor(new Type[] {typeof(string)});

            il.MarkLabel(defaultCase);
            // format the exception message
            il.Emit(OpCodes.Ldstr, "Unknown message ID {0}.");
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Box, typeof(int));
            il.EmitCall(OpCodes.Call, stringFormat, new Type[0]);
            // throw an exception
            il.Emit(OpCodes.Newobj, exceptionConstructor);
            il.Emit(OpCodes.Throw);

            return methodBuilder;
        }

        /// <summary>
        /// We declare the cast method here because we have to await and it's hard to emit all that code.
        /// </summary>
        public static async Task<TResponse> GenericCast<TResponse>(Task<object> taskObject)
        {
            return (TResponse) await taskObject.ConfigureAwait(false);
        }
    }
}

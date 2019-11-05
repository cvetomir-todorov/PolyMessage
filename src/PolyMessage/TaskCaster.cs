using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace PolyMessage
{
    /// <summary>
    /// Task{Result} is not covariant so we cannot cast Task{Result} to Task{object}.
    /// </summary>
    internal interface ITaskCaster
    {
        Task<object> CastTaskResultToTaskObject(object taskOfResultType, Type resultType);

        object CastTaskObjectToTaskResult(Task<object> task, Type resultType);
    }

    // TODO: make casting faster
    internal sealed class TaskCaster : ITaskCaster
    {
        // result to object
        private readonly MethodInfo _resultToObjectMethod;
        private readonly Dictionary<Type, MethodInfo> _resultToObjectMap;
        private readonly object _resultToObjectLock;
        // object to result
        private readonly MethodInfo _objectToResultMethod;
        private readonly Dictionary<Type, MethodInfo> _objectToResultMap;
        private readonly object _objectToResultLock;

        public TaskCaster()
        {
            _resultToObjectMethod = GetType().GetMethod(nameof(ResultToObject), BindingFlags.Static | BindingFlags.NonPublic);
            _resultToObjectMap = new Dictionary<Type, MethodInfo>();
            _resultToObjectLock = new object();

            _objectToResultMethod = GetType().GetMethod(nameof(ObjectToResult), BindingFlags.Static | BindingFlags.NonPublic);
            _objectToResultMap = new Dictionary<Type, MethodInfo>();
            _objectToResultLock = new object();
        }

        public Task<object> CastTaskResultToTaskObject(object taskOfResultType, Type resultType)
        {
            MethodInfo specificMethod = GetResultToObjectMethod(resultType);
            Task<object> task = (Task<object>) specificMethod.Invoke(null, new object[] {taskOfResultType});
            return task;
        }

        private MethodInfo GetResultToObjectMethod(Type resultType)
        {
            MethodInfo specificMethod;
            if (!_resultToObjectMap.TryGetValue(resultType, out specificMethod))
            {
                lock (_resultToObjectLock)
                {
                    if (!_resultToObjectMap.TryGetValue(resultType, out specificMethod))
                    {
                        specificMethod = _resultToObjectMethod.MakeGenericMethod(resultType);
                        _resultToObjectMap.Add(resultType, specificMethod);
                    }
                }
            }

            return specificMethod;
        }

        private static async Task<object> ResultToObject<TSource>(Task<TSource> sourceTask)
        {
            object destination = await sourceTask.ConfigureAwait(false);
            return destination;
        }

        public object CastTaskObjectToTaskResult(Task<object> task, Type resultType)
        {
            MethodInfo specificMethod = GetObjectToResultMethod(resultType);
            object taskOfResultType = specificMethod.Invoke(null, new object[] {task});
            return taskOfResultType;
        }

        private MethodInfo GetObjectToResultMethod(Type resultType)
        {
            MethodInfo specificMethod;
            if (!_objectToResultMap.TryGetValue(resultType, out specificMethod))
            {
                lock (_objectToResultLock)
                {
                    if (!_objectToResultMap.TryGetValue(resultType, out specificMethod))
                    {
                        specificMethod = _objectToResultMethod.MakeGenericMethod(resultType);
                        _objectToResultMap.Add(resultType, specificMethod);
                    }
                }
            }

            return specificMethod;
        }

        private static async Task<TDestination> ObjectToResult<TDestination>(Task<object> sourceTask)
        {
            TDestination destination = (TDestination)await sourceTask.ConfigureAwait(false);
            return destination;
        }
    }
}

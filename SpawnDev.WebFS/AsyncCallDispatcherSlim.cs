using SpawnDev.BlazorJS.WebWorkers;
using System.Linq.Expressions;
using System.Reflection;

namespace SpawnDev.WebFS
{
    /// <summary>
    /// A slimmed down version of AsyncCallDispatcher<br/>
    /// Supports GetService interface proxies and Run expressions for method calls only (not getters or setters.)
    /// </summary>
    public abstract class AsyncCallDispatcherSlim
    {
        /// <summary>
        /// All calls are handled by this method.<br/>
        /// Usually serialized, and sent somewhere else to be ran.
        /// </summary>
        /// <param name="serviceType"></param>
        /// <param name="methodInfo"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        protected abstract Task<object?> Call(Type serviceType, MethodInfo methodInfo, object?[]? args = null);

        #region Expressions
        /// <summary>
        /// Converts an Expression into a MethodInfo and a call arguments array<br />
        /// Then calls DispatchCall with them
        /// </summary>
        /// <param name="expr"></param>
        /// <param name="argsExt"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        protected Task<object?> CallStatic(Expression expr, object?[]? argsExt = null)
        {
            if (expr is MethodCallExpression methodCallExpression)
            {
                var methodInfo = methodCallExpression.Method;
                var serviceType = methodInfo.ReflectedType;
                var args = methodCallExpression.Arguments.Select(arg => Expression.Lambda<Func<object>>(Expression.Convert(arg, typeof(object)), null).Compile()()).ToArray();
                return Call(serviceType!, methodInfo, args);
            }
            else if (expr is MemberExpression memberExpression)
            {
                if (argsExt == null || argsExt.Length == 0)
                {
                    // get call
                    if (memberExpression.Member is PropertyInfo propertyInfo)
                    {
                        var methodInfo = propertyInfo.GetMethod;
                        if (methodInfo == null)
                        {
                            throw new Exception("Property getter does not exist.");
                        }
                        var serviceType = methodInfo.ReflectedType;
                        return Call(serviceType!, methodInfo);
                    }
                    else if (memberExpression.Member is FieldInfo fieldInfo)
                    {
                        throw new Exception("Fields are not supported. Properties are supported.");
                    }
                    throw new Exception("Property getter does not exist.");
                }
                else
                {
                    // set call
                    if (memberExpression.Member is PropertyInfo propertyInfo)
                    {
                        var methodInfo = propertyInfo.SetMethod;
                        if (methodInfo == null)
                        {
                            throw new Exception("Property setter does not exist.");
                        }
                        var serviceType = methodInfo.ReflectedType;
                        return Call(serviceType!, methodInfo, argsExt);
                    }
                    else if (memberExpression.Member is FieldInfo fieldInfo)
                    {
                        throw new Exception("Fields are not supported. Properties are supported.");
                    }
                    throw new Exception("Property setter does not exist.");
                }
            }
            else if (expr is NewExpression newExpression)
            {
                throw new Exception("Run does not support constructors. Use New()");
            }
            else
            {
                throw new Exception($"Unsupported dispatch call: {expr.GetType().Name}");
            }
        }
        /// <summary>
        /// Converts an Expression into a MethodInfo and a call arguments array<br />
        /// Then calls DispatchCall with them
        /// </summary>
        /// <param name="serviceType"></param>
        /// <param name="expr"></param>
        /// <param name="argsExt"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        protected Task<object?> Call(Type serviceType, Expression expr, object?[]? argsExt = null)
        {
            if (expr is MethodCallExpression methodCallExpression)
            {
                var methodInfo = methodCallExpression.Method;
                var args = methodCallExpression.Arguments.Select(arg => Expression.Lambda<Func<object>>(Expression.Convert(arg, typeof(object)), null).Compile()()).ToArray();
                return Call(serviceType, methodInfo, args);
            }
            else if (expr is MemberExpression memberExpression)
            {
                if (argsExt == null || argsExt.Length == 0)
                {
                    // get call
                    if (memberExpression.Member is PropertyInfo propertyInfo)
                    {
                        var methodInfo = propertyInfo.GetMethod;
                        if (methodInfo == null)
                        {
                            throw new Exception("Property getter does not exist.");
                        }
                        return Call(serviceType, methodInfo);
                    }
                    else if (memberExpression.Member is FieldInfo fieldInfo)
                    {
                        throw new Exception("Fields are not supported. Properties are supported.");
                    }
                    throw new Exception("Property getter does not exist.");
                }
                else
                {
                    // set call
                    if (memberExpression.Member is PropertyInfo propertyInfo)
                    {
                        var methodInfo = propertyInfo.SetMethod;
                        if (methodInfo == null)
                        {
                            throw new Exception("Property setter does not exist.");
                        }
                        return Call(serviceType, methodInfo, argsExt);
                    }
                    else if (memberExpression.Member is FieldInfo fieldInfo)
                    {
                        throw new Exception("Fields are not supported. Properties are supported.");
                    }
                    throw new Exception("Property setter does not exist.");
                }
            }
            else if (expr is NewExpression newExpression)
            {
                throw new Exception("Run does not support constructors. Use New()");
            }
            else
            {
                throw new Exception($"Unsupported dispatch call: {expr.GetType().Name}");
            }
        }

        #region Non-Keyed
        // Static
        // Method Calls
        // Action
        /// <summary>
        /// Call a method or get the value of a property
        /// </summary>
        /// <param name="expr"></param>
        /// <returns></returns>
        public async Task Run(Expression<Action> expr) => await CallStatic(expr.Body);
        // Func<Task>
        /// <summary>
        /// Call a method or get the value of a property
        /// </summary>
        /// <param name="expr"></param>
        /// <returns></returns>
        public async Task Run(Expression<Func<Task>> expr) => await CallStatic(expr.Body);
        // Func<ValueTask>
        /// <summary>
        /// Call a method or get the value of a property
        /// </summary>
        /// <param name="expr"></param>
        /// <returns></returns>
        public async Task Run(Expression<Func<ValueTask>> expr) => await CallStatic(expr.Body);
        // Func<...,TResult>
        /// <summary>
        /// Call a method or get the value of a property
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="expr"></param>
        /// <returns></returns>
        public async Task<TResult> Run<TResult>(Expression<Func<TResult>> expr) => (TResult)(await CallStatic(expr.Body))!;
        // Func<...,Task<TResult>>
        /// <summary>
        /// Call a method or get the value of a property
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="expr"></param>
        /// <returns></returns>
        public async Task<TResult> Run<TResult>(Expression<Func<Task<TResult>>> expr) => (TResult)(await CallStatic(expr.Body))!;
        // Func<...,ValueTask<TResult>>
        /// <summary>
        /// Call a method or get the value of a property
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="expr"></param>
        /// <returns></returns>
        public async Task<TResult> Run<TResult>(Expression<Func<ValueTask<TResult>>> expr) => (TResult)(await CallStatic(expr.Body))!;

        // Instance
        // Method Calls and Property Getters
        // Action
        /// <summary>
        /// Call a service method or get the value of a service property
        /// </summary>
        /// <typeparam name="TInstance"></typeparam>
        /// <param name="expr"></param>
        /// <returns></returns>
        public async Task Run<TInstance>(Expression<Action<TInstance>> expr) => await Call(typeof(TInstance), expr.Body);
        // Func<Task>
        /// <summary>
        /// Call a service method or get the value of a service property
        /// </summary>
        /// <typeparam name="TInstance"></typeparam>
        /// <param name="expr"></param>
        /// <returns></returns>
        public async Task Run<TInstance>(Expression<Func<TInstance, Task>> expr) => await Call(typeof(TInstance), expr.Body);
        // Func<ValueTask>
        /// <summary>
        /// Call a service method or get the value of a service property
        /// </summary>
        /// <typeparam name="TInstance"></typeparam>
        /// <param name="expr"></param>
        /// <returns></returns>
        public async Task Run<TInstance>(Expression<Func<TInstance, ValueTask>> expr) => await Call(typeof(TInstance), expr.Body);
        // Func<...,TResult>
        /// <summary>
        /// Call a service method or get the value of a service property
        /// </summary>
        /// <typeparam name="TInstance"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="expr"></param>
        /// <returns></returns>
        public async Task<TResult> Run<TInstance, TResult>(Expression<Func<TInstance, TResult>> expr) => (TResult)(await Call(typeof(TInstance), expr.Body))!;
        // Func<...,Task<TResult>>
        /// <summary>
        /// Call a service method or get the value of a service property
        /// </summary>
        /// <typeparam name="TInstance"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="expr"></param>
        /// <returns></returns>
        public async Task<TResult> Run<TInstance, TResult>(Expression<Func<TInstance, Task<TResult>>> expr) => (TResult)(await Call(typeof(TInstance), expr.Body))!;
        // Func<...,ValueTask<TResult>>
        /// <summary>
        /// Call a service method or get the value of a service property
        /// </summary>
        /// <typeparam name="TInstance"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="expr"></param>
        /// <returns></returns>
        public async Task<TResult> Run<TInstance, TResult>(Expression<Func<TInstance, ValueTask<TResult>>> expr) => (TResult)(await Call(typeof(TInstance), expr.Body))!;
        #endregion
        #endregion

        #region DispatchProxy
        Dictionary<Type, object> ServiceInterfaces = new Dictionary<Type, object>();
        /// <summary>
        /// Returns a service call dispatcher that can call async methods using the returned interface
        /// </summary>
        /// <typeparam name="TServiceInterface"></typeparam>
        /// <returns></returns>
        public TServiceInterface GetService<TServiceInterface>() where TServiceInterface : class
        {
            var typeofT = typeof(TServiceInterface);
            if (ServiceInterfaces.TryGetValue(typeofT, out var serviceWorker)) return (TServiceInterface)serviceWorker;
            var ret = InterfaceCallDispatcher<TServiceInterface>.CreateInterfaceDispatcher(Call);
            ServiceInterfaces[typeofT] = ret;
            return ret;
        }
        #endregion
    }
}

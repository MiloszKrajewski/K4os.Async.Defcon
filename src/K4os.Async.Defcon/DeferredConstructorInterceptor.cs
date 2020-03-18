using System;
using System.Threading;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using K4os.Async.Defcon.Internal;

namespace K4os.Async.Defcon
{
	/*
	 * Intercepts for async code are hard. You think they are easy and
	 * then you are buried in corner cases for next two weeks.
	 *
	 * https://github.com/castleproject/Core/blob/master/docs/dynamicproxy-async-interception.md
	 *
	 * Quote:
	 * "As you have seen, async interception is only trivial in very simple scenarios,
	 * but can get quite complex very quickly."
	 *
	 * One of main problems is the fact that task is a class not interface. Seems benign but
	 * Task<B> cannot be used in place of Task<A> even if B inherits from A. This leads to
	 * situations where type of Task must precisely the same as expected. But this is generic
	 * interceptor (the same for all calls, so they are all downgraded to Task<object>).
	 */

	/// <summary>
	/// Interceptor for object with asynchronous constructor.
	/// You probably do NOT want to use this class directly (but you can).
	/// Use <c>DeferredConstructorProxy.Create</c> instead. 
	/// </summary>
	/// <typeparam name="T">Proxied type.</typeparam>
	public class DeferredConstructorInterceptor<T>: IInterceptor
	{
		private readonly Lazy<Task<T>> _target;

		/// <summary>Create new interceptor with given object factory.</summary>
		/// <param name="factory">Object factory.</param>
		public DeferredConstructorInterceptor(Func<Task<T>> factory)
		{
			_target = new Lazy<Task<T>>(factory, LazyThreadSafetyMode.ExecutionAndPublication);
		}

		/// <summary>Intercepts the call ensuring that decorated object has been created.</summary>
		/// <param name="invocation">Intercepted invocation.</param>
		public void Intercept(IInvocation invocation)
		{
			var returnType = invocation.Method.ReturnType;
			invocation.ReturnValue =
				IsTaskOfT(returnType) ? ToTaskOfT(returnType, InterceptAsyncResult(invocation)) :
				IsJustTask(returnType) ? InterceptAsyncVoid(invocation) :
				WaitAndIntercept(invocation);
		}

		private static Task ToTaskOfT(Type returnType, Task task)
		{
			// this looks benign but this is crucial part:
			// convert a Task of something to exactly Task<T> (which is returnType)
			// We do not have T during compilation, so we need to
			// dynamically (using reflection) create right Task<T>
			// DynamicPromise helps a little bit with that
			// (it's not magic, just hides some ugliness)
			var innerType = returnType.GetGenericArguments()[0];
			var promise = DynamicPromise.Create(innerType);
			promise.ConnectTo(task); // Task<object> -> Task<T>

			// even though it is declared as just Task, it is the right Task<T>
			return promise.Task;
		}

		private object WaitAndIntercept(IInvocation invocation)
		{
			// blocking
			var target = _target.Value.GetAwaiter().GetResult();
			return invocation.Method.Invoke(target, invocation.Arguments);
		}

		private async Task InterceptAsyncVoid(IInvocation invocation)
		{
			// non blocking
			var target = await _target.Value;
			var result = (Task) invocation.Method.Invoke(target, invocation.Arguments);
			// implicit (void) return
			await result;
		}

		private async Task<object> InterceptAsyncResult(IInvocation invocation)
		{
			// non blocking
			var target = await _target.Value;
			var result = (Task) invocation.Method.Invoke(target, invocation.Arguments);
			return await result.AsObject();
		}

		private static bool IsAnyTask(Type type) => typeof(Task).IsAssignableFrom(type);
		private static bool IsJustTask(Type type) => type == typeof(Task);

		private static bool IsTaskOfT(Type type) =>
			IsAnyTask(type) &&
			type.IsGenericType &&
			type.GetGenericTypeDefinition() == typeof(Task<>);
	}
}

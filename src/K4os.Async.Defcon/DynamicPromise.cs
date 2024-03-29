using System;
using System.Threading.Tasks;
using K4os.Async.Defcon.Internal;

namespace K4os.Async.Defcon
{
	internal interface IDynamicPromise
	{
		Task Task { get; }
		object Value { get; }
		void ConnectTo(Task task);
	}

	internal class DynamicPromise<T>: IDynamicPromise
	{
		private readonly TaskCompletionSource<T> _tcs = new();

		public Task Task => _tcs.Task;
		public object Value => _tcs.Task.IsCompleted ? _tcs.Task.Result : default;

		public void ConnectTo(Task task) =>
			task.ContinueWith(OnFinished);

		private void OnFinished(Task t)
		{
			if (t.IsFaulted) OnFailed(t.Exception.Unwrap());
			else if (t.IsCanceled) OnCancelled();
			else OnCompleted(t.AsObject().Result);
		}

		public void OnCompleted(object value) => _tcs.SetResult((T) value);
		public void OnFailed(Exception error) => _tcs.SetException(error);
		public void OnCancelled() => _tcs.SetCanceled();
	}

	internal abstract class DynamicPromise
	{
		public static IDynamicPromise Create(Type type) =>
			(IDynamicPromise) Activator.CreateInstance(
				typeof(DynamicPromise<>).MakeGenericType(type));

		public static Type InnerTypeOf(Type returnType) =>
			returnType.GetGenericArguments()[0];
	}
}

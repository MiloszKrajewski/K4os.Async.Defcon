using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace K4os.Async.Defcon.Internal
{
	internal static class TaskExtensions
	{
		private static readonly ConcurrentDictionary<Type, Type> TaskInnerTypes =
			new ConcurrentDictionary<Type, Type>();

		public static Type GetResultType(this Task task) =>
			TaskInnerTypes.GetOrAdd(task.GetType(), ResolveTaskInnerType);

		private static Type ResolveTaskInnerType(Type type)
		{
			EnsureIsTask(type);
			return GetResultProperty(type)?.PropertyType;
		}

		private static readonly ConcurrentDictionary<Type, Func<Task, object>> TaskExtractors =
			new ConcurrentDictionary<Type, Func<Task, object>>();

		public static async Task<object> AsObject(this Task task)
		{
			await task;
			return GetTaskExtractor(task.GetType())(task);
		}

		public static async Task<T> AsObject<T>(this Task task) =>
			(T) await task.AsObject();

		private static Func<Task, object> GetTaskExtractor(Type type) =>
			TaskExtractors.GetOrAdd(type, NewTaskExtractor);

		private static readonly Expression NullExpression = Expression.Constant(null);

		private static Func<Task, object> NewTaskExtractor(Type type)
		{
			EnsureIsTask(type);

			var property = GetResultProperty(type);

			// (Task argument) => (object) ((T) argument).Result
			var argument = Expression.Parameter(typeof(Task));
			var extractor = property == null
				? NullExpression
				: Expression.Convert(
					Expression.Property(Expression.Convert(argument, type), property),
					typeof(object));
			return Expression.Lambda<Func<Task, object>>(extractor, argument).Compile();
		}

		private static void EnsureIsTask(Type type)
		{
			if (typeof(Task).IsAssignableFrom(type))
				return;

			throw new InvalidCastException(
				$"{type.GetFriendlyName()} is not a Task");
		}

		private static PropertyInfo GetResultProperty(Type type) =>
			type.GetProperty(nameof(Task<object>.Result));
	}
}

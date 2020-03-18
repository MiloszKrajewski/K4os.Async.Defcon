using System;
using System.Linq;
using System.Reflection;

namespace K4os.Async.Defcon.Internal
{
	internal static class ObjectExtensions
	{
		// ReSharper disable once HeapView.BoxingAllocation
		internal static bool IsNull<T>(this T value) => ReferenceEquals(value, null);

		/// <summary>
		/// Gets the friendly name of given type.
		/// </summary>
		/// <param name="type">The type.</param>
		/// <returns>Friendly name.</returns>
		public static string GetFriendlyName(this Type type) =>
			GetFriendlyName(type, false);

		/// <summary>
		/// Gets the friendly name of given type.
		/// </summary>
		/// <param name="type">The type.</param>
		/// <param name="withNamespace">includes namespace (not on generic arguments).</param>
		/// <returns>Friendly name.</returns>
		public static string GetFriendlyName(this Type type, bool withNamespace)
		{
			if (type == null)
				return "<null>";

			var typeName = (withNamespace ? type.FullName : type.Name) ?? "Unknown";
			if (!type.GetTypeInfo().IsGenericType)
				return typeName;

			var length = typeName.IndexOf('`');
			if (length < 0)
				length = typeName.Length;

			var genericTypes = string.Join(",", type.GenericTypeArguments.Select(GetFriendlyName));

			return $"{typeName.Substring(0, length)}<{genericTypes}>";
		}
	}
}

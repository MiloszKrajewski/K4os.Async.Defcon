using System;
using System.Threading.Tasks;
using Castle.DynamicProxy;

namespace K4os.Async.Defcon
{
	/// <summary>
	/// Proxy factory for objects with asynchronous constructor.
	/// </summary>
	public static class DeferredConstructorProxy
	{
		private static readonly IProxyGenerator ProxyGenerator = new ProxyGenerator();

		/// <summary>Create proxy object.</summary>
		/// <param name="factory">Object factory.</param>
		/// <typeparam name="T">Proxied type.</typeparam>
		/// <returns>Proxy.</returns>
		public static T Create<T>(Func<Task<T>> factory) where T: class =>
			ProxyGenerator.CreateInterfaceProxyWithoutTarget<T>(
				new DeferredConstructorInterceptor<T>(factory));
	}
}

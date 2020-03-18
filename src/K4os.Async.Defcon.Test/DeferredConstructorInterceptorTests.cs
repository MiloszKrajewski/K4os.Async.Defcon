using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using Xunit;

namespace K4os.Async.Defcon.Test
{
	public class DeferredConstructorInterceptorTests
	{
		private readonly ProxyGenerator _generator = new ProxyGenerator();
		private readonly ConcurrentQueue<string> _events;
		private readonly ISomeMethods _decorator;

		public DeferredConstructorInterceptorTests()
		{
			_events = new ConcurrentQueue<string>();
			_decorator = _generator.CreateInterfaceProxyWithoutTarget<ISomeMethods>(
				new DeferredConstructorInterceptor<ISomeMethods>(
					async () => await SomeMethods.Create(_events)));
		}

		[Fact]
		public void ImplementationIsNotCreatedAutomatically()
		{
			Assert.NotNull(_decorator);
			Assert.Empty(_events);
		}

		[Fact]
		public async Task ImplementationIsNotCreatedWhenAnyMethodIsUsed()
		{
			Assert.Empty(_events);
			await _decorator.Dummy();
			Assert.Contains("created", _events);
		}

		[Fact]
		public async Task ReturnedValueIsPassedProperly()
		{
			Assert.Empty(_events);
			var value = await _decorator.ReturnAsync(1337);
			Assert.Equal(1337, value);
			Assert.Contains("created", _events);
			Assert.Contains("1337", _events);
		}

		[Fact]
		public async Task ReturnedComplexValueIsPassedProperly()
		{
			Assert.Empty(_events);
			var now = DateTimeOffset.UtcNow;
			var value = await _decorator.ReturnAsync(now);
			Assert.Equal(now, value);
			Assert.Contains("created", _events);
			Assert.Contains(now.ToString(), _events);
		}

		[Fact]
		public void ItStillWorksForNonAsyncMethods()
		{
			Assert.Empty(_events);
			var now = DateTimeOffset.UtcNow;
			var value = _decorator.Return(now);
			Assert.Equal(now, value);
			Assert.Contains("created", _events);
			Assert.Contains(now.ToString(), _events);
		}

		[Fact]
		public async Task VoidTaskIsNotAProblem()
		{
			Assert.Empty(_events);
			await _decorator.NoResultAsync();
			Assert.Contains("created", _events);
			Assert.Contains("void", _events);
		}

		[Fact]
		public void PureVoidMethodIsNotAProblem()
		{
			Assert.Empty(_events);
			_decorator.NoResult();
			Assert.Contains("created", _events);
			Assert.Contains("void", _events);
		}
	}

	public interface ISomeMethods
	{
		Task Dummy();
		T Return<T>(T value);
		Task<T> ReturnAsync<T>(T value);
		void NoResult();
		Task NoResultAsync();
	}

	public class SomeMethods: ISomeMethods
	{
		private readonly ConcurrentQueue<string> _events;

		public static async Task<SomeMethods> Create(ConcurrentQueue<string> events)
		{
			await Task.Delay(10);
			await Task.CompletedTask;
			await Task.Delay(10);
			return new SomeMethods(events);
		}

		private void Log(string @event) => _events.Enqueue(@event);

		public SomeMethods(ConcurrentQueue<string> events)
		{
			_events = events;
			Log("created");
		}

		public async Task Dummy()
		{
			await Task.Delay(10);
			await Task.CompletedTask;
			await Task.Delay(10);
		}

		public T Return<T>(T value)
		{
			Log(value?.ToString());
			return value;
		}

		public async Task<T> ReturnAsync<T>(T value)
		{
			await Dummy();
			Log(value?.ToString());
			return value;
		}

		public void NoResult() { Log("void"); }

		public async Task NoResultAsync()
		{
			await Dummy();
			Log("void");
		}
	}
}

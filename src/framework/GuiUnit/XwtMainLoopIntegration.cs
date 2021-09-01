using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace GuiUnit
{
	public class XwtMainLoopIntegration : IMainLoopIntegration
	{
		// List of Xwt backends we will try to use in order of priority
		Tuple<string,string>[] backends = new[] {
			Tuple.Create ("Xwt.WPF.dll", "Xwt.WPFBackend.WPFEngine, Xwt.WPF"),
			Tuple.Create ("Xwt.XamMac.dll", "Xwt.Mac.MacEngine, Xwt.XamMac")
		};

		Type Application {
			get; set;
		}

		public XwtMainLoopIntegration ()
		{
			Application = Type.GetType ("Xwt.Application, Xwt");
			if (Application == null)
				throw new NotSupportedException ();
		}

		public void InitializeToolkit ()
		{
			Type assemblyType = typeof (Assembly);
			PropertyInfo locationProperty = assemblyType.GetProperty ("Location");
			if (locationProperty == null)
				throw new NotSupportedException();

			if (TestRunner.LoadFileMethod == null)
				throw new NotSupportedException();

			string assemblyDirectory = Path.GetDirectoryName ((string)locationProperty.GetValue (Application.Assembly, null));

			// Firstly init Xwt
			var initialized = false;
			var initMethods = Application.GetMethods (System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
			var initMethod = initMethods.First (m => m.Name == "Initialize" && m.GetParameters ().Length == 1 && m.GetParameters ()[0].ParameterType == typeof (string));

			foreach (var impl in backends) {
				var xwtImpl = Path.Combine (assemblyDirectory, impl.Item1);
				if (File.Exists (xwtImpl)) {
					TestRunner.LoadFileMethod.Invoke (null, new[] { xwtImpl });
					initMethod.Invoke (null, new object[] { impl.Item2 });
					initialized = true;
					break;
				}
			}
			if (!initialized)
				initMethod.Invoke (null, new object[] { null });
		}

		public void InvokeOnMainLoop (InvokerHelper helper)
		{
			var application = Type.GetType ("Xwt.Application, Xwt");
			var invokeOnMainThreadMethod = application.GetMethod ("Invoke", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
			var invoker = Delegate.CreateDelegate (invokeOnMainThreadMethod.GetParameters () [0].ParameterType, helper, "Invoke");
			invokeOnMainThreadMethod.Invoke (null, new [] { invoker });
		}

		public void RunMainLoop ()
		{
			Application.GetMethod ("Run").Invoke (null, null);
		}

		public void Shutdown (int exitCode)
		{
			var method = Application.GetMethod("Exit", new Type[] { typeof(int) });
			if (method != null)
				method.Invoke(null, new object[] { exitCode });
			else
				Application.GetMethod("Exit").Invoke(null, null);
		}
	}

	class ConsoleMainLoop : System.Threading.SynchronizationContext, IMainLoopIntegration
	{
		System.Collections.Generic.Queue<InvokerHelper> work =
			new System.Collections.Generic.Queue<InvokerHelper>();

		System.Collections.Generic.Queue<Tuple<System.Threading.SendOrPostCallback, object>> contextWork =
			new System.Collections.Generic.Queue<Tuple<System.Threading.SendOrPostCallback, object>>();

		bool endLoop;

		public void InitializeToolkit()
		{
			var runtime = Type.GetType("MonoDevelop.Core.Runtime, MonoDevelop.Core");
			if (runtime == null)
				return;

			var property = runtime.GetProperty ("MainSynchronizationContext", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
			if (property == null)
				return;

			System.Threading.SynchronizationContext.SetSynchronizationContext(this);
			property.SetValue (null, System.Threading.SynchronizationContext.Current);
		}

		public void InvokeOnMainLoop (InvokerHelper helper)
		{
			lock (work)
			{
				work.Enqueue (helper);
				System.Threading.Monitor.Pulse (work);
			}
		}

		public void RunMainLoop ()
		{
			do
			{
				InvokerHelper next = null;
				Tuple<System.Threading.SendOrPostCallback, object> nextContext = null;
				lock (work)
				{
					if (work.Count > 0 && !endLoop)
						next = work.Dequeue ();
					else if (contextWork.Count > 0 && !endLoop)
						nextContext = contextWork.Dequeue ();
					else if (!endLoop)
						System.Threading.Monitor.Wait (work);
				}
				if (next != null)
				{
					try
					{
						next.Invoke ();
					}
					catch (Exception ex)
					{
						Console.WriteLine (ex);
					}
				}
				if (nextContext != null)
				{
					try
					{
						nextContext.Item1(nextContext.Item2);
					}
					catch (Exception ex)
					{
						Console.WriteLine(ex);
					}
				}
			} while (!endLoop);
		}

		public void Shutdown (int exitCode)
		{
			lock (work)
			{
				endLoop = true;
				System.Threading.Monitor.Pulse (work);
			}
		}

		public override void Post (System.Threading.SendOrPostCallback d, object state)
		{
			lock (work)
			{
				contextWork.Enqueue (new Tuple<System.Threading.SendOrPostCallback, object>(d, state));
				System.Threading.Monitor.Pulse (work);
			}
		}

		public override void Send (System.Threading.SendOrPostCallback d, object state)
		{
			var evt = new System.Threading.ManualResetEventSlim (false);
			Exception exception = null;
			Post (s =>
			{
				try
				{
					d.Invoke (state);
				}
				catch (Exception ex)
				{
					exception = ex;
				}
				finally
				{
					System.Threading.Thread.MemoryBarrier ();
					evt.Set ();
				}
			}, null);
			evt.Wait ();
			if (exception != null)
				throw exception;
		}
	}
}


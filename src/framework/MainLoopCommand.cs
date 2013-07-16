using System;
using NUnit.Framework.Internal.Commands;
using NUnit.Framework.Internal;
using System.Linq;

namespace MacUnit
{
	public static class MainLoopHelper
	{
		class InvokerHelper
		{
			public object Result;
			public Func<object> Func;
			public Exception ex;
			public TestExecutionContext Context;
			public System.Threading.ManualResetEvent Waiter = new System.Threading.ManualResetEvent(false);

			public void Invoke ()
			{
				TestExecutionContext.SetCurrentContext (Context);
				try {
					Result = Func ();
				} catch (Exception e) {
					ex = e;
				} finally {
					Waiter.Set ();
				}
			}
		}

		public static object ExecuteOnMainThread (Func<object> func)
		{
			// This is thread static so ensure we propagate it to the main thread
			var helper = new InvokerHelper {
				Func = func,
				Context = TestExecutionContext.CurrentContext
			};

			if (InvokeOnXwtLoop (helper)) {

			} else if (InvokeOnMonoMacLoop (helper)) {

			} else {
				throw new Exception ("Could not invoke on main loop");
			}
			
			helper.Waiter.WaitOne ();
			if (helper.ex != null)
				throw helper.ex;
			return helper.Result;
		}

		static bool InvokeOnXwtLoop (InvokerHelper helper)
		{
			try {
				var application = Type.GetType ("Xwt.Application, Xwt");
				var invokeOnMainThreadMethod = application.GetMethod ("Invoke", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
				var invoker = Delegate.CreateDelegate (invokeOnMainThreadMethod.GetParameters () [0].ParameterType, helper, "Invoke");
				invokeOnMainThreadMethod.Invoke (null, new [] { invoker });
				return true;
			} catch {
				return false;
			}
		}

		static bool InvokeOnMonoMacLoop (InvokerHelper helper)
		{
			try {
				var nsapp = Type.GetType ("MonoMac.AppKit.NSApplication, MonoMac");

				var sharedAppProperty = nsapp.GetProperty ("SharedApplication", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
				var potentialMethods = nsapp.GetMethods (System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
				var invokeOnMainThreadMethod = potentialMethods.First (m => m.Name == "InvokeOnMainThread" && m.GetParameters ().Length == 1);
				var invoker = Delegate.CreateDelegate (invokeOnMainThreadMethod.GetParameters () [0].ParameterType, helper, "Invoke");
				invokeOnMainThreadMethod.Invoke (sharedAppProperty.GetValue (null, null), new [] { invoker });
				return true;
			} catch {
				return false;
			}
		}
	}
}

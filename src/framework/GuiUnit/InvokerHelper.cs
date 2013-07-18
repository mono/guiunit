using System;
using NUnit.Framework.Internal;

namespace GuiUnit
{
	public class InvokerHelper
	{
		internal object Result;
		internal Func<object> Func;
		internal Exception ex;
		internal TestExecutionContext Context;
		internal System.Threading.ManualResetEvent Waiter = new System.Threading.ManualResetEvent(false);

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
}


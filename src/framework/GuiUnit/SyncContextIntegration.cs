using System;
using System.Linq;
using System.Threading;

namespace GuiUnit
{
	class SyncContextIntegration
		: IMainLoopIntegration
	{
		public void InitializeToolkit()
		{
			this.context = SynchronizationContext.Current;
		}

		public void InvokeOnMainLoop (InvokerHelper helper)
		{
			this.context.Send (h => ((InvokerHelper)h).Invoke(), helper);
		}

		public void RunMainLoop()
		{
		}

		public void Shutdown()
		{
		}

		private SynchronizationContext context;
	}
}
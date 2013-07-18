using System;

namespace GuiUnit
{
	public interface IMainLoopIntegration
	{
		void InitializeToolkit ();
		void InvokeOnMainLoop (InvokerHelper helper);
		void RunMainLoop ();
		void Shutdown ();
	}
}


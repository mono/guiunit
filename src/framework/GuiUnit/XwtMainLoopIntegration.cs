using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace GuiUnit
{
	public class XwtMainLoopIntegration : IMainLoopIntegration
	{
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
			foreach (var impl in new [] { "Xwt.Gtk.dll", "Xwt.Mac.dll", "Xwt.Wpf.dll"}) {
				var xwtImpl = Path.Combine (assemblyDirectory, impl);
				if (File.Exists (xwtImpl))
					TestRunner.LoadFileMethod.Invoke (null, new[] { xwtImpl });
			}

			var initMethods = Application.GetMethods (System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
			var initMethod = initMethods.First (m => m.Name == "Initialize" && m.GetParameters ().Length == 1 && m.GetParameters () [0].ParameterType == typeof(string));
			initMethod.Invoke (null, new [] { "Xwt.GtkBackend.GtkEngine, Xwt.Gtk" });
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

		public void Shutdown ()
		{
			Application.GetMethod ("Exit").Invoke (null, null);
		}
	}
}


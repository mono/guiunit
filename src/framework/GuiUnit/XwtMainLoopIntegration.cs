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
			Tuple.Create ("Xwt.Gtk.dll", "Xwt.GtkBackend.GtkEngine, Xwt.Gtk"),
			Tuple.Create ("Xwt.WPF.dll", "Xwt.WPFBackend.WPFEngine, Xwt.WPF"),
			Tuple.Create ("Xwt.Mac.dll", "Xwt.Mac.MacEngine, Xwt.Mac")
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

		public void Shutdown ()
		{
			Application.GetMethod ("Exit").Invoke (null, null);
		}
	}
}


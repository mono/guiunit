using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace GuiUnit
{
	public class MonoMacMainLoopIntegration : IMainLoopIntegration
	{
		Type Application {
			get; set;
		}

		object SharedApplication {
			get; set;
		}

		public MonoMacMainLoopIntegration ()
		{
			Application =
				Type.GetType ("AppKit.NSApplication, Xamarin.Mac") ??
				Type.GetType ("MonoMac.AppKit.NSApplication, XamMac") ??
				Type.GetType ("MonoMac.AppKit.NSApplication, MonoMac");
			if (Application == null)
				throw new NotSupportedException ();
		}

		public void InitializeToolkit ()
		{
			// First, dlopen libxammac.dylib from the same dir we loaded the Xamarin.Mac dll
			var dylibPath = Path.Combine (Path.GetDirectoryName (Application.Assembly.Location), "libxammac.dylib");
			if (dlopen (dylibPath, 0) == IntPtr.Zero) {
				var errPtr = dlerror ();
				var errStr = (errPtr == IntPtr.Zero)? "<unknown error>" : Marshal.PtrToStringAnsi (errPtr);
				Console.WriteLine ("WARNING: Cannot load {0}: {1}", dylibPath, errStr);
			}

			var initMethod = Application.GetMethod ("Init", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
			initMethod.Invoke (null, null);

			var sharedAppProperty = Application.GetProperty ("SharedApplication", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
			SharedApplication = sharedAppProperty.GetValue (null, null);
		}

		public void InvokeOnMainLoop (InvokerHelper helper)
		{
			var potentialMethods = Application.GetMethods (System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
			var invokeOnMainThreadMethod = potentialMethods.First (m => m.Name == "InvokeOnMainThread" && m.GetParameters ().Length == 1);
			var invoker = Delegate.CreateDelegate (invokeOnMainThreadMethod.GetParameters () [0].ParameterType, helper, "Invoke");
			invokeOnMainThreadMethod.Invoke (SharedApplication, new [] { invoker });
		}

		public void RunMainLoop ()
		{
			Application.GetMethod ("Run").Invoke (SharedApplication, null);
		}

		public void Shutdown ()
		{
			Application.GetMethod ("Terminate").Invoke (SharedApplication, new [] { SharedApplication });
		}

		[DllImport ("/usr/lib/libSystem.dylib")]
		static extern IntPtr dlopen (string path, int mode);

		[DllImport ("/usr/lib/libSystem.dylib")]
		static extern IntPtr dlerror ();
	}
}


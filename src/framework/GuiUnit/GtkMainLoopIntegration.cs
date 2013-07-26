//
// GtkMainLoopIntegration.cs
//
// Author:
//       alan <>
//
// Copyright (c) 2013 alan
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Linq;

namespace GuiUnit
{
	public class GtkMainLoopIntegration : IMainLoopIntegration
	{
		Type Application {
			get; set;
		}

		public GtkMainLoopIntegration ()
		{
			Application = Type.GetType ("Gtk.Application, gtk-sharp");
			if (Application == null)
				throw new NotSupportedException ();
		}

		public void InitializeToolkit ()
		{
			var initMethods = Application.GetMethods (System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
			var initMethod = initMethods.First (m => m.Name == "Init" && m.GetParameters ().Length == 0);
			initMethod.Invoke (null, null);
		}
		public void InvokeOnMainLoop (GuiUnit.InvokerHelper helper)
		{
			var invokeOnMainThreadMethods = Application.GetMethods (System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
			var invokeOnMainThreadMethod = invokeOnMainThreadMethods.First (d => d.Name == "Invoke" && d.GetParameters ().Length == 1);
			EventHandler invoker = delegate { helper.Invoke (); };
			invokeOnMainThreadMethod.Invoke (null, new [] { invoker });
		}

		public void RunMainLoop ()
		{
			Application.GetMethod ("Run").Invoke (null, null);
		}

		public void Shutdown ()
		{
			Application.GetMethod ("Quit").Invoke (null, null);
		}
	}
}


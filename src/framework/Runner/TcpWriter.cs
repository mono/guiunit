// ***********************************************************************
// Copyright (c) 2008 Charlie Poole
// Copyright 2013 Xamarin Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// ***********************************************************************

using System;
using System.IO;
using System.Net.Sockets;
using System.Net;
using System.Threading;

namespace NUnitLite.Runner
{
    /// <summary>
    /// Redirects output to a Tcp connection
    /// </summary>
    class TcpWriter : TextWriter
    {
        public TcpWriter(IPEndPoint endpoint)
        {
            if (endpoint == null)
                throw new ArgumentNullException ("endpoint");

            this.socket = new Socket (endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            var args = new SocketAsyncEventArgs { RemoteEndPoint = endpoint };
            args.Completed += OnConnectCompleted;

            if (!socket.ConnectAsync (args))
                OnConnectCompleted (this, args);

            writer = new StreamWriter (stream, Encoding);
        }

		protected override void Dispose(bool disposing)
		{
			if (disposing) {
				using (socket)
					socket.Shutdown(SocketShutdown.Both);
			}
			base.Dispose(disposing);
		}

        public override void Write(char value)
        {
            writer.Write(value);
        }

        public override void Write(string value)
        {
            writer.Write(value);
        }

        public override void WriteLine(string value)
        {
            writer.WriteLine(value);
            writer.Flush ();
            Flush();
        }

        public override System.Text.Encoding Encoding
        {
            get { return System.Text.Encoding.UTF8; }
        }

        private readonly Socket socket;

        private readonly StreamWriter writer;
        private readonly MemoryStream stream = new MemoryStream();

        private readonly ManualResetEvent connectWait = new ManualResetEvent (false);
        private readonly AutoResetEvent wait = new AutoResetEvent (false);
        private SocketAsyncEventArgs args;

        SocketError error = SocketError.Success;

    
        public override void Flush ()
        {
            connectWait.WaitOne();

            if (error != SocketError.Success)
                throw new SocketException ((int)error);

            if (args == null) {
                args = new SocketAsyncEventArgs();
                args.Completed += OnSocketCompleted;
            }

            byte[] buffer = this.stream.GetBuffer();

            args.SetBuffer (buffer, 0, (int)this.stream.Position);
            if (!this.socket.SendAsync (args))
                OnSocketCompleted (this, args);

            wait.WaitOne();
            this.stream.Position = 0;
        }

        private void OnConnectCompleted (object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError != SocketError.Success)
                this.error = e.SocketError;
            connectWait.Set();
        }

        private void OnSocketCompleted (object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError != SocketError.Success)
                this.error = e.SocketError;
            else if (e.BytesTransferred == 0)
                this.error = SocketError.Disconnecting;
            wait.Set();
        }
    }
}

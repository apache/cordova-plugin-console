using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Phone.Shell;
using Windows.Networking.Sockets;

namespace WPCordovaClassLib.Cordova.Commands
{
	public class Logger
	{
		private const int Port = 6510;
		private const string ConnectedNotification = "Connected";

		private Queue<string> messageQueue;

		private StreamSocketListener connectionListener;
		private StreamSocket socket;

		private bool IsConnected
		{
			get
			{
				return this.socket != null;
			}
		}

		public Logger()
		{
			this.messageQueue = new Queue<string>();

			this.BindPort();

			PhoneApplicationService.Current.Activated += (s, e) => this.OnActivated();
			PhoneApplicationService.Current.Deactivated += (s, e) => this.OnDeativated();
		}

		public void Log(string message)
		{
			if (this.IsConnected)
			{
				this.Send(message);
			}
			else
			{
				this.messageQueue.Enqueue(message);
			}
		}
  
		private async Task BindPort()
		{
			if (this.connectionListener == null)
			{
				this.connectionListener = new StreamSocketListener();
				this.connectionListener.ConnectionReceived += this.OnConnectionReceived;
				await this.connectionListener.BindServiceNameAsync(Port.ToString());
			}
		}

		private void OnConnectionReceived(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
		{
			if (this.socket != null)
			{
				this.socket.Dispose();
			}

			this.socket = args.Socket;
			this.Send(ConnectedNotification);

			this.ProcessMessagesFromQueue();
		}
  
		private async Task ProcessMessagesFromQueue()
		{
			while (this.messageQueue.Count > 0)
			{
				var message = this.messageQueue.Dequeue();
				await this.Send(message);
			}
		}
  
		private async Task OnActivated()
		{
			await this.BindPort();
		}

		private void OnDeativated()
		{
			if (this.connectionListener != null)
			{
				this.connectionListener.Dispose();
				this.connectionListener = null;
			}

			if (this.socket != null)
			{
				this.socket.OutputStream.Dispose();
				this.socket.Dispose();
				this.socket = null;
			}
		}

		private async Task Send(string message)
		{
			var bytes = Encoding.Unicode.GetBytes(message);
			await this.socket.OutputStream.WriteAsync(bytes.AsBuffer());
		}
	}
}
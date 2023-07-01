using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUI
{

	public delegate void MessageEventHandler(object sender, MessageEventArgs e);
	public class MessageEventArgs : EventArgs
	{
		public string Message { get; private set; }

		public MessageEventArgs(string message)
		{
			Message = message;
		}
	}
}

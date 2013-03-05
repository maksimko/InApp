using System;
using Touchin.iOS.InApp.Common;
using Touchin.iOS.InApp.Contracts;

namespace Touchin.iOS.InApp
{
	public class LogMessage
	{
		private static string _sessionIdentity;
		private string _message;
		
		public ILog Logger { get; private set; }
		public string Message {	get { return _message; } }
		
		public LogMessage(string entry, ILog logger)
		{
			if (String.IsNullOrWhiteSpace(_sessionIdentity))
			{
				_sessionIdentity = Guid.NewGuid().ToString().Substring(0, 8);
			}
			
			Logger = logger;
			
			Clear();
			
			entry += String.Concat(" SessionId: ", _sessionIdentity, " ");
			
			Append(entry);
		}
		
		public void Clear()
		{
			_message = "";
		}
		
		public void Append(string message)
		{
			_message += (message + Environment.NewLine);
		}
		
		public void Send(string message = "")
		{
			Append(message);
			Append("");			
			
			Logger.Error(_message);
		}
	}
}


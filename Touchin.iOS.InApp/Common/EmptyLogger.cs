using System;

namespace Touchin.iOS.InApp.Common
{
	public class EmptyLogger : ILog
	{
		#region ILog implementation		
		public void Trace()
		{
		}

		public void Debug(string text, params object[] opt)
		{
		}

		public void Error(string text, params object[] opt)
		{
		}		

		public void Fatal(Exception ex)
		{
		}		
		#endregion
	}
}


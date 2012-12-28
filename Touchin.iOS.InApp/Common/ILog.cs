using System;

namespace Touchin.iOS.InApp.Common
{
	public interface ILog
	{
		void Trace();
		void Debug(string text = "", params object[] opt);
		void Error(string text = "", params object[] opt);
		void Fatal(Exception ex);
	}
}


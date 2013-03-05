using System;
using MonoTouch.StoreKit;
using System.Collections.Generic;

namespace Touchin.iOS.InApp
{
	public interface IContentManager
	{
		void SaveDownload (string productId, IEnumerable<string> sourcesPath, Action<IEnumerable<string>> savingCompleteHandler);		
	}
}


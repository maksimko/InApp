using System;
using MonoTouch.UIKit;

namespace InApp.Sample
{
	public class Buttons
	{
		public static UIButton GetWithTitle(string title)
		{
			var _requestButton = UIButton.FromType(UIButtonType.RoundedRect);

			_requestButton.SetTitle(title, UIControlState.Normal);

			_requestButton.TranslatesAutoresizingMaskIntoConstraints = false;
			_requestButton.AutoresizingMask = UIViewAutoresizing.None;

			_requestButton.SizeToFit();

			return _requestButton;
		}
	}
}


using System;
using System.Drawing;

using MonoTouch.Foundation;
using MonoTouch.UIKit;
using Touchin.iOS.InApp;

namespace InApp.Sample
{
	public class InAppViewController : UIViewController
	{
		public InAppViewController ()
		{
			InitSubviews();
			ApplyStyles();
		}

		private void InitSubviews()
		{
			var view = new InAppView();

			view.ProductInfoRequested += OnProductInfoRequested;
			view.ProductRestore += OnProductRestore;
			view.ProductBuy += OnProductBuy;

			View = view;
		}

		private void ApplyStyles()
		{
			View.BackgroundColor = UIColor.White;
		}

		private void OnProductInfoRequested()
		{}

		private void OnProductRestore()
		{}

		private void OnProductBuy()
		{}

	}
}


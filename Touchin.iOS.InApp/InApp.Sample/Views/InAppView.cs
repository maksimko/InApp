using System;
using MonoTouch.UIKit;
using MonoTouch.CoreGraphics;
using System.Drawing;

namespace InApp.Sample
{
	public class InAppView : UIView
	{
		public event Action ProductInfoRequested;
		public event Action ProductRestore;		
		public event Action ProductBuy;

		UIButton _requestButton;
		UIButton _buyButton;
		UIButton _restoreButton;

		public InAppView ()
		{
			InitSubviews();
		}

		private void InitSubviews()
		{
			_requestButton = UIButton.FromType(UIButtonType.RoundedRect);
			_requestButton.SetTitle("Request product info", UIControlState.Normal);
			_requestButton.SizeToFit();

			_restoreButton = UIButton.FromType(UIButtonType.RoundedRect);
			_restoreButton.SetTitle("Restore", UIControlState.Normal);
			_restoreButton.SizeToFit();

			_buyButton = UIButton.FromType(UIButtonType.RoundedRect);
			_buyButton.SetTitle("Buy", UIControlState.Normal);
			_buyButton.SizeToFit();

			_requestButton.TouchUpInside += (sender, e) => { if (ProductInfoRequested != null) ProductInfoRequested(); };
			_restoreButton.TouchUpInside += (sender, e) => { if (ProductRestore != null) ProductRestore(); };
			_buyButton.TouchUpInside += (sender, e) => { if (ProductBuy != null) ProductBuy(); };

			AddSubviews(_requestButton, _buyButton, _restoreButton);
		}

		public void BindTo()
		{

		}

		public override void LayoutSubviews ()
		{
			_requestButton.Center = new PointF(Bounds.GetMidX(), Bounds.GetMidY());
			_restoreButton.Center = new PointF(Bounds.Right - _restoreButton.Bounds.Width, _restoreButton.Frame.Height);
			_buyButton.Center = new PointF(Bounds.Left + _buyButton.Bounds.Width, _buyButton.Frame.Height);
		}
	}
}


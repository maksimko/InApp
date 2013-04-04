using System;
using MonoTouch.UIKit;
using MonoTouch.CoreGraphics;
using System.Drawing;
using MonoTouch.Foundation;
using System.Linq;
using System.Collections.Generic;
using MonoTouch.StoreKit;
using Touchin.iOS.InApp.Extensions;

namespace InApp.Sample
{
	public class InAppView : UIView
	{
		public event Action ProductInfoRequested;
		public event Action ProductRestore;		
		public event Action<SKProduct> ProductBuy;

		UIButton _requestButton;
		UIButton _buyButton;
		UIButton _restoreButton;

		UITextView _textView;
		UILabel _priceLabel;

		SKProduct _product;

		public InAppView ()
		{
			InitSubviews();
			InitConstraint();
			ApplyStyles();
		}

		private void InitSubviews()
		{
			_requestButton = Buttons.GetWithTitle("Request product info");
			_restoreButton = Buttons.GetWithTitle("Restore");
			_buyButton = Buttons.GetWithTitle("Buy");

			_buyButton.Enabled = false;

			_textView = new UITextView();
			_textView.TranslatesAutoresizingMaskIntoConstraints = false;
			_textView.Editable = false;

			_priceLabel = new UILabel(new RectangleF(PointF.Empty, new SizeF(100, 44)));
			_priceLabel.TranslatesAutoresizingMaskIntoConstraints = false;
			_priceLabel.Enabled = false;

			_requestButton.TouchUpInside += (sender, e) => { if (ProductInfoRequested != null) ProductInfoRequested(); };
			_restoreButton.TouchUpInside += (sender, e) => { if (ProductRestore != null) ProductRestore(); };
			_buyButton.TouchUpInside += (sender, e) => { if (ProductBuy != null) ProductBuy(_product); };

			Frame = UIScreen.MainScreen.Bounds;

			AddSubviews(_requestButton, _buyButton, _restoreButton, _textView, _priceLabel);
		}

		void InitConstraint ()
		{
			var views = new NSMutableDictionary();
			views.Add(new NSString("buyButton"), _buyButton);
			views.Add(new NSString("restoreButton"), _restoreButton);
			views.Add(new NSString("requestButton"), _requestButton);
			views.Add(new NSString("textView"), _textView);
			views.Add(new NSString("priceLabel"), _priceLabel);			

			var constraints = NSLayoutConstraint.FromVisualFormat(@"V:|-[buyButton]"
			                                                      , NSLayoutFormatOptions.AlignAllTop
			                                                      , null
			                                                      , views);

			AddConstraints(constraints);

			constraints = NSLayoutConstraint.FromVisualFormat(@"H:|-[buyButton(80)]-0@1-[restoreButton(==buyButton)]-|"
			                                                     , NSLayoutFormatOptions.AlignAllCenterY
			                                                     , null
			                                                     , views);

			AddConstraints(constraints);

			constraints = NSLayoutConstraint.FromVisualFormat(@"V:|-100-[requestButton(44)]"
			                                                  , NSLayoutFormatOptions.AlignAllTop
			                                                  , null
			                                                  , views);
			AddConstraints(constraints);

			constraints = NSLayoutConstraint.FromVisualFormat(@"H:|-<=50@400-[requestButton(>=200@100)]-<=50@500-|"
			                                                  , NSLayoutFormatOptions.AlignAllCenterY
			                                                  , null
			                                                  , views);
			AddConstraints(constraints);

			constraints = NSLayoutConstraint.FromVisualFormat(@"H:|-<=50@400-[requestButton(>=200@100)]-<=50@500-|"
			                                                  , NSLayoutFormatOptions.AlignAllCenterY
			                                                  , null
			                                                  , views);
			AddConstraints(constraints);

			constraints = NSLayoutConstraint.FromVisualFormat(@"H:|-[textView]-|"
			                                                  , NSLayoutFormatOptions.AlignAllTop
			                                                  , null
			                                                  , views);
			AddConstraints(constraints);

			constraints = NSLayoutConstraint.FromVisualFormat(@"V:[requestButton]-15-[textView(>=100)]-|"
			                                                  , NSLayoutFormatOptions.AlignAllCenterX
			                                                  , null
			                                                  , views);
			AddConstraints(constraints);

			constraints = NSLayoutConstraint.FromVisualFormat(@"[buyButton]-[priceLabel(100)]"
			                                                  , NSLayoutFormatOptions.AlignAllCenterY
			                                                  , null
			                                                  , views);
			AddConstraints(constraints);
		}

		void ApplyStyles ()
		{
			_textView.BackgroundColor = UIColor.Yellow.ColorWithAlpha(0.2f);
			_priceLabel.BackgroundColor = UIColor.Red.ColorWithAlpha(0.2f);
		}

		public void BindTo(List<SKProduct> products)
		{
			if (products.Count == 0)
				return;

			_product = products.First();
			_priceLabel.Text = _product.LocalizedPrice();
			_textView.Text = _product.LocalizedTitle + " - " +_product.LocalizedDescription;
		}

		public void ActivateBuyButton ()
		{
			_buyButton.Enabled = true;
		}

		public override void LayoutSubviews ()
		{
			base.LayoutSubviews();

		}
	}
}


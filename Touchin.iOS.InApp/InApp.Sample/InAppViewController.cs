using System;
using System.Drawing;

using MonoTouch.Foundation;
using MonoTouch.UIKit;
using Touchin.iOS.InApp;
using System.Collections.Generic;
using MonoTouch.StoreKit;
using System.Linq;

namespace InApp.Sample
{
	public class InAppViewController : UIViewController
	{
		public new InAppView View
		{
			get { return base.View as InAppView; }
			set { base.View = value; }
		}

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

			InAppManager.Default.ProductsInfoReceived += OnProductsInfoReceived;
		}

		private void ApplyStyles()
		{
			View.BackgroundColor = UIColor.White;
		}

		private void OnProductInfoRequested()
		{
			InAppManager.Default.RequestProductsData(Purchases.PossiblePurchases);
		}

		void OnProductsInfoReceived (Dictionary<string, SKProduct> products)
		{
			View.BindTo(products.Values.ToList());
			View.ActivateBuyButton();
		}

		private void OnProductRestore()
		{
			InAppManager.Default.RestorePurchases();
		}

		private void OnProductBuy(SKProduct product)
		{
			InAppManager.Default.Purchase(product);
		}
	}
}


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

			InAppManager.Instance.ProductsInfoReceived += OnProductsInfoReceived;			
		}

		private void ApplyStyles()
		{
			View.BackgroundColor = UIColor.White;
		}

		private void OnProductInfoRequested()
		{
			var purchases = (NSDictionary)NSBundle.MainBundle.ObjectForInfoDictionary("PurchaseProducts");

			foreach(var key in purchases.Keys)
			{
				var identifier = purchases[key].ToString();

				InAppManager.Instance.RequestProductsData(identifier);
			}
		}

		void OnProductsInfoReceived (Dictionary<string, SKProduct> products)
		{
			((InAppView)View).BindTo(products.Values.ToList());
		}

		private void OnProductRestore()
		{}

		private void OnProductBuy()
		{}

	}
}


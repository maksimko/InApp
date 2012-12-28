using System;
using MonoTouch.StoreKit;
using MonoTouch.Foundation;

namespace Touchin.iOS.InApp.Extensions
{
	public static class SKProductExtension
	{
		public static string LocalizedPrice (this SKProduct product)
		{
			var formatter = new NSNumberFormatter ();
			formatter.FormatterBehavior = NSNumberFormatterBehavior.Version_10_4;   
			formatter.NumberStyle = NSNumberFormatterStyle.Currency;
			formatter.Locale = product.PriceLocale;

			var formattedString = formatter.StringFromNumber(product.Price);

			return formattedString;
		}
	}
}


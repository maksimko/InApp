using System;
using System.Collections.Generic;
using MonoTouch.Foundation;
using System.Linq;

namespace InApp.Sample
{
	public static class Purchases
	{
		public static IEnumerable<string> PossiblePurchases
		{
			get {
				var purchases = (NSDictionary)NSBundle.MainBundle.ObjectForInfoDictionary("PurchaseProducts");

				return new List<string> { "com.touchin.sooner.lite.full" };

				//return purchases.Keys.Select(key => purchases[key].ToString());
			}
		}
	}
}


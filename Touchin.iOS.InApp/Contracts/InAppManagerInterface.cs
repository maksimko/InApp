using System;
using System.Collections.Generic;
using Touchin.iOS.InApp.Contracts;
using MonoTouch.StoreKit;
using Touchin.iOS.InApp.Common;
using MonoTouch.Foundation;

namespace Touchin.iOS.InApp.Contracts
{
	public interface InAppManagerInterface
	{
		event Action<NSError> ProductRequestFailed;
		event Action<InAppManagerInterface> ProductRequestSucceed;
		event Action<Dictionary<string, SKProduct>> ProductsInfoReceived;

		event Action<string> ProductNotAvailable;
		
		event Action<InAppManagerInterface, string> PaymentTransactionInitiated;
		event Action<InAppManagerInterface, string> PaymentTransactionSucceed;
		event Action<InAppManagerInterface, string, NSError> PaymentTransactionFailed;
		event Action<InAppManagerInterface, string> PaymentTransactionRestored;
		
		event Action UserCancelled;
		
		event Action<InAppManagerInterface> RestoreSucceed;
		event Action<InAppManagerInterface, NSError> RestoreFailed;

		event Action<string, float, double> DownloadEstimateChanged;
		event Action<string, NSError> DownloadPaused;
		event Action<string, string> DownloadFinished;
		event Action<string, NSError> DownloadFailed;
		event Action<string, NSError> DownloadCancelled;		
		event Action<IEnumerable<string>> SavingCompleted;		
		
		Dictionary<string, SKProduct> AvaliableProducts { get; }
		List<string> NotAvaliableProducts { get; }
		bool CanMakePayments { get; }
		bool IsPurchasing { get; set; }
		OperationType LastOperation { get; }
		ILog Logger { get; set; }
		IContentManager ContentManager { get; set; }
		
		void RequestProductsData(string productIdentifiers);
		void RequestProductsData(List<string> productIdentifiers);
		void Purchase(string productId);
		void Purchase(SKProduct product);
		void RestorePurchases();
	}
}


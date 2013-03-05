using System;

using MonoTouch.StoreKit;
using MonoTouch.Foundation;
using System.Linq;

namespace Touchin.iOS.InApp
{
	public class InAppPaymentObserver : SKPaymentTransactionObserver
	{
		private InAppManager _inAppManager;

		public InAppPaymentObserver(InAppManager inAppManager)
		{
			_inAppManager = inAppManager;
		}

		public override void UpdatedTransactions(SKPaymentQueue queue, SKPaymentTransaction[] transactions)
		{
			foreach (var transaction in transactions)
			{
				switch(transaction.TransactionState)
				{
					case SKPaymentTransactionState.Purchasing:
						_inAppManager.RaisePaymentTransactionInitiated(transaction);
						break;
					case SKPaymentTransactionState.Purchased:
						if (transaction.Downloads != null && transaction.Downloads.Any())
							SKPaymentQueue.DefaultQueue.StartDownloads(transaction.Downloads);
						else
							_inAppManager.RaiseCompletePaymentTransaction(transaction);
						break;
					case SKPaymentTransactionState.Restored:
						_inAppManager.RaiseRestoredPaymentTransaction(transaction);
						break;
					case SKPaymentTransactionState.Failed:
						_inAppManager.RaiseFailedPaymentTransaction(transaction);
						break;
				}
			}
		}

		public override void PaymentQueueRestoreCompletedTransactionsFinished(SKPaymentQueue queue)
		{
			_inAppManager.RaiseRestoreSucceed();
		}
		
		public override void RestoreCompletedTransactionsFailedWithError(SKPaymentQueue queue, NSError error)
		{
			_inAppManager.RaiseRestoreFailed(error);
		}

		public override void UpdatedDownloads (SKPaymentQueue queue, SKDownload[] downloads)
		{
			foreach (var download in downloads) {
				switch (download.DownloadState) {
					case SKDownloadState.Active:
						// TODO: implement a notification to the UI (progress bar or something?)
						Console.WriteLine ("Download progress:" + download.Progress);
						Console.WriteLine ("Time remaining:   " + download.TimeRemaining); // -1 means 'still calculating'
						break;
					case SKDownloadState.Finished:
						Console.WriteLine ("Finished!!!!");
						Console.WriteLine ("Content URL:" + download.ContentUrl);
						
						// UNPACK HERE! Calls FinishTransaction when it's done
						_inAppManager.SaveDownload (download);
						
						break;
					case SKDownloadState.Failed:
						Console.WriteLine ("Failed"); 
						// TODO: UI?
						break;
					case SKDownloadState.Cancelled:
						Console.WriteLine ("Cancelled"); 
						// TODO: UI?
						break;
					case SKDownloadState.Paused:
					case SKDownloadState.Waiting:
						break;
					default:
						break;
				}
			}
		}
	}
}


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
						_inAppManager.RaiseDownloadEstimateChanged(download);

						break;
					case SKDownloadState.Finished:
						_inAppManager.RaiseDownloadCompleted(download);						
						_inAppManager.SaveDownload (download);
						
						break;
					case SKDownloadState.Failed:
						_inAppManager.RaiseDownloadFailed(download);						

						break;
					case SKDownloadState.Cancelled:
						_inAppManager.RaiseDownloadCancelled(download);												

						break;
					case SKDownloadState.Paused:
					case SKDownloadState.Waiting:
						_inAppManager.RaiseDownloadPaused(download);						

						break;
					default:
						break;
				}
			}
		}
	}
}


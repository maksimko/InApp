using System;

using MonoTouch.StoreKit;
using MonoTouch.Foundation;

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
						_inAppManager.CompletePaymentTransaction(transaction);
						break;
					case SKPaymentTransactionState.Restored:
						_inAppManager.RestorePaymentTransaction(transaction);
						break;
					case SKPaymentTransactionState.Failed:
						_inAppManager.FailedPaymentTransaction(transaction);
						break;
				}
			}
		}
	}
}


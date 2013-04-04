using System;

using MonoTouch.StoreKit;
using MonoTouch.Foundation;
using System.Collections.Generic;
using System.Linq;
using Touchin.iOS.InApp.Common;
using Touchin.iOS.InApp.Extensions;
using System.IO;
using Touchin.iOS.InApp.Contracts;
using MonoTouch.UIKit;

namespace Touchin.iOS.InApp
{
	public class InAppManager : SKProductsRequestDelegate, InAppManagerInterface
	{
		public event Action<InAppManagerInterface> ProductRequestSucceed;
		public event Action<Dictionary<string, SKProduct>> ProductsInfoReceived;
		public event Action<NSError> ProductRequestFailed;

		public event Action<string> ProductPurchaseFailed;

		public event Action<InAppManagerInterface, string> PaymentTransactionInitiated;
		public event Action<InAppManagerInterface, string> PaymentTransactionSucceed;
		public event Action<InAppManagerInterface, string, NSError> PaymentTransactionFailed;
		public event Action<InAppManagerInterface, string> PaymentTransactionRestored;

		public event Action UserCancelled;

		public event Action<InAppManagerInterface> RestoreSucceed;
		public event Action<InAppManagerInterface, NSError> RestoreFailed;

		public event Action<string, float, double> DownloadEstimateChanged;
		public event Action<string, NSError> DownloadPaused;
		public event Action<string, string> DownloadFinished;
		public event Action<string, NSError> DownloadFailed;
		public event Action<string, NSError> DownloadCancelled;		
		public event Action<IEnumerable<string>> SavingCompleted;

		private SKPaymentTransactionObserver _paymentTransactionObserver;		
		private SKProductsRequest _productsRequest;
		private Dictionary<string, SKProduct> _avaliableProducts;
		private string[] _notAvaliableProducts;
		private bool _productInfoReceived;

		public bool IsPurchasing { get; set; }

		private OperationType _latOperation;
		public OperationType LastOperation
		{ 
			get
			{
				return _latOperation;
			}
		}

		private static ILog _logger;
		public ILog Logger
		{ 
			get
			{
				return _logger ?? (_logger = new EmptyLogger());
			} 

			set
			{
				_logger = value;
			}
		}

		private IContentManager _contentManager;
		public IContentManager ContentManager 
		{ 
			get
			{
				return _contentManager ?? (_contentManager = new ContentManager());
			}

			set
			{
				if (IsPurchasing)
					throw new OperationCanceledException("Can't change content manager during purchasing");

				_contentManager = value;
			}
		}

		private static InAppManagerInterface _inAppManagerInstance;
		public static InAppManagerInterface Default
		{
			get { return _inAppManagerInstance ?? (_inAppManagerInstance = new InAppManager()); }
		}

		private InAppManager()
		{
			_paymentTransactionObserver = new InAppPaymentObserver(this);
			SKPaymentQueue.DefaultQueue.AddTransactionObserver(_paymentTransactionObserver);

			_productInfoReceived = false;
		}

		public Dictionary<string, SKProduct> AvaliableProducts
		{
			get 
			{ 
				if (_avaliableProducts == null)
				{
					_avaliableProducts = new Dictionary<string, SKProduct>();
				}

				return _avaliableProducts; 
			}
		}

		public IEnumerable<string> NotAvaliableProducts
		{
			get 
			{
				if (_notAvaliableProducts == null)
				{
					_notAvaliableProducts = new string[1];
				}

				return _notAvaliableProducts.ToList();
			}
		}

		public bool CanMakePayments
		{ 
			get { return SKPaymentQueue.CanMakePayments; }
		}

		public void RequestProductsData(string productIdentifiers)
		{
			RequestProductsData(new List<string>(1) { productIdentifiers });
		}

		public void RequestProductsData(IEnumerable<string> productIdentifiers)
		{
			IsPurchasing = true;

			var array = new NSString[productIdentifiers.Count()];

			var i = 0;

			foreach(var identifier in productIdentifiers)
				array[i++] = new NSString(identifier);

			var products = NSSet.MakeNSObjectSet<NSString>(array);

			_productsRequest = new SKProductsRequest(products);
			_productsRequest.Delegate = this;
			_productsRequest.Start();
		}

		public void Purchase(string productId)
		{
			if (!_productInfoReceived)
					throw new OperationCanceledException ("Can't make purchase, request product data before. Call RequestProductsData before Purchase.");

			_latOperation = OperationType.Activation;

			if (!AvaliableProducts.ContainsKey(productId)) 
			{
				ProductPurchaseFailed.Raise(productId);
				SendErorrData(String.Format ("Can't purchase '{0}'. Product not available.", productId), null);

				return;
			}

			var product = AvaliableProducts [productId];
			var payment = SKPayment.PaymentWithProduct(product);

			AddPayment(payment);
		}
		
		public void Purchase(SKProduct product)
		{
			_latOperation = OperationType.Activation;			
			var payment = SKPayment.PaymentWithProduct(product);
			
			AddPayment(payment);
		}
		
		public void RestorePurchases()
		{
			_latOperation = OperationType.Restoring;
			SKPaymentQueue.DefaultQueue.RestoreCompletedTransactions();
		}




		// Delegate logic

		public override void ReceivedResponse(SKProductsRequest request, SKProductsResponse response)
		{
			_productInfoReceived = true;

			SKProduct[] products = response.Products;

			foreach (var product in products)
			{
				AvaliableProducts[product.ProductIdentifier] = product;
			}

			_notAvaliableProducts = response.InvalidProducts;

			ProductsInfoReceived.Raise(AvaliableProducts);
		}

		public override void RequestFinished(SKRequest request)
		{
			ProductRequestSucceed.Raise(_inAppManagerInstance);
		}

		public override void RequestFailed(SKRequest request, NSError error)
		{			
			IsPurchasing = false;

			ProductRequestFailed.Raise(error);

			SendErorrData("InApp products request failed.", error);
		}

		internal void RaiseCompletePaymentTransaction(SKPaymentTransaction transaction, bool isSuccessfull = true)
		{
			IsPurchasing = false;

			SKPaymentQueue.DefaultQueue.FinishTransaction(transaction);

			var isValid = VerificationManager.Instance.VerifyPurchase(transaction);

			if (isValid && isSuccessfull)
			{
				PaymentTransactionSucceed.Raise(_inAppManagerInstance, transaction.Payment.ProductIdentifier);
			}
			else
			{
				if (transaction.Error.Code == 2)
					UserCancelled.Raise();
				else
					PaymentTransactionFailed.Raise(_inAppManagerInstance, transaction.Payment.ProductIdentifier, transaction.Error);
			}
		}

		internal void RaisePaymentTransactionInitiated(SKPaymentTransaction transaction)
		{			
			IsPurchasing = true;

			PaymentTransactionInitiated.Raise(_inAppManagerInstance, transaction.Payment.ProductIdentifier);
		}

		internal void RaiseRestoredPaymentTransaction(SKPaymentTransaction transaction)
		{
			RaiseCompletePaymentTransaction(transaction);

			PaymentTransactionRestored.Raise(_inAppManagerInstance, transaction.Payment.ProductIdentifier);
		}

		internal void RaiseFailedPaymentTransaction(SKPaymentTransaction transaction)
		{
			RaiseCompletePaymentTransaction(transaction, false);

			SendErorrData("InApp payment transaction failed.", transaction.Error);
		}

		internal void RaiseRestoreSucceed()
		{
			RestoreSucceed.Raise(_inAppManagerInstance);
		}
		
		internal void RaiseRestoreFailed(NSError error)
		{
			if (error.Code == 2)
				UserCancelled.Raise ();
			else
				RestoreFailed.Raise(_inAppManagerInstance, error);
			
			SendErorrData("InApp restore completed transactions failed.", error);
		}

		internal void AddPayment(SKPayment payment)
		{
			SKPaymentQueue.DefaultQueue.AddPayment(payment);
		}

		internal void SaveDownload (SKDownload download)
		{
			var contentsPath = Path.Combine(download.ContentUrl.Path, "Contents");
			var sourceFilesPatches = Directory.EnumerateFiles (contentsPath);
			var productId = download.Transaction.Payment.ProductIdentifier;

			ContentManager.SaveDownload (productId, sourceFilesPatches, (savedFilePatches) => { 
				SavingCompleted.Raise(savedFilePatches);
				RaiseCompletePaymentTransaction(download.Transaction);
			});
		}

		internal void RaiseDownloadEstimateChanged(SKDownload download)
		{
			DownloadEstimateChanged.Raise(download.Transaction.Payment.ProductIdentifier, download.Progress, download.TimeRemaining);
		}

		internal void RaiseDownloadPaused(SKDownload download)
		{
			DownloadPaused.Raise (download.Transaction.Payment.ProductIdentifier, download.Error);
		}

		internal void RaiseDownloadCompleted(SKDownload download)
		{
			DownloadFinished.Raise (download.Transaction.Payment.ProductIdentifier, download.ContentIdentifier);
		}

		internal void RaiseDownloadFailed(SKDownload download)
		{
			DownloadFailed.Raise (download.Transaction.Payment.ProductIdentifier, download.Error);

			SendErorrData ("Download failed", download.Error);
		}

		internal void RaiseDownloadCancelled(SKDownload download)
		{
			DownloadCancelled.Raise (download.Transaction.Payment.ProductIdentifier, download.Error);

			SendErorrData ("Download cancelled", download.Error);			
		}

		private void SendErorrData (string message, NSError error)
		{
			if (error == null) 
			{
				Logger.Error(message);
				return;
			}

			var errorMessage = String.Concat(message, Environment.NewLine, error.Code, Environment.NewLine, error.ToString(), Environment.NewLine);

			if (error.UserInfo != null)
			{
				error.UserInfo.Aggregate(errorMessage, (current, data) => String.Concat(current, data.Key + ": " + data.Value, Environment.NewLine));
			}

			Logger.Error(errorMessage);
		}
	}
}


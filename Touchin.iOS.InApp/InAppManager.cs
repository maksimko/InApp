using System;

using MonoTouch.StoreKit;
using MonoTouch.Foundation;
using System.Collections.Generic;
using System.Linq;
using Touchin.iOS.InApp.Common;
using Touchin.iOS.InApp.Extensions;

namespace Touchin.iOS.InApp
{
	public enum OperationType
	{
		None,
		Activation,
		Restoring
	}

	public interface InAppManagerInterface
	{
		event Action<Dictionary<string, SKProduct>> ProductsInfoReceived;
		event Action<NSError> ProductRequestFailed;
		event Action<InAppManagerInterface> ProductRequestSucceed;

		event Action<InAppManagerInterface, SKPaymentTransaction> PaymentTransactionInitiated;
		event Action<InAppManagerInterface, SKPaymentTransaction> PaymentTransactionSucceed;
		event Action<InAppManagerInterface, SKPaymentTransaction> PaymentTransactionFailed;
		event Action<InAppManagerInterface, SKPaymentTransaction> PaymentRestoreTransactionSucceed;
						
		Dictionary<string, SKProduct> AvaliableProducts { get; }
		List<string> NotAvaliableProducts { get; }
		bool CanMakePayments { get; }
		bool IsPurchasing { get; set; }
		OperationType LastOperation { get; }
		ILog Logger { get; set; }

		void RequestProductsData(string productIdentifiers);
		void RequestProductsData(List<string> productIdentifiers);
		void Purchase(string productId);
		void Purchase(SKProduct product);
		void RestorePurchases();
	}



	public class InAppManager : SKProductsRequestDelegate, InAppManagerInterface
	{
		public event Action<Dictionary<string, SKProduct>> ProductsInfoReceived;
		public event Action<NSError> ProductRequestFailed;
		public event Action<InAppManagerInterface> ProductRequestSucceed;

		public event Action<InAppManagerInterface, SKPaymentTransaction> PaymentTransactionInitiated;
		public event Action<InAppManagerInterface, SKPaymentTransaction> PaymentTransactionSucceed;
		public event Action<InAppManagerInterface, SKPaymentTransaction> PaymentTransactionFailed;
		public event Action<InAppManagerInterface, SKPaymentTransaction> PaymentRestoreTransactionSucceed;		

		private SKPaymentTransactionObserver _paymentTransactionObserver;		
		private SKProductsRequest _productsRequest;
		private Dictionary<string, SKProduct> _avaliableProducts;
		private string[] _notAvaliableProducts;

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

		private static InAppManagerInterface _inAppManagerInstance;
		public static InAppManagerInterface Instance
		{
			get { return _inAppManagerInstance ?? (_inAppManagerInstance = new InAppManager()); }
		}

		private InAppManager()
		{
			_paymentTransactionObserver = new InAppPaymentObserver(this);
			SKPaymentQueue.DefaultQueue.AddTransactionObserver(_paymentTransactionObserver);
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

		public List<string> NotAvaliableProducts
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

		public void RequestProductsData(List<string> productIdentifiers)
		{
			IsPurchasing = true;

			var array = new NSString[productIdentifiers.Count];

			for(var i=0; i < productIdentifiers.Count; i++)
			{
				array[i] = new NSString(productIdentifiers[i]);
			}

			var products = NSSet.MakeNSObjectSet<NSString>(array);

			_productsRequest = new SKProductsRequest(products);
			_productsRequest.Delegate = this;
			_productsRequest.Start();
		}

		public void Purchase(string productId)
		{
			_latOperation = OperationType.Activation;
			var payment = SKPayment.PaymentWithProduct(productId);

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

		internal void CompletePaymentTransaction(SKPaymentTransaction transaction, bool isSuccessfull = true)
		{
			IsPurchasing = false;

			var isValid = VerificationManager.Instance.VerifyPurchase(transaction);

			if (isValid && isSuccessfull)
			{
				SKPaymentQueue.DefaultQueue.FinishTransaction(transaction);
				PaymentTransactionSucceed.Raise(_inAppManagerInstance, transaction);
			}
			else
			{
				PaymentTransactionFailed.Raise(_inAppManagerInstance, transaction);
			}
		}

		internal void RaisePaymentTransactionInitiated(SKPaymentTransaction transaction)
		{			
			IsPurchasing = true;

			PaymentTransactionInitiated.Raise(_inAppManagerInstance, transaction);
		}

		internal void RestorePaymentTransaction(SKPaymentTransaction transaction)
		{
			CompletePaymentTransaction(transaction);

			PaymentRestoreTransactionSucceed.Raise(_inAppManagerInstance, transaction);
		}

		internal void FailedPaymentTransaction(SKPaymentTransaction transaction)
		{
			CompletePaymentTransaction(transaction, false);

			SendErorrData("InApp payment transaction failed.", transaction.Error);
		}

		internal void AddPayment(SKPayment payment)
		{
			SKPaymentQueue.DefaultQueue.AddPayment(payment);
		}

		private void SendErorrData(string message, NSError error)
		{
			var errorMessage = String.Concat(message, Environment.NewLine, error.Code, Environment.NewLine, error.ToString(), Environment.NewLine);

			if (error.UserInfo != null)
			{
				error.UserInfo.Aggregate(errorMessage, (current, data) => String.Concat(current, data.Key + ": " + data.Value, Environment.NewLine));
			}

			Logger.Error(errorMessage);
		}
	}
}


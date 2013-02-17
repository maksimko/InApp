using System;
using MonoTouch.Foundation;
using MonoTouch.StoreKit;
using System.Json;
using System.Net;
using MonoTouch.UIKit;
using MonoTouch.Security;
using Touchin.iOS.InApp.Extensions;
using Touchin.iOS.InApp.Common;

namespace Touchin.iOS.InApp
{
	internal class VerificationManager
	{
		const string TransactionsIdSettingsKey = @"SoonerTransactions";
		const string ContentProviderSharedSecret = "";
		const string RealVerificationServerUrl = "https://buy.itunes.apple.com/verifyReceipt";
		const string SandboxVerificationServerUrl = "https://sandbox.itunes.apple.com/verifyReceipt";
		
		#if INAPP_SANDBOX
		const string VerificationServerUrl = SandboxVerificationServerUrl;
		#else
		const string VerificationServerUrl = RealVerificationServerUrl;
		#endif
		
		private NSMutableDictionary _transactionReceiptStorageDictionary;
		private static VerificationManager _verificationManager;
		
		public static VerificationManager Instance
		{
			get
			{
				if(_verificationManager == null)
					_verificationManager = new VerificationManager();
				
				return _verificationManager;
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
		
		private VerificationManager()
		{
			_transactionReceiptStorageDictionary = new NSMutableDictionary();
		}
		
		public bool VerifyPurchase(SKPaymentTransaction transaction)
		{
			bool isValid = IsTransactionAndReceiptValid(transaction);
			
			if (!isValid)
				return isValid;
			
			var message = new LogMessage("VerifyPurchase (transaction and receipt are valid)", Logger);
			
			var jsonObjectString = EncodeBase64 (transaction.TransactionReceipt.ToString());			
			var payload = @"{""receipt-data"" : """ + jsonObjectString + @""", ""password"" : """ + ContentProviderSharedSecret + @"""}";
			var serverURL = VerificationServerUrl; 
			
			var client = new WebClient();
			
			message.Append("serverURL: " + serverURL);
			
			try 
			{
				var response = client.UploadData(serverURL, System.Text.Encoding.UTF8.GetBytes (payload));
				
				var responseString = System.Text.Encoding.UTF8.GetString(response);
				
				isValid = DoesTransactionInfoMatchReceipt (responseString);
			} 
			catch (WebException e) 
			{
				message.Append("Request exception: " + e.ToString());
				
				isValid = false;
			}
			
			if (!isValid)
				message.Send();
			
			return isValid;
		}
		
		private bool DoesTransactionInfoMatchReceipt(string receiptString)
		{			
			var message = new LogMessage("DoesTransactionInfoMatchReceipt", Logger);
			
			var verifiedReceiptDictionary = JsonValue.Parse(receiptString);
			var status = verifiedReceiptDictionary["status"].ToString();
			
			if (status == null)
			{
				message.Send("status is null");
				
				return false;
			}
			
			int verifyReceiptStatus = Convert.ToInt32(status);
			
			if (verifyReceiptStatus != 0 && verifyReceiptStatus != 21006)
			{
				message.Send(String.Format("verifyReceiptStatus: {0}", verifyReceiptStatus));
				
				return false; // 21006 = This receipt is valid but the subscription has expired.
			}
			
			var verifiedReceiptReceiptDictionary = verifiedReceiptDictionary.GetValue("receipt");
			var verifiedReceiptUniqueIdentifier = verifiedReceiptReceiptDictionary.GetValue("unique_identifier");
			var transactionIdFromVerifiedReceipt = verifiedReceiptReceiptDictionary.GetValue("transaction_id");
			
			var transaction = _transactionReceiptStorageDictionary[new NSString(transactionIdFromVerifiedReceipt)];
			var purchaseInfoFromTransaction = JsonValue.Parse(transaction.ToString());
			if (purchaseInfoFromTransaction == null)
			{
				message.Send("purchaseInfoFromTransaction is null");
				
				return false; 
			}
			
			int failCount = 0;
			
			if (verifiedReceiptReceiptDictionary["bid"].ToString() != purchaseInfoFromTransaction["bid"].ToString())
			{
				message.Append(String.Format("bid is not equal {0} | {1}", verifiedReceiptReceiptDictionary["bid"].ToString(), purchaseInfoFromTransaction["bid"].ToString()));
				
				failCount++;
			}
			
			if (verifiedReceiptReceiptDictionary["product_id"].ToString() != purchaseInfoFromTransaction["product-id"].ToString())
			{
				message.Append(String.Format("product_id is not equal {0} | {1}", verifiedReceiptReceiptDictionary["product_id"].ToString(), purchaseInfoFromTransaction["product-id"].ToString()));
				
				failCount++;
			}
			
			if (verifiedReceiptReceiptDictionary["quantity"].ToString() != purchaseInfoFromTransaction["quantity"].ToString())
			{
				message.Append(String.Format("quantity is not equal {0} | {1}", verifiedReceiptReceiptDictionary["quantity"].ToString(), purchaseInfoFromTransaction["quantity"].ToString()));
				
				failCount++;
			}
			
			if (verifiedReceiptReceiptDictionary["item_id"].ToString() != purchaseInfoFromTransaction["item-id"].ToString())
			{
				message.Append(String.Format("item_id is not equal {0} | {1}", verifiedReceiptReceiptDictionary["item_id"].ToString(), purchaseInfoFromTransaction["item-id"].ToString()));
				
				failCount++;
			}
			
			var isValidIdentifier = false;
			
			if (UIDevice.CurrentDevice.RespondsToSelector(new MonoTouch.ObjCRuntime.Selector("identifierForVendor"))) 
			{
				var localIdentifier = UIDevice.CurrentDevice.IdentifierForVendor.AsString();
				
				var purchaseInfoUniqueVendorId = purchaseInfoFromTransaction.GetValue("unique-vendor-identifier");
				var verifiedReceiptVendorIdentifier = verifiedReceiptReceiptDictionary.GetValue("unique_vendor_identifier");
				
				if (purchaseInfoUniqueVendorId != null && verifiedReceiptVendorIdentifier != null) 
				{
					var vendorId = purchaseInfoUniqueVendorId.AsString();
					
					if (!vendorId.Equals(verifiedReceiptVendorIdentifier.AsString(), StringComparison.InvariantCultureIgnoreCase) || !vendorId.Equals(localIdentifier, StringComparison.InvariantCultureIgnoreCase))
					{
						//#if !DEBUG
						message.Append(String.Format("vendorId is not equal verifiedReceiptVendorIdentifier {0} | {1}", vendorId, verifiedReceiptVendorIdentifier.AsString()));
						message.Append(String.Format("vendorId is not equal localIdentifier {0} | {1}", vendorId, localIdentifier));						
						
						failCount++; // comment this line out to test in the Simulator
						//#endif
					}
					else 
					{
						isValidIdentifier = true;
					}
				}
			} 
			
			
			if(!isValidIdentifier) 
			{
				var localIdentifier = UIDevice.CurrentDevice.UniqueIdentifier;
				
				var purchaseInfoUniqueId = purchaseInfoFromTransaction["unique-identifier"].AsString();
				if (!purchaseInfoUniqueId.Equals(verifiedReceiptUniqueIdentifier.AsString(), StringComparison.InvariantCultureIgnoreCase) || !purchaseInfoUniqueId.Equals(localIdentifier, StringComparison.InvariantCultureIgnoreCase))
				{					
					//#if !DEBUG	
					message.Append(String.Format("purchaseInfoUniqueId is not equal verifiedReceiptUniqueIdentifier {0} | {1}", purchaseInfoUniqueId, verifiedReceiptUniqueIdentifier.AsString()));
					message.Append(String.Format("purchaseInfoUniqueId is not equal localIdentifier {0} | {1}", purchaseInfoUniqueId, localIdentifier));
					
					failCount++; // comment this line out to test in the Simulator
					//#endif
				}
			}
			
			if (failCount > 0) {
				message.Send();
				
				return false;
			}
			
			return true;
		}
		
		private bool IsTransactionAndReceiptValid(SKPaymentTransaction transaction)
		{
			var message = new LogMessage("IsTransactionAndReceiptValid", Logger);
			
			var isTransactionValid = transaction != null && transaction.TransactionReceipt != null && transaction.TransactionReceipt.Length > 0;
			
			if (!isTransactionValid)
			{
				message.Send("transaction is not valid");
				
				return false;
			}
			
			var receiptDict = JsonValue.Parse(transaction.TransactionReceipt.ToString().Replace(" = ", " : "));
			var transactionPurchaseInfo = receiptDict["purchase-info"].ToString();
			var decodedPurchaseInfo = DecodeBase64(transactionPurchaseInfo);
			var purchaseInfoDict = JsonValue.Parse(decodedPurchaseInfo.ToString().Replace(" = ", " : "));
			
			var transactionId = purchaseInfoDict["transaction-id"].AsString();
			var purchaseDateString = purchaseInfoDict["purchase-date"].AsString();
			var signature = receiptDict["signature"].ToString();
			
			var dateFormat = "yyyy-MM-dd HH:mm:ss GMT";
			purchaseDateString = purchaseDateString.Replace("Etc/", "");
			var purchaseDate = DateTime.ParseExact(purchaseDateString, dateFormat, System.Globalization.CultureInfo.InvariantCulture);
			
			if (!IsTransactionUnique(transactionId))
			{
				message.Send("transaction id is not unique: " + transactionId);
				
				return false;		
			}
			
			//			var result = CheckReceiptSecurity(transactionPurchaseInfo, signature, purchaseDate);
			//			if (!result) return false;
			
			if (!DoTransactionDetailsMatchPurchaseInfo(transaction, purchaseInfoDict))
			{				
				return false;
			}
			
			SaveTransactionId (transactionId);
			
			_transactionReceiptStorageDictionary.SetValueForKey(new NSString(purchaseInfoDict.ToString ()), 
			                                                    new NSString(transactionId));
			
			return true;
		}
		
		private void SaveTransactionId (string transactionId)
		{			
			var defaults = NSUserDefaults.StandardUserDefaults;
			var transactionDictionary = TransactionsIdSettingsKey;
			var dictionary = NSMutableDictionary.FromDictionary (defaults [transactionDictionary] as NSDictionary);
			
			if (dictionary == null) {
				dictionary = NSMutableDictionary.FromObjectAndKey (new NSNumber (1), new NSString (transactionId));
			} else {
				dictionary.SetValueForKey (new NSNumber (1), new NSString (transactionId));
			}
			defaults[transactionDictionary] = dictionary;
			defaults.Synchronize ();
		}
		
		private bool DoTransactionDetailsMatchPurchaseInfo(SKPaymentTransaction transaction, JsonValue purchaseInfoDict)
		{
			var message = new LogMessage("DoTransactionDetailsMatchPurchaseInfo", Logger);
			
			if (transaction == null || purchaseInfoDict == null)
			{
				message.Send(String.Format("transaction == {0} | purchaseInfoDict == {1}", transaction == null, purchaseInfoDict == null));
				
				return false;
			}
			
			int failCount = 0;
			
			if (transaction.Payment.ProductIdentifier != purchaseInfoDict["product-id"].ToString().Trim('"'))
			{
				message.Append(String.Format("transaction.Payment.ProductIdentifier != purchaseInfoDict[product-id] {0} transaction.Payment.ProductIdentifier: {1} purchaseInfoDict[product-id]: {2} ", Environment.NewLine, transaction.Payment.ProductIdentifier, purchaseInfoDict["product-id"].ToString().Trim('"')));
				failCount++;
			}
			
			if (transaction.TransactionIdentifier != purchaseInfoDict["transaction-id"].ToString().Trim('"'))
			{
				message.Append(String.Format("transaction.TransactionIdentifier != purchaseInfoDict[transaction-id] {0} transaction.TransactionIdentifier: {1} purchaseInfoDict[transaction-id]: {2} ", Environment.NewLine, transaction.TransactionIdentifier, purchaseInfoDict["transaction-id"].ToString().Trim('"')));					
				failCount++;
			}
			
			if (failCount > 0) {
				message.Send();
				
				return false;
			}
			
			return true;
		}
		
		private bool IsTransactionUnique(string transactionId)
		{
			var defaults = NSUserDefaults.StandardUserDefaults;
			defaults.Synchronize ();
			
			var key = new NSString(TransactionsIdSettingsKey);
			
			if (defaults[key] == null) 
			{
				var transactionIdDictionaries = new NSMutableDictionary ();
				
				defaults.SetValueForKey(transactionIdDictionaries, key);
				defaults.Synchronize();
			}
			
			var transactionIds = defaults[TransactionsIdSettingsKey] as NSDictionary;
			
			if (transactionIds[transactionId] == null) 
			{
				return true;
			}
			
			return false;
		}
		
		private string EncodeBase64(string toEncode)
		{
			byte[] toEncodeAsBytes = System.Text.Encoding.UTF8.GetBytes(toEncode);
			var returnValue = System.Convert.ToBase64String(toEncodeAsBytes);
			
			return returnValue;
		}
		
		private string DecodeBase64(string encodedData) 
		{
			encodedData = encodedData.Trim ('"');
			byte[] encodedDataAsBytes = System.Convert.FromBase64String(encodedData);
			var returnValue = System.Text.Encoding.UTF8.GetString(encodedDataAsBytes);
			
			return returnValue;
		}
		
		bool CheckReceiptSecurity (string purchaseInfoString, string signatureInfo, DateTime purchaseDate)
		{
			return true;
			// Can't be implemented now
			
			var isValid = false;
			
			SecCertificate leaf = null, intermediate = null;
			SecTrust trust = null;
			SecPolicy policy = null;
			
			NSData certificate_data;
			NSArray anchors;
			
			var purchaseInfo = NSData.FromString(purchaseInfoString, NSStringEncoding.ASCIIStringEncoding);
			var signature = NSData.FromString(signatureInfo, NSStringEncoding.ASCIIStringEncoding);
			
			if (purchaseInfo == null || signature == null)
				return false;
			
			/*
			 size_t purchase_info_length;
		    uint8_t *purchase_info_bytes = base64_decode([purchase_info_string cStringUsingEncoding:NSASCIIStringEncoding],
		                                                 &purchase_info_length);
		    
		    size_t signature_length;
		    uint8_t *signature_bytes = base64_decode([signature_string cStringUsingEncoding:NSASCIIStringEncoding],
		                                             &signature_length);
		    
		    require(purchase_info_bytes && signature_bytes, outLabel);
			    

			//     Binary format looks as follows:
			//     
			//     RECEIPTVERSION | SIGNATURE | CERTIFICATE SIZE | CERTIFICATE
			//     1 byte           128         4 bytes
			//     big endian
			     
			//     Extract version, signature and certificate(s).
			//     Check receipt version == 2.
			//     Sanity check that signature is 128 bytes.
			//     Sanity check certificate size <= remaining payload data.

						
			#pragma pack(push, 1)
						struct signature_blob {
							uint8_t version;
							uint8_t signature[128];
							uint32_t cert_len;
							uint8_t certificate[];
						} *signature_blob_ptr = (struct signature_blob *)signature_bytes;
			#pragma pack(pop)
						uint32_t certificate_len;
					

		     //Make sure the signature blob is long enough to safely extract the version and
		     //cert_len fields, then perform a sanity check on the fields.
     
			require(signature_length > offsetof(struct signature_blob, certificate), outLabel);
			require(signature_blob_ptr->version == 2, outLabel);
			certificate_len = ntohl(signature_blob_ptr->cert_len);
			
			require(signature_length - offsetof(struct signature_blob, certificate) >= certificate_len, outLabel);
			

		     //Validate certificate chains back to valid receipt signer; policy approximation for now
		     //set intermediate as a trust anchor; current intermediate lapses in 2016.

			
			certificate_data = [NSData dataWithBytes:signature_blob_ptr->certificate length:certificate_len];
			require(leaf = SecCertificateCreateWithData(NULL, (__bridge CFDataRef) certificate_data), outLabel);
			
			certificate_data = [NSData dataWithBytes:iTS_intermediate_der length:iTS_intermediate_der_len];
			require(intermediate = SecCertificateCreateWithData(NULL, (__bridge CFDataRef) certificate_data), outLabel);
			
			anchors = [NSArray arrayWithObject:(__bridge id)intermediate];
			require(anchors, outLabel);
			
			require_noerr(SecTrustCreateWithCertificates(leaf, policy, &trust), outLabel);
			require_noerr(SecTrustSetAnchorCertificates(trust, (__bridge CFArrayRef) anchors), outLabel);
			
			if (purchaseDate)
			{
				require_noerr(SecTrustSetVerifyDate(trust, purchaseDate), outLabel);
			}
			
			SecTrustResultType trust_result;
			require_noerr(SecTrustEvaluate(trust, &trust_result), outLabel);
			require(trust_result == kSecTrustResultUnspecified, outLabel);
			
			require(2 == SecTrustGetCertificateCount(trust), outLabel);
			

		     //Chain is valid, use leaf key to verify signature on receipt by
		     //calculating SHA1(version|purchaseInfo)

			
			CC_SHA1_CTX sha1_ctx;
			uint8_t to_be_verified_data[CC_SHA1_DIGEST_LENGTH];
			
			CC_SHA1_Init(&sha1_ctx);
			CC_SHA1_Update(&sha1_ctx, &signature_blob_ptr->version, sizeof(signature_blob_ptr->version));
			CC_SHA1_Update(&sha1_ctx, purchase_info_bytes, purchase_info_length);
			CC_SHA1_Final(to_be_verified_data, &sha1_ctx);
			
			SecKeyRef receipt_signing_key = SecTrustCopyPublicKey(trust);
			require(receipt_signing_key, outLabel);
			require_noerr(SecKeyRawVerify(receipt_signing_key, kSecPaddingPKCS1SHA1,
			                              to_be_verified_data, sizeof(to_be_verified_data),
			                              signature_blob_ptr->signature, sizeof(signature_blob_ptr->signature)),
			              outLabel);
			

		     //Optional:  Verify that the receipt certificate has the 1.2.840.113635.100.6.5.1 Null OID     
		     //The signature is a 1024-bit RSA signature.

			
			valid = YES;
			
		outLabel:
				if (leaf) CFRelease(leaf);
			if (intermediate) CFRelease(intermediate);
			if (trust) CFRelease(trust);
			if (policy) CFRelease(policy);
			
			return valid;
		
			*/
			
			return true;
		}
	}
}


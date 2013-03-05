using System;
using System.Collections.Generic;
using System.IO;
using Touchin.iOS.InApp.Extensions;

namespace Touchin.iOS.InApp
{
	public class ContentManager : IContentManager
	{
		public void SaveDownload (string productId, IEnumerable<string> sourcesPath, Action<IEnumerable<string>> savingCompleteHandler)
		{
			var documentsPath = Environment.GetFolderPath (Environment.SpecialFolder.Personal);
			var targetFolder = Path.Combine (documentsPath, "Purchases", productId);
			
			if (!Directory.Exists (targetFolder))
				Directory.CreateDirectory (targetFolder);

			var destinationFilePaths = new List<string>();

			foreach (var sourcePath in sourcesPath)
			{			
				var fileName = Path.GetFileName(sourcePath);
				var destinationPath = Path.Combine(targetFolder, fileName);
				
				File.Copy (sourcePath, destinationPath, true);

				destinationFilePaths.Add(destinationPath);
			}

			savingCompleteHandler.Raise(destinationFilePaths);
		}
	}
}


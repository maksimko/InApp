using System;
using System.Json;

namespace Touchin.iOS.InApp.Extensions
{
	public static class JsonValueExtensions
	{
		public static JsonValue GetValue(this JsonValue @this, string key)
		{
			return @this.ContainsKey(key) ? @this[key] : null;
		}
		
		public static string AsString(this JsonValue @this)
		{
			return @this == null ? null : @this.ToString().Trim('"');
		}
	}
}


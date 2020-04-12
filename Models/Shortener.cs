using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShortnerService.Models
{
    public static class Shortener
    {
		private static string Token { get; set; }
		// The method with which we generate the token
		public static string GenerateToken()
		{
			string urlsafe = "AaBbCcDdEeFfGgHhIiJjKkLlMmNnOoPpQqRrSsTtUuVvWwXxYyZz0123456789";
			Enumerable.Range(48, 75)
				.Where(i => i < 58 || i > 64 && i < 91 || i > 96)
				.OrderBy(o => new Random().Next())
				.ToList()
				.ForEach(i => urlsafe += Convert.ToChar(i)); // Store each char into urlsafe
			Token = urlsafe.Substring(new Random().Next(0, urlsafe.Length), new Random().Next(2, 6));
			return Token;
		}
	}
}

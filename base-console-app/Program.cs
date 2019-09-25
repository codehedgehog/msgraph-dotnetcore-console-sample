﻿namespace ConsoleGraphTest
{

	using Microsoft.Extensions.Configuration;
	using Microsoft.Graph;
	using Microsoft.Identity.Client;
	using System;
	using System.Collections.Generic;
	using System.Net.Http;

	internal class Program
	{
		private static GraphServiceClient _graphServiceClient;
		private static HttpClient _httpClient;

		private static void Main(string[] args)
		{
			// Load appsettings.json
			var config = LoadAppSettings();
			if (null == config)
			{
				Console.WriteLine("Missing or invalid appsettings.json file. Please see README.md for configuration instructions.");
				return;
			}

			//Query using Graph SDK (preferred when possible)
			GraphServiceClient graphClient = GetAuthenticatedGraphClient(config);
			List<QueryOption> options = new List<QueryOption> { new QueryOption("$top", "1") };

			var graphResult = graphClient.Users.Request(options).GetAsync().Result;
			Console.WriteLine("Graph SDK Result");
			Console.WriteLine(graphResult[0].DisplayName);

			//Direct query using HTTPClient (for beta endpoint calls or not available in Graph SDK)
			HttpClient httpClient = GetAuthenticatedHTTPClient(config);
			Uri Uri = new Uri("https://graph.microsoft.com/v1.0/users?$top=5");
			var httpResult = httpClient.GetStringAsync(Uri).Result;

			Console.WriteLine("HTTP Result");
			Console.WriteLine(httpResult);
		}

		private static GraphServiceClient GetAuthenticatedGraphClient(IConfigurationRoot config)
		{
			var authenticationProvider = CreateAuthorizationProvider(config);
			_graphServiceClient = new GraphServiceClient(authenticationProvider);
			return _graphServiceClient;
		}

		private static HttpClient GetAuthenticatedHTTPClient(IConfigurationRoot config)
		{
			var authenticationProvider = CreateAuthorizationProvider(config);
			_httpClient = new HttpClient(new AuthHandler(authenticationProvider, new HttpClientHandler()));
			return _httpClient;
		}

		private static IAuthenticationProvider CreateAuthorizationProvider(IConfigurationRoot config)
		{
			var clientId = config["applicationId"];
			var clientSecret = config["applicationSecret"];
			var redirectUri = config["redirectUri"];
			var authority = $"https://login.microsoftonline.com/{config["tenantId"]}/v2.0";

			//this specific scope means that application will default to what is defined in the application registration rather than using dynamic scopes
			List<string> scopes = new List<string> { "https://graph.microsoft.com/.default" };
			ConfidentialClientApplication cca = (ConfidentialClientApplication)ConfidentialClientApplicationBuilder.Create(clientId: clientId)
				.WithAdfsAuthority(authorityUri: authority)
				.WithRedirectUri(redirectUri: redirectUri)
				.WithClientSecret(clientSecret: clientSecret)
				.Build();
			return new MsalAuthenticationProvider(cca, scopes.ToArray());
		}

		private static IConfigurationRoot LoadAppSettings()
		{
			try
			{
				var config = new ConfigurationBuilder()
				.SetBasePath(System.IO.Directory.GetCurrentDirectory())
				.AddJsonFile("appsettings.json", false, true)
				.Build();

				// Validate required settings
				if (string.IsNullOrEmpty(config["applicationId"]) ||
						string.IsNullOrEmpty(config["applicationSecret"]) ||
						string.IsNullOrEmpty(config["redirectUri"]) ||
						string.IsNullOrEmpty(config["tenantId"]) ||
						string.IsNullOrEmpty(config["domain"]))
				{
					return null;
				}

				return config;
			}
			catch (System.IO.FileNotFoundException)
			{
				return null;
			}
		}
	}
}
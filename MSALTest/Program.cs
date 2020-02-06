using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Newtonsoft.Json.Linq;
using RestSharp;
using Newtonsoft.Json;

namespace MSALTest
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                RunAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message);
                Console.ResetColor();
            }

            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
        }

        private static async Task RunAsync()
        {
            AuthenticationConfig config = AuthenticationConfig.ReadFromJsonFile("C:\\Users\\jhramirez\\source\\repos\\MSALTest\\MSALTest\\appsettings.json");
            IPublicClientApplication app;

            app = PublicClientApplicationBuilder.Create(config.ClientId)
                .WithAuthority(new Uri(config.Authority))
                .WithRedirectUri(config.redirectURI)
                .Build();
     
            TokenCacheHelper.EnableSerialization(app.UserTokenCache);

            // With client credentials flows the scopes is ALWAYS of the shape "resource/.default", as the 
            // application permissions need to be set statically (in the portal or by PowerShell), and then granted by
            // a tenant administrator
            
            string[] scopes = { $"https://api.businesscentral.dynamics.com/Financials.ReadWrite.All" };

            var accounts = await app.GetAccountsAsync();
            var firstAccount = accounts.FirstOrDefault();

            AuthenticationResult result = null;
            try
            {
                result = await app.AcquireTokenSilent(scopes, firstAccount).ExecuteAsync();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Token acquired \n");
                Console.ResetColor();
            }
            catch (MsalServiceException ex) when (ex.Message.Contains("AADSTS70011"))
            {
                // Invalid scope. The scope has to be of the form "https://resourceurl/.default"
                // Mitigation: change the scope to be as expected
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Scope provided is not supported");
                Console.ResetColor();
            }
            catch (MsalServiceException ex)
            {
                result = await app.AcquireTokenInteractive(scopes)
                .ExecuteAsync();

            }
            if (result != null)
            {
                var clientBC = new RestClient("https://api.businesscentral.dynamics.com/v2.0/Production/api/CertTech/Integrations/v1.0/companies(147adb4d-73df-41ea-8de3-f2ca7cc9d73f)/cTCustomers");
                clientBC.Timeout = -1;
                var requestBC = new RestRequest(Method.GET);
                requestBC.AddHeader("Authorization", "Bearer " + result.AccessToken);
                IRestResponse responseBC = clientBC.Execute(requestBC);
                var bcResponse = responseBC.Content;
                object resposeDataBC = JsonConvert.DeserializeObject(bcResponse);

                Console.Write(resposeDataBC);
                Console.ReadKey();

                //var httpClient = new HttpClient();
                //var apiCaller = new ProtectedApiCallHelper(httpClient);
                //await apiCaller.CallWebApiAndProcessResultASync($"{config.TodoListBaseAddress}", result.AccessToken, Display);
            }
        }

        /// <summary>
        /// Display the result of the Web API call
        /// </summary>
        /// <param name="result">Object to display</param>
        private static void Display(IEnumerable<JObject> result)
        {
            Console.WriteLine("Web Api result: \n");

            foreach (var item in result)
            {
                foreach (JProperty child in item.Properties().Where(p => !p.Name.StartsWith("@")))
                {
                    Console.WriteLine($"{child.Name} = {child.Value}");
                }

                Console.WriteLine("");
            }
        }
    }
}

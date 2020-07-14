using Microsoft.Rest;
using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FastJira.api
{
    public class ClientCredentials : ServiceClientCredentials
    {
        public string Username { get; set; }
        public string Password { get; set; }

        public override Task ProcessHttpRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password))
            {
                string headerVal = Username + ":" + Password;
                headerVal = Convert.ToBase64String(Encoding.UTF8.GetBytes(headerVal));
                request.Headers.Add("Authorization", "Basic " + headerVal);
            }
            return base.ProcessHttpRequestAsync(request, cancellationToken);
        }
    }
}

using Microsoft.Rest;
using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Fast_Jira.api
{
    class ClientCredentials : ServiceClientCredentials
    {
        public string Username { get; set; }
        public string Password { get; set; }

        public override Task ProcessHttpRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password))
            {
                string HeaderVal = Username + ":" + Password;
                HeaderVal = Convert.ToBase64String(Encoding.UTF8.GetBytes(HeaderVal));
                request.Headers.Add("Authorization", "Basic " + HeaderVal);
            }
            return base.ProcessHttpRequestAsync(request, cancellationToken);
        }
    }
}

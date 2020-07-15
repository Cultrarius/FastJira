﻿// Code generated by Microsoft (R) AutoRest Code Generator 0.16.0.0
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.

namespace FastJira
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Rest;
    using Microsoft.Rest.Serialization;
    using Newtonsoft.Json;
    using Models;

    public partial class JiraAPI : ServiceClient<JiraAPI>, IJiraAPI
    {
        /// <summary>
        /// The base URI of the service.
        /// </summary>
        public Uri BaseUri { get; set; }

        /// <summary>
        /// Gets or sets json serialization settings.
        /// </summary>
        public JsonSerializerSettings SerializationSettings { get; private set; }

        /// <summary>
        /// Gets or sets json deserialization settings.
        /// </summary>
        public JsonSerializerSettings DeserializationSettings { get; private set; }        

        /// <summary>
        /// Subscription credentials which uniquely identify client subscription.
        /// </summary>
        public ServiceClientCredentials Credentials { get; private set; }

        /// <summary>
        /// Initializes a new instance of the JiraAPI class.
        /// </summary>
        /// <param name='handlers'>
        /// Optional. The delegating handlers to add to the http client pipeline.
        /// </param>
        protected JiraAPI(params DelegatingHandler[] handlers) : base(handlers)
        {
            this.Initialize();
        }

        /// <summary>
        /// Initializes a new instance of the JiraAPI class.
        /// </summary>
        /// <param name='rootHandler'>
        /// Optional. The http client handler used to handle http transport.
        /// </param>
        /// <param name='handlers'>
        /// Optional. The delegating handlers to add to the http client pipeline.
        /// </param>
        protected JiraAPI(HttpClientHandler rootHandler, params DelegatingHandler[] handlers) : base(rootHandler, handlers)
        {
            this.Initialize();
        }

        /// <summary>
        /// Initializes a new instance of the JiraAPI class.
        /// </summary>
        /// <param name='baseUri'>
        /// Optional. The base URI of the service.
        /// </param>
        /// <param name='handlers'>
        /// Optional. The delegating handlers to add to the http client pipeline.
        /// </param>
        protected JiraAPI(Uri baseUri, params DelegatingHandler[] handlers) : this(handlers)
        {
            if (baseUri == null)
            {
                throw new ArgumentNullException("baseUri");
            }
            this.BaseUri = baseUri;
        }

        /// <summary>
        /// Initializes a new instance of the JiraAPI class.
        /// </summary>
        /// <param name='baseUri'>
        /// Optional. The base URI of the service.
        /// </param>
        /// <param name='rootHandler'>
        /// Optional. The http client handler used to handle http transport.
        /// </param>
        /// <param name='handlers'>
        /// Optional. The delegating handlers to add to the http client pipeline.
        /// </param>
        protected JiraAPI(Uri baseUri, HttpClientHandler rootHandler, params DelegatingHandler[] handlers) : this(rootHandler, handlers)
        {
            if (baseUri == null)
            {
                throw new ArgumentNullException("baseUri");
            }
            this.BaseUri = baseUri;
        }

        /// <summary>
        /// Initializes a new instance of the JiraAPI class.
        /// </summary>
        /// <param name='credentials'>
        /// Required. Subscription credentials which uniquely identify client subscription.
        /// </param>
        /// <param name='handlers'>
        /// Optional. The delegating handlers to add to the http client pipeline.
        /// </param>
        public JiraAPI(ServiceClientCredentials credentials, params DelegatingHandler[] handlers) : this(handlers)
        {
            if (credentials == null)
            {
                throw new ArgumentNullException("credentials");
            }
            this.Credentials = credentials;
            if (this.Credentials != null)
            {
                this.Credentials.InitializeServiceClient(this);
            }
        }

        /// <summary>
        /// Initializes a new instance of the JiraAPI class.
        /// </summary>
        /// <param name='credentials'>
        /// Required. Subscription credentials which uniquely identify client subscription.
        /// </param>
        /// <param name='rootHandler'>
        /// Optional. The http client handler used to handle http transport.
        /// </param>
        /// <param name='handlers'>
        /// Optional. The delegating handlers to add to the http client pipeline.
        /// </param>
        public JiraAPI(ServiceClientCredentials credentials, HttpClientHandler rootHandler, params DelegatingHandler[] handlers) : this(rootHandler, handlers)
        {
            if (credentials == null)
            {
                throw new ArgumentNullException("credentials");
            }
            this.Credentials = credentials;
            if (this.Credentials != null)
            {
                this.Credentials.InitializeServiceClient(this);
            }
        }

        /// <summary>
        /// Initializes a new instance of the JiraAPI class.
        /// </summary>
        /// <param name='baseUri'>
        /// Optional. The base URI of the service.
        /// </param>
        /// <param name='credentials'>
        /// Required. Subscription credentials which uniquely identify client subscription.
        /// </param>
        /// <param name='handlers'>
        /// Optional. The delegating handlers to add to the http client pipeline.
        /// </param>
        public JiraAPI(Uri baseUri, ServiceClientCredentials credentials, params DelegatingHandler[] handlers) : this(handlers)
        {
            if (baseUri == null)
            {
                throw new ArgumentNullException("baseUri");
            }
            if (credentials == null)
            {
                throw new ArgumentNullException("credentials");
            }
            this.BaseUri = baseUri;
            this.Credentials = credentials;
            if (this.Credentials != null)
            {
                this.Credentials.InitializeServiceClient(this);
            }
        }

        /// <summary>
        /// Initializes a new instance of the JiraAPI class.
        /// </summary>
        /// <param name='baseUri'>
        /// Optional. The base URI of the service.
        /// </param>
        /// <param name='credentials'>
        /// Required. Subscription credentials which uniquely identify client subscription.
        /// </param>
        /// <param name='rootHandler'>
        /// Optional. The http client handler used to handle http transport.
        /// </param>
        /// <param name='handlers'>
        /// Optional. The delegating handlers to add to the http client pipeline.
        /// </param>
        public JiraAPI(Uri baseUri, ServiceClientCredentials credentials, HttpClientHandler rootHandler, params DelegatingHandler[] handlers) : this(rootHandler, handlers)
        {
            if (baseUri == null)
            {
                throw new ArgumentNullException("baseUri");
            }
            if (credentials == null)
            {
                throw new ArgumentNullException("credentials");
            }
            this.BaseUri = baseUri;
            this.Credentials = credentials;
            if (this.Credentials != null)
            {
                this.Credentials.InitializeServiceClient(this);
            }
        }

        /// <summary>
        /// An optional partial-method to perform custom initialization.
        ///</summary> 
        partial void CustomInitialize();
        /// <summary>
        /// Initializes client properties.
        /// </summary>
        private void Initialize()
        {
            this.BaseUri = new Uri("http://localhost");
            SerializationSettings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                NullValueHandling = NullValueHandling.Ignore,
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                ContractResolver = new ReadOnlyJsonContractResolver(),
                Converters = new List<JsonConverter>
                    {
                        new Iso8601TimeSpanConverter()
                    }
            };
            DeserializationSettings = new JsonSerializerSettings
            {
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                NullValueHandling = NullValueHandling.Ignore,
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                ContractResolver = new ReadOnlyJsonContractResolver(),
                Converters = new List<JsonConverter>
                    {
                        new Iso8601TimeSpanConverter()
                    }
            };
            CustomInitialize();
        }    
        /// <summary>
        /// Get issue
        /// </summary>
        /// <param name='issueIdOrKey'>
        /// The ID or key of the issue.
        /// </param>
        /// <param name='fields'>
        /// </param>
        /// <param name='fieldsByKeys'>
        /// Whether fields in `fields` are referenced by keys rather than IDs. This
        /// parameter is useful where fields have been added by a connect app and a
        /// field's key may differ from its ID.
        /// </param>
        /// <param name='expand'>
        /// Use [expand](#expansion) to include additional information about the
        /// issues in the response. This parameter accepts a comma-separated list.
        /// Expand options include:
        /// 
        /// *  `renderedFields` Returns field values rendered in HTML format.
        /// *  `names` Returns the display name of each field.
        /// *  `schema` Returns the schema describing a field type.
        /// *  `transitions` Returns all possible transitions for the issue.
        /// *  `editmeta` Returns information about how each field can be edited.
        /// *  `changelog` Returns a list of recent updates to an issue, sorted by
        /// date, starting from the most recent.
        /// *  `versionedRepresentations` Returns a JSON array for each version of a
        /// field's value, with the highest number representing the most recent
        /// version. Note: When included in the request, the `fields` parameter is
        /// ignored.
        /// </param>
        /// <param name='properties'>
        /// A list of issue properties to return for the issue. This parameter accepts
        /// a comma-separated list. Allowed values:
        /// 
        /// *  `*all` Returns all issue properties.
        /// *  Any issue property key, prefixed with a minus to exclude.
        /// 
        /// Examples:
        /// 
        /// *  `*all` Returns all properties.
        /// *  `*all,-prop1` Returns all properties except `prop1`.
        /// *  `prop1,prop2` Returns `prop1` and `prop2` properties.
        /// 
        /// This parameter may be specified multiple times. For example,
        /// `properties=prop1,prop2&amp; properties=prop3`.
        /// </param>
        /// <param name='updateHistory'>
        /// Whether the project in which the issue is created is added to the user's
        /// **Recently viewed** project list, as shown under **Projects** in Jira.
        /// This also populates the [JQL issues search](#api-rest-api-3-search-get)
        /// `lastViewed` field.
        /// </param>
        /// <param name='customHeaders'>
        /// Headers that will be added to request.
        /// </param>
        /// <param name='cancellationToken'>
        /// The cancellation token.
        /// </param>
        /// <return>
        /// A response object containing the response body and response headers.
        /// </return>
        public async Task<HttpOperationResponse<IssueBean>> GetIssueWithHttpMessagesAsync(string issueIdOrKey, string fields = default(string), bool? fieldsByKeys = false, string expand = default(string), string properties = default(string), bool? updateHistory = false, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (issueIdOrKey == null)
            {
                throw new ValidationException(ValidationRules.CannotBeNull, "issueIdOrKey");
            }
            // Tracing
            bool _shouldTrace = ServiceClientTracing.IsEnabled;
            string _invocationId = null;
            if (_shouldTrace)
            {
                _invocationId = ServiceClientTracing.NextInvocationId.ToString();
                Dictionary<string, object> tracingParameters = new Dictionary<string, object>();
                tracingParameters.Add("issueIdOrKey", issueIdOrKey);
                tracingParameters.Add("fields", fields);
                tracingParameters.Add("fieldsByKeys", fieldsByKeys);
                tracingParameters.Add("expand", expand);
                tracingParameters.Add("properties", properties);
                tracingParameters.Add("updateHistory", updateHistory);
                tracingParameters.Add("cancellationToken", cancellationToken);
                ServiceClientTracing.Enter(_invocationId, this, "GetIssue", tracingParameters);
            }
            // Construct URL
            var _baseUrl = this.BaseUri.AbsoluteUri;
            var _url = new Uri(new Uri(_baseUrl + (_baseUrl.EndsWith("/") ? "" : "/")), "rest/api/latest/issue/{issueIdOrKey}").ToString();
            _url = _url.Replace("{issueIdOrKey}", Uri.EscapeDataString(issueIdOrKey));
            List<string> _queryParameters = new List<string>();
            if (fields != null)
            {
                _queryParameters.Add(string.Format("fields={0}", Uri.EscapeDataString(fields)));
            }
            if (fieldsByKeys != null)
            {
                _queryParameters.Add(string.Format("fieldsByKeys={0}", Uri.EscapeDataString(SafeJsonConvert.SerializeObject(fieldsByKeys, this.SerializationSettings).Trim('"'))));
            }
            if (expand != null)
            {
                _queryParameters.Add(string.Format("expand={0}", Uri.EscapeDataString(expand)));
            }
            if (properties != null)
            {
                _queryParameters.Add(string.Format("properties={0}", Uri.EscapeDataString(properties)));
            }
            if (updateHistory != null)
            {
                _queryParameters.Add(string.Format("updateHistory={0}", Uri.EscapeDataString(SafeJsonConvert.SerializeObject(updateHistory, this.SerializationSettings).Trim('"'))));
            }
            if (_queryParameters.Count > 0)
            {
                _url += "?" + string.Join("&", _queryParameters);
            }
            // Create HTTP transport objects
            HttpRequestMessage _httpRequest = new HttpRequestMessage();
            HttpResponseMessage _httpResponse = null;
            _httpRequest.Method = new HttpMethod("GET");
            _httpRequest.RequestUri = new Uri(_url);
            // Set Headers
            if (customHeaders != null)
            {
                foreach(var _header in customHeaders)
                {
                    if (_httpRequest.Headers.Contains(_header.Key))
                    {
                        _httpRequest.Headers.Remove(_header.Key);
                    }
                    _httpRequest.Headers.TryAddWithoutValidation(_header.Key, _header.Value);
                }
            }

            // Serialize Request
            string _requestContent = null;
            // Set Credentials
            if (this.Credentials != null)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await this.Credentials.ProcessHttpRequestAsync(_httpRequest, cancellationToken).ConfigureAwait(false);
            }
            // Send Request
            if (_shouldTrace)
            {
                ServiceClientTracing.SendRequest(_invocationId, _httpRequest);
            }
            cancellationToken.ThrowIfCancellationRequested();
            _httpResponse = await this.HttpClient.SendAsync(_httpRequest, cancellationToken).ConfigureAwait(false);
            if (_shouldTrace)
            {
                ServiceClientTracing.ReceiveResponse(_invocationId, _httpResponse);
            }
            HttpStatusCode _statusCode = _httpResponse.StatusCode;
            cancellationToken.ThrowIfCancellationRequested();
            string _responseContent = null;
            if ((int)_statusCode != 200 && (int)_statusCode != 401 && (int)_statusCode != 404)
            {
                var ex = new HttpOperationException(string.Format("Operation returned an invalid status code '{0}'", _statusCode));
                _responseContent = await _httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                ex.Request = new HttpRequestMessageWrapper(_httpRequest, _requestContent);
                ex.Response = new HttpResponseMessageWrapper(_httpResponse, _responseContent);
                if (_shouldTrace)
                {
                    ServiceClientTracing.Error(_invocationId, ex);
                }
                _httpRequest.Dispose();
                if (_httpResponse != null)
                {
                    _httpResponse.Dispose();
                }
                throw ex;
            }
            // Create Result
            var _result = new HttpOperationResponse<IssueBean>();
            _result.Request = _httpRequest;
            _result.Response = _httpResponse;
            // Deserialize Response
            if ((int)_statusCode == 200)
            {
                _responseContent = await _httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                try
                {
                    _result.Body = SafeJsonConvert.DeserializeObject<IssueBean>(_responseContent, this.DeserializationSettings);
                }
                catch (JsonException ex)
                {
                    _httpRequest.Dispose();
                    if (_httpResponse != null)
                    {
                        _httpResponse.Dispose();
                    }
                    throw new SerializationException("Unable to deserialize the response.", _responseContent, ex);
                }
            }
            if (_shouldTrace)
            {
                ServiceClientTracing.Exit(_invocationId, _result);
            }
            return _result;
        }

        /// <summary>
        /// Search for issues using JQL (GET)
        /// </summary>
        /// Searches for issues using [JQL](https://confluence.atlassian.com/x/egORLQ).
        /// 
        /// If the JQL query expression is too large to be encoded as a query
        /// parameter, use the [POST](#api-rest-api-3-search-post) version of this
        /// resource.
        /// 
        /// This operation can be accessed anonymously.
        /// 
        /// **[Permissions](#permissions) required:** Issues are included in the
        /// response where the user has:
        /// 
        /// *  *Browse projects* [project
        /// permission](https://confluence.atlassian.com/x/yodKLg) for the project
        /// containing the issue.
        /// *  If [issue-level security](https://confluence.atlassian.com/x/J4lKLg)
        /// is configured, issue-level security permission to view the issue.
        /// <param name='jql'>
        /// The [JQL](https://confluence.atlassian.com/x/egORLQ) that defines the
        /// search. Note:
        /// 
        /// *  If no JQL expression is provided, all issues are returned.
        /// *  `username` and `userkey` cannot be used as search terms due to privacy
        /// reasons. Use `accountId` instead.
        /// *  If a user has hidden their email address in their user profile,
        /// partial matches of the email address will not find the user. An exact
        /// match is required.
        /// </param>
        /// <param name='startAt'>
        /// The index of the first item to return in a page of results (page offset).
        /// </param>
        /// <param name='maxResults'>
        /// The maximum number of items to return per page. To manage page size, Jira
        /// may return fewer items per page where a large number of fields are
        /// requested. The greatest number of items returned per page is achieved
        /// when requesting `id` or `key` only.
        /// </param>
        /// <param name='fields'>
        /// A list of fields to return for each issue, use it to retrieve a subset of
        /// fields. This parameter accepts a comma-separated list. Expand options
        /// include:
        /// 
        /// *  `*all` Returns all fields.
        /// *  `*navigable` Returns navigable fields.
        /// *  Any issue field, prefixed with a minus to exclude.
        /// 
        /// Examples:
        /// 
        /// *  `summary,comment` Returns only the summary and comments fields.
        /// *  `-description` Returns all navigable (default) fields except
        /// description.
        /// *  `*all,-comment` Returns all fields except comments.
        /// 
        /// This parameter may be specified multiple times. For example,
        /// `fields=field1,field2&amp;fields=field3`.
        /// 
        /// Note: All navigable fields are returned by default. This differs from [GET
        /// issue](#api-rest-api-3-issue-issueIdOrKey-get) where the default is all
        /// fields.
        /// </param>
        /// <param name='customHeaders'>
        /// Headers that will be added to request.
        /// </param>
        /// <param name='cancellationToken'>
        /// The cancellation token.
        /// </param>
        /// <return>
        /// A response object containing the response body and response headers.
        /// </return>
        public async Task<HttpOperationResponse<SearchResults>> SearchIssuesWithHttpMessagesAsync(string jql = default(string), int? startAt = default(int?), int? maxResults = 50, string fields = default(string), Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Tracing
            bool _shouldTrace = ServiceClientTracing.IsEnabled;
            string _invocationId = null;
            if (_shouldTrace)
            {
                _invocationId = ServiceClientTracing.NextInvocationId.ToString();
                Dictionary<string, object> tracingParameters = new Dictionary<string, object>();
                tracingParameters.Add("jql", jql);
                tracingParameters.Add("startAt", startAt);
                tracingParameters.Add("maxResults", maxResults);
                tracingParameters.Add("fields", fields);
                tracingParameters.Add("cancellationToken", cancellationToken);
                ServiceClientTracing.Enter(_invocationId, this, "SearchIssues", tracingParameters);
            }
            // Construct URL
            var _baseUrl = this.BaseUri.AbsoluteUri;
            var _url = new Uri(new Uri(_baseUrl + (_baseUrl.EndsWith("/") ? "" : "/")), "rest/api/latest/search").ToString();
            List<string> _queryParameters = new List<string>();
            if (jql != null)
            {
                _queryParameters.Add(string.Format("jql={0}", Uri.EscapeDataString(jql)));
            }
            if (startAt != null)
            {
                _queryParameters.Add(string.Format("startAt={0}", Uri.EscapeDataString(SafeJsonConvert.SerializeObject(startAt, this.SerializationSettings).Trim('"'))));
            }
            if (maxResults != null)
            {
                _queryParameters.Add(string.Format("maxResults={0}", Uri.EscapeDataString(SafeJsonConvert.SerializeObject(maxResults, this.SerializationSettings).Trim('"'))));
            }
            if (fields != null)
            {
                _queryParameters.Add(string.Format("fields={0}", Uri.EscapeDataString(fields)));
            }
            if (_queryParameters.Count > 0)
            {
                _url += "?" + string.Join("&", _queryParameters);
            }
            // Create HTTP transport objects
            HttpRequestMessage _httpRequest = new HttpRequestMessage();
            HttpResponseMessage _httpResponse = null;
            _httpRequest.Method = new HttpMethod("GET");
            _httpRequest.RequestUri = new Uri(_url);
            // Set Headers
            if (customHeaders != null)
            {
                foreach(var _header in customHeaders)
                {
                    if (_httpRequest.Headers.Contains(_header.Key))
                    {
                        _httpRequest.Headers.Remove(_header.Key);
                    }
                    _httpRequest.Headers.TryAddWithoutValidation(_header.Key, _header.Value);
                }
            }

            // Serialize Request
            string _requestContent = null;
            // Set Credentials
            if (this.Credentials != null)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await this.Credentials.ProcessHttpRequestAsync(_httpRequest, cancellationToken).ConfigureAwait(false);
            }
            // Send Request
            if (_shouldTrace)
            {
                ServiceClientTracing.SendRequest(_invocationId, _httpRequest);
            }
            cancellationToken.ThrowIfCancellationRequested();
            _httpResponse = await this.HttpClient.SendAsync(_httpRequest, cancellationToken).ConfigureAwait(false);
            if (_shouldTrace)
            {
                ServiceClientTracing.ReceiveResponse(_invocationId, _httpResponse);
            }
            HttpStatusCode _statusCode = _httpResponse.StatusCode;
            cancellationToken.ThrowIfCancellationRequested();
            string _responseContent = null;
            if ((int)_statusCode != 200 && (int)_statusCode != 400 && (int)_statusCode != 401)
            {
                var ex = new HttpOperationException(string.Format("Operation returned an invalid status code '{0}'", _statusCode));
                _responseContent = await _httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                ex.Request = new HttpRequestMessageWrapper(_httpRequest, _requestContent);
                ex.Response = new HttpResponseMessageWrapper(_httpResponse, _responseContent);
                if (_shouldTrace)
                {
                    ServiceClientTracing.Error(_invocationId, ex);
                }
                _httpRequest.Dispose();
                if (_httpResponse != null)
                {
                    _httpResponse.Dispose();
                }
                throw ex;
            }
            // Create Result
            var _result = new HttpOperationResponse<SearchResults>();
            _result.Request = _httpRequest;
            _result.Response = _httpResponse;
            // Deserialize Response
            if ((int)_statusCode == 200)
            {
                _responseContent = await _httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                try
                {
                    _result.Body = SafeJsonConvert.DeserializeObject<SearchResults>(_responseContent, this.DeserializationSettings);
                }
                catch (JsonException ex)
                {
                    _httpRequest.Dispose();
                    if (_httpResponse != null)
                    {
                        _httpResponse.Dispose();
                    }
                    throw new SerializationException("Unable to deserialize the response.", _responseContent, ex);
                }
            }
            if (_shouldTrace)
            {
                ServiceClientTracing.Exit(_invocationId, _result);
            }
            return _result;
        }

    }
}

using System.Net.Http.Headers;

namespace Ocelot.Request.Middleware
{
    public class DownstreamRequest
    {
        private readonly HttpRequestMessage _request;

        public DownstreamRequest(HttpRequestMessage request)
        {
            _request = request;
            Method = _request.Method.Method;
            Headers = _request.Headers;
            Content = _request.Content;

            if (_request.RequestUri == null)
            {
                throw new NullReferenceException("RequestUri is null");
            }

            var requestUri = _request.RequestUri;
            OriginalString = requestUri.OriginalString;
            Scheme = requestUri.Scheme;
            Host = requestUri.Host;
            Port = requestUri.Port;
            AbsolutePath = requestUri.AbsolutePath;
            Query = requestUri.Query;
            
        }

        public HttpHeaders Headers { get; }

        public string Method { get; }

        public string OriginalString { get; }

        public string Scheme { get; set; }

        public string Host { get; set; }

        public int Port { get; set; }

        public string AbsolutePath { get; set; }

        public string Query { get; set; }

        public HttpContent Content { get; }

        public HttpRequestMessage ToHttpRequestMessage()
        {
            var uriBuilder = new UriBuilder
            {
                Port = Port,
                Host = Host,
                Path = AbsolutePath,
                Query = RemoveLeadingQuestionMark(Query),
                Scheme = Scheme,
            };

            _request.RequestUri = uriBuilder.Uri;
            _request.Method = new HttpMethod(Method);
            return _request;
        }

        public string ToUri()
        {
            var uriBuilder = new UriBuilder
            {
                Port = Port,
                Host = Host,
                Path = AbsolutePath,
                Query = RemoveLeadingQuestionMark(Query),
                Scheme = Scheme,
            };

            return uriBuilder.Uri.AbsoluteUri;
        }

        public override string ToString()
        {
            return ToUri();
        }

        private static string RemoveLeadingQuestionMark(string query)
        {
            if (!string.IsNullOrEmpty(query) && query.StartsWith('?'))
            {
                return query[1..];
            }

            return query;
        }
    }
}

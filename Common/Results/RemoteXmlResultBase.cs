namespace Common.Results
{
    using System;
    using System.Net.Http;
    using System.ServiceModel;
    using System.Threading.Tasks;
    using System.Xml.Linq;
    using Common.Interfaces;

    public abstract class RemoteXmlResultBase<T> : ExtendedResultBase, IRemoteXmlResultBase<T>
    {
        private readonly IConfiguration _configuration;

        #region Fields

        private HttpClient _client;
        private Action<T> _onSuccess;
        private readonly IApiCallErrorResponseHandler _apiCallErrorResponseHandler;

        #endregion

        #region Public Properties

        public T Result { get; set; }

        public Func<Task<HttpStreamResult>> GetResourceFunc { get; set; }

        public virtual string RequestUrl
        {
            get
            {
                return _configuration.BaseUrl + ResourcePath;
            }
        }

        public abstract string ResourcePath { get; }

        #endregion

        #region Properties

        protected HttpClient Client
        {
            get
            {
                return _client ?? (_client = GetHttpClient());
            }
        }

        #endregion

        #region Constructors

        protected RemoteXmlResultBase(IConfiguration configuration, IApiCallErrorResponseHandler apiCallErrorResponseHandler)
        {
            _configuration = configuration;
            GetResourceFunc = GetResource;
            _apiCallErrorResponseHandler = apiCallErrorResponseHandler;
        }

        #endregion

        #region Public Methods

        public IRemoteXmlResultBase<T> OnSuccess(Action<T> onSuccess)
        {
            if (_onSuccess != null)
            {
                var oldOnSuccess = _onSuccess;
                _onSuccess = result =>
                {
                    oldOnSuccess(result);
                    onSuccess(result);
                };
            }
            else
            {
                _onSuccess = onSuccess;
            }

            return this;
        }

        public new IRemoteXmlResultBase<T> WithErrorHandler(IErrorHandler errorHandler)
        {
            ErrorHandler = errorHandler;

            return this;
        }

        #endregion

        #region Methods

        public abstract void HandleResponse(XDocument xDocument);

        protected virtual HttpClient GetHttpClient()
        {
            return new HttpClient(new HttpClientHandler
            {
                AllowAutoRedirect = true,
                PreAuthenticate = true,
            });
        }

        protected override async Task ExecuteCore()
        {
            var response = await GetResourceFunc();
            if (response.Exception != null)
            {
                throw response.Exception;
            }

            var xDocument = XDocument.Load(response.Stream);

            HandleFailedCall(xDocument);
            HandleResponse(xDocument);
        }

        protected override void ExecuteOnSuccessAction()
        {
            base.ExecuteOnSuccessAction();
            if (_onSuccess != null)
            {
                _onSuccess(Result);
            }
        }

        protected virtual void HandleFailedCall(XDocument xDocument)
        {
            _apiCallErrorResponseHandler.HandleFailedCall(xDocument);
        }

        protected virtual HttpRequestMessage CreateRequest()
        {
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, RequestUrl);

            return httpRequestMessage;
        }

        private async Task<HttpStreamResult> GetResource()
        {
            const int maxRetries = 3;
            int delay = 1000; // 起始延迟 1 秒

            for (int attempt = 0; attempt <= maxRetries; attempt++)
            {
                var result = new HttpStreamResult();
                try
                {
                    var httpRequestMessage = CreateRequest();
                    var response = await Client.SendAsync(httpRequestMessage);
                    
                    // 处理可重试的错误状态码: 429, 502, 503
                    var statusCode = (int)response.StatusCode;
                    if (statusCode == 429 || statusCode == 502 || statusCode == 503)
                    {
                        if (attempt < maxRetries)
                        {
                            await Task.Delay(delay);
                            delay *= 2; // 指数退避
                            continue;
                        }
                        throw new CommunicationException(
                            string.Format("Server returned {0} after multiple retries.", response.StatusCode));
                    }
                    
                    if (response.IsSuccessStatusCode)
                    {
                        result.Stream = await response.Content.ReadAsStreamAsync();
                    }
                    else
                    {
                        throw new CommunicationException(
                            string.Format(
                                "Response was:\r\nStatus Code: {0}\r\nReason: {1}", response.StatusCode, response.ReasonPhrase));
                    }
                    
                    return result;
                }
                catch (HttpRequestException exception)
                {
                    var innerMessage = exception.InnerException != null
                                           ? exception.Message + "\r\n" + exception.InnerException.Message
                                           : exception.Message;
                    result.Exception =
                        new CommunicationException(
                            string.Format("Could not perform Http request.\r\nMessage:\r\n{0}", innerMessage), exception);
                    return result;
                }
                catch (Exception exception)
                {
                    result.Exception = exception;
                    return result;
                }
            }

            return new HttpStreamResult { Exception = new CommunicationException("Max retries exceeded.") };
        }

        #endregion
    }
}

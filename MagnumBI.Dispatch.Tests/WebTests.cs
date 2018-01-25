using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Authentication;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MagnumBI.Dispatch.Engine.Config;
using MagnumBI.Dispatch.Engine.Config.Datastore;
using MagnumBI.Dispatch.Engine.Config.Queue;
using MagnumBI.Dispatch.Web;
using MagnumBI.Dispatch.Web.Config;
using Newtonsoft.Json;
using Serilog;
using Serilog.Events;
using Xunit;
using Xunit.Abstractions;

namespace MagnumBI.Dispatch.Tests {
    public class WebTests : IDisposable {
        public WebTests(ITestOutputHelper output) {
            this.output = output;
            LoggerConfiguration logConfig = new LoggerConfiguration().WriteTo.Console()
                .WriteTo.TestOutput(output, LogEventLevel.Verbose);
            Log.Logger = logConfig.CreateLogger();

            // Set up web module
            if (!File.Exists(this.configFile)) {
                File.WriteAllText(this.configFile,
                    JsonConvert.SerializeObject(new WebConfig {
                            EngineConfig = new EngineConfig {
                                DatastoreConfig = new MongoDbConfig {
                                    MongoHostnames = new[] {
                                        "127.0.0.1:27017"
                                    },
                                    MongoAuthDb = "admin",
                                    MongoCollection = "MagnumMicroservices",
                                    MongoPassword = "Password1",
                                    MongoUser = "user",
                                    UseReplicaSet = false,
                                    SslConfig = new MongoDbSslConfig {
                                        UseSsl = false
                                    }
                                },
                                QueueConfig = new RabbitQueueConfig {
                                    Hostname = "127.0.0.1",
                                    Username = "radmin",
                                    Password = "radmin"
                                }
                            },
                            UseSsl = true,
                            Port = 6883,
                            SslCertLocation = "Cert.pfx",
                            SslCertPassword = "Password",
                            UseAuth = true,
                            UseCloudWatchLogging = false
                        },
                        Formatting.Indented));
                throw new Exception("Failed to find config file. Created default.");
            }
            string configText = File.ReadAllText(this.configFile);
            this.webConfig = WebConfigHelper.FromJson(configText);
            this.webModule = new Task(() => {
                Program.Main(new[] {
                    "--config", this.configFile, "--tokens", this.tokensFile
                });
            });
            this.webModule.Start();
            Thread.Sleep(10000);

            // Read in tokens
            string tokensText = File.ReadAllText(this.tokensFile);
            string[] split = tokensText.Split('\"');
            tokens = split[1] + ":" + split[3];

            // Set up queue id
            this.testQueueId = "TESTAPPLICATION" +
                               DateTime.UtcNow.ToString("u").Replace(" ", "").Replace("-", "").Replace(":", "");

            // Store default SSL certificate validation method so we can revert back after we change it
            HttpClientHandler handler = new HttpClientHandler {
                SslProtocols = SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12,
                ServerCertificateCustomValidationCallback = (message, certificate2, arg3, arg4) => true
            };
            client = new HttpClient(handler);

            submitUri = $"https://localhost:{this.webConfig.Port}/job/submit/";
            requestUri = $"https://localhost:{this.webConfig.Port}/job/request/";
            completeUri = $"https://localhost:{this.webConfig.Port}/job/complete/";
            emptyUri = $"https://localhost:{this.webConfig.Port}/job/isempty/";
            failUri = $"https://localhost:{this.webConfig.Port}/job/fail/";
        }

        public void Dispose() {
            string text = File.ReadAllText(this.configFile);
            this.webConfig = JsonConvert.DeserializeObject<WebConfig>(text);
//            this.webModule.Dispose();
        }

        private readonly string configFile = Path.Combine(AppContext.BaseDirectory, "TestWebConfig.json");
        private readonly string tokensFile = Path.Combine(AppContext.BaseDirectory, "Tokens.json");

        private readonly string testQueueId;

        private WebConfig webConfig;
        private static string tokens;
        private ITestOutputHelper output;
        private readonly Task webModule;
        private static HttpClient client;
        private static string submitUri;
        private static string requestUri;
        private static string completeUri;
        private static string emptyUri;
        private static string failUri;

        public async Task<HttpResponseMessage> HttpPost(string uri, dynamic body) {
            // convert body to json
            string json = JsonConvert.SerializeObject(body);
            StringContent jsonContent = new StringContent(json);
            // set headers
            jsonContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
            byte[] passString = Encoding.ASCII.GetBytes(tokens);
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", Convert.ToBase64String(passString));
            // get response
            return await client.PostAsync(uri, jsonContent);
        }

        [Fact]
        public async void TestFail() {
            dynamic requestBody = new {
                JobId = "123",
                AppId = this.testQueueId,
                PreviousJobIds = new string[] {
                },
                jobHandleTimeoutSeconds = 120
            };
            // submit job
            HttpResponseMessage submitResponse = await this.HttpPost(submitUri, requestBody);
            Assert.True(submitResponse.IsSuccessStatusCode);
            // request job so it is available to be failed
            HttpResponseMessage requestResponse = await this.HttpPost(requestUri, requestBody);
            Assert.True(requestResponse.IsSuccessStatusCode);
            // fail job
            HttpResponseMessage failResponse = await this.HttpPost(failUri, requestBody);
            Assert.True(failResponse.IsSuccessStatusCode);
        }

        [Fact]
        public async void TestIsEmpty() {
            // Empty queue
            dynamic emptyBody = new {
                AppId = this.testQueueId
            };
            // check empty
            HttpResponseMessage emptyResponse = await this.HttpPost(emptyUri, emptyBody);
            Assert.True(emptyResponse.IsSuccessStatusCode);
            string responseContent = await emptyResponse.Content.ReadAsStringAsync();
            Assert.Equal("{\"empty\":true}", responseContent);

            // Add a job
            dynamic requestBody = new {
                JobId = "123",
                AppId = this.testQueueId,
                PreviousJobIds = new string[] {
                },
                jobHandleTimeoutSeconds = 120
            };
            // submit job
            HttpResponseMessage submitResponse = await this.HttpPost(submitUri, requestBody);
            Assert.True(submitResponse.IsSuccessStatusCode);
            // check empty
            emptyResponse = await this.HttpPost(emptyUri, requestBody);
            Assert.True(emptyResponse.IsSuccessStatusCode);
            responseContent = await emptyResponse.Content.ReadAsStringAsync();
            Assert.Equal("{\"empty\":false}", responseContent);

            // Remove job
            HttpResponseMessage requestResponse = await this.HttpPost(requestUri, requestBody);
            Assert.True(requestResponse.IsSuccessStatusCode);
            HttpResponseMessage completionResponse = await this.HttpPost(completeUri, requestBody);
            Assert.True(completionResponse.IsSuccessStatusCode);
            // check empty
            emptyResponse = await this.HttpPost(emptyUri, emptyBody);
            Assert.True(emptyResponse.IsSuccessStatusCode);
            responseContent = await emptyResponse.Content.ReadAsStringAsync();
            Assert.Equal("{\"empty\":true}", responseContent);
        }

        /// <summary>
        ///     Tests the typical process of submitting a job, requesting it and then completing it.
        /// </summary>
        [Fact]
        public async void TestProcess() {
            dynamic requestBody = new {
                JobId = "123",
                AppId = this.testQueueId,
                PreviousJobIds = new string[] {
                },
                jobHandleTimeoutSeconds = 120
            };
            // submit job
            HttpResponseMessage submitResponse = await this.HttpPost(submitUri, requestBody);
            Assert.True(submitResponse.IsSuccessStatusCode);
            // request job so it is available to be completed
            HttpResponseMessage requestResponse = await this.HttpPost(requestUri, requestBody);
            Assert.True(requestResponse.IsSuccessStatusCode);
            // complete job
            HttpResponseMessage completionResponse = await this.HttpPost(completeUri, requestBody);
            Assert.True(completionResponse.IsSuccessStatusCode);
        }
    }
}
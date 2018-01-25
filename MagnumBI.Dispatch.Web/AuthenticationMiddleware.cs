// 
// 0918
// 2017091812:37 PM

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Serilog;

namespace MagnumBI.Dispatch.Web {
    /// <summary>
    ///     Authenticates connections to MagnumBI Depot
    /// </summary>
    public class AuthenticationMiddleware {
        private static Dictionary<string, string> accessKeys;

        /// <summary>
        ///     The last time access tokens were loaded in.
        /// </summary>
        private static DateTime lastLoaded = DateTime.MinValue;

        private readonly RequestDelegate next;

        /// <summary>
        ///     Default constructor for AuthenticationMiddleware.
        /// </summary>
        public AuthenticationMiddleware(RequestDelegate next) {
            this.next = next;
        }

        /// <summary>
        ///     File to find the access key definitions.
        /// </summary>
        public static string AccessKeyFile { get; set; } = new FileInfo("Tokens.json").FullName;

        private void LoadAccessTokens() {
            // Check access key file
            if (!File.Exists(AccessKeyFile)) {
                FileStream fileStream = File.Create(AccessKeyFile);
                fileStream.Dispose();
                Dictionary<string, string> tempDict = new Dictionary<string, string> {
                    {
                        "Example", "Key"
                    }
                };
                File.WriteAllText(AccessKeyFile, JsonConvert.SerializeObject(tempDict, Formatting.Indented));
                lastLoaded = DateTime.MinValue;
                Log.Warning($"No key file found, creating {AccessKeyFile}");
            }

            if (accessKeys == null) {
                accessKeys = new Dictionary<string, string>();
                lastLoaded = DateTime.MinValue;
            } else if (File.GetLastWriteTime(AccessKeyFile) <= lastLoaded) {
                return;
            }

            // There is new stuff to load.
            Log.Debug($"Loading new token file.");
            accessKeys =
                JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(AccessKeyFile));
            lastLoaded = File.GetLastWriteTime(AccessKeyFile);
        }

        /// <summary>
        ///     ASP.Net default method to invoke this middle ware.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task Invoke(HttpContext context) {
            try {
                if (!Program.Config.UseAuth) {
                    await this.next.Invoke(context);
                    return;
                }

                string authHeader = context.Request.Headers["Authorization"];
                if (authHeader == null || !authHeader.StartsWith("Basic")) {
                    // Not a valid request, reject
                    context.Response.StatusCode = 401;
                } else {
                    this.LoadAccessTokens();

                    // Get credentials from request.
                    string encodedUsernamePassword = authHeader.Substring("Basic ".Length).Trim();

                    Encoding encoding = Encoding.GetEncoding("iso-8859-1");
                    string usernamePassword = encoding.GetString(Convert.FromBase64String(encodedUsernamePassword));

                    int separatorIndex = usernamePassword.IndexOf(':');

                    string accessKey = usernamePassword.Substring(0, separatorIndex);
                    string secretKey = usernamePassword.Substring(separatorIndex + 1);
                    if (accessKey == "Example") {
                        // Not a valid request, reject
                        context.Response.StatusCode = 401;
                        return;
                    }

                    if (accessKeys.ContainsKey(accessKey)) {
                        if (accessKeys[accessKey] == secretKey) {
                            context.Items["decodedUser"] = accessKey;
                            await this.next.Invoke(context);
                        } else {
                            context.Response.StatusCode = 401;
                        }
                    } else {
                        context.Response.StatusCode = 401;
                    }
                }
            } catch (Exception e) {
                Log.Error($"Failed to authenticate connection", e);
                context.Response.StatusCode = 401;
            }
        }
    }
}
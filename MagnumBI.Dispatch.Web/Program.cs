// 
// 0915
// 2017091812:37 PM

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Amazon;
using Amazon.CloudWatchLogs;
using Amazon.Runtime;
using MagnumBI.Dispatch.Engine;
using MagnumBI.Dispatch.Engine.Config;
using MagnumBI.Dispatch.Engine.Config.Datastore;
using MagnumBI.Dispatch.Engine.Config.Queue;
using MagnumBI.Dispatch.Web.Config;
using MagnumBI.Dispatch.Web.Controllers;
using MagnumBI.Dispatch.Web.Logging;
using Microsoft.AspNetCore.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.AwsCloudWatch;

namespace MagnumBI.Dispatch.Web {
    /// <summary>
    ///     The main entry point for MagnumBI Dispatch.
    /// </summary>
    [SuppressMessage("ReSharper", "ConditionIsAlwaysTrueOrFalse")]
    public class Program {
        private const string ConsoleOutputTemplate =
            "[{Level:u4}]" +
            "{DispatchMachineName}" +
            " {Timestamp:yyyy-MM-dd HH:mm:ss zz} {Message:lj}{NewLine}{Exception}";

        private const string FileOutputTemplate =
            "[{Level:u4}]" +
            "{DispatchMachineName}" +
            " { Timestamp:yyyy-MM-dd HH:mm:ss zz} {Message:lj}{NewLine}{Exception}";

        private static string configFile = new FileInfo("Config.json").FullName;
        private static readonly string LogFile = new FileInfo("MagnumMicroservices.log").FullName;

        /// <summary>
        ///     The configuration for this application.
        /// </summary>
        public static WebConfig Config;
#pragma warning disable 414
        private static bool active;
#pragma warning restore 414
#pragma warning disable 169
        private static Thread monitorThread;
#pragma warning restore 169
        /// <summary>
        ///     The static instance of MagnumBiDispatchController
        /// </summary>
        public static MagnumBiDispatchController MagnumBiDispatchController { get; private set; }

        /// <summary>
        ///     The main entry point for MagnumBI Dispatch.
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args) {
            CreateInitialConsoleLogger();
            ReadConsoleCommands(args);

            if (!File.Exists(configFile)) {
                Console.Error.WriteLine($"Not config file found, creating default. ({configFile})");
                CreateDefaultConfig();

#if DEBUG
                Console.WriteLine("Press enter to close...");
                Console.ReadLine();
#endif
                Environment.Exit(-1);
            }

            // Setup Serilog
            LoggerConfiguration logBuilder = new LoggerConfiguration().MinimumLevel.Debug()
                .MinimumLevel.Verbose()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.AspNetCore.Authentication",
                    LogEventLevel.Warning)
                .MinimumLevel.Override("System", LogEventLevel.Warning)
                .Enrich.WithDispatchMachineName()
#if DEBUG
                .WriteTo.Console(LogEventLevel.Debug, ConsoleOutputTemplate);
#else
                .WriteTo.Console(LogEventLevel.Information,consoleOutputTemplate);
#endif

            Config = WebConfigHelper.FromJson(File.ReadAllText(configFile));

            if (Config.LogToFile) {
                logBuilder.WriteTo.File(LogFile,
                    outputTemplate: FileOutputTemplate,
                    restrictedToMinimumLevel: LogEventLevel.Information,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 10);
            }

            if (Config.UseCloudWatchLogging) {
                CloudWatchSinkOptions cloudWatchOptions = new CloudWatchSinkOptions {
                    LogGroupName = "MagnumBiDispatch",
                    MinimumLogEventLevel = LogEventLevel.Warning
                };
                IAmazonCloudWatchLogs cloudWatch;
                if (string.IsNullOrWhiteSpace(Config.AwsSecretKey)) {
                    cloudWatch = new AmazonCloudWatchLogsClient(new AmazonCloudWatchLogsConfig {
                        RegionEndpoint = RegionEndpoint.GetBySystemName(Config.AwsLogRegion)
                    });
                } else {
                    cloudWatch = new AmazonCloudWatchLogsClient(
                        new BasicAWSCredentials(Config.AwsAccessKey,
                            Config.AwsSecretKey),
                        RegionEndpoint.GetBySystemName(Config.AwsLogRegion));
                }

                logBuilder.WriteTo.AmazonCloudWatch(cloudWatchOptions, cloudWatch);
            }

            // Change log level based on config.
            switch (Config.LogLevel.ToLower()) {
                case "debug":
                    logBuilder.MinimumLevel.Debug();
                    break;
                case "info":
                case "information":
                    logBuilder.MinimumLevel.Information();
                    break;
                case "error":
                    logBuilder.MinimumLevel.Error();
                    break;
                case "warn":
                    logBuilder.MinimumLevel.Warning();
                    break;
                default:
                    logBuilder.MinimumLevel.Information();
                    break;
            }

            Log.Logger = logBuilder.CreateLogger();

            SetupMagnumBiDispatch();

            active = true;

            // Check setup
            if (MagnumBiDispatchController == null || !MagnumBiDispatchController.Running) {
                Log.Error("Failed to connect to backend services. NOT RUNNING");
#if DEBUG
                Console.ReadLine();
#endif
                Environment.Exit(111);
            }

            JobMaintenanceHelper.StartJobTimeoutThread();
//            EngineHealthChecker.StartMonitorThread();
            SetupWebHost();
        }

        private static void CreateInitialConsoleLogger() {
            LoggerConfiguration logBuilder = new LoggerConfiguration().MinimumLevel.Debug()
                .MinimumLevel.Verbose()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.AspNetCore.Authentication",
                    LogEventLevel.Warning)
                .MinimumLevel.Override("System", LogEventLevel.Warning)
                .Enrich.WithDispatchMachineName()
#if DEBUG
                .WriteTo.Console(LogEventLevel.Debug, ConsoleOutputTemplate);
#else
                .WriteTo.Console(LogEventLevel.Information,consoleOutputTemplate);
#endif
            Log.Logger = logBuilder.CreateLogger();
        }

        /// <summary>
        ///     Parse console commands if they exist.
        /// </summary>
        /// <param name="args">Console commands</param>
        private static void ReadConsoleCommands(string[] args) {
            List<string> argList = args.ToList();
            // --config
            try {
                if (argList.Contains("--config")) {
                    int i = argList.IndexOf("--config");
                    if (i != -1) {
                        configFile = new FileInfo(args[i + 1]).FullName;
                    } else {
                        Log.Warning("No user-defined config file provided, using default.");
                    }
                }
            } catch (Exception) {
                Log.Warning("Error loading user-defined config file. Using default.");
            }

            // --tokens
            try {
                if (argList.Contains("--tokens")) {
                    int i = argList.IndexOf("--tokens");
                    if (i != -1) {
                        AuthenticationMiddleware.AccessKeyFile = new FileInfo(args[i + 1]).FullName;
                    } else {
                        Log.Warning("No user-defined tokens file provided, using default.");
                    }
                }
            } catch (Exception) {
                Log.Error("ERROR: Error loading user-defined tokens file. Using default.");
            }
        }

        private static void CreateDefaultConfig() {
            // Create default config
            using (TextWriter configFileStream = File.CreateText(configFile)) {
                WebConfig webConfig = new WebConfig {
                    EngineConfig = new EngineConfig {
                        DatastoreConfig = new MongoDbConfig {
                            MongoAuthDb = "admin",
                            MongoHostnames = new[] {
                                "127.0.0.1:27017"
                            },
                            UseReplicaSet = false,
                            MongoPassword = "password",
                            MongoUser = "user",
                            MongoCollection = "MagnumBIDispatch",
                            SslConfig = new MongoDbSslConfig {
                                UseSsl = true,
                                VerifySsl = false,
                                ClientCertificates = new List<KeyValuePair<string, string>>()
                            }
                        },
                        QueueConfig = new RabbitQueueConfig {
                            Hostname = "127.0.0.1",
                            Password = "password",
                            Username = "user"
                        }
                    },
                    Port = 6883,
                    UseSsl = true,
                    SslCertLocation = "Cert.pfx",
                    SslCertPassword = "EXAMPLE-PASSWORD",
                    UseCloudWatchLogging = false
                };
                configFileStream.Write(webConfig.ToJson());
            }
        }

        /// <summary>
        ///     Sets up the Dispatch engine.
        /// </summary>
        private static void SetupMagnumBiDispatch() {
            CreateAndConnectEngine();
        }

        /// <summary>
        ///     Sets up web host.
        /// </summary>
        private static void SetupWebHost() {
            if (Config.UseSsl) {
                FileInfo certFile = new FileInfo(Config.SslCertLocation);

                // Check cert exists
                if (!certFile.Exists) {
                    throw new Exception($"SSL Cert not found! {certFile.FullName}");
                }

                X509Certificate2
                    cert = new X509Certificate2(Config.SslCertLocation,
                        Config.SslCertPassword);

                IWebHost host = new WebHostBuilder().UseKestrel(options => {
                        options.Listen(IPAddress.Any,
                            Config.Port,
                            listenOptions => { listenOptions.UseHttps(cert); });
                    })
                    .UseUrls($"https://*:{Config.Port}")
                    .UseContentRoot(Directory.GetCurrentDirectory())
                    .UseIISIntegration()
                    .UseStartup<Startup>()
                    .Build();
                host.Run();
            } else {
                IWebHost host = new WebHostBuilder().UseKestrel()
                    .UseUrls($"http://*:{Config.Port}")
                    .UseContentRoot(Directory.GetCurrentDirectory())
                    .UseIISIntegration()
                    .UseStartup<Startup>()
                    .Build();
                host.Run();
            }
        }

        /// <summary>
        ///     Creates a new MagnumBiDispatchController and connects it to its backing services.
        /// </summary>
        public static void CreateAndConnectEngine() {
            try {
                MagnumBiDispatchController = new MagnumBiDispatchController(Config.EngineConfig);
                if (!MagnumBiDispatchController?.Running ?? true) {
                    // Did not connect, kill.
                    throw new Exception("Failed to connect to backing services in sufficient time.");
                }
            } catch (Exception e) {
                Log.Error("Failed to start MagnumBI Dispatch Controller", e);
                Log.Information("NOT RUNNING.");
#if DEBUG
                Console.ReadLine();
                throw;
#else
                Environment.Exit(501);
#endif
            }
        }
    }
}
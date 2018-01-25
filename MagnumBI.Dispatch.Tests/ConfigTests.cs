using System;
using System.IO;
using MagnumBI.Dispatch.Engine.Config;
using MagnumBI.Dispatch.Engine.Config.Datastore;
using MagnumBI.Dispatch.Engine.Config.Queue;
using Xunit;

namespace MagnumBI.Dispatch.Tests {
    public class ConfigTests : IDisposable {
        public ConfigTests() {
            if (!Directory.Exists("ConfigTests")) {
                Directory.CreateDirectory("ConfigTests");
            }
        }

        public void Dispose() {
        }

        [Fact]
        public void TestMongoConfigSerialization() {
            string fileName = Path.Combine("ConfigTests", "TestMongoConfigOutput.json");
            if (File.Exists(fileName)) {
                File.Delete(fileName);
            }
            EngineConfig engineConfig = new EngineConfig {
                DatastoreConfig = new MongoDbConfig {
                    MongoUser = "TestUser",
                    MongoAuthDb = "admin",
                    MongoHostnames = new[] {
                        "127.0.0.1:27017"
                    },
                    MongoPassword = "Password1",
                    MongoCollection = "TestingTable"
                },
                QueueConfig = new RabbitQueueConfig {
                    Hostname = "127.0.0.1",
                    Username = "radmin",
                    Password = "Password1",
                    Port = 5672,
                    ManagementPort = 15672
                }
            };
            File.WriteAllText(fileName, engineConfig.ToJson());

            // Check
            string fileText = File.ReadAllText(fileName);
            Assert.False(string.IsNullOrWhiteSpace(fileText));
            EngineConfig engineConfigOut = EngineConfig.FromJson(fileText);
            Assert.Equal(engineConfig.DatastoreConfig.DatastoreType, engineConfigOut.DatastoreConfig.DatastoreType);
        }

        [Fact]
        public void TestRabbitConfigSerialization() {
            string fileName = Path.Combine("ConfigTests", "TestRabbitConfigOutput.json");
            if (File.Exists(fileName)) {
                File.Delete(fileName);
            }
            EngineConfig engineConfig = new EngineConfig {
                DatastoreConfig = new MongoDbConfig {
                    MongoUser = "TestUser",
                    MongoHostnames = new[] {
                        "127.0.0.1:27017"
                    },
                    MongoAuthDb = "admin",
                    MongoPassword = "Password1",
                    MongoCollection = "TestingTable"
                },
                QueueConfig = new RabbitQueueConfig {
                    Hostname = "127.0.0.1",
                    Username = "radmin",
                    Password = "Password1",
                    Port = 5672,
                    ManagementPort = 15672
                }
            };
            File.WriteAllText(fileName, engineConfig.ToJson());

            // Check
            string fileText = File.ReadAllText(fileName);
            Assert.False(string.IsNullOrWhiteSpace(fileText));
            EngineConfig engineConfigOut = EngineConfig.FromJson(fileText);
            Assert.Equal(engineConfig.ToString(), engineConfigOut.ToString());
        }
    }
}
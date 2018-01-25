# Configuring MagnumBI Dispatch
MagnumBI Dispatch uses a json file for configuration. By default this file is called "Config.json" and found in the root directory of the Dispatch server folder. You can also provide a config file with the --config command line argument.

MagnumBI Dispatch can use a number of different backing services for its queue and datastore requirements.  
Currently Postgresql and MongoDB are supported. The connections details for these services are provided by configuration parameters in the config file.

## General Configuration Parameters
Here are the configuration parameters shared between all installs of MagnumBI Dispatch

### Port
Port is the port that the MagnumBI Dispatch will listen to HTTP(s) connections on.  
  
**Acceptable values:** a valid port number.  
**Default**: 6883.

### UseSsl
This is the configuration parameter for specifying that MagnumBI Dispatch should use HTTPs rather than HTTP.  
  
**Acceptable values:** true|false.  
**Default**: true.
**Related Options**: SslCertLocation, SslCertPassword

### SslCertLocation
Location of the certificate to serve https connections with. Should be a PEM format certificate and secured with a password.
  
**Acceptable values:** any valid certificate file path.  
**Default**: "Cert.pfx".  
**Related Options**: UseSsl, SslCertPassword  

### SslCertPassword
The password to decrypt the ssl certificate.

  
**Acceptable values:** true|false.  
**Default**: true.  
**Related Options**: UseSsl, SslCertPassword  

### UseAuth
Should the MagnumBi Dispatch server require clients to authenticate using access keys and secrets?  
Tokens are read in from Token.json or a different file passed in with "--tokens file.json"  
  
**Acceptable values:** true|false.  
**Default**: true.  

### UseCloudWatchLogging
Specifies if the MagnumBI Dispatch should log to AWS CloudWatch.  

  
**Acceptable values:** true|false.  
**Default**: true.  

### CloudWatchLogErrorsOnly
If true will only log errors to CloudWatch, false will log all levels above info.

  
**Acceptable values:** true|false.  
**Default**: true.  

### LogLevel
The level of log events that should be written to file and standard output.
Note, debug events will not be written to file.

**Acceptable values:** "error","warn","info","debug"  
**Default**: "info"

### LogToFile
If true MagnumBI Dispatch will log to a log file.  
  
**Acceptable values:** true|false.  
**Default**: true.  


## MongoDB and RabbitMQ Example Configuration File
```javascript
{
  "CloudWatchLogErrorsOnly": true,
  "EngineConfig": {
    "DatastoreConfig": {
      "$type": "MagnumBI.Dispatch.Engine.Config.Datastore.MongoDbConfig, MagnumBI.Dispatch.Engine",
      "MongoAuthDb": "admin",
      "UseReplicaSet": false,
      "ReplicaSetName": "rs0",
      "MongoCollection": "MagnumBiDispatch",
      "MongoHostnames": [
        "mongodb.example.com:27017"
      ],
      "MongoPassword": "PasswordForMongoDb",
      "MongoUser": "mongo-user-name",
      "SslConfig": {
        "ClientCertificates": [],
        "UseSsl": false,
        "VerifySsl": false
      }
    },
    "QueueConfig": {
      "$type": "MagnumBI.Dispatch.Engine.Config.Queue.RabbitQueueConfig, MagnumBI.Dispatch.Engine",
      "Hostname": "rabbitmq.example.com",
      "Password": "radmin",
      "Username": "radmin",
      "ManagementPort": 15672,
      "Port": 5672
    },
    "TimeoutSeconds": 12,
    "DatastoreType": "MongoDb",
    "QueueType": "RabbitMQ"
  },
  "Port": 6883,
  "UseAuth": true,
  "UseCloudWatchLogging": false,
  "LogLevel": "Debug",
  "LogToFile": true,
  "SslCertLocation": "Cert.pfx",
  "SslCertPassword": "EXAMPLE-PASSWORD",
  "UseSsl": true
}
```

## Postgresql and RabbitMQ Example Configuration File
```javascript
{
  "CloudWatchLogErrorsOnly": true,
  "EngineConfig": {
    "DatastoreConfig": {
    "$type": "MagnumBI.Dispatch.Engine.Config.Datastore.PostgreSqlConfig, MagnumBI.Dispatch.Engine",
    "PostgreSqlDb": "magnumbidispatch",
    "PostgreSqlHostnames": [
      "127.0.0.1:5432"
    ],
    "PostgreSqlPassword": "password",
    "PostgreSqlUser": "postgres",
    "PostgreSqlAdminDb": "postgres",
    "SslConfig": {
      "ClientCertificates": [],
      "UseSsl": false,
      "VerifySsl": false
    }
  },
    "QueueConfig": {
      "$type": "MagnumBI.Dispatch.Engine.Config.Queue.RabbitQueueConfig, MagnumBI.Dispatch.Engine",
      "Hostname": "rabbitmq.example.com",
      "Password": "radmin",
      "Username": "radmin",
      "ManagementPort": 15672,
      "Port": 5672
    },
    "TimeoutSeconds": 12,
    "DatastoreType": "MongoDb",
    "QueueType": "RabbitMQ"
  },
  "Port": 6883,
  "UseAuth": true,
  "UseCloudWatchLogging": false,
  "LogLevel": "Debug",
  "LogToFile": true,
  "SslCertLocation": "Cert.pfx",
  "SslCertPassword": "EXAMPLE-PASSWORD",
  "UseSsl": true
}
```
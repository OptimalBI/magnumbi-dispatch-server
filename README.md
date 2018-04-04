# MagnumBI Dispatch
MagnumBI Dispatch manages microservice communication and interaction simply. It is easy to develop and integrate with your small to medium sized development teams. 

## What MagnumBI Dispatch does
Think of MagnumBI Dispatch as a police call centre. It can tell you if there are jobs to be done, takes calls for jobs to be done and sends the instructions and any data needed to the cop (or microservice) that will handle the job. If there are any more steps that need to then be completed the cop will send these back to the call centre for another cop who specialises in that area to do. It is the communication centre between what needs to be done and the 'doing' which results in processed data or a completed action. 

Using MagnumBI Dispatch will allow you to integrate microservices into your systems to create a simple to update and develop ecosystem where everything can talk to everything else. Microservices hold plenty of value and MagnumBI Dispatch takes all the stress out of embracing this style of application development. 

## Application Dependencies
To run the MagnumBI Dispatch Server you require:

##### Windows:
* Windows 7 SP1
* Windows 8.1
* Windows 10, Windows 10 Anniversary Update (version 1607) or later versions
* Windows Server 2008 R2 SP1 (Full Server or Server Core)
* Windows Server 2012 SP1 (Full Server or Server Core)
* Windows Server 2012 R2 (Full Server or Server Core)
* Windows Server 2016 (Full Server, Server Core, or Nano Server)

##### Linux:
* gettext
* libcurl4-openssl-dev
* libicu-dev
* libssl-dev
* libunwind8
* zlib1g

On Ubuntu (or other similar Linux distributions) you can install with:
```bash
apt-get install gettext libcurl4-openssl-dev libicu-dev libssl-dev libunwind8 zlib1g
```

## External Dependencies
MagnumBI Dispatch requires a Datastore and Queue Engine to function. The currently supported Datastores are:
* MongoDB (3.4)
* Postgresql (10.1 or higher)

*Note for datastores*: The version number specifies the tested version of that datastore. It is likely that MagnumBI dispatch works on other versions, but testing has not taken place. If you have found a datastore version that works and is older, drop us an issue so we can update this list!  

Supported queue engines:  
* RabbitMQ  

More datastores and queues can be added easily. See the development documentation for more information.

## Running MagnumBI Dispatch Server
To run the MagnumBI Dispatch server you will need:
* The latest release from GitHub.
* A SSL certificate in .pfx format (if we are using HTTPS, self-signed will do).
* 

1. Extract the archive.
1. Run the executable (MangnumBi.Dispatch.Web or MagnumBi.Dispatch.Web.exe).
1. This will create a default configuration file (Config.json).
1. Configure MagnumBI Dispatch (see Configuring below).
1. Change the contents of Tokens.json to include some access keys.
1. Start the server again once configured.
1. Done!

#### Creating Linux Service (Systemd)
There is an example service file that can be found [here](MagnumBI.Dispatch.Web/dispatch-server.service)

1. Copy the magnumbi dispatch build to /opt/magnumbi/dispatch/ and follow the above instructions.
1. Make the file MagnumBI.Dispatch.Web executable.
1. Copy the example service file to /lib/systemd/system/dispatch-server.service.
1. Reload systemd with: ```sudo systemctl daemon-reload```
1. Start the service with: ```sudo systemctl start dispatch-server```
1. Check its running by reading /var/log/syslog and look for dispatch-server lines.
1. If you would like dispatch to run on boot run: ```sudo systemctl enable dispatch-server```

## Configuring MagnumBI Dispatch

See [Configuration.md](Configuration.md)

## Developing MagnumBI Dispatch
MagnumBI Dispatch is easy to extend. Visual Studio 2017 is recommended to develop, but any C# IDE's work including:
* JetBrain's Rider
* Visual Studio Code
* Visual Studio for macOs
* Any text editor + terminal

### Development Dependencies
To develop MagnumBI Dispatch you require:
* .Net Core 2.0+ SDK
* Git

### Development Notes
The project is split up two major component, with a few extra components to help with ease of development.

To see what each folder holds [see here](Development.md)

## Branches:
Currently,  has two Branches available:
* master and
* develop  

Master contains code which is known to be running in a production environment.  
Develop contains the latest, locally tested version of the codebase.  

To see more on this subject see [git-flow](https://www.atlassian.com/git/tutorials/comparing-workflows/gitflow-workflow)

## Current functionality: ##
* Queues that clients can retrieve jobs and data from.
* API for submitting jobs.
* Unique ID generation for jobs.
* Secure access to API (access keys)
* HTTPS Support (recommended to use!)

## API Documentation
To see what that API routes can do [click here](ApiDocumentation.md).

## Feedback, suggestions, bugs, contributions: ##
Please submit these to GitHub issue tracking or join us in developing by forking the project and then making a pull request!


# Licence
Copyright 2017 Optimal BI Ltd - Licensed under the Apache Licence 2.0

## Change log: ##
```
v1.0.0
	* Initial Build.

```

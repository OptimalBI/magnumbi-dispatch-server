This file gives a quick run down on where to find each section of MagnumBI Dispatch's code.
# Main Engine
## MagnumBI.Dispatch.Engine
This is the main area for the code that operates on jobs and interacts with Queue's and Datastore's. It also holds the configuration models for Queue's and Datastore's  
In this folder you wil find
##### Config/
This is the directory responsible for holding the configuration models for MagnumBI Dispatch's external dependencies.

##### Datastore/
This directory contains the definitions and implementations for datastores.

##### Queue/
This directory contains the definitions and implementations for queues.

###### Exceptions/
This directory contains the exceptions that might be thrown by the MagnumBI Dispatch Engine.

## MagnumBI.Dispatch.Web
This is the main directory for the RESTful API and other web services.

##### Config/
Contains the definitions for the configuration required for MagnumBI.Dispatch's web component.

##### Controllers/
Contains the controllers.

##### Models/
Contains all the model definitions for the controllers.

##### Logging/
Contains the custom logging code.

# Helper code
## OptimalBI.Collections
Contains the custom collections for MagnumBI Dispatch.

## *.Tests
Contains the tests where * is the project that the tests are for.

## OptimalBI.Logger (Deprecated)
Contains the project logging code.


To see how to developer certain areas of MagnumBI Dispatch see the wiki.
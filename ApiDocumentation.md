# API Documentation
The MagnumBI Dispatch serer has a number of API functions that form the core functionality of the MagnumBI dispatch system.

All API Requests require basic authentication parameters at this time.

## GET /job/
Gets the current status of the MagnumBI Dispatch Server (200 is OK)

**Request Parameters**
None   


## POST /job/complete/
The id of the queue to complete a job against.

#####Request Parameters
######Required
* queueId=[string] ID of the queue.
* jobId=[string] ID of the job we are completing.

## 

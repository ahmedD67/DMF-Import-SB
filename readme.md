<!-- Headings -->
# D365 DMF Import with Service Bus
This document serves as a guide to the function app designed as well as the entire import process. The overall main steps are as follows:

1. Create app registration in Vihc
2. Enter app registration in D365
3. Create a blob container within a storage account
4. Create a service bus with a topic and subscriber
5. Create a function app with two functions

Now that the steps are laid out, the import flow using the **Data management package REST API** should be understood.

## Import APIs
To import a data entity into D365, an import project is created in Data Management. Go to *workspaces*, then choose *Data Management*, and press the *Import* tile.

Click on *New*, select the correct file format, choose the relevant data entity, and then provide a blank file containing only the headers of the field you will be entering.

Now, new files of the same format could be imported into Dynamics by calling two import REST APIs.

### GetAzureWritableUrl
This API is used to get a writable blob URL which is used to drop the file into.
```
POST /data/DataManagementDefinitionGroups/Microsoft.Dynamics.DataEntities.GetAzureWriteUrl  
BODY
{
    "uniqueFileName":"<string>"
}
```

The payload to this call is:
|Parameter|Type|Description|
|---------|----|-----------|
|uniqueFileName|string|A unique name to track the blob. Can be a GUID|

It returns a nested json object containing the blob SAS URL.

### ImportFromPackage
Once the file has been successfully uploaded to the URL, an API call needs to be made to initiate the import process into the relevant project.
```
POST /data/DataManagementDefinitionGroups/Microsoft.Dynamics.DataEntities.ImportFromPackage 
BODY
{
    "packageUrl":"<string>",
    "definitionGroupId":"<string>",
    "executionId":"<string>",
    "execute":<bool>,
    "overwrite":<bool>,
    "legalEntityId":"<string>"
}
```
The payload is described as follows:
|Parameter|Type|Description|
|---------|----|-----------|
|packageUrl|string|The blob url provided by the previous request|
|definitionGroupId|string|Despite saying GroupId, it's the name of the import project.|
|executionId|string|Unique ID to track the job, can be same as filename or GUID|
|execute|bool|Set to **True** to do both staging and target, otherwise just staging.|
|overwrite|bool|Does something. Not sure yet.|
|legalEntityId|string|Company or Legal Entity you're in|


## Function App
The function app does the heavy lifting in this project. There are two functions in this app: 
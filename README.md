# Azure.BlobArchiver

dotnet tool that uploads your local/network files to Azure Blob Storage.

NOTE: WIP

# Environment variables needed

| Variable name                            | Example value                                                                                                                                                                                  | Description                                                                                                            |
| ---------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------- |
| UPLOAD_THREADS                           | `100`                                                                                                                                                                                          | Number of threads to upload files in.                                                                                  |
| DELETE_ON_UPLOAD                         | `true`/`false`                                                                                                                                                                                 | Delete files on local disk when finished uploading                                                                     |
| BLOB_TIER                                | `Hot`/`Cool`/`Archive`                                                                                                                                                                         | Set the accesstier on the uploaded blob.                                                                               |
| GRACE_PERIOD_BEFORE_UPLOADING_IN_SECONDS | `15`                                                                                                                                                                                           | Wait number of seconds before taking files into the upload list (use this if you actively write to this local folder). |
| EXCLUDE_FILES                            | `/myfolder/myfile.ext,/myfolder/somefile.txt`                                                                                                                                                  | The files to exclude from uploading.                                                                                   |
| BASE_DIR                                 | `/data`                                                                                                                                                                                        | OPTIONAL: Path to the folder to upload. This defaults to `/data`                                                       |
| BLOB_STORAGE_CONNECTIONSTRING            | `DefaultEndpointsProtocol=https;AccountName=myaccountname;AccountKey=W1x29dv6UjOQQ838BBk/9GaaV5Tlv/ITmuXko7Rp5UNHB7y03foBy0t31wdgw6FOWGX41cg4Y4C0eAevYFP/gQ==;EndpointSuffix=core.windows.net` | Connectionstring to the Azure Blob Storage account to upload to.                                                       |
| BLOB_STORAGE_CONTAINERNAME               | `mydata`                                                                                                                                                                                       | Name of the container within the Azure Blob Storage account to upload to..                                             |

# How to use

1. Make sure you filled all the above environment variables.
2. The data to be uploaded should be mounted at /data inside the container.

## Example

```
docker run --name=myuploader -e UPLOAD_CRON_EXPRESSION='*/2 * * * *' -e UPLOAD_THREADS=100 -e DELETE_ON_UPLOAD=true -e BLOB_TIER=Hot -e BLOB_TIER="Archive" -e GRACE_PERIOD_BEFORE_UPLOADING_IN_SECONDS=15 -e EXCLUDE_FILES="/data/somefile.txt,/data/anotherfile.ext" -e BLOB_STORAGE_CONNECTIONSTRING="DefaultEndpointsProtocol=https;AccountName=myaccountname;AccountKey=W1x29dv6UjOQQ838BBk/9GaaV5Tlv/ITmuXko7Rp5UNHB7y03foBy0t31wdgw6FOWGX41cg4Y4C0eAevYFP/gQ==;EndpointSuffix=core.windows.net" -e BLOB_STORAGE_CONTAINERNAME=mydata -v /some/data/path/on/host:/data -d robhofmann/azureblobarchiver
```

## Build it yourself

In the root of this repository:

```
docker build -f Docker/Dockerfile -t yourimagename .
```

# Sleeper Queue

## Build
```
dotnet publish -r linux-x64 --self-contained=true -c release
cd bin/Release/net5.0/linux-x64/publish && zip sleeper.zip * && cd -
```

## Run 

To Produce messages
```
export AZURE_STORAGE_CONNECTION_STRING=...
dotnet run produce 2 2
```

To Consume messages:
```
export AZURE_STORAGE_CONNECTION_STRING=...
dotnet run consume
```
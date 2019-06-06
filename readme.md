## Storage Server
Our storage server was written in C# using ASP.NET Web-API 2. Storage gives two
different API for both a clients and the naming server

## Naming Server
Naming server was developed using C#. It provides to client and storage server API
functions to perform operations on file system. To built REST API on C# naming
server Nancy framework was used. Naming server also uses SQL database to store
file system information, users and storage servers. In our case it is SQLite, since
only naming server have connection to database and we don’t need dedicated
database server.

## Client
The client is a browser application developed by using Angular 7. It is responsible for
making the distributed nature of the system transparent to the user. When a client
wishes to access a file, it first contacts the naming server to obtain information about
the storage server hosting it. After that, it communicates directly with the storage
server to complete the operation.

## How to launch our system.
Client. Accessible through ​ http://localhost:4200
```
docker pull demmyart/dfs_client
docker run -i -t -p 4200:4200 demmyart/dfs_clientNaming server
```

Running docker container:
```
docker pull brain2998/ns
docker run -itd -p 33033:33033 brain2998/ns
```
After that you can query simple request to see on which ip naming server is started
In this case it is 172.17.0.2.

Storage server
To launch storage server you should define port and address of naming server
through environmental variables.
```
docker pull alexey1122/storageserver:latest
sudo docker run -e SERVER_PORT='9002' -p 9002:9002 -e
NAMING_ADDRESS='172.17.0.3:33033' alexey1122/storageserver
```

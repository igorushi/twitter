### Initialization steps
1) Alter *appsettings.json* fill:
 - DB connection string 
 - twitter tokens ans secrets
2) restore packages by `dotnet restore`
3) create database using migration `dotnet ef database update`
4) run by `dotnet run`

The the api can be queried using `curl -k "https://localhost:5002/api/tweets?latitude=-22.7824&longtitude=-43.4090&radiusMeter=40000000"`
this querry will bring you all tweets from point defined by lat:-22.7824 lon:-43.4090 at radius of 40`000km

# Purdue.io

### An open API for Purdue University's course catalog and scheduling system.

Purdue.io was started in 2015 as a CS Senior Design project with the goal of improving
access to Purdue's course catalog data. It is still maintained and functional today, 
years later!

# Querying

## Public API Instance

There is a public instance of the Purdue.io API available at [api.purdue.io](https://api.purdue.io)

## OData Queries

Purdue.io allows you to construct OData queries that you can run via RESTful HTTP calls to query
for course catalog information. For example, this URL

`http://api.purdue.io/odata/Course?$filter=contains(Title, 'Algebra')`

will return this:

```json
{
    "@odata.context": "http://api.purdue.io/odata/$metadata#Course",
    "value": [
        {
            "Id": "893b204e-616d-42bb-bf7b-49689878bdac",
            "Number": "51500",
            "SubjectId": "7c0ec82f-fe5d-466a-8816-db7d85a79ee8",
            "Title": "Numerical Linear Algebra",
            "CreditHours": 3,
            "Description": "Students registering for this course must contact the Engineering Professional Education office – http://proed.purdue.edu"
        },
        {
            "Id": "ea42d0c8-f3f0-467c-9202-5ab2ed72e765",
            "Number": "50300",
            "SubjectId": "939c74b7-d2d5-4a4c-958e-c4d008c256b6",
            "Title": "Abstract Algebra",
            "CreditHours": 3,
            "Description": ""
        },
        [...]
```

## What kind of queries can I run?

Check out the [wiki](https://github.com/Purdue-io/PurdueApi/wiki/)!
You can run the [sample queries](https://github.com/Purdue-io/PurdueApi/wiki/OData-Queries#example-queries)
there through the query tester at [http://api.purdue.io/](api.purdue.io/).

# Building and Running

## Tools

Purdue.io is written in C# on .NET 5. It will run natively on most major
architectures and operating systems (Windows, Linux, Mac OS).

Entity Framework is used to communicate with an underlying database provider. Currently,
Purdue.io supports PostgreSQL and SQLite, but
[additional providers](https://docs.microsoft.com/en-us/ef/core/providers/)
could be added with minimal effort.

To start developing locally, install the .NET SDK.

[Install .NET SDK](https://dotnet.microsoft.com/download)

## CatalogSync

CatalogSync is the process used to pull course data from MyPurdue and synchronize it to a
relational database store.

In order to access detailed course section information, CatalogSync requires a valid
MyPurdue username and password.

CatalogSync also accepts options to configure which database provider and connection it uses.

Additional flags are available to configure CatalogSync behavior. 
Use the `--help` flag for more information.

```sh
cd src/CatalogSync

# To sync to default SQLite file purdueio.sqlite
dotnet run -- -u USERNAME -p PASSWORD

# To sync to a specific SQLite file
dotnet run -- -u USERNAME -p PASSWORD -d Sqlite -c "Data Source=path/to/file.sqlite"
```

CatalogSync will begin synchronizing course catalog data to `purdueio.sqlite`.

To sync to another database provider, use the `-d` and `-c` options to specify a database provider
and connection string:

```sh
# To sync to a local PostgreSQL instance:
dotnet run -- -u USERNAME -p PASSWORD -d Npgsql -c "Host=localhost;Database=purdueio;Username=purdueio;Password=purdueio"
```

## API

The API project contains the ASP web service used to provide the OData API.

To start the API, update `appsettings.json` with the database provider and connection string
used with CatalogSync, and `dotnet run`.

The web service will be available by default at `http://localhost:5000`.

# Contributing

See the [contributing](https://github.com/Purdue-io/PurdueApi/wiki/Contributing) wiki page!

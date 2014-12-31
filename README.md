# Purdue.io
### An open API for Purdue University's course catalog and scheduling system.
Purdue.io is an open source senior design project by Purdue Computer Science students.
The goal of this project is to make Purdue's course catalog easily accessible
to any developer, and by doing so, providing a better course registration / browsing
experience for Purdue students.

###This project contains the Purdue.io API only. Front-end applications can be found in other repositories.

##Pull Requests
Pull requests will be accepted / merged on the dev branch only. 

##Build Status
<table>
	<tr>
		<td>master</td>
		<td><a href="https://api.purdue.io/">https://api.purdue.io/</a></td>
		<td><a href="https://ci.appveyor.com/project/haydenmc/purdueapi-375"><img src="https://ci.appveyor.com/api/projects/status/x7kniwt04y9gw4ad?svg=true" alt="Build Status" /></a></td>
	</tr>
	<tr>
		<td>dev</td>
		<td><a href="https://api-dev.purdue.io/">https://api-dev.purdue.io/</a></td>
		<td><a href="https://ci.appveyor.com/project/haydenmc/purdueapi"><img src="https://ci.appveyor.com/api/projects/status/xw580mgx67375c8v?svg=true" alt="Build Status" /></a></td>
	</tr>
</table>

##Developers: Getting Started
1. Install Visual Studio 2013 Update 4
2. Install [Visual Studio Web Essentials](http://vswebessentials.com/download) (for LESS and TypeScript compilation / tooling)
3. Clone the repository by selecting "clone" in the "Local Git Repositories" section of Team Explorer and pasting the clone URL
4. Switch to the dev branch by selecting "New Branch" from the Branches section of team explorer and selecting "origin/dev" from the drop-down menu.
5. Select the Home button in Team Explorer and open the Purdue.io API Visual Studio Solution.
6. Run!

##Layout
This repository contains 4 separate projects that make up the Purdue.io API.

###A Purdue.io API
This project is the ASP.Net Web API project that hosts the actual API. This 
includes exposing the database via OData, and exposing RESTful API methods.

###CatalogApi
This is a class library of methods that provide access to raw myPurdue data.
`CatalogApi` impersonates a myPurdue user, and uses authenticated HTTPS requests
to scrape data from myPurdue pages.

###CatalogSync
This is a console application that's responsible for scraping information from myPurdue
via the `CatalogApi` project and storing it in the SQL database. This application is run
regularly as an Azure Webjob to keep the database up to date with the latest myPurdue 
catalog information.

###PurdueIoDb
This is a class library that provides access to the SQL database via entity framework.
`CatalogSync` and `A Purdue.io API` utilize this library for database access.

###Test Projects
These contain unit tests for various parts of the API. Automated builds will fail
if these tests don't pass. You're encouraged to write new tests for added functionality,
or improve tests for existing features.
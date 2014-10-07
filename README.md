# Purdue.io
### An open API for Purdue University's course catalog and scheduling system.

##Important Places
* [https://purdue.io](http://purdue.io) - landing page
* [https://api-dev.purdue.io](https://api-dev.purdue.io) - development API
* [https://api.purdue.io](https://api.purdue.io) - production API
* [https://hayden.visualstudio.com/DefaultCollection/Purdue.io](https://hayden.visualstudio.com/DefaultCollection/Purdue.io) - Visual Studio Online

##Getting Started
1. Install Visual Studio 2013
2. Install [Visual Studio Web Essentials](http://vswebessentials.com/download)
3. Visit the [VSO Project Page](https://hayden.visualstudio.com/DefaultCollection/Purdue.io) and select "Open in Visual Studio"
4. Clone the repository using the "Unsynced Commits" section of Team Explorer
5. Switch to the dev branch by selecting "New Branch" from the Branches section of team explorer and selecting "origin/dev" from the drop-down menu.
6. Select the Home button in Team Explorer and open the Purdue.io API Visual Studio Solution.
7. Run!

##Layout
The `Controllers` folder contains API controllers used to handle REST endpoints.

The `Models` folder contains C# classes that represent database entities.
You should not change these unless you understand how database migrations work.
Each `Model` has a `ToViewModel()` method that outputs an appropriate viewmodel.
This should always be used when returning data to a client through a controller.

The `Purdue.io API.Tests` project contains unit test classes.
You should **make sure these pass before committing any code to the repository**.
You should also write new tests for any new code you have added.
See the existing test methods and classes to get an idea for how to write your own unit tests.
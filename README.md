# Purdue.io
### An open API for Purdue University's course catalog and scheduling system.
Purdue.io is an open source senior design project by Purdue Computer Science students.
The goal of this project is to make Purdue's course catalog easily accessible
to any developer, and by doing so, providing a better course registration / browsing
experience for Purdue students.

### This project contains the Purdue.io API only. Front-end applications can be found in other repositories.

## How do I use it?

Purdue.io allows you to construct OData queries that you can run via RESTful HTTP calls to query
for course catalog information. For example, this URL

`http://api.purdue.io/odata/Courses?$filter=contains(Title, 'Algebra')`

will return this:

	{
		"@odata.context":"http://api-dev.purdue.io/odata/$metadata#Courses","value":[
		{
		"CourseId":"0c3ace0d-8317-466e-aad8-0e47a027a8a3","Number":"15300","Title":"Algebra And Trigonometry I","CreditHours":3.0,"Description":"Supplemental Instruction (SI) study sessions are available for students in this course. Evening Exams Required."
		},{
		"CourseId":"69bb933f-95bb-4cf4-9b72-2b9c9f0d296f","Number":"15400","Title":"Algebra And Trigonometry II","CreditHours":3.0,"Description":"Evening Exams Required."
		},{
		"CourseId":"5fdfd551-ac9a-485b-991b-3e60bb0b0fae","Number":"26200","Title":"Linear Algebra And Differential Equations","CreditHours":4.0,"Description":""
		},{
		"CourseId":"e5111623-39a0-4c1a-9553-432493e3f3c4","Number":"45300","Title":"Elements Of Algebra I","CreditHours":3.0,"Description":""
		}, ...

## What kind of queries can I run?

Check out the [wiki](https://github.com/Purdue-io/PurdueApi/wiki/)!
You can run the [sample queries](https://github.com/Purdue-io/PurdueApi/wiki/OData-Queries#example-queries)
there through the query tester at [http://api.purdue.io/](http://api.purdue.io/).

## Pull Requests

Pull requests will be accepted / merged on the dev branch only. 

## Build Status

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

# Contributing

See the [contributing](https://github.com/Purdue-io/PurdueApi/wiki/Contributing) wiki page!

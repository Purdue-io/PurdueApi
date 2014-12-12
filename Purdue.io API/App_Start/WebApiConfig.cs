using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web.Http;
using Microsoft.Owin.Security.OAuth;
using Newtonsoft.Json.Serialization;
using System.Web.OData.Builder;
using System.Web.OData.Extensions;
using PurdueIoDb.Catalog;
using System.Web.OData.Batch;
using System.Web.Http.Cors;

namespace PurdueIo
{
	public static class WebApiConfig
	{
		public static void Register(HttpConfiguration config)
		{
			// Web API configuration and services
			// Configure Web API to use only bearer token authentication.
			config.SuppressDefaultHostAuthentication();
			config.Filters.Add(new HostAuthenticationFilter(OAuthDefaults.AuthenticationType));

			// Use camel case for JSON data.
			config.Formatters.JsonFormatter.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();

			// Web API routes
			config.MapHttpAttributeRoutes();

			// Enable CORS
			config.EnableCors();

			config.Routes.MapHttpRoute(
				name: "DefaultApi",
				routeTemplate: "api/{controller}/{id}",
				defaults: new { id = RouteParameter.Optional }
			);

			config.AddODataQueryFilter();
			config.MapODataServiceRoute("odata", "odata", model: GetModel());
		}

		public static Microsoft.OData.Edm.IEdmModel GetModel()
		{
			//OData
			ODataConventionModelBuilder builder = new ODataConventionModelBuilder();

			EntitySetConfiguration<Course>	courseEnt	= builder.EntitySet<Course>("Courses");
			EntitySetConfiguration<Class>	classEnt	= builder.EntitySet<Class>("Classes");
			EntitySetConfiguration<Section> sectionEnt	= builder.EntitySet<Section>("Sections");
			builder.EntitySet<Term>("Terms");
			builder.EntitySet<Campus>("Campuses");
			builder.EntitySet<Building>("Buildings");
			builder.EntitySet<Room>("Rooms");
			builder.EntitySet<Instructor>("Instructors");
			builder.EntitySet<Meeting>("Meetings");
			builder.EntitySet<Subject>("Subjects");

			//Course Functions
			FunctionConfiguration courseByTermFunc;
			courseByTermFunc = courseEnt.EntityType.Collection.Function("ByTerm");
			courseByTermFunc.Parameter<String>("Term");
			courseByTermFunc.ReturnsCollectionFromEntitySet<Course>("Courses");

			FunctionConfiguration courseByNumberFunc;
			courseByNumberFunc = courseEnt.EntityType.Collection.Function("ByNumber");
			courseByNumberFunc.Parameter<String>("Number");
			courseByNumberFunc.ReturnsCollectionFromEntitySet<Course>("Courses");

			FunctionConfiguration courseByTermAndNumberFunc;
			courseByTermAndNumberFunc = courseEnt.EntityType.Collection.Function("ByTermAndNumber");
			courseByTermAndNumberFunc.Parameter<String>("Term");
			courseByTermAndNumberFunc.Parameter<String>("Number");
			courseByTermAndNumberFunc.ReturnsCollectionFromEntitySet<Course>("Courses");

			//Class Functions
			FunctionConfiguration classByTermFunc;
			classByTermFunc = classEnt.EntityType.Collection.Function("ByTerm");
			classByTermFunc.Parameter<String>("Term");
			classByTermFunc.ReturnsCollectionFromEntitySet<Class>("Classes");

			FunctionConfiguration classByNumberFunc;
			classByNumberFunc = classEnt.EntityType.Collection.Function("ByNumber");
			classByNumberFunc.Parameter<String>("Number");
			classByNumberFunc.ReturnsCollectionFromEntitySet<Class>("Classes");

			FunctionConfiguration classByTermAndNumberFunc;
			classByTermAndNumberFunc = classEnt.EntityType.Collection.Function("ByTermAndNumber");
			classByTermAndNumberFunc.Parameter<String>("Term");
			classByTermAndNumberFunc.Parameter<String>("Number");
			classByTermAndNumberFunc.ReturnsCollectionFromEntitySet<Class>("Classes");

			//Section Functions
			FunctionConfiguration sectionByTermFunc;
			sectionByTermFunc = sectionEnt.EntityType.Collection.Function("ByTerm");
			sectionByTermFunc.Parameter<String>("Term");
			sectionByTermFunc.ReturnsCollectionFromEntitySet<Section>("Sections");

			FunctionConfiguration sectionByNumberFunc;
			sectionByNumberFunc = sectionEnt.EntityType.Collection.Function("ByNumber");
			sectionByNumberFunc.Parameter<String>("Number");
			sectionByNumberFunc.ReturnsCollectionFromEntitySet<Section>("Sections");

			FunctionConfiguration sectionByTermAndNumberFunc;
			sectionByTermAndNumberFunc = sectionEnt.EntityType.Collection.Function("ByTermAndNumber");
			sectionByTermAndNumberFunc.Parameter<String>("Term");
			sectionByTermAndNumberFunc.Parameter<String>("Number");
			sectionByTermAndNumberFunc.ReturnsCollectionFromEntitySet<Section>("Sections");

			return builder.GetEdmModel();
		}
	}
}

using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using PurdueIo.Database.Models;

namespace PurdueIo.Api
{
    public static class EdmModelBuilder
    {
        public const int MAX_EXPAND_DEPTH = 5;

        private const int MAX_RESULTS = 10000;

        public static IEdmModel GetEdmModel()
        {
            var builder = new ODataConventionModelBuilder();

            builder.GenerateEntitySet<Campus>("Campuses");
            builder.GenerateEntitySet<Building>("Buildings");
            builder.GenerateEntitySet<Room>("Rooms");
            builder.GenerateEntitySet<Term>("Terms");
            builder.GenerateEntitySet<Course>("Courses");
            builder.GenerateEntitySet<Class>("Classes");
            builder.GenerateEntitySet<Section>("Sections");
            builder.GenerateEntitySet<Meeting>("Meetings");
            builder.GenerateEntitySet<Subject>("Subjects");
            builder.GenerateEntitySet<Instructor>("Instructors");

            return builder.GetEdmModel();
        }


        private static StructuralTypeConfiguration<T> GenerateEntitySet<T>(
            this ODataConventionModelBuilder builder, string name) where T : class
        {
            return builder.EntitySet<T>(name).EntityType
                .Filter()
                .Count()
                .Expand(MAX_EXPAND_DEPTH)
                .OrderBy()
                // .Page(MAX_RESULTS, MAX_RESULTS) // Adding pagination introduces performance
                                                   // problems on expand queries.
                                                   // https://github.com/OData/AspNetCoreOData/issues/1041
                .Select();
        }
    }
}

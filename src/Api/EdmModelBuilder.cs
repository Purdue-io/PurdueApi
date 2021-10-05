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

            builder.GenerateEntitySet<Campus>("Campus");
            builder.GenerateEntitySet<Building>("Building");
            builder.GenerateEntitySet<Room>("Room");
            builder.GenerateEntitySet<Term>("Term");
            builder.GenerateEntitySet<Course>("Course");
            builder.GenerateEntitySet<Class>("Class");
            builder.GenerateEntitySet<Section>("Section");
            builder.GenerateEntitySet<Meeting>("Meeting");
            builder.GenerateEntitySet<Subject>("Subject");
            builder.GenerateEntitySet<Instructor>("Instructor");

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
                .Page(MAX_RESULTS, MAX_RESULTS)
                .Select();
        }
    }
}
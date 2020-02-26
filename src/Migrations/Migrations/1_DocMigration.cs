using CSharpMongoMigrations;
using MongoDB.Bson;

namespace Migrations.Migrations
{
    [Migration(1, "Document migration")]
    public class DocMigration : DocumentMigration
    {
        protected override string CollectionName => "ContinentCodes";

        protected override void UpgradeDocument(BsonDocument document)
        {
            document.AddProperty("IsActive", true);
        }

        protected override void DowngradeDocument(BsonDocument document)
        {
            document.RemoveProperty("IsActive");
        }
    }
}
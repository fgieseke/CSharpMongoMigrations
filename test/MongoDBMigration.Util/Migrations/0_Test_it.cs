using CSharpMongoMigrations;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Migrations.Migrations
{
    [Migration(0, "Test it")]
    public class Test_it : Migration
    {
        protected string CollectionName => "user";

        public override void Up()
        {
            var collection = GetCollection("ContinentCodes");
            var document = new BsonDocument();

            document.AddProperty("code", 999);
            document.AddProperty("value", "AT");
            document.AddProperty("display", "Atlantis");
            collection.InsertOne(document);

        }
        public override void Down() 
        {
            var collection = GetCollection("ContinentCodes");
            var idFilter = Builders<BsonDocument>.Filter.Eq("code", 999);
            collection.DeleteOne(idFilter);
        }
    }
}

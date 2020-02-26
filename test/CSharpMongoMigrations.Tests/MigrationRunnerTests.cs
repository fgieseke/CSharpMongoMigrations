using MongoDB.Driver;
using System;
using Xunit;

namespace CSharpMongoMigrations.Tests
{
    public class MigrationRunnerTests
    {
        [Fact]
        public void ShouldNotCreateMigrationRunnerWithNullUrl()
        {
            Assert.Throws<NullReferenceException>(() =>
            {
                var runner = new MigrationRunner(null, typeof(MigrationFacts).Assembly, null);
            });
        }

        [Fact]
        public void ShouldNotCreateMigrationRunnerWithInvalidConnectionString()
        {
            Assert.Throws<MongoConfigurationException>(() =>
            {
                var runner = new MigrationRunner("test", typeof(MigrationFacts).Assembly, null);
            });
        }

        [Fact]
        public void ShouldCreateMigrationRunnerWithValidUrl()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                var runner = new MigrationRunner("mongodb://valid", typeof(MigrationFacts).Assembly, null);
            });
        }
    }
}

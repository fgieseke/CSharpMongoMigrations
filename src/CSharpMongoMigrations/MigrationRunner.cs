using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace CSharpMongoMigrations
{
    /// <summary>
    /// The migration runner, that's responsible for running migrations from the user-specified assembly.
    /// </summary>
    public sealed class MigrationRunner
    {
        private readonly ILogger _logger;
        private readonly IDatabaseMigrations _dbMigrations;
        private readonly IMigrationLocator _locator;

        /// <summary>
        /// Creates a new instance of <seealso cref="MigrationRunner" />
        /// </summary>
        /// <param name="connectionString">The Mongo connection string in form of mongodb://host:port/database</param>
        /// <param name="migrationAssembly">The target assembly containing migrations.</param>
        /// <param name="logger">Logger for logging</param>
        public MigrationRunner(string connectionString, Assembly migrationAssembly, ILogger logger = null)
        {
            var url = MongoUrl.Create(connectionString);
            _dbMigrations = new DatabaseMigrations(url);
            _locator = new MigrationLocator(migrationAssembly, _dbMigrations.GetDatabase(), new MigrationFactory());
            _logger = logger;
        }

        /// <summary>
        /// Apply all migrations before specified version.
        /// Use -1 as a version parameter to apply all existing migrations.
        /// </summary>
        /// <param name="version">The desired migration version.</param>
        public void Up(long version = -1)
        {
            Up(null, version);
        }

        /// <summary>
        /// Apply all migrations before specified version for target collection.
        /// Use -1 as a version parameter to apply all existing migrations.
        /// </summary>
        /// <param name="collection">The target collection name.</param>
        /// <param name="version">The desired migration version.</param>
        public void Up(string collection, long version = -1)
        {
            version = version == -1 ? long.MaxValue : version;

            //_logger.LogDebug($"Discovering migrations in {_locator.LocatedAssembly.FullName}");

            var appliedMigrations = _dbMigrations.GetAppliedMigrations(collection);
            var inapplicableMigrations =
                _locator.GetMigrations(MigrationVersion.Min(collection), new MigrationVersion(collection, version))
                    .Where(m => appliedMigrations.All(x => x.Version != m.Version.Version || !string.Equals(x.Collection, m.Version.Collection)))
                    .OrderBy(x => x.Version.Collection)
                    .ThenBy(x => x.Version.Version)
                    .ToList();

            _logger?.LogInformation($"Found ({inapplicableMigrations.Count}) migrations in {_locator.LocatedAssembly.FullName}");

            foreach (var migration in inapplicableMigrations)
            {
                try
                {
                    migration.Up();
                    _dbMigrations.ApplyMigration(migration.Version);
                    _logger?.LogInformation($"Applied migration {migration.Version.Version} [{migration.Version.Description}].");
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, $"Could not apply migration {migration.Version.Version} [{migration.Version.Description}]!");
                    break;
                }
            }
        }

        /// <summary>
        /// Roll back all migrations after specified version.
        /// Use -1 as a version parameter to downgrade all existing migrations.
        /// </summary>
        /// <param name="version">The desired migration version.</param>
        public void Down(long version = -1)
        {
            Down(null, version);
        }

        /// <summary>
        /// Roll back all collection migrations after specified version.
        /// Use -1 as a version parameter to downgrade all existing migrations.
        /// </summary>
        /// <param name="collection">The target collection name.</param>
        /// <param name="version">The desired migration version.</param>
        public void Down(string collection, long version = -1)
        {
            //Console.WriteLine($"Discovering migrations in {_locator.LocatedAssembly.FullName}");

            var appliedMigrations = _dbMigrations.GetAppliedMigrations(collection);
            var downgradedMigrations =
                _locator.GetMigrations(new MigrationVersion(collection, version), MigrationVersion.Max(collection))
                    .Where(m => appliedMigrations.Any(x => x.Version == m.Version.Version && string.Equals(x.Collection, m.Version.Collection)))
                    .OrderByDescending(x => x.Version.Collection)
                    .ThenByDescending(x => x.Version.Version)
                    .ToList();

            _logger?.LogInformation($"Found ({downgradedMigrations.Count}) migrations in {_locator.LocatedAssembly.FullName}");

            foreach (var migration in downgradedMigrations)
            {
                try
                {
                    migration.Down();
                    _dbMigrations.CancelMigration(migration.Version);
                    _logger?.LogInformation($"Applied down migration: {migration.Version.Version}  [{migration.Version.Description}].");
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, $"Could not apply down migration {migration.Version.Version} [{migration.Version.Description}]!");
                    break;
                }
            }
        }
    }
}
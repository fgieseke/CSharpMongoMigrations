using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using CSharpMongoMigrations;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Dotnet.MongoDB
{
    class Program
    {
        private static string Usings => "using CSharpMongoMigrations;" + Environment.NewLine +
                                        "using MongoDB.Bson;" + Environment.NewLine +
                                        "using MongoDB.Driver; " + Environment.NewLine;

        private static string Namespace => $"namespace {new DirectoryInfo(Environment.CurrentDirectory).Name}.Migrations"
                                           + Environment.NewLine;

        private const string MigrationFolder = "Migrations";

        private static ILoggerFactory _loggerFactory;

        private static void Main(string[] args)
        {
            var app = new CommandLineApplication();

            var host = new HostBuilder()
                .ConfigureLogging((hostingContext, logging) => {
                    logging.AddConsole();
                })
                .Build();

            _loggerFactory = (ILoggerFactory)host.Services.GetService(typeof(ILoggerFactory));

            app.Name = "MongoDBMigration";
            app.Description = "Manages migrations for MongoDB";
            app.Command("migration", AddMigration);

            app.HelpOption("-?|-h|--help");

            app.OnExecute(() =>
                {
                    try
                    {


                        return Task.FromResult(0);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error: " + ex.Message + "\n" + ex.StackTrace);
                        return Task.FromResult(1);
                    }
                });

            app.Execute(args);
        }

        private static void AddMigration(CommandLineApplication command)
        {
            command.Description = "Adds a new migration";
            command.HelpOption("-?|-h|--help");

            var actionArg = command.Argument("action",
                "Type of the migration [seed|doc|schema]");

            var titleOption = command.Option("-t|--title",
                "Title of the migration", CommandOptionType.SingleValue);

            var collectionOption = command.Option("-c|--collection",
                "Name of the collection to migrate", CommandOptionType.SingleValue);

            var migrationsDir = CheckMigrationsDir();

            var migrationCount = GetMaxMigrationCount(migrationsDir);


            command.Command("up", cmd =>
            {
                cmd.Description = "Runs migrations";
                cmd.HelpOption("-?|-h|--help");

                var connectionStringOption = cmd.Option("-c|--connectionString",
                    "ConnectionString to the MongoDb", CommandOptionType.SingleValue);

                var migrationMaxOption = cmd.Option("-m|--migrationNr",
                    "Number of the highest migration to invoke", CommandOptionType.SingleValue);

                var assemblyOption = cmd.Option("-a|--assemblyName",
                    "Namepattern to discover the assembly with migrations", CommandOptionType.SingleValue);

                cmd.OnExecute(() =>
                {
                    long max;
                    var maxMigration = long.TryParse(migrationMaxOption.Value(), out max) ? max : -1;
                    MigrationUp(assemblyOption.Value(), connectionStringOption.Value(), maxMigration);
                    return 0;
                });

            });

            command.Command("down", cmd =>
            {
                cmd.Description = "Revokes migrations";
                cmd.HelpOption("-?|-h|--help");

                var connectionStringOption = cmd.Option("-c|--connectionString",
                    "ConnectionString to the MongoDb", CommandOptionType.SingleValue);

                var migrationMaxOption = cmd.Option("-m|--migrationNr",
                    "Number of the highest migration to invoke", CommandOptionType.SingleValue);

                var assemblyOption = cmd.Option("-a|--assemblyName",
                    "Namepattern to discover the assembly with migrations", CommandOptionType.SingleValue);


                cmd.OnExecute(() =>
                {
                    long max;
                    var maxMigration = long.TryParse(migrationMaxOption.Value(), out max) ? max : -1;
                    MigrationDown(assemblyOption.Value(), connectionStringOption.Value(), maxMigration);
                    return 0;
                });

            });

            command.OnExecute(() =>
            {
                if (actionArg.Value.ToLower() == "seed")
                {
                    AddSeedMigration(titleOption.Value(), collectionOption.Value(), migrationCount, migrationsDir);
                    return 0;
                }
                if (actionArg.Value.ToLower() == "doc")
                {
                    AddDocumentMigration(titleOption.Value(), collectionOption.Value(), migrationCount, migrationsDir);
                    return 0;
                }

                if (actionArg.Value.ToLower() == "schema")
                {
                    AddSchemaMigration(titleOption.Value(), collectionOption.Value(), migrationCount, migrationsDir);
                    return 0;
                }



                return 1;
            });
        }

        private static void AddSeedMigration(string titleRaw, string collectionName,
            long migrationCount, string migrationsDir)
        {
            var title = titleRaw.Replace(" ", "_");

            var fileName = GetFileName(migrationCount, title);
            var content = Usings +
                          Environment.NewLine +
                          Namespace +
                           "{" + Environment.NewLine +
                          $"    [Migration({migrationCount}, \"{titleRaw}\")]" + Environment.NewLine +
                          $"    public class Seed_{title} : Migration" + Environment.NewLine +
                           "    {" + Environment.NewLine +
                          $"        protected string CollectionName => \"{collectionName}\";" + Environment.NewLine +
                           Environment.NewLine +
                           "        public override void Up() " + Environment.NewLine +
                           "        {" + Environment.NewLine +
                           "        }" + Environment.NewLine +
                           "        public override void Down()" + Environment.NewLine +
                           "        { " + Environment.NewLine +
                           "        }" + Environment.NewLine +
                           "    }" + Environment.NewLine +
                           "}" + Environment.NewLine;

            File.WriteAllText(Path.Combine(migrationsDir, fileName), content);
        }

        private static void AddDocumentMigration(string titleRaw, string collectionName, long migrationCount, string migrationsDir)
        {
            var title = titleRaw.Replace(" ", "_");

            var fileName = GetFileName(migrationCount, title);
            var content = Usings +
                          Environment.NewLine +
                          Namespace +
                          "{" + Environment.NewLine +
                          $"    [Migration({migrationCount}, \"{titleRaw}\")]" + Environment.NewLine +
                          $"    public class DocumentMigration_{title} : DocumentMigration" + Environment.NewLine +
                           "    {" + Environment.NewLine +
                          $"        protected override string CollectionName => \"{collectionName}\";" + Environment.NewLine +
                          Environment.NewLine +
                           "        protected override void UpgradeDocument(BsonDocument document) " + Environment.NewLine +
                           "        {" + Environment.NewLine +
                           "            // document.AddProperty('prop', true);" + Environment.NewLine +
                           "        }" + Environment.NewLine +
                           "        protected override void DowngradeDocument(BsonDocument document)" + Environment.NewLine +
                           "        { " + Environment.NewLine +
                           "            // document.RemoveProperty('prop', true);" + Environment.NewLine +
                           "        }" + Environment.NewLine +
                           "    }" + Environment.NewLine +
                           "}" + Environment.NewLine;

            File.WriteAllText(Path.Combine(migrationsDir, fileName), content);
        }

        private static string GetFileName(long migrationCount, string title)
        {
            var fileName = $"{migrationCount}_{title}.cs";
            return fileName;
        }

        private static void AddSchemaMigration(string titleRaw, string collectionName, long migrationCount, string migrationsDir)
        {
            var title = titleRaw.Replace(" ", "_");
            var fileName = GetFileName(migrationCount, title);
            var content = Usings +
                          Environment.NewLine +
                          Namespace +
                           "{" + Environment.NewLine +
                          $"    [Migration(\"{collectionName}\", {migrationCount})]" + Environment.NewLine +
                          $"    public class SchemaMigration_{title} : Migration" + Environment.NewLine +
                           "    {" + Environment.NewLine +
                          $"        protected string CollectionName => \"{collectionName}\";" + Environment.NewLine +
                          Environment.NewLine +
                           "        public override void Up() " + Environment.NewLine +
                           "        {" + Environment.NewLine +
                           "            var collection = GetCollection(CollectionName);" + Environment.NewLine +
                           "        }" + Environment.NewLine +
                           "        public override void Down()" + Environment.NewLine +
                           "        { " + Environment.NewLine +
                           "            var collection = GetCollection(CollectionName);" + Environment.NewLine +
                           "        }" + Environment.NewLine +
                           "    }" + Environment.NewLine +
                           "}" + Environment.NewLine;

            File.WriteAllText(Path.Combine(migrationsDir, fileName), content);
        }

        private static void MigrationUp(string assemblyName, string connectionString, long maxMigration)
        {
            var runner = CreateMigrationRunner(assemblyName, connectionString);
            runner.Up(maxMigration);
            Thread.Sleep(500);
        }

        private static void MigrationDown(string assemblyName, string connectionString, long maxMigration)
        {
            var runner = CreateMigrationRunner(assemblyName, connectionString);
            runner.Down(maxMigration);
            Thread.Sleep(500);
        }

        private static MigrationRunner CreateMigrationRunner(string assemblyName, string connectionString)
        {
            var probingPath = Path.Combine(Environment.CurrentDirectory, "bin");

            var assemblyFiles = Directory.GetFiles(probingPath, $"{assemblyName}*.dll", SearchOption.AllDirectories);
            if (assemblyFiles.Length > 1)
            {
                throw new ApplicationException("Found multiple assemblies! Specify a more restrictive pattern.");
            }

            var assemblyFile = assemblyFiles.First();

            var assembly = Assembly.LoadFile(assemblyFile);

            ILogger logger = _loggerFactory.CreateLogger<MigrationRunner>();
            var runner = new MigrationRunner(connectionString, assembly, logger);
            return runner;
        }

        private static int GetMaxMigrationCount(string migrationsDir)
        {
            var migrationCount = Directory.GetFiles(migrationsDir, "*.cs").Length;
            return migrationCount;
        }

        private static string CheckMigrationsDir()
        {
            var migrationsDir = Path.Combine(Environment.CurrentDirectory, MigrationFolder);
            if (!Directory.Exists(migrationsDir))
            {
                Directory.CreateDirectory(migrationsDir);
            }

            return migrationsDir;
        }

        private static string ReadFromConsole(string propertyName, string currentValue, string defaultValue, bool canBeNull)
        {
            string data = null;
            do
            {
                Console.Write($"{propertyName}: ({currentValue ?? defaultValue ?? (canBeNull ? "null" : "")}) ");

                data = Console.ReadLine().Trim();

                if (!canBeNull && string.IsNullOrEmpty(data))
                {
                    data = currentValue ?? defaultValue;
                }
            }
            while (!canBeNull && string.IsNullOrEmpty(data));

            return data;
        }
    }


}
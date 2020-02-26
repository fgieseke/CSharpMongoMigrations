# dotnet mongodb

## Commands

### migration

```cmd
dotnet mongodb migration up --help

# migrate to a specific version (-m 1) using migrations defined in the assembly Migrations.dll (-a Migrations) located beneath bin\ folder
dotnet mongodb migration up -c mongodb://localhost/Test -m 1 -a Migrations

# migrate all versions not migrated yet using migrations defined in the assembly Migrations.dll (-a Migrations) located beneath bin\ folder
dotnet mongodb migration up -c mongodb://localhost/Test -a Migrations

dotnet mongodb migration down --help

# roll back migrations to a specific version (-m 0) using migrations defined in the assembly Migrations.dll (-a Migrations) located beneath bin\ folder
dotnet mongodb migration down -c mongodb://localhost/Test -m 0 -a Migrations

# roll back migrations to the initial version using migrations defined in the assembly Migrations.dll (-a Migrations) located beneath bin\ folder
dotnet mongodb migration down -c mongodb://localhost/Test -a Migrations

```

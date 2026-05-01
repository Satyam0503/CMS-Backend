// See https://aka.ms/new-console-template for more information
using System.Reflection;
using Codeji.CMS.GenericRepository.Settings;
using Codeji.CMS.Migrations;
using Codeji.CMS.Migrations.Interface;
using Codeji.CMS.Migrations.Migrations;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .Build();

// connect to database
var connection = configuration.GetConnectionString("mongodb")
    ?? throw new InvalidOperationException("ConnectionStrings:mongodb is not configured.");
var mongoUrl = MongoUrl.Create(connection);
var mongodb = new MongoClient(connection);
var db = mongodb.GetDatabase(mongoUrl.DatabaseName);
// Load migrations dynamically
var migrations = MigrationLoader.LoadMigrations();

var runner = new MigrationRunner(migrations, db);
await runner.RunAsync();
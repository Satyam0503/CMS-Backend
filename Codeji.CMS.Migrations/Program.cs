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

// get specific section from configuration file
var section = configuration.GetSection("MongoDbSettings");
var _dbSettings = section.Get<MongoDbSettings>();

// connect to database 

var mongodb = new MongoClient(_dbSettings.Connection);
var db = mongodb.GetDatabase(_dbSettings.DatabaseName);
// Load migrations dynamically
var migrations = MigrationLoader.LoadMigrations();

var runner = new MigrationRunner(migrations, db);
await runner.RunAsync();
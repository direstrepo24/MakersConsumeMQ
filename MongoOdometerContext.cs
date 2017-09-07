using System;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Microsoft.Extensions.Options;
using makerscoreDatalib.Domain.Entities;
using MakersConsumeMQ;

namespace makerscoreDatalib.Domain
{
    public class MongoOdometerContext
    {
             private readonly IMongoDatabase _database = null;

      public MongoOdometerContext(IOptions<MySettings> settings)
            {
                var client = new MongoClient(settings.Value.ConnectionString);
                if (client != null)
                    _database = client.GetDatabase(settings.Value.Database);
            }
/* var client = new MongoClient("mongodb://adminMakers:12345678@54.89.149.254:27017/?maxPoolSize=555");

                    // var db = client.GetDatabase("gpsodometerdb");
                        // var client = new MongoClient(settings.Value.ConnectionString);
                if (client != null)
                _database = client.GetDatabase("gpsodometerdb");

               */
            /* 
            public OdometerContext(string connectionString, string database)
            {
                if (connectionString == null)
            {
                throw new ArgumentException("Missing MongoDB connection string");
            }

            var client = new MongoClient(connectionString);
            MongoUrl mongoUrl = MongoUrl.Create(connectionString);
            this._database = client.GetDatabase(database);
           // this._collection = this.SetupCollection();

           /*var client = new MongoClient(settings.Value.ConnectionString);
                if (client != null)
                    _database = client.GetDatabase(settings.Value.Database); 
            }
            */
            public IMongoCollection<TEntity> GetCollection<TEntity>()
        {
            return _database.GetCollection<TEntity>(typeof(TEntity).Name.ToLower() + "s");
        }
         public IMongoQueryable<TEntity> GetCollectionQuery<TEntity>()
        {
            return _database.GetCollection<TEntity>(typeof(TEntity).Name.ToLower() + "s").AsQueryable();
        }

                    public IMongoCollection<GpsOdometerModel> Odometer
            {
                get
                {
                    return _database.GetCollection<GpsOdometerModel>("GpsOdometerModel");
                }
            }

             public IMongoQueryable<GpsOdometerModel> OdometerQuery
            {
                get
                {
                    return _database.GetCollection<GpsOdometerModel>("GpsOdometerModel").AsQueryable();
                }
            }
             public IMongoCollection<Zero> dts
            {
                get
                {
                    return _database.GetCollection<Zero>("dts");
                }
            }
             public IMongoCollection<Zero> dtsQuery
            {
                get
                {
                    return _database.GetCollection<Zero>("dts");
                }
            }

    }
}
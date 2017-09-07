using System;
using System.Collections.Generic;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using makerscoreDatalib;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using makerscoreDatalib.Domain.Repositories;
using makerscoreDatalib.Domain.IRepositories;
using makerscoreDatalib.Domain.Entities;
using makerscoreDatalib.Utils;
using System.IO;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Threading;
using FluentScheduler;
using makerscoreDatalib.Domain;

namespace MakersConsumeMQ
{
   public class Program
    {
      
         // public static  IRepository<GpsOdometerModel> _GpsOdometerRepository;
         public static IConfigurationRoot config { get; set; }
     //initial iobjects 

         public static IdtsLocation dts;
        //basic variables connection rrabit
        public static IodometergpsRepository gpsodometer;

//config json
         public static string AppSettingsFile = "appsettings.json";
        public static string AppSettingsSection = "MongoConnection";
        public static string DateSetting = "ConnectionString";

        public static Application app;
        public static RabbitServerQuery rabbit;

        

        public static string DataBaseName ="Database";
         public static GpsOdometerModel odometerRealTime;
         static void Main(string[] args)
        {
                
                
                IServiceCollection serviceCollection = new ServiceCollection();

                ConfigureServices(serviceCollection);
                IServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();
             var context=   serviceProvider.GetService<OdometerContext>();
                  
                  dts = serviceProvider.GetService<IdtsLocation>();
                  gpsodometer=serviceProvider.GetService<IodometergpsRepository>();
                
                // For async
              app = serviceProvider.GetService<Application>();
              Application.dtslocal=dts;

             
            rabbit=serviceProvider.GetService<RabbitServerQuery>();
             RabbitServerQuery.gpsodometer=gpsodometer;
             RabbitServerQuery.dts=dts;
         

        
      //  Task.Run(() => app.Run()).Wait();
     Console.WriteLine("Iniciando Servicio.........................");
        Task.Run(()=>rabbit.RabbitServerQueryMQ()).Wait();
      /* 
// Start the scheduler
JobManager.Initialize(new ScheduleSetup());
            JobManager.Start();
            Console.ReadLine();


        }
        ///clase para hacer tareas con tiempos
     
        public class ScheduleSetup : Registry
    {
        public ScheduleSetup()
        {
            // Schedule an IJob to run once, delayed by a specific time interval
       
           // Schedule<MyJob>().toToRunOnceIn(20).Seconds();
       Action RabbitServerQuer= new Action(() =>
        {
            Console.WriteLine("Timed Task - Actualizar DTS con WS");
          //  RabbitServerQuery();
         
          // Task.Run(() =>  app.Run()).Wait();
           
           
          
        }


        );
        Action RabbitServerQuer2= new Action(() =>
        {
            Console.WriteLine("Timed Task - Odometer in Real Time");
          //  Task.Run(() =>  RabbitServerQuery());
         
           //Task.Run(() =>  app.Run()).Wait();
          
        });

        // Schedule schedule = new Schedule(someMethod);
        // schedule.ToRunNow();

        
        // this.Schedule(RabbitServerQuer2).ToRunNow().AndEvery(30).Seconds();
        // this.Schedule(RabbitServerQuer).ToRunEvery(6).Hours();;//ToRunEvery(7).Minutes();//  ToRunNow().AndEvery(60).Seconds();
        // JobManager.RemoveJob("RabbitServerQuer");//.RemoveTask("UnreadAlertRegistry");
          // Console.WriteLine("FIN DTS con WS");
        }
    }*/
   

    
        }

        private static void ConfigureServices(IServiceCollection serviceCollection)
        {
                IConfigurationRoot configuration = GetConfiguration();
                serviceCollection.AddSingleton<IConfigurationRoot>(configuration);
                serviceCollection.AddSingleton<RabbitServerQuery>();
                // Support typed Options
                serviceCollection.AddOptions();

                  serviceCollection.Configure<Settings>(options =>
                {
                    options.ConnectionString = configuration.GetSection("MongoConnection:ConnectionString").Value;
                    options.Database = configuration.GetSection("MongoConnection:Database").Value;
                }); 
              //  serviceCollection.Configure<Settings>(configuration.GetSection(Program.AppSettingsSection));
            // serviceCollection.Configure<Settings>(configuration.GetSection(appSettings[Program.DateSetting];
                //   string database =appSettings[Program.DataBaseName];))  
                 serviceCollection.AddSingleton<OdometerContext>();
                serviceCollection.AddTransient<IodometergpsRepository, odometergpsRepository>();
                serviceCollection .AddTransient<IdtsLocation, DtsLocation>();
               
                //serviceCollection .AddTransient<Programgpsodometer>();
                serviceCollection.AddTransient<Application>();
        }
private static IConfigurationRoot GetConfiguration()
    {
        return new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile($"appsettings.json", optional: true)
            .Build();

/* 
           return new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(Program.AppSettingsFile, optional: true, 
                             reloadOnChange: true)
                             .Build();*/
    }


        /* 
public void ConfigureServices(IServiceCollection services)
{

// services.AddTransient<IodometergpsRepository, odometergpsRepository>();
//   services.AddTransient<IdtsLocation, DtsLocation>();
    services.Configure<Settings>(options =>
       {
           options.ConnectionString = config.GetSection("MongoConnection:ConnectionString").Value;
           options.Database = config.GetSection("MongoConnection:Database").Value;
       }); 
}
*/

    }
}

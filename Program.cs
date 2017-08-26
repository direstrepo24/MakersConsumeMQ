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

        private static ConnectionFactory factory;
        private static IConnection connection;
        private static IModel channel;
//config json
         public static string AppSettingsFile = "appsettings.json";
        public static string AppSettingsSection = "MongoConnection";
        public static string DateSetting = "ConnectionString";

        public static Application app;

        public static string DataBaseName ="Database";
         public static GpsOdometerModel odometerRealTime;
         static void Main(string[] args)
        {
                
                
                IServiceCollection serviceCollection = new ServiceCollection();

                ConfigureServices(serviceCollection);
                IServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();

                  dts = serviceProvider.GetService<IdtsLocation>();
                  gpsodometer=serviceProvider.GetService<IodometergpsRepository>();
                
                // For async
          app = serviceProvider.GetService<Application>();
         
        Application.dtslocal=dts;

        
      //  Task.Run(() => app.Run()).Wait();
   
     //   Task.Run(()=>RabbitServerQuery()).Wait();
   
// Start the scheduler
JobManager.Initialize(new ScheduleSetup());
            JobManager.Start();
            Console.ReadLine();


        }
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
         
           Task.Run(() =>  app.Run()).Wait();
          
        });
        Action RabbitServerQuer2= new Action(() =>
        {
            Console.WriteLine("Timed Task - Odometer in Real Time");
            Task.Run(() =>  RabbitServerQuery());
         
           //Task.Run(() =>  app.Run()).Wait();
          
        });

        // Schedule schedule = new Schedule(someMethod);
        // schedule.ToRunNow();

        
         this.Schedule(RabbitServerQuer2).ToRunNow().AndEvery(30).Seconds();
         this.Schedule(RabbitServerQuer).ToRunEvery(7).Minutes();//  ToRunNow().AndEvery(60).Seconds();
        }
    }
   

 public static async Task RabbitServerQuery(){
        
        try{


             factory = new ConnectionFactory()
            {

                HostName = "104.196.62.27",
                Port = 5672,
                UserName = "mavesa.canbus",
                Password = "m4$$35%1",
                VirtualHost = "prod"
                
            };
            using (connection = factory.CreateConnection())
            using (channel = connection.CreateModel())
            {
                var queueName = "HistoryCanbus.Mavesa";
                     channel.QueueDeclare(queueName,
                     durable: true, 
                     exclusive: false,
                     autoDelete: false, 
                     arguments: new Dictionary<string, object> { { "x-queue-mode", "lazy" } });

                 
				channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);
               // var arguments = new Dictionary<string, object> { { "x-queue-mode", "lazy" } }; 
                Console.WriteLine(" [*] Waiting for logs.");

                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += async (model, ea) =>
                {
                    var body = ea.Body;
                    var message = Encoding.UTF8.GetString(body);
                    Console.WriteLine(" [x] {0}", message);
                     var deserialized = JsonConvert.DeserializeObject<RootObject>(message);
                     if(deserialized!=null){


                    
                     odometerRealTime = new GpsOdometerModel{
                        Imei= deserialized.imei,
                        odometer=deserialized.OBDConfigInfo.totalDistance,
                        fLatitude=deserialized.GPSInfo.fAltitude,
                        fLongitude=deserialized.GPSInfo.fLongitude,
                        iGPSStatus=deserialized.GPSInfo.iGPSStatus

                     };

            //Primero verifico si no esta guardada en la base de datos del servidor o local
            // var existImeiw=  gpsodometer.GetAllOdometers().Result;
                var existImei=  gpsodometer.Getodometer(odometerRealTime.Imei);// ListRealOdometerfindID(odometerRealTime.Imei);
                var imeiExist=gpsodometer.ListRealOdometerfindID(odometerRealTime.Imei);
                
                        if(existImei.Result!=null || imeiExist.Result.Count>0 ){
                            
                         await   gpsodometer.UpdateMultiOdometer(odometerRealTime.Imei, odometerRealTime.odometer);     
                                Console.WriteLine("Actualizó odometro");
                    
                        }
                        else{
                            //Si no se encuentra resgistrado ese imei, lo registro en la collection local
                      
                        await  gpsodometer.AddOdometer(odometerRealTime);
                            Console.WriteLine("nueva entidad");
                            var ImeitoUpdate=gpsodometer.Getodometer(odometerRealTime.Imei);
                            if(ImeitoUpdate.Result.Automotor_chasis==null){
                                //consulto el chasis del GpsOdometerModel
                               var chasis=   dts.GetDts(odometerRealTime.Imei);
                               
                               if(chasis.Result!=null){
                               await  gpsodometer.UpdateChasisTimeReal(odometerRealTime.Imei, chasis.Result.automotor_chasis);
                                    Console.WriteLine("Actualizó chasis");
                               }

                            }
                
                        
                    }           
            }
            else{

                Console.WriteLine("No se pudieron cargar objetos del servicio Rabbit");
            }
                    
                   // stop();
                    
                };
                channel.BasicConsume(queue:queueName,
                                     autoAck: true,
                                     consumer: consumer);
     
                Console.WriteLine(" Press [enter] to exit.");
                Console.ReadLine();
                

             }
        }
             catch (Exception ex)
                {
                    // log or manage the exception
                    throw ex;
                }
     
            Console.WriteLine("DoWork Complete");
                
     }
     public static void stop()
         {

              channel.Close(200, "Goodbye");
              channel.Abort();
              connection.Close();
              connection.Abort();
         }


        private static void ConfigureServices(IServiceCollection serviceCollection)
        {
           IConfigurationRoot configuration = GetConfiguration();
        serviceCollection.AddSingleton<IConfigurationRoot>(configuration);

        // Support typed Options
        serviceCollection.AddOptions();
        serviceCollection.Configure<Settings>(configuration.GetSection(Program.AppSettingsSection));
       // serviceCollection.Configure<Settings>(configuration.GetSection(appSettings[Program.DateSetting];
         //   string database =appSettings[Program.DataBaseName];))  
         serviceCollection.AddTransient<IodometergpsRepository, odometergpsRepository>();
        serviceCollection .AddTransient<IdtsLocation, DtsLocation>();
         //serviceCollection .AddTransient<Programgpsodometer>();
        serviceCollection.AddTransient<Application>();
        }
private static IConfigurationRoot GetConfiguration()
    {
       /* return new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile($"appsettings.json", optional: true)
            .Build();*/


           return new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(Program.AppSettingsFile, optional: true, 
                             reloadOnChange: true)
                             .Build();
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

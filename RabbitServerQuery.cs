using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using makerscoreDatalib.Domain.Entities;
using makerscoreDatalib.Domain.IRepositories;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MakersConsumeMQ
{
    
    public class RabbitServerQuery
    {

        private static ConnectionFactory factory;
        private static IConnection connection;
        private static IModel channel;
        public static IodometergpsRepository gpsodometer;
        public static GpsOdometerModel odometerRealTime;
        public static IdtsLocation dts;
        public static Application app;

        public RabbitServerQuery(IodometergpsRepository Sgpsodometer, IdtsLocation _dts){
            gpsodometer=Sgpsodometer;
            dts=_dts;

        }
         public async Task RabbitServerQueryMQ(){
        
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
                var existImei=await  gpsodometer.Getodometer(odometerRealTime.Imei);// ListRealOdometerfindID(odometerRealTime.Imei);
               // var imeiExist=await gpsodometer.ListRealOdometerfindID(odometerRealTime.Imei);
                 var chasis= await  dts.GetDts(odometerRealTime.Imei);
                        if(existImei!=null  ){
                            
                         await   gpsodometer.UpdateMultiOdometer(odometerRealTime.Imei, odometerRealTime.odometer);     
                                 
                                Console.WriteLine("Actualizó odometro");
                                if(existImei.Automotor_chasis==null){
                                //consulto el chasis del servicio remoto
                                      if(chasis!=null){
                                    await  gpsodometer.UpdateChasisTimeReal(odometerRealTime.Imei, chasis.automotor_chasis);
                                    Console.WriteLine("Actualizó chasis");
                                    }
                                    else{
                                          Console.WriteLine("Inicio Consulta DTS remoto.........");   
                                        Task.Run(() =>  app.Run()).Wait();
                                         Console.WriteLine("Fanalizado  DTS remoto.........");   
                                    }
                                   
                                }
                              
                    
                        }
                        else{
                            //Si no se encuentra resgistrado ese imei, lo registro en la collection local
                      
                        await  gpsodometer.AddOdometer(odometerRealTime);
                            Console.WriteLine("nueva entidad");
                           // var ImeitoUpdate=gpsodometer.Getodometer(odometerRealTime.Imei);
                            

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

    }
}
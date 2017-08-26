using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using makerscoreDatalib.Domain.Entities;
using System.Collections.Generic;
using makerscoreDatalib.Domain.IRepositories;

namespace MakersConsumeMQ
{
    public class Application
    {
       // ILogger _logger;
        MySettings _settings;
        public static  odometerResponse dtsremote;
        public static  List<Zero> dtsqueryremote;
         public static IdtsLocation dtslocal;
//ILogger<Application> logger
        public Application(IOptions<odometerResponse> dts)
        {
        // _logger = logger;
            dtsremote = dts.Value;
        }

    public async  Task Run()
    {
        try
        {
        //    _logger.LogInformation($"This is a console application for {_settings.ConnectionString}");
        //consultar web services remoto
                 HttpClientHandler handler = new HttpClientHandler();
        handler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
           using (var httpClient = new HttpClient(handler))
          {
        
            httpClient.BaseAddress =  new Uri("http://dts.location-world.com/");  
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "deflate"); 
           var response  = httpClient.GetStringAsync("api/fleet/businessodometers?token=2985F9B5B0914F24808B5DDFC94A5AF1&time_zone_offset=-5").Result;
            var data = JsonConvert.DeserializeObject<odometerResponse>(response);
            dtsremote = data;
             dtsqueryremote= dtsremote.zero;
               // return  dtsremote.zero;
                 foreach (var item in dtsqueryremote)
            {
               
                //consulta en la coleccion "GpsOdometerModel" , aqui se guarda localmente
                // los datos que se consulta en tiempo real. 
                // var migpsOdometermodel= gpsodometer.Getodometer(item.imei);
                 //consulta en la coleccion "dts" , aqui guardo la consulta al dts remota o web service
                 var midtsLocal=dtslocal.ListfindID(item.imei);
                //
               if(midtsLocal.Result.Count>0){

                  Console.WriteLine("actualizando");
                       // _dtsRepository.UpdateDts(midts.Result.imei, item.automotor_chasis);
                        await dtslocal.UpdateChasis(item.imei, item.automotor_chasis);
                            //adiciono a la coleccion
                             Console.WriteLine("se actualizo todo");
                        

               }

               else {
              await  dtslocal.AddDts(item); 
                Console.WriteLine("Añadido nuevo elemento");
               }
              
            }
              }
        
        }
         catch (Exception ex)
                {
                    // log or manage the exception
                    throw ex;
                }



        }
    }

}
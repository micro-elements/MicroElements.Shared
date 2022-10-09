using MicroElements.Logging;

namespace DisclosureParser.Api
{
    public partial class SamplesProgram
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication
                .CreateBuilder(args);

            builder.Logging.ClearProviders();
            builder.Logging.AddConsole(options => options.IncludeScopes = true);
            // builder.Services.AddThrottlingLogging(options =>
            // {
            //     options.AppendMetricsToMessage = true;
            //     options.ThrottleCategory("MicroElements.Samples.Api.Logging.LoggingSampleController");
            // });
            
            // Add services to the container.
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            
            app.MapControllers();
            app.Run();
        }
    }
}
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

            var services = builder.Services;
            
            // Add log throttling
            services.AddThrottlingLogging(options =>
            {
                options.AppendMetricsToMessage = true;
                options.ThrottleCategory("MicroElements.Samples.Api.Logging.LoggingSampleController");
            });

            // Configure default throttling options
            services.ConfigureThrottling(options => options.ThrottlingPeriod = TimeSpan.FromMinutes(1));
            
            // Configure some category options
            services.ConfigureThrottling("MicroElements.Samples.Api.Logging.LoggingSampleController", options => options.GetMessageKey = s => s);
            
            // Add services to the container.
            services.AddControllers();
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();

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
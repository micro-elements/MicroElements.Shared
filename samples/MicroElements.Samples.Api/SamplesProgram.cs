namespace DisclosureParser.Api
{
    public partial class SamplesProgram
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication
                .CreateBuilder(args);

            builder.Logging.ClearProviders();
            builder.Logging.AddJsonConsole();
            
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
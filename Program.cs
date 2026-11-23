using BankaExtreAnaliz.Models;
using BankaExtreAnaliz.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Hosting providers such as Render provide the HTTP port through PORT.
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

// Veritabanı Servisi
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=bankextre.db"));

// Ekstre Okuma Servisi
builder.Services.AddScoped<StatementParserService>();

// Razor Pages Servisi
builder.Services.AddRazorPages();

var app = builder.Build();

// Apply the checked-in EF Core migrations when the app starts.
await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

// HTTP İstek Hattı
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// Telefondan HTTP ile rahatça girebilmek için yönlendirmeyi Development ortamında kaldırıyoruz
// app.UseHttpsRedirection(); 

app.UseRouting();
app.UseAuthorization();
app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
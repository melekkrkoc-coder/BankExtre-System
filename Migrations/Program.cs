using BankaExtreAnaliz.Models;
using BankaExtreAnaliz.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Kestrel'in tüm ağdan (telefondan) HTTP ve HTTPS dinlemesini sağla:
builder.WebHost.UseUrls("http://0.0.0.0:5132", "https://0.0.0.0:7055");

// Veritabanı Servisi
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Ekstre Okuma Servisi
builder.Services.AddScoped<StatementParserService>();

// Razor Pages Servisi
builder.Services.AddRazorPages();

var app = builder.Build();

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
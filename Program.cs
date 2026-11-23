using BankaExtreAnaliz.Models;
using BankaExtreAnaliz.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var publicDemo = string.Equals(builder.Configuration["PUBLIC_DEMO"], "true", StringComparison.OrdinalIgnoreCase);

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

// Apply migrations and seed clearly fictional data for the public portfolio demo.
await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();

    if (publicDemo && !await db.Accounts.AnyAsync())
    {
        var demoAccount = new Account { BankName = "Örnek Banka", AccountName = "Demo Hesabı" };
        db.Accounts.Add(demoAccount);
        await db.SaveChangesAsync();

        db.Transactions.AddRange(
            new Transaction { TransactionDate = DateTime.Today.AddDays(-2), Description = "Örnek market alışverişi", Amount = 1240m, IsExpense = true, AccountId = demoAccount.Id, CategoryId = 1 },
            new Transaction { TransactionDate = DateTime.Today.AddDays(-5), Description = "Örnek elektrik faturası", Amount = 860m, IsExpense = true, AccountId = demoAccount.Id, CategoryId = 2 },
            new Transaction { TransactionDate = DateTime.Today.AddDays(-9), Description = "Örnek ulaşım harcaması", Amount = 320m, IsExpense = true, AccountId = demoAccount.Id, CategoryId = 3 },
            new Transaction { TransactionDate = DateTime.Today.AddDays(-14), Description = "Örnek kafe harcaması", Amount = 275m, IsExpense = true, AccountId = demoAccount.Id, CategoryId = 4 },
            new Transaction { TransactionDate = DateTime.Today.AddDays(-20), Description = "Örnek maaş geliri", Amount = 32000m, IsExpense = false, AccountId = demoAccount.Id, CategoryId = 5 }
        );
        await db.SaveChangesAsync();
    }
}

// Public demo is read-only. Reject writes even if a visitor crafts a request manually.
app.Use(async (context, next) =>
{
    if (publicDemo &&
        !HttpMethods.IsGet(context.Request.Method) &&
        !HttpMethods.IsHead(context.Request.Method) &&
        !HttpMethods.IsOptions(context.Request.Method))
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        await context.Response.WriteAsync("This public demo is read-only.");
        return;
    }

    await next();
});

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
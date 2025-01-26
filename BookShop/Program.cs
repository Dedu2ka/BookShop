var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews(); // ��������� ��������� MVC

// ��������� ������
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();

// ��������� ��������������
builder.Services.AddAuthentication("ApplicationCookie")
    .AddCookie("ApplicationCookie", options =>
    {
        options.LoginPath = "/Home/Login"; // �������� ��� �����
        options.LogoutPath = "/Home/Logout"; // �������� ��� ������
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts(); // �������� HSTS (HTTP Strict Transport Security)
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// �������� �������������� � �����������
app.UseAuthentication();
app.UseAuthorization();

// �������� ������
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

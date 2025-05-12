using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using System.Data.Common;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using LibraryAPI;
using LibraryAPI.Data;
using Newtonsoft.Json;
using Bogus;
using LibraryAPI.Model;
using LibraryAPI.Services;
using Moq;

namespace LibraryAPITests;

public class APIFixture
    : WebApplicationFactory<Program>
{
    public readonly HttpClient client;
    public readonly Faker faker;
    public readonly Faker<NewLoan> loanFaker;
    public Mock<IDateLibrary> dateLibraryMock;
    public Mock<IBestSellersService> bestSellersServiceMock;

    protected DbConnection conexaoSqlite { get; set; }
    protected ServiceProvider serviceProvider { get; set; }
    
    public DateTime FixedDate { get; set; } = DateTime.Now;

    public APIFixture()
    {
        client = CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        faker = new Faker();

        loanFaker = new Faker<NewLoan>();
        loanFaker.RuleFor(m => m.UserName, f => f.Name.FirstName() + "_" + f.Name.LastName());
        loanFaker.RuleFor(m => m.Isbn, f => f.Random.String2(13, "0123456789"));
        
    }

    public StringContent ConvertToRawJson(object obj)
        => new StringContent(JsonConvert.SerializeObject(obj), Encoding.Default, "application/json");
    
    public ApplicationDbContext CriarNovoContexto()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(conexaoSqlite)
            .Options;
        return new ApplicationDbContext(options);
    }

    private void UtilizarSQLiteComoBanco(IServiceCollection services)
    {
        //Usar SQLite
        var descriptorContexto = services.SingleOrDefault(
            d => d.ServiceType ==
                 typeof(DbContextOptions<ApplicationDbContext>));
        services.Remove(descriptorContexto);

        conexaoSqlite = new SqliteConnection("Filename=:memory:");
        conexaoSqlite.Open();
        services.AddDbContext<ApplicationDbContext>(options => { options.UseSqlite(conexaoSqlite); });
    }
    
    private void UtilizarMoqParaIDateLibrary(IServiceCollection services)
    {
        // Remove the existing IDateLibrary registration
        var dateLibraryDescriptor = services.SingleOrDefault(
            d => d.ServiceType == typeof(IDateLibrary));
        if (dateLibraryDescriptor != null)
        {
            services.Remove(dateLibraryDescriptor);
        }
    
        // Create and configure a mock for IDateLibrary
        dateLibraryMock = new Mock<IDateLibrary>();
        dateLibraryMock.Setup(dl => dl.GetCurrentDate()).Returns(() => FixedDate);
    
        // Register the mock instance in the service collection
        services.AddSingleton(dateLibraryMock.Object);
    }

    private void UtilizarMoqParaIBestSellersService(IServiceCollection services)
    {
        // Remove the existing IBestSellersService registration
        var bestSellersServiceDescriptor = services.SingleOrDefault(
            d => d.ServiceType == typeof(IBestSellersService));
        if (bestSellersServiceDescriptor != null)
        {
            services.Remove(bestSellersServiceDescriptor);
        }
    
        // Create and configure a mock for IBestSellersService
        bestSellersServiceMock = new Mock<IBestSellersService>();
        bestSellersServiceMock.Setup(dl => dl.GetBookRankAsync(It.IsAny<string>())).ReturnsAsync(99);
        
        // Register the mock instance in the service collection
        services.AddSingleton(bestSellersServiceMock.Object);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            UtilizarSQLiteComoBanco(services);
            UtilizarMoqParaIDateLibrary(services);
            UtilizarMoqParaIBestSellersService(services);
    
            serviceProvider = services.BuildServiceProvider();
        });
    }

    public async Task<TResponse> GetTypedResponse<TResponse>(HttpResponseMessage httpResponseMessage)
    {
        string respostaConteudo = await httpResponseMessage.Content.ReadAsStringAsync();

        TResponse respostaTipada = default(TResponse);
        respostaTipada = JsonConvert.DeserializeObject<TResponse>(respostaConteudo);

        return respostaTipada;
    }
}
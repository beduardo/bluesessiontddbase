using System.Net;
using FluentAssertions;
using LibraryAPI.Data;
using LibraryAPI.Model;
using LibraryAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace LibraryAPITests;

public class LoanTests
{
    private readonly string _baseEndpoint = "/Loan";
    
    [Fact]
    public async Task Post_Loan_Return_Created()
    {
        var fixture = new APIFixture();

        var newLoan = fixture.loanFaker.Generate();
        var content = fixture.ConvertToRawJson(newLoan);
        var result = await fixture.client.PostAsync(_baseEndpoint, content);

        result.StatusCode.Should().Be(HttpStatusCode.Created);
    }
    
    [Fact]
    public async Task Post_Loan_Return_Location_Header()
    {
        var fixture = new APIFixture();
    
        var newLoan = fixture.loanFaker.Generate();
        var content = fixture.ConvertToRawJson(newLoan);
        var result = await fixture.client.PostAsync(_baseEndpoint, content);
    
        result.Headers.Location.ToString().Should().Be("/Loan/1");
    }
    
    [Fact]
    public async Task Post_Loan_Return_New_Loan_Entity()
    {
        var fixture = new APIFixture();
        fixture.FixedDate = fixture.faker.Date.Recent();
    
        var newLoan = fixture.loanFaker.Generate();
        var content = fixture.ConvertToRawJson(newLoan);
        var result = await fixture.client.PostAsync(_baseEndpoint, content);
        
        var loanEntity = await fixture.GetTypedResponse<Loan>(result);

        loanEntity.Id.Should().Be(1);
        loanEntity.User.Should().Be(newLoan.UserName);
        loanEntity.ISBN.Should().Be(newLoan.Isbn);
        loanEntity.Date.Should().Be(fixture.FixedDate);
        loanEntity.DueDate.Should().Be(fixture.FixedDate.AddDays(14));
        loanEntity.Return.Should().BeFalse();
    }
    
    [Fact]
    public async Task Post_Loan_Register_In_Database()
    {
        var fixture = new APIFixture();
    
        var newLoan = fixture.loanFaker.Generate();
        var content = fixture.ConvertToRawJson(newLoan);
        await fixture.client.PostAsync(_baseEndpoint, content);
    
        var conn = fixture.CriarNovoContexto();
        conn.Loans.Count().Should().Be(1);
        var f = conn.Loans.First();
        f.Id.Should().Be(1);
        f.User.Should().Be(newLoan.UserName);
        f.ISBN.Should().Be(newLoan.Isbn);
    }
    
    [Fact]
    public async Task Post_Loan_Return_BadRequest_When_Empty_UserName()
    {
        var fixture = new APIFixture();
    
        var newLoan = fixture.loanFaker.Generate();
        newLoan.UserName = "";
        var content = fixture.ConvertToRawJson(newLoan);
        
        var result = await fixture.client.PostAsync(_baseEndpoint, content);
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var badRequestResponse = await fixture.GetTypedResponse<ValidationProblemDetails>(result);
        badRequestResponse.Status.Should().Be(400);
        badRequestResponse.Errors.Should().BeEquivalentTo(new Dictionary<string, string[]>
        {
            { "UserName", new[] { "User name is required" } }
        });
    }
    
    [Fact]
    public async Task Post_Loan_Return_BadRequest_When_Empty_ISBN()
    {
        var fixture = new APIFixture();
    
        var newLoan = fixture.loanFaker.Generate();
        newLoan.Isbn = "";
        var content = fixture.ConvertToRawJson(newLoan);
        
        var result = await fixture.client.PostAsync(_baseEndpoint, content);
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var badRequestResponse = await fixture.GetTypedResponse<ValidationProblemDetails>(result);
        badRequestResponse.Status.Should().Be(400);
        badRequestResponse.Errors.Should().BeEquivalentTo(new Dictionary<string, string[]>
        {
            { "Isbn", new[] { "ISBN is required" } }
        });
    }
        
    [Fact]
    public async Task Post_Loan_Return_BadRequest_When_User_Exceeds_Max_Books()
    {
        var fixture = new APIFixture();
        var newLoan = fixture.loanFaker.Generate();

        //create 3 not returned books
        var db = fixture.CriarNovoContexto();
        db.Add(new Loan() { ISBN = "1", User = newLoan.UserName, Date = DateTime.Now, DueDate = DateTime.Now.AddDays(14) });
        db.Add(new Loan() { ISBN = "2", User = newLoan.UserName, Date = DateTime.Now, DueDate = DateTime.Now.AddDays(14) });
        db.Add(new Loan() { ISBN = "3", User = newLoan.UserName, Date = DateTime.Now, DueDate = DateTime.Now.AddDays(14) });
        await db.SaveChangesAsync();

        var content = fixture.ConvertToRawJson(newLoan);
        
        var result = await fixture.client.PostAsync(_baseEndpoint, content);
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var badRequestResponse = await fixture.GetTypedResponse<ValidationProblemDetails>(result);
        badRequestResponse.Status.Should().Be(400);
        badRequestResponse.Errors.Should().BeEquivalentTo(new Dictionary<string, string[]>
        {
            { "Entity", new[] { "User has reached the maximum number of books" } }
        });
    }
        
    [Theory]
    [InlineData(true, false, false)]
    [InlineData(true, true, false)]
    [InlineData(true, true, true)]
    public async Task Post_Loan_Return_AllBooksReturned_Create_New_Loan(bool returned1, bool returned2, bool returned3)
    {
        var fixture = new APIFixture();
        var newLoan = fixture.loanFaker.Generate();

        //create 3 not returned books
        var db = fixture.CriarNovoContexto();
        db.Add(new Loan() { ISBN = "1", User = newLoan.UserName, Date = DateTime.Now, DueDate = DateTime.Now.AddDays(14), Return = returned1 });
        db.Add(new Loan() { ISBN = "2", User = newLoan.UserName, Date = DateTime.Now, DueDate = DateTime.Now.AddDays(14), Return = returned2 });
        db.Add(new Loan() { ISBN = "3", User = newLoan.UserName, Date = DateTime.Now, DueDate = DateTime.Now.AddDays(14), Return = returned3 });
        await db.SaveChangesAsync();

        var content = fixture.ConvertToRawJson(newLoan);
        
        var result = await fixture.client.PostAsync(_baseEndpoint, content);
        result.StatusCode.Should().Be(HttpStatusCode.Created);
    }
        
    [Fact]
    public async Task Post_Loan_Return_BadRequest_When_User_Has_Overdue_Loans()
    {
        var fixture = new APIFixture();
        var newLoan = fixture.loanFaker.Generate();

        //create 3 not returned books
        var db = fixture.CriarNovoContexto();
        var currentDate = fixture.faker.Date.Recent();
        var loanDate = currentDate.AddDays(-15);
        var dueDate = loanDate.AddDays(14);
        fixture.FixedDate = currentDate;
        
        db.Add(new Loan() { ISBN = "1", User = newLoan.UserName, Date = loanDate, DueDate = dueDate });
        await db.SaveChangesAsync();

        var content = fixture.ConvertToRawJson(newLoan);
        
        var result = await fixture.client.PostAsync(_baseEndpoint, content);
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var badRequestResponse = await fixture.GetTypedResponse<ValidationProblemDetails>(result);
        badRequestResponse.Status.Should().Be(400);
        badRequestResponse.Errors.Should().BeEquivalentTo(new Dictionary<string, string[]>
        {
            { "Entity", new[] { "User has overdue books" } }
        });
    }
            
    [Fact]
    public async Task Post_Loan_Return_Created_EvenIf_User_Had_Overdue_Loans_Before()
    {
        var fixture = new APIFixture();
        var newLoan = fixture.loanFaker.Generate();

        //create 3 not returned books
        var db = fixture.CriarNovoContexto();
        var currentDate = fixture.faker.Date.Recent();
        var loanDate = currentDate.AddDays(-15);
        var dueDate = loanDate.AddDays(14);
        fixture.FixedDate = currentDate;
        
        db.Add(new Loan() { ISBN = "1", User = newLoan.UserName, Date = loanDate, DueDate = dueDate, Return = true });
        await db.SaveChangesAsync();

        var content = fixture.ConvertToRawJson(newLoan);
        
        var result = await fixture.client.PostAsync(_baseEndpoint, content);
        result.StatusCode.Should().Be(HttpStatusCode.Created);
    }
    
    [Fact]
    public async Task Post_Loan_Return_BadRequest_When_Book_Already_Loaned()
    {
        var fixture = new APIFixture();
        var newLoan = fixture.loanFaker.Generate();
    
        // Mark the book as already loaned out
        var db = fixture.CriarNovoContexto();
        db.Add(new Loan() { ISBN = newLoan.Isbn, User = "AnotherUser", Date = DateTime.Now, DueDate = DateTime.Now.AddDays(14) });
        await db.SaveChangesAsync();
    
        var content = fixture.ConvertToRawJson(newLoan);
    
        // Attempt to loan the same book again
        var result = await fixture.client.PostAsync(_baseEndpoint, content);
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var badRequestResponse = await fixture.GetTypedResponse<ValidationProblemDetails>(result);
        badRequestResponse.Status.Should().Be(400);
        badRequestResponse.Errors.Should().BeEquivalentTo(new Dictionary<string, string[]>
        {
            { "Entity", new[] { "Book is already loaned" } }
        });
    }
    
    [Fact]
    public async Task Post_Loan_Popular_Books_Enforce_Renewal_Limit()
    {
        var fixture = new APIFixture();
        var newLoan = fixture.loanFaker.Generate();
    
        // Mock rank for bestseller verification
        var bestsellerRank = fixture.faker.Random.Int(1, 15);
        fixture.bestSellersServiceMock.Setup(bs => bs.GetBookRankAsync(newLoan.Isbn)).ReturnsAsync(bestsellerRank);
    
        // Add 2 previous loan records for the same ISBN
        var db = fixture.CriarNovoContexto();
        db.Add(new Loan() { ISBN = newLoan.Isbn, User = newLoan.UserName, Date = DateTime.Now.AddDays(-30), DueDate = DateTime.Now.AddDays(-16), Return = true });
        db.Add(new Loan() { ISBN = newLoan.Isbn, User = newLoan.UserName, Date = DateTime.Now.AddDays(-15), DueDate = DateTime.Now.AddDays(-1), Return = true });
        await db.SaveChangesAsync();
    
        var content = fixture.ConvertToRawJson(newLoan);
    
        // Attempt to loan a book that is a bestseller and was already loaned twice
        var result = await fixture.client.PostAsync(_baseEndpoint, content);
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var badRequestResponse = await fixture.GetTypedResponse<ValidationProblemDetails>(result);
        badRequestResponse.Status.Should().Be(400);
        badRequestResponse.Errors.Should().BeEquivalentTo(new Dictionary<string, string[]>
        {
            { "Entity", new[] { "Top 15 Bestseller books cannot be loaned more than 2 times" } }
        });
    }

    [Fact]
    public async Task Post_Loan_Popular_Books_LoanedToOtherUsers_DoNotImpact_Renewal_Limit()
    {
        var fixture = new APIFixture();
        var newLoan = fixture.loanFaker.Generate();
    
        // Mock rank for bestseller verification
        var bestsellerRank = fixture.faker.Random.Int(1, 15);
        fixture.bestSellersServiceMock.Setup(bs => bs.GetBookRankAsync(newLoan.Isbn)).ReturnsAsync(bestsellerRank);
    
        // Add 2 previous loan records for the same ISBN
        var db = fixture.CriarNovoContexto();
        var otherUser = fixture.faker.Name.FirstName() + "_" + fixture.faker.Name.LastName();
        db.Add(new Loan() { ISBN = newLoan.Isbn, User = otherUser, Date = DateTime.Now.AddDays(-30), DueDate = DateTime.Now.AddDays(-16), Return = true });
        db.Add(new Loan() { ISBN = newLoan.Isbn, User = otherUser, Date = DateTime.Now.AddDays(-15), DueDate = DateTime.Now.AddDays(-1), Return = true });
        await db.SaveChangesAsync();
        
        var content = fixture.ConvertToRawJson(newLoan);
    
        // Attempt to loan a book that is a bestseller and was already loaned twice
        var result = await fixture.client.PostAsync(_baseEndpoint, content);
        result.StatusCode.Should().Be(HttpStatusCode.Created);
    }
    
}
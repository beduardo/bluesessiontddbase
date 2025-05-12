namespace LibraryAPI.Data;

public class Loan
{
    public int Id { get; set; }
    public string User { get; set; }
    public string ISBN { get; set; }
    public DateTime Date { get; set; }
    public DateTime DueDate { get; set; }
    public bool Return { get; set; }
}
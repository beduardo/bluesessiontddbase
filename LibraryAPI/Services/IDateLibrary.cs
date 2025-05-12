namespace LibraryAPI.Services;

public interface IDateLibrary
{
    DateTime GetCurrentDate();
}

public class DateLibrary : IDateLibrary
{
    public DateTime GetCurrentDate()
    {
        return DateTime.Now;
    }
}
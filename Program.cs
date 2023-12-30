
using Microsoft.WindowsAPICodePack.Shell;

namespace VideoDeleter;

class Program
{
    static void Main(string[] args)
    {
        int fileCount = 0;
        int movedCount = 0;
        int endingFileCount = 0;

        string directoryPath = @"C:\Users\seanh\Pictures\Video Projects\Stage\THESE_HAVE_BEEN_COMBINED_INTO_MPEGS\2023";
        if (!Path.Exists(directoryPath)) {
            Console.WriteLine($"Path {directoryPath} does not exist");
            return;
        }

        string vacationPath = Path.Combine(directoryPath, "Vacations");
        if(!Path.Exists(vacationPath))
        {
            Directory.CreateDirectory(vacationPath);
        }

        // Define the date ranges for deletion
        var vacationDates = new List<Tuple<DateTime, DateTime>>()
        {
            new(new(2023, 1, 30), new(2023, 2, 4)),
            new(new(2023, 4, 12), new(2023, 4, 15)),
            new(new(2023, 6, 9), new(2023, 6, 17)),
            new(new(2023, 8, 4), new(2023, 8, 7)),
            new(new(2023, 9, 24), new(2023, 10, 1)),
            new(new(2023, 11, 6), new(2023, 11, 10)),
        };

        foreach (string path in Directory.GetFiles(directoryPath))
        {
            fileCount++;

            // Get the media created date
            DateTime? mediaCreatedDate = GetMediaCreatedDate(path);

            // Check if the media created date is within the specified ranges
            if (mediaCreatedDate.HasValue)
            {
                foreach(var dateRange in vacationDates)
                {
                    if(mediaCreatedDate >= dateRange.Item1 && mediaCreatedDate <= dateRange.Item2)
                    {
                        // move the file
                        try
                        {
                            File.Move(path, Path.Combine(vacationPath, Path.GetFileName(path)));
                            Console.WriteLine($"Moved: {Path.GetFileName(path)} with date {mediaCreatedDate.Value}");
                            movedCount++;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error deleting {path}: {ex.Message}");
                        }
                    }
                }                
            }
        }

        Console.WriteLine($"Beginning File Count: {fileCount}");
        Console.WriteLine($"Deleted File Count: {movedCount}");

        
        foreach (string path in Directory.GetFiles(directoryPath))
        {
            endingFileCount++;
        }
        Console.WriteLine($"Ending File Count: {endingFileCount}");
    }

    static DateTime? GetMediaCreatedDate(string imagePath)
    {
        ShellObject shell = ShellObject.FromParsingName(imagePath);

        var data = shell.Properties.System.Media.DateEncoded;

        return data?.Value;   
    }
}

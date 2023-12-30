
using Microsoft.WindowsAPICodePack.Shell;

namespace VideoDeleter;

class Program
{
    static void Main(string[] args)
    {
        int year = 2023; // you still have to change vacation dates manually
        int fileCount = 0;
        int vacationCount = 0;
        int janCount = 0;
        int aprCount = 0;
        int julCount = 0;
        int octCount = 0;
        int noDateCount = 0;
        int couldNotMoveCount = 0;

        string directoryPath = @$"C:\Users\seanh\Pictures\Video Projects\Stage\THESE_HAVE_BEEN_COMBINED_INTO_MPEGS\{year}";
        if (!Path.Exists(directoryPath)) {
            Console.WriteLine($"Path {directoryPath} does not exist");
            return;
        }

        string vacationPath = Path.Combine(directoryPath, "Vacations");
        string janPath = Path.Combine(directoryPath, "Jan-Mar");
        string aprPath = Path.Combine(directoryPath, "Apr-Jun");
        string julPath = Path.Combine(directoryPath, "Jul-Sep");
        string octPath = Path.Combine(directoryPath, "Oct-Dec");
        if(!Path.Exists(vacationPath)) Directory.CreateDirectory(vacationPath);
        if(!Path.Exists(janPath)) Directory.CreateDirectory(janPath);
        if(!Path.Exists(aprPath)) Directory.CreateDirectory(aprPath);
        if(!Path.Exists(julPath)) Directory.CreateDirectory(julPath);
        if(!Path.Exists(octPath)) Directory.CreateDirectory(octPath);

        // Define the date ranges for deletion
        var vacationDates = new List<Tuple<DateTime, DateTime>>()
        {
            new(new(year, 1, 30), new(year, 2, 4)),
            new(new(year, 4, 12), new(year, 4, 15)),
            new(new(year, 6, 9), new(year, 6, 17)),
            new(new(year, 8, 4), new(year, 8, 7)),
            new(new(year, 9, 24), new(year, 10, 1)),
            new(new(year, 11, 6), new(year, 11, 10)),
        };

        foreach (string path in Directory.GetFiles(directoryPath))
        {
            fileCount++;
            var movedToVacation = false;

            DateTime? mediaCreatedDate = GetMediaCreatedDate(path);

            if (mediaCreatedDate.HasValue)
            {
                foreach(var dateRange in vacationDates)
                {
                    if(mediaCreatedDate >= dateRange.Item1 && mediaCreatedDate < dateRange.Item2.AddDays(1) && File.Exists(path))
                    {
                        MoveFile(vacationPath, path, mediaCreatedDate.Value);
                        vacationCount++;
                        movedToVacation = true;
                    }                    
                }      

                if(!movedToVacation && File.Exists(path))
                {
                    if(mediaCreatedDate >= new DateTime(year, 1, 1) && mediaCreatedDate < new DateTime(year, 4, 1))
                    {
                        MoveFile(janPath, path, mediaCreatedDate.Value);
                        janCount++;
                    }
                    else if(mediaCreatedDate >= new DateTime(year, 4, 1) && mediaCreatedDate <= new DateTime(year, 7, 1))
                    {
                        MoveFile(aprPath, path, mediaCreatedDate.Value);
                        aprCount++;
                    }
                    else if(mediaCreatedDate >= new DateTime(year, 7, 1) && mediaCreatedDate <= new DateTime(year, 10, 1))
                    {
                        MoveFile(julPath, path, mediaCreatedDate.Value);
                        julCount++;
                    }
                    else if(mediaCreatedDate >= new DateTime(year, 10, 1) && mediaCreatedDate <= new DateTime(year + 1, 1, 1))
                    {
                        MoveFile(octPath, path, mediaCreatedDate.Value);
                        octCount++;
                    }
                    else 
                    {
                        Console.WriteLine($"Could not move : {path} with date {mediaCreatedDate.Value}");
                        couldNotMoveCount++;
                    }
                }          
            }
            else 
            {
                Console.WriteLine($"Could not get file date for {path}");
                noDateCount++;
            }
        }

        Console.WriteLine($"File Count: {fileCount}");
        Console.WriteLine($"Vacation File Count: {vacationCount}");     
        Console.WriteLine($"Jan-Mar File Count: {janCount}");     
        Console.WriteLine($"Apr-Jun File Count: {aprCount}");     
        Console.WriteLine($"Jul-Sep File Count: {julCount}");     
        Console.WriteLine($"Oct-Dec File Count: {octCount}");     
        Console.WriteLine($"No Media Date File Count: {noDateCount}");     
        Console.WriteLine($"Could not move File Count: {couldNotMoveCount}");     
        Console.WriteLine($"Total Processed: {vacationCount + janCount + aprCount + julCount + octCount + noDateCount + couldNotMoveCount}");     
    }

    private static void MoveFile(string destPath, string path, DateTime mediaCreatedDate)
    {
        // move the file
        try
        {
            File.Move(path, Path.Combine(destPath, Path.GetFileName(path)));
            Console.WriteLine($"Moved: {Path.GetFileName(path)} with date {mediaCreatedDate}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error moving {path}: {ex.Message}");
        }
    }

    static DateTime? GetMediaCreatedDate(string imagePath)
    {
        ShellObject shell = ShellObject.FromParsingName(imagePath);

        var data = shell.Properties.System.Media.DateEncoded;

        return data?.Value;   
    }
}

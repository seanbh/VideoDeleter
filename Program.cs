
using System.Diagnostics;
using Microsoft.WindowsAPICodePack.Shell;

int year = 2024; // you still have to change vacation dates manually

// Define the date ranges for deletion
var vacationDates = new List<Tuple<DateTime, DateTime>>()
{
    new(new(year, 3, 16), new(year, 3, 23)),
    new(new(year, 6, 8), new(year, 6, 14)),
    new(new(year, 7, 21), new(year, 7, 26)),
    new(new(year, 10, 5), new(year, 10, 12))
};

int fileCount = 0;
int vacationCount = 0;
int janCount = 0;
int aprCount = 0;
int julCount = 0;
int octCount = 0;
int noDateCount = 0;
int couldNotMoveCount = 0;

string directoryPath = @$"C:\Users\seanh\Pictures\Video Projects\Stage\THESE_HAVE_BEEN_COMBINED_INTO_MPEGS\{year}";
if (!Path.Exists(directoryPath))
{
    Console.WriteLine($"Path {directoryPath} does not exist");
    return;
}

string vacationPath = Path.Combine(directoryPath, "Vacations");
string janPath = Path.Combine(directoryPath, "Jan-Mar");
string aprPath = Path.Combine(directoryPath, "Apr-Jun");
string julPath = Path.Combine(directoryPath, "Jul-Sep");
string octPath = Path.Combine(directoryPath, "Oct-Dec");
if (!Path.Exists(vacationPath)) Directory.CreateDirectory(vacationPath);
if (!Path.Exists(janPath)) Directory.CreateDirectory(janPath);
if (!Path.Exists(aprPath)) Directory.CreateDirectory(aprPath);
if (!Path.Exists(julPath)) Directory.CreateDirectory(julPath);
if (!Path.Exists(octPath)) Directory.CreateDirectory(octPath);

foreach (string path in Directory.GetFiles(directoryPath))
{
    fileCount++;
    var movedToVacation = false;

    DateTime? mediaCreatedDate = GetMediaCreatedDate(path);

    if (!mediaCreatedDate.HasValue)
    {
        mediaCreatedDate = GetDateUsingExif(path);
    }

    if (mediaCreatedDate.HasValue)
    {
        foreach (var dateRange in vacationDates)
        {
            if (mediaCreatedDate >= dateRange.Item1 && mediaCreatedDate < dateRange.Item2.AddDays(1) && File.Exists(path))
            {
                MoveFile(vacationPath, path, mediaCreatedDate.Value);
                vacationCount++;
                movedToVacation = true;
            }
        }

        if (!movedToVacation && File.Exists(path))
        {
            if (mediaCreatedDate >= new DateTime(year, 1, 1) && mediaCreatedDate < new DateTime(year, 4, 1))
            {
                MoveFile(janPath, path, mediaCreatedDate.Value);
                janCount++;
            }
            else if (mediaCreatedDate >= new DateTime(year, 4, 1) && mediaCreatedDate <= new DateTime(year, 7, 1))
            {
                MoveFile(aprPath, path, mediaCreatedDate.Value);
                aprCount++;
            }
            else if (mediaCreatedDate >= new DateTime(year, 7, 1) && mediaCreatedDate <= new DateTime(year, 10, 1))
            {
                MoveFile(julPath, path, mediaCreatedDate.Value);
                julCount++;
            }
            else if (mediaCreatedDate >= new DateTime(year, 10, 1) && mediaCreatedDate <= new DateTime(year + 1, 1, 1))
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

Console.WriteLine();
Console.WriteLine("Doing photos now...");

int picCount = 0;
int picMovedCount = 0;
int noPicDateCount = 0;

var picPath = @$"F:\Pictures\{year}";
foreach (string path in Directory.GetFiles(picPath))
{
    picCount++;

    DateTime? takenDate = GetDateTakenDate(path);

    if (!takenDate.HasValue)
    {
        takenDate = GetDateUsingExif(path);
    }

    if (takenDate.HasValue)
    {
        var month = takenDate.Value.ToString("MMMM");
        var monthPath = Path.Combine(picPath, month);

        if (!Path.Exists(month)) Directory.CreateDirectory(monthPath);

        File.Move(path, Path.Combine(monthPath, Path.GetFileName(path)));
        Console.WriteLine($"Moved: {Path.GetFileName(path)} with date {takenDate} to {monthPath}");

        picMovedCount++;
    }
    else
    {
        Console.WriteLine($"Could not get file date for {path}");
        noPicDateCount++;
    }
}

Console.WriteLine();
Console.WriteLine("Video Totals");
Console.WriteLine($"File Count: {fileCount}");
Console.WriteLine($"Vacation File Count: {vacationCount}");
Console.WriteLine($"Jan-Mar File Count: {janCount}");
Console.WriteLine($"Apr-Jun File Count: {aprCount}");
Console.WriteLine($"Jul-Sep File Count: {julCount}");
Console.WriteLine($"Oct-Dec File Count: {octCount}");
Console.WriteLine($"No Media Date File Count: {noDateCount}");
Console.WriteLine($"Could not move File Count: {couldNotMoveCount}");
Console.WriteLine($"Total Processed: {vacationCount + janCount + aprCount + julCount + octCount + noDateCount + couldNotMoveCount}");

Console.WriteLine();
Console.WriteLine("Photo Totals");
Console.WriteLine($"File Count: {picCount}");
Console.WriteLine($"Moved Count: {picMovedCount}");
Console.WriteLine($"Could Not Move Count: {noPicDateCount}");

void MoveFile(string destPath, string path, DateTime mediaCreatedDate)
{
    // move the file
    try
    {
        File.Move(path, Path.Combine(destPath, Path.GetFileName(path)));
        Console.WriteLine($"Moved: {Path.GetFileName(path)} with date {mediaCreatedDate} to {destPath}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error moving {path}: {ex.Message}");
    }
}

DateTime? GetMediaCreatedDate(string filePath)
{
    ShellObject shell = ShellObject.FromParsingName(filePath);

    var data = shell.Properties.System.Media.DateEncoded;

    return data?.Value;
}

DateTime? GetDateTakenDate(string filePath)
{
    ShellObject shell = ShellObject.FromParsingName(filePath);

    var data = shell.Properties.System.Photo.DateTaken;

    return data?.Value;
}

DateTime? GetDateUsingExif(string filePath)
{
    Console.WriteLine($"Could not get date, trying Exif...");

    string exifToolPath = @"C:\Program Files\ExifTool\exiftool-13.10_64\exiftool.exe";
    try
    {
        // Run ExifTool to update metadata
        Process process = new()
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = exifToolPath,
                Arguments = $"-overwrite_original \"-FileCreateDate<CreateDate\" \"{filePath}\"",
                RedirectStandardOutput = false,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        process.WaitForExit();

        Console.WriteLine($"Updated metadata for: {filePath}");

        var mediaCreatedDate = new FileInfo(filePath).CreationTime;

        return mediaCreatedDate;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error processing file {filePath}: {ex.Message}");
        return null;
    }
}






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

bool doVideosByQuarter = false;
bool doPhotos = false;
bool doVideosByMonth = false;
bool fixDatesOnly = true;

int fileCount = 0;
int vacationCount = 0;
int janCount = 0;
int aprCount = 0;
int julCount = 0;
int octCount = 0;
int noDateCount = 0;
int couldNotMoveCount = 0;

string videoDirectoryPath = @$"C:\Users\seanh\Pictures\Video Projects\Stage\Savannah\Sunday";
string photoDirectoryPath = $@"C:\Users\seanh\Pictures\Video Projects\Stage\Savannah\Sunday";

if (doVideosByQuarter)
{
    //Flatten(videoDirectoryPath);
    GroupByQuarterAndVacation(videoDirectoryPath);
}
else if (doVideosByMonth)
{
    //Flatten(videoDirectoryPath);
    GroupByMonth(videoDirectoryPath);
}

if (doPhotos)
{
    //Flatten(photoDirectoryPath);
    GroupByMonth(photoDirectoryPath);
}

if (fixDatesOnly)
{
    FixDates(photoDirectoryPath);
    FixDates(videoDirectoryPath);
}

void FixDates(string directoryPath)
{
    Console.WriteLine($"Fixing dates in {directoryPath}");

    foreach (string path in Directory.GetFiles(directoryPath))
    {
        try
        {
            DateTime? mediaCreatedDate = GetDateTakenDate(path);

            if (!mediaCreatedDate.HasValue)
            {
                mediaCreatedDate = GetMediaCreatedDate(path);
            }

            if (!mediaCreatedDate.HasValue)
            {
                mediaCreatedDate = GetDateUsingExif(path);
            }

            if (mediaCreatedDate.HasValue)
            {
                if (mediaCreatedDate.Value.Hour < 4)
                {
                    mediaCreatedDate = mediaCreatedDate.Value.Subtract(new TimeSpan(0, 4, 0, 0));
                    // Adjusting the date to the start of the day if it's before 5 AM
                    Console.WriteLine($"Subtracting 4 hours from {mediaCreatedDate.Value} for {Path.GetFileName(path)}");
                }
                File.SetCreationTime(path, mediaCreatedDate.Value);
                File.SetLastWriteTime(path, mediaCreatedDate.Value);
                Console.WriteLine($"Fixed date for: {Path.GetFileName(path)} to {mediaCreatedDate.Value}");
            }
            else
            {
                Console.WriteLine($"Could not get date for {path}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fixing date for {path}: {ex.Message}");
        }
    }
}


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

bool GroupByQuarterAndVacation(string directoryPath)
{
    if (!Path.Exists(directoryPath))
    {
        Console.WriteLine($"Path {directoryPath} does not exist");
        return false;
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

    var filesToMove = Directory.GetFiles(directoryPath).Length;
    foreach (string path in Directory.GetFiles(directoryPath))
    {
        try
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
        catch (Exception ex)
        {
            couldNotMoveCount++;
            Console.WriteLine($"Error moving {path}: {ex.Message}");
        }
        finally
        {
            var filesLeftToMove = filesToMove - fileCount;
            Console.WriteLine($"Files left to move: {filesLeftToMove}");
        }
    }

    Console.WriteLine();
    Console.WriteLine("By Quarter/Vcacation Totals");
    Console.WriteLine($"File Count: {fileCount}");
    Console.WriteLine($"Vacation File Count: {vacationCount}");
    Console.WriteLine($"Jan-Mar File Count: {janCount}");
    Console.WriteLine($"Apr-Jun File Count: {aprCount}");
    Console.WriteLine($"Jul-Sep File Count: {julCount}");
    Console.WriteLine($"Oct-Dec File Count: {octCount}");
    Console.WriteLine($"No Media Date File Count: {noDateCount}");
    Console.WriteLine($"Could not move File Count: {couldNotMoveCount}");
    Console.WriteLine($"Total Processed: {vacationCount + janCount + aprCount + julCount + octCount + noDateCount + couldNotMoveCount}");

    return true;
}

bool GroupByMonth(string directoryPath)
{
    Console.WriteLine();
    Console.WriteLine($"Grouping {directoryPath} by month.");

    int byMonthCount = 0;
    int byMonthMovedCount = 0;
    int byMonthNoDateCount = 0;

    // Rename all directories from MMMM to MM_MMMM
    foreach (string folder in Directory.GetDirectories(directoryPath))
    {
        DateTime folderDate;
        if (DateTime.TryParseExact(Path.GetFileName(folder), "MMMM", null, System.Globalization.DateTimeStyles.None, out folderDate))
        {
            var newFolderName = folderDate.ToString("MM_MMMM");
            var newFolderPath = Path.Combine(directoryPath, newFolderName);
            if (!Directory.Exists(newFolderPath))
            {
                Directory.Move(folder, newFolderPath);
                Console.WriteLine($"Renamed folder {folder} to {newFolderPath}");
            }
        }
    }

    var filesToMove = Directory.GetFiles(directoryPath).Length;
    foreach (string path in Directory.GetFiles(directoryPath))
    {
        try
        {
            byMonthCount++;

            DateTime? takenDate = GetDateTakenDate(path);

            if (!takenDate.HasValue)
            {
                takenDate = GetMediaCreatedDate(path);
            }

            if (!takenDate.HasValue)
            {
                takenDate = GetDateUsingExif(path);
            }

            if (takenDate.HasValue)
            {
                var month = takenDate.Value.ToString("MM_MMMM");
                var monthPath = Path.Combine(directoryPath, month);

                if (!Path.Exists(month)) Directory.CreateDirectory(monthPath);

                File.Move(path, Path.Combine(monthPath, Path.GetFileName(path)));
                Console.WriteLine($"Moved: {Path.GetFileName(path)} with date {takenDate} to {monthPath}");

                byMonthMovedCount++;

                var filesLeftToMove = filesToMove - byMonthMovedCount;
                Console.WriteLine($"Files left to move: {filesLeftToMove}");
            }
            else
            {
                Console.WriteLine($"Could not get file date for {path}");
                byMonthNoDateCount++;
            }
        }
        catch (Exception ex)
        {
            couldNotMoveCount++;
            Console.WriteLine($"Error moving {path}: {ex.Message}");
        }
    }


    Console.WriteLine();
    Console.WriteLine("By Month Totals");
    Console.WriteLine($"File Count: {byMonthCount}");
    Console.WriteLine($"Moved Count: {byMonthMovedCount}");
    Console.WriteLine($"Could Not Move Count: {byMonthNoDateCount}");

    return true;
}

void Flatten(string directoryPath)
{
    Console.WriteLine("Reversing...");

    var fileCount = 0;
    var fileMovedCount = 0;

    foreach (string folder in Directory.GetDirectories(directoryPath))
    {
        foreach (string path in Directory.GetFiles(folder))
        {
            try
            {
                fileCount++;
                File.Move(path, Path.Combine(directoryPath, Path.GetFileName(path)));
                Console.WriteLine($"Moved: {Path.GetFileName(path)} to {directoryPath}");
                fileMovedCount++;
            }
            catch (Exception ex)
            {
                couldNotMoveCount++;
                Console.WriteLine($"Error moving {path}: {ex.Message}");
            }
        }

        var left = Directory.GetFiles(folder).Length;
        if (left == 0)
        {
            Console.WriteLine($"Deleting folder {folder}");
            Directory.Delete(folder);
        }
    }

    Console.WriteLine($"Moved {fileMovedCount} of {fileCount} to base folder");
}

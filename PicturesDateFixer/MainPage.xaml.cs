using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Text;
using System;
using Microsoft.Maui.Controls.PlatformConfiguration;
using static System.Net.Mime.MediaTypeNames;
using Microsoft.Maui.Controls.Shapes;
using Plugin.Maui.Audio;
using ExifLibrary;
using System.Text.RegularExpressions;

namespace PicturesDateFixer;

public partial class MainPage : ContentPage
{
    // Fill which will be found
    List<DriveFile> foundFiles = new List<DriveFile>();
    // Exceptions
    List<ExceptionCustom> exceptList = new List<ExceptionCustom>();
    // Target Folders Names
    List<String> targetFolderList = new List<String>();

    public MainPage()
	{
		InitializeComponent();

        // Add the target in the SD Card
        //targetFolderList.Add("DCIM/Camera");
        targetFolderList.Add("DCIM/NBP_2019");
        //targetFolderList.Add("DCIM/Screenshots");
        //targetFolderList.Add("DCIM/Test EXIF");

        //targetFolderList.Add("WhatsApp/Media/Whatsapp Images");
    }

    // Click on the button to find SD Card Mount Name
    private void btnFindSDPath(object sender, EventArgs e)
	{
        // Drives which will be found -- /storage/emulated/0/Android/data is the internal storage
        List<DriveFile> targetDrives = new List<DriveFile>();

        // Get mounting points
        string[] logicalDrives = Environment.GetLogicalDrives();
        
        // Exceptions
        List<ExceptionCustom> exceptions = new List<ExceptionCustom>();

        // For each mounting points
        foreach (string drv in logicalDrives)
        {
            try
            {
                // Get the mounting points root directories
                string[] theDirectories = Directory.GetDirectories(drv, "*");
                // For each directory, match the searched one if found
                foreach (var aDirectory in theDirectories)
                    if (aDirectory.ToLower().EndsWith("/" + txtSearchFolder.Text.ToLower().Trim()))
                        targetDrives.Add(new DriveFile { Name = drv });
            }
            catch (Exception ex)
            {
                //if (drv.Contains("/storage/emulated/0/Android/data"))
                    exceptions.Add(new ExceptionCustom { Drive = drv, ExceptionMessage = ex.Message });
            }
        }

        // Feed the found drives in the collection view
        cvDrives.ItemsSource = targetDrives;
        lblFolder.Text = $"'{txtSearchFolder.Text}' found in {targetDrives.Count} drive{(targetDrives.Count > 1 ? "s":"")}";

        // Hide the SD Card searcher Part 2
        slLabels.IsVisible = false;
        btnSearchFolder.IsVisible = false;
        btnLog.IsVisible = false;
        btnDewitt.IsVisible = false;
        slOverwrite.IsVisible = false;
        prgBarEXIF.Progress = 0;
        prgBarEXIF.IsVisible = false;
        lblProgressEXIF.IsVisible = false;
        lblProgressEXIF.Text = "";
        chkUseDatePickerStart.IsVisible = false;
        dpPickerStart.IsVisible = false;
        chkUseDatePickerEnd.IsVisible = false;
        dpPickerEnd.IsVisible = false;
        prgBar.IsVisible = false;
        prgBar.Progress = 0;
        lblCurrentFolder.IsVisible = false;
        lblCurrentFolder.Text = "";
        lblProgress.IsVisible = false;
        lblProgress.Text = "";
    }

    // A Mount Name button has been clicked, display the form of the next part
    private void cvButton_Clicked(object sender, EventArgs e)
    {
        Button button = sender as Button;
        DriveFile selectedDrive = button.BindingContext as DriveFile;

        // Display the SD Card searcher Part 2
        lblRootFolder.Text = selectedDrive.Name;
        slLabels.IsVisible = true;
        btnSearchFolder.IsVisible = true;
        chkUseDatePickerStart.IsVisible = true;
        dpPickerStart.IsVisible = true;
        chkUseDatePickerEnd.IsVisible = true;
        dpPickerEnd.IsVisible = true;
        prgBar.IsVisible = true;
        lblCurrentFolder.IsVisible = true;
        lblProgress.IsVisible = true;

        // Clear the results
        cvResults.ItemsSource = null;
    }

    // Search the target folder based on the filters
    private async void btnSearchFolders_Clicked(object sender, EventArgs e)
    {
        // File extensions to search for
        string[] validExtensions = { ".jpg", ".jpeg", ".png", ".gif" };

        // See Android/MainActivity.cs for Write Authorization
        // Directory.CreateDirectory("/storage/3439-3532/BEB");
        // using FileStream outputStream = File.OpenWrite("/storage/3439-3532/BEB/test.txt");
        // using StreamWriter streamWriter = new StreamWriter(outputStream);
        // await streamWriter.WriteAsync("OKAY");

        // Files found will be stored there
        foundFiles = new List<DriveFile>();

        // Lists of Exception
        exceptList = new List<ExceptionCustom>();

        lblCurrentFolder.Text = "";
        prgBar.Progress = 0;
        lblProgress.Text = "";

        // For each Target Folder specified in the main
        foreach (string aTargetFolder in targetFolderList)
        {
            // Target folder
            string targetFolder = $"{lblRootFolder.Text}/{aTargetFolder}";
            try
            {
                // Get the files
                lblCurrentFolder.Text = aTargetFolder;
                //string[] theFiles = Directory.GetFiles("/storage/3439-3532/DCIM/Camera", "*");
                string[] theFiles = Directory.GetFiles(targetFolder, "*");

                int cnt = 0;
                // For each directory, match the searched one if found
                foreach (var aFile in theFiles)
                {

                    // Progress
                    cnt++;
                    lblProgress.Text = $"{cnt} / {theFiles.Count()} - {Math.Round((double)cnt / theFiles.Count()*100)}%";
                    await prgBar.ProgressTo((double)cnt / theFiles.Count(), 100, Easing.Linear);

                    // Only take the files in scope
                    if (validExtensions.Contains(new FileInfo(aFile).Extension.ToLower()))
                    {
                        DriveFile aDriveFile = new DriveFile();
                        // BASIC DATA ====================================================================
                        aDriveFile.FullPath = aFile;
                        aDriveFile.Name = System.IO.Path.GetFileName(aFile);
                        aDriveFile.Created = File.GetCreationTime(aFile);
                        aDriveFile.Modified = File.GetLastWriteTime(aFile);
                        aDriveFile.Extension = new FileInfo(aFile).Extension;

                        // EXIF DATA =====================================================================
                        // Use of Nuget : EXIFLibNet : GET
                        try
                        {
                            var exifFile = ImageFile.FromFile(aFile);
                            if (aDriveFile.Extension.ToLower() == ".png")
                            {
                                ExifProperty tagPng = exifFile.Properties.Get(ExifTag.PNGCreationTime);
                                if (tagPng != null)
                                {
                                    aDriveFile.DateTimeOriginal = DateTime.ParseExact(tagPng.Value.ToString(),
                                                                          "yyyy:MM:dd HH:mm:ss",
                                                                          System.Globalization.CultureInfo.InvariantCulture);
                                }
                            }
                            else if (aDriveFile.Extension.ToLower() == ".jpg" || aDriveFile.Extension.ToLower() == ".jpeg")
                            {
                                if (aDriveFile.Name == "2019_10_05_173730_JCS.JPG")
                                {
                                    int abc = 3;
                                }

                                ExifProperty tagJpg = exifFile.Properties.Get(ExifTag.DateTimeOriginal);
                                if (tagJpg != null)
                                {
                                    aDriveFile.DateTimeOriginal = (tagJpg as ExifDateTime).Value;
                                }
                            }
                            else if(aDriveFile.Extension.ToLower() == ".gif")
                            {
                                aDriveFile.DateTimeOriginal = null;
                            }
                            else
                            {
                                aDriveFile.DateTimeOriginal = null;
                            }
                        }
                        catch (Exception ex)
                        {
                            exceptList.Add(new ExceptionCustom { Drive = aDriveFile.FullPath, ExceptionMessage = ex.Message });
                        }

                        // FILTERS =====================================================================
                        // If the date time is null : to be set : add it to the list
                        if (aDriveFile.DateTimeOriginal == null)
                        {
                            foundFiles.Add(aDriveFile);
                        }
                        else
                        {
                            // Start Checked / End Checked : filter between
                            if (chkUseDatePickerStart.IsChecked & chkUseDatePickerEnd.IsChecked)
                            {
                                if (aDriveFile.DateTimeOriginal.Value.Date >= dpPickerStart.Date.Date & aDriveFile.DateTimeOriginal.Value.Date <= dpPickerEnd.Date.Date)
                                    foundFiles.Add(aDriveFile);
                            }
                            // Start Checked / End Unchecked : only filter on the Start Date
                            else if (chkUseDatePickerStart.IsChecked & !chkUseDatePickerEnd.IsChecked)
                            {
                                if (aDriveFile.DateTimeOriginal.Value.Date >= dpPickerStart.Date.Date)
                                    foundFiles.Add(aDriveFile);
                            }
                            // Start Unchecked / End Checked : only filter on the End Date
                            else if (!chkUseDatePickerStart.IsChecked & chkUseDatePickerEnd.IsChecked)
                            {
                                if (aDriveFile.DateTimeOriginal.Value.Date <= dpPickerEnd.Date.Date)
                                    foundFiles.Add(aDriveFile);
                            }
                            // Last case : Start Unchecked / End Unchecked : add the file
                            else
                                foundFiles.Add(aDriveFile);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                exceptList.Add(new ExceptionCustom { Drive = aTargetFolder, ExceptionMessage = ex.Message });
            }
        }

        // If files where found...
        if (foundFiles.Count > 0)
        {
            // Sort
            foundFiles = foundFiles.OrderBy(x => x.FullPath).ToList();

            // Feed the found drives in the collection view
            cvResults.Header = $"{foundFiles.Count} {(foundFiles.Count > 1 ? "files" : "file")} found - showing first 10";
            cvResults.ItemsSource = foundFiles.Take(10);

            // Show the Log and Dewitt Button
            btnLog.IsVisible = true;
            btnDewitt.IsVisible = true;
            slOverwrite.IsVisible = true;
            prgBarEXIF.IsVisible = true;
            lblProgressEXIF.IsVisible = true;

            // DO IT !
            // Install Nuget plugin.maui.audio and put the file in the Resource/Raw folder
            IAudioPlayer audioPlayerTick;
            audioPlayerTick = AudioManager.Current.CreatePlayer(await FileSystem.OpenAppPackageFileAsync("doit.mp3"));
            audioPlayerTick.Play();
        }
    }

    private async void btnLog_Clicked(object sender, EventArgs e)
    {
        // Log Part : Files
        string logFile = $"{lblRootFolder.Text}/Files.txt";
        if (File.Exists(logFile))
            File.Delete(logFile);

        using (var sw = new StreamWriter(logFile))
        {
            sw.WriteLine("FullPath|Name|Extension|Created|Modified|Original");
            foreach (DriveFile aDriveFile in foundFiles)
            {
                // Use Regex to identify file naming type
                // Classic Android File Naming : 20230207_180652.jpg

                // Whatsapp Naming : IMG-20180420-WA0001.jpg
                sw.WriteLine($"{aDriveFile.FullPath}|{aDriveFile.Name}|{aDriveFile.Extension}|{aDriveFile.CreatedStr}|{aDriveFile.ModifiedStr}|{aDriveFile.DateTimeOriginalStr}");
            }
        }

        // Log Part : Errors
        string logFileError = $"{lblRootFolder.Text}/Files_Errors.txt";
        if (File.Exists(logFileError))
            File.Delete(logFileError);

        using (var sw = new StreamWriter(logFileError))
        {
            sw.WriteLine("Realm|Error");
            foreach (ExceptionCustom anException in exceptList)
            {
                sw.WriteLine($"{anException.Drive}|{anException.ExceptionMessage}");
            }
        }

        await DisplayAlert("Export Done", $"File Export is done in {logFile}. Errors in {logFileError} ", "Kewl");
    }

    // Add EXIF Data
    private async void btnDewitt_Clicked(object sender, EventArgs e)
    {
        int cnt = 0;
        int cntTotal = 0;
        List<ExceptionCustom> successEXIFList = new List<ExceptionCustom>();
        List<ExceptionCustom> exceptEXIFList = new List<ExceptionCustom>();

        foreach (DriveFile aDriveFile in foundFiles)
        {
            // Progress
            cntTotal++;
            lblProgressEXIF.Text = $"{cntTotal} / {foundFiles.Count()} - {Math.Round((double)cntTotal / foundFiles.Count() * 100)}%";
            await prgBarEXIF.ProgressTo((double)cntTotal / foundFiles.Count(), 100, Easing.Linear);

            try
            {
                // Add EXIF Tags for images lacking one or if Overwrite is checked
                if (aDriveFile.DateTimeOriginal == null || chkOverwrite.IsChecked)
                {
                    // Part I : Use Regex to identify file naming type and extract date from file name
                    string fileWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(aDriveFile.Name);

                    DateTime? newDate = GetDateTimeFromFileName(fileWithoutExtension);
                    if (newDate == null)
                    {
                        // Unable to parse
                        exceptEXIFList.Add(new ExceptionCustom { Drive = aDriveFile.FullPath, ExceptionMessage = "Unable to parse."});
                    }
                    else
                    { 
                        // Part II : Add EXIF data. Method depends on the file type
                        if (aDriveFile.Extension.ToLower() == ".jpg" || aDriveFile.Extension.ToLower() == ".jpeg")
                        {
                            if (aDriveFile.Name == "2019_10_05_173730_JCS.JPG")
                            {
                                DateTime test = new DateTime(2023, 04, 02);
                                DateTime testout;
                                var fileJpg = ImageFile.FromFile(aDriveFile.FullPath);
                                fileJpg.Properties.Set(ExifTag.DateTimeOriginal, test);
                                fileJpg.Save(aDriveFile.FullPath);

                                var exifFile = ImageFile.FromFile(aDriveFile.FullPath);
                                ExifProperty tagJpg = exifFile.Properties.Get(ExifTag.DateTimeOriginal);
                                if (tagJpg != null)
                                {
                                    testout = (tagJpg as ExifDateTime).Value;
                                }
                            }
                            else
                            { 
                                var fileJpg = ImageFile.FromFile(aDriveFile.FullPath);
                                fileJpg.Properties.Set(ExifTag.DateTimeOriginal, newDate.Value);
                                fileJpg.Save(aDriveFile.FullPath);
                            }
                            cnt++;
                            successEXIFList.Add(new ExceptionCustom { Drive = aDriveFile.FullPath, ExceptionMessage = $"{aDriveFile.Extension.ToLower()} => {newDate.Value.ToString("yyyy/MM/dd HH:mm:ss")}"});
                        }
                        else if (aDriveFile.Extension.ToLower() == ".png")
                        {
                            // Do not work : ":"
                            //DateTime newDateTimePNG = new DateTime(2023, 03, 30);
                            //var filePng = ImageFile.FromFile(pathPNG);
                            //filePng.Properties.Set(ExifTag.PNGCreationTime, "2021:06:30 13:37:00");
                            //filePng.Save(pathPNG);
                            cnt++;
                            successEXIFList.Add(new ExceptionCustom { Drive = aDriveFile.FullPath, ExceptionMessage = $"{aDriveFile.Extension.ToLower()} => TO IMPLEMENT" });

                        }
                        else if (aDriveFile.Extension.ToLower() == ".gif")
                        {
                            cnt++;
                            successEXIFList.Add(new ExceptionCustom { Drive = aDriveFile.FullPath, ExceptionMessage = $"{aDriveFile.Extension.ToLower()} => TO IMPLEMENT" });
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                exceptEXIFList.Add(new ExceptionCustom { Drive = aDriveFile.FullPath, ExceptionMessage = ex.Message });
            }
        }

        // Log Part : Success
        string logFileSuccess = $"{lblRootFolder.Text}/Files_Success_EXIF.txt";
        if (File.Exists(logFileSuccess))
            File.Delete(logFileSuccess);

        using (var sw = new StreamWriter(logFileSuccess))
        {
            sw.WriteLine("Realm|Result");
            foreach (ExceptionCustom aSuccess in successEXIFList)
            {
                sw.WriteLine($"{aSuccess.Drive}|{aSuccess.ExceptionMessage}");
            }
        }

        // Log Part : Errors
        string logFileError = $"{lblRootFolder.Text}/Files_Errors_EXIF.txt";
        if (File.Exists(logFileError))
            File.Delete(logFileError);

        using (var sw = new StreamWriter(logFileError))
        {
            sw.WriteLine("Realm|Error");
            foreach (ExceptionCustom anException in exceptEXIFList)
            {
                sw.WriteLine($"{anException.Drive}|{anException.ExceptionMessage}");
            }
        }
        await DisplayAlert("EXIF Tagging Done", $"{cnt} out of {cntTotal} done. {exceptEXIFList.Count} errors loggued in {logFileError} ", "Kewl");
    }

    // Extract a Datetime based on the file name
    private static DateTime? GetDateTimeFromFileName(string fileWithoutExtension)
    {
        // Classic Android File Naming : 20230207_180652.jpg
        string reAndroid = @"(\d{4})(\d{2})(\d{2})_(\d{2})(\d{2})(\d{2})";
        // Whatsapp Naming : IMG-20180420-WA0001.jpg
        string reWhatsApp = @"IMG-(\d{4})(\d{2})(\d{2})-WA(\d{4})";
        // Screenshot : Screenshot_20170614-230912.png
        string reScreenshot = @"Screenshot_(\d{4})(\d{2})(\d{2})-(\d{2})(\d{2})(\d{2})";
        // NBP
        string reNBP = @"(\d{4})_(\d{2})_(\d{2})_(\d{2})(\d{2})(\d{2})_JCS";
        // Other : 20190217194435_1.jpg
        string reOther = @"(\d{4})(\d{2})(\d{2})(\d{2})(\d{2})(\d{2})_\d";

        Match mAndroid = new Regex(reAndroid).Match(fileWithoutExtension);
        Match mWhatsApp = new Regex(reWhatsApp).Match(fileWithoutExtension);
        Match mScreenshot = new Regex(reScreenshot).Match(fileWithoutExtension);
        Match mNBP = new Regex(reNBP).Match(fileWithoutExtension);
        Match mOther = new Regex(reOther).Match(fileWithoutExtension);

        DateTime? newDate;
        if (mAndroid.Success)
        {
            int y = int.Parse(mAndroid.Groups[1].Value);
            int M = int.Parse(mAndroid.Groups[2].Value);
            int d = int.Parse(mAndroid.Groups[3].Value);
            int h = int.Parse(mAndroid.Groups[4].Value);
            int m = int.Parse(mAndroid.Groups[5].Value);
            int s = int.Parse(mAndroid.Groups[6].Value);
            newDate = new DateTime(y, M, d, h, m, s);
        }
        else if (mWhatsApp.Success)
        {
            int y = int.Parse(mWhatsApp.Groups[1].Value);
            int M = int.Parse(mWhatsApp.Groups[2].Value);
            int d = int.Parse(mWhatsApp.Groups[3].Value);
            newDate = new DateTime(y, M, d, 12, 0, 0);
        }
        else if (mScreenshot.Success)
        {
            int y = int.Parse(mScreenshot.Groups[1].Value);
            int M = int.Parse(mScreenshot.Groups[2].Value);
            int d = int.Parse(mScreenshot.Groups[3].Value);
            int h = int.Parse(mScreenshot.Groups[4].Value);
            int m = int.Parse(mScreenshot.Groups[5].Value);
            int s = int.Parse(mScreenshot.Groups[6].Value);
            newDate = new DateTime(y, M, d, h, m, s);
        }
        else if (mNBP.Success)
        {
            int y = int.Parse(mNBP.Groups[1].Value);
            int M = int.Parse(mNBP.Groups[2].Value);
            int d = int.Parse(mNBP.Groups[3].Value);
            int h = int.Parse(mNBP.Groups[4].Value);
            int m = int.Parse(mNBP.Groups[5].Value);
            int s = int.Parse(mNBP.Groups[6].Value);
            newDate = new DateTime(y, M, d, h, m, s);
        }
        else if (mOther.Success)
        {
            int y = int.Parse(mOther.Groups[1].Value);
            int M = int.Parse(mOther.Groups[2].Value);
            int d = int.Parse(mOther.Groups[3].Value);
            int h = int.Parse(mOther.Groups[4].Value);
            int m = int.Parse(mOther.Groups[5].Value);
            int s = int.Parse(mOther.Groups[6].Value);
            newDate = new DateTime(y, M, d, h, m, s);
        }
        else
        {
            newDate = null;
        }

        return newDate;
    }
}


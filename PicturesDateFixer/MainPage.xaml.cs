using Plugin.Maui.Audio;
using ExifLibrary;
using System.Text.RegularExpressions;
using System.Diagnostics;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;

namespace PicturesDateFixer;

public partial class MainPage : ContentPage
{
    // README : Features
    #region README
    // STATUS BAR COLOR ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    // Change status bar color ? Add the CommunityToolkit.Maui Nuget
    // Then add it in the MauiProgram.cs :
    // using CommunityToolkit.Maui;

    // public static MauiApp CreateMauiApp()
    // {
    //     var builder = MauiApp.CreateBuilder();
    //     builder
    //         .UseMauiApp<App>()
    //         // Initialize the .NET MAUI Community Toolkit by adding the below line of code
    //         .UseMauiCommunityToolkit()
    //         [...]
    //
    // Then add it to the MainPage.xaml : in the <ContentPage> tag, reference the xaml as mct :
    // xmlns:mct="clr-namespace:CommunityToolkit.Maui.Behaviors;assembly=CommunityToolkit.Maui"
    //
    // Then add the piece of code :
    // <ContentPage.Behaviors>
    //      <mct:StatusBarBehavior StatusBarColor = "pink" />
    // </ContentPage.Behaviors>

    // READ / WRITE TO SD CARD ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    // Add in the Android Manifest : <uses-permission android:name="android.permission.MANAGE_EXTERNAL_STORAGE"/>
    //
    // In the Platforms / Android / MainActivity, add the OnCreate :
    //
    // public class MainActivity : MauiAppCompatActivity
    // {
    //     // Added this v
    //     protected override void OnCreate(Bundle savedInstanceState)
    //     {
    //         if (!Android.OS.Environment.IsExternalStorageManager)
    //         {
    //             Intent intent = new Intent();
    //             intent.SetAction(Android.Provider.Settings.ActionManageAppAllFilesAccessPermission);
    //             Android.Net.Uri uri = Android.Net.Uri.FromParts("package", this.PackageName, null);
    //             intent.SetData(uri);
    //             StartActivity(intent);
    //         }
    //         base.OnCreate(savedInstanceState);
    //     }
    // }

    // EXIF MANIPULATION ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    // For reading purposes : MetadataExtractor 2.7.2 NuGet
    // For writing purposes : ExifLibNet 2.1.4 NuGet
    #endregion

    // Fill which will be found
    List<DriveFile> foundFiles = new List<DriveFile>();
    // Found files to display in the list
    int cvFoundFilesDisplay = 10;

    // Logs : exceptions of findings, sucess of EXIFing, exceptions of EXIFing
    List<LogLine> exceptList = new List<LogLine>();
    List<LogLine> successEXIFList = new List<LogLine>();
    List<LogLine> exceptEXIFList = new List<LogLine>();

    // Target Folders Names
    List<PrefFolder> targetFolderList;

    public MainPage()
    {
        InitializeComponent();
    }

    // Initialisation : get preferences, create the log directory
    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Load the previously stored prefs
        // Add the items in targetFolder
        targetFolderList = new List<PrefFolder>();
        foreach (string aPref in Preferences.Get("prefTargetFolderList", "").Split(";"))
            targetFolderList.Add(new PrefFolder { Name = aPref, IsChecked = false });

        // Update the collection view preferences
        RefreshCvPrefs();

        // Create the Log Folder
        System.IO.Directory.CreateDirectory("/storage/3439-3532/PicturesDateFixer");
    }

    // EXIF READING AND EXTRACTION, AND REGEX ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    #region EXIF READING AND EXTRACTION, AND REGEX
    // This function dump to the Debug window all the EXIF Data of a file
    private void DebugAllEXIFforFile(string FilePath)
    {
        Debug.WriteLine("-------------------------------");
        Debug.WriteLine($"GOING THROUGH ALL TAGS FOR {FilePath}...");

        IEnumerable<MetadataExtractor.Directory> directories = ImageMetadataReader.ReadMetadata(FilePath);
        foreach (var directory in directories)
            foreach (var tag in directory.Tags)
                Debug.WriteLine($"{directory.Name} - {tag.Name} = {tag.Description}");

        // Finished
        Debug.WriteLine("DONE !");
    }

    // This function will read EXIF File Data.
    private DriveFile ReadEXIFforFile(string FilePath)
    {
        DriveFile aDriveFile = new DriveFile();
        // BASIC DATA ====================================================================
        aDriveFile.FullPath = FilePath;
        aDriveFile.Name = System.IO.Path.GetFileName(FilePath);
        aDriveFile.Folder = new FileInfo(FilePath).DirectoryName;
        aDriveFile.Created = File.GetCreationTime(FilePath);
        aDriveFile.Modified = File.GetLastWriteTime(FilePath);
        aDriveFile.Extension = new FileInfo(FilePath).Extension;

        // EXIF DATA =====================================================================
        // Use of Nuget : EXIFLibNet : GET
        try
        {
            if (aDriveFile.Extension.ToLower() == ".jpg" || aDriveFile.Extension.ToLower() == ".jpeg")
            {
                // Dump all EXIF Found in the Debug
                DebugAllEXIFforFile(FilePath);

                IEnumerable<MetadataExtractor.Directory> directories = ImageMetadataReader.ReadMetadata(FilePath);
                var subIfdDirectory = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();
                var theDate = subIfdDirectory?.GetDescription(ExifDirectoryBase.TagDateTimeOriginal);
                aDriveFile.DateTimeOriginal = (theDate == null ? null : DateTime.ParseExact(theDate, "yyyy:MM:dd HH:mm:ss", null));
            }
            else if (aDriveFile.Extension.ToLower() == ".png")
            {
                // Dump all EXIF Found in the Debug
                DebugAllEXIFforFile(FilePath);

                IEnumerable<MetadataExtractor.Directory> directories = ImageMetadataReader.ReadMetadata(FilePath);
                var subIfdDirectory = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();
                var theDate = subIfdDirectory?.GetDescription(ExifDirectoryBase.TagDateTimeOriginal);
                aDriveFile.DateTimeOriginal = (theDate == null ? null : DateTime.ParseExact(theDate, "yyyy:MM:dd HH:mm:ss", null));
            }
            else if (aDriveFile.Extension.ToLower() == ".gif")
            {
                // Field does not exist : it's the Modified Field
                aDriveFile.DateTimeOriginal = aDriveFile.Modified;
            }
            else if (aDriveFile.Extension.ToLower() == ".mp4")
            {
                // To Implement.
                /*
                 * The extended file properties can be obtained by using Folder.GetDetailsOf() method.
                 * The Media Created Date can be retrieved using a property id of 177.
                */
                
                aDriveFile.DateTimeOriginal = null;
            }
            else
            {
                aDriveFile.DateTimeOriginal = null;
            }
        }
        catch (Exception ex)
        {
            exceptList.Add(new LogLine { Drive = aDriveFile.FullPath, ExceptionMessage = ex.Message });
        }

        return aDriveFile;
    }

    // This function will write EXIF File Data.
    // If it comes from dewitt, it'll take the options (overwrite & for real checkbox)
    // other wise, it'll be default values : Overwrite & For Real
    private void WriteEXIFforFile(DriveFile aDriveFile, bool fromDewitt)
    {
        try
        {
            string AdditionnalLog = "";

            // Add EXIF Tags for images lacking one or if Overwrite is checked
            if ((fromDewitt && (aDriveFile.DateTimeOriginal == null || chkOverwrite.IsChecked)) || !fromDewitt)
            {
                // Part I : Use Regex to identify file naming type and extract date from file name
                string fileWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(aDriveFile.Name);

                DateTime? newDate = GetDateTimeFromFileName(fileWithoutExtension);
                if (newDate == null)
                {
                    // Unable to parse
                    exceptEXIFList.Add(new LogLine { Drive = aDriveFile.FullPath, ExceptionMessage = "Unable to parse." });
                }
                else
                {
                    // Part II : Add EXIF data. Method depends on the file type
                    if (aDriveFile.Extension.ToLower() == ".jpg" || aDriveFile.Extension.ToLower() == ".jpeg")
                    {
                        if ((fromDewitt & chkForReal.IsChecked) || !fromDewitt)
                        {
                            var fileJpg = ImageFile.FromFile(aDriveFile.FullPath);
                            fileJpg.Properties.Set(ExifTag.DateTimeOriginal, newDate.Value);
                            fileJpg.Save(aDriveFile.FullPath);
                        }
                        else
                        {
                            AdditionnalLog = " - SKIPPED";
                        }
                    }
                    else if (aDriveFile.Extension.ToLower() == ".png")
                    {
                        if ((fromDewitt & chkForReal.IsChecked) || !fromDewitt)
                        {
                            // To Implement
                            AdditionnalLog = " - TO IMPLEMENT";
                            var filePng = ImageFile.FromFile(aDriveFile.FullPath);
                            filePng.Properties.Set(ExifTag.PNGCreationTime,newDate.Value);
                            filePng.Properties.Set(ExifTag.DateTimeOriginal, newDate.Value);
                            filePng.Properties.Set(ExifTag.DateTime, newDate.Value);
                            filePng.Save(aDriveFile.FullPath);
                            //File.SetLastWriteTime(aDriveFile.FullPath, newDate.Value);
                        }
                        else
                        {
                            AdditionnalLog = " - SKIPPED";
                        }
                    }
                    else if (aDriveFile.Extension.ToLower() == ".gif")
                    {
                        if ((fromDewitt & chkForReal.IsChecked) || !fromDewitt)
                        {
                            // No metadata : it's the Modified Date (LastWriteTime). Note that it will also overwrite Created Date
                            File.SetLastWriteTime(aDriveFile.FullPath, newDate.Value);
                        }
                        else
                        {
                            AdditionnalLog = " - SKIPPED";
                        }
                    }
                    else if (aDriveFile.Extension.ToLower() == ".mp4")
                    {
                        if ((fromDewitt & chkForReal.IsChecked) || !fromDewitt)
                        {
                            // To Implement.
                            /*
                             * The extended file properties can be obtained by using Folder.GetDetailsOf() method.
                             * The Media Created Date can be retrieved using a property id of 177.
                            */
                            AdditionnalLog = " - TO IMPLEMENT";
                        }
                        else
                        {
                            AdditionnalLog = " - SKIPPED";
                        }
                    }

                    // Log the result
                    successEXIFList.Add(new LogLine
                    {
                        Drive = aDriveFile.FullPath,
                        ExceptionMessage = $"from {(aDriveFile.DateTimeOriginal == null ? "null" : aDriveFile.DateTimeOriginal.Value.ToString("yyyy/MM/dd HH:mm:ss"))} " +
                                           $"to => {newDate.Value.ToString("yyyy/MM/dd HH:mm:ss")}" +
                                           $"{AdditionnalLog}"
                    });
                }
            }
        }
        catch (Exception ex)
        {
            exceptEXIFList.Add(new LogLine { Drive = aDriveFile.FullPath, ExceptionMessage = ex.Message });
        }
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
        // Video Capture : VideoCapture_20220502-192015.jpg
        string reVideoCapture = @"VideoCapture_(\d{4})(\d{2})(\d{2})-(\d{2})(\d{2})(\d{2})";
        // Other : 20190217194435_1.jpg
        string reOther = @"(\d{4})(\d{2})(\d{2})(\d{2})(\d{2})(\d{2})_\d";

        Match mAndroid = new Regex(reAndroid).Match(fileWithoutExtension);
        Match mWhatsApp = new Regex(reWhatsApp).Match(fileWithoutExtension);
        Match mScreenshot = new Regex(reScreenshot).Match(fileWithoutExtension);
        Match mNBP = new Regex(reNBP).Match(fileWithoutExtension);
        Match mVideoCapture = new Regex(reVideoCapture).Match(fileWithoutExtension);
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
        else if (mVideoCapture.Success)
        {
            int y = int.Parse(mVideoCapture.Groups[1].Value);
            int M = int.Parse(mVideoCapture.Groups[2].Value);
            int d = int.Parse(mVideoCapture.Groups[3].Value);
            int h = int.Parse(mVideoCapture.Groups[4].Value);
            int m = int.Parse(mVideoCapture.Groups[5].Value);
            int s = int.Parse(mVideoCapture.Groups[6].Value);
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
    #endregion

    // MENU ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    #region MENU
    private void btnMenuMain_Clicked(object sender, EventArgs e)
    {
        btnMenuMain.Source = "main.png";
        bxMenuMain.Color = Color.FromArgb("#9a0089");
        btnMenuSettings.Source = "settings_disabled.png";
        bxMenuSettings.Color = Color.FromArgb("#00FFFFFF");
        btnMenuResults.Source = "results_disabled.png";
        bxMenuResults.Color = Color.FromArgb("#00FFFFFF");

        btnMenuResults.IsVisible = (foundFiles.Count() != 0);
        lblMenuResults.IsVisible = (foundFiles.Count() != 0);

        slMain.IsVisible = true;
        slSettings.IsVisible = false;
        slResults.IsVisible = false;
    }

    private void btnMenuSettings_Clicked(object sender, EventArgs e)
    {
        btnMenuMain.Source = "main_disabled.png";
        bxMenuMain.Color = Color.FromArgb("#00FFFFFF");
        btnMenuSettings.Source = "settings.png";
        bxMenuSettings.Color = Color.FromArgb("#9a0089");
        btnMenuResults.Source = "results_disabled.png";
        bxMenuResults.Color = Color.FromArgb("#00FFFFFF");

        btnMenuResults.IsVisible = (foundFiles.Count() != 0);
        lblMenuResults.IsVisible = (foundFiles.Count() != 0);

        slMain.IsVisible = false;
        slSettings.IsVisible = true;
        slResults.IsVisible = false;
    }

    private void btnMenuResults_Clicked(object sender, EventArgs e)
    {
        btnMenuMain.Source = "main_disabled.png";
        bxMenuMain.Color = Color.FromArgb("#00FFFFFF");
        btnMenuSettings.Source = "settings_disabled.png";
        bxMenuSettings.Color = Color.FromArgb("#00FFFFFF");
        btnMenuResults.Source = "results.png";
        bxMenuResults.Color = Color.FromArgb("#9a0089");

        btnMenuResults.IsVisible = (foundFiles.Count() != 0);
        lblMenuResults.IsVisible = (foundFiles.Count() != 0);

        slMain.IsVisible = false;
        slSettings.IsVisible = false;
        slResults.IsVisible = true;
    }
    #endregion

    // UI PARTS - SHOW / HIDE + OTHER UI
    #region UI PARTS - SHOW / HIDE + OTHER UI
    // Part II : Search File Progress Bars
    private void ShowPartII(bool show)
    {
        lblProgressFolderValue.IsVisible = show;
        prgBarFolder.IsVisible = show;
        lblProgressFolderPercent.IsVisible = show;
        lblCurrentFolder.IsVisible = show;

        lblProgressFileValue.IsVisible = show;
        prgBar.IsVisible = show;
        lblProgressFilePercent.IsVisible = show;
        lblCurrentFile.IsVisible = show;

        // If hidden, clear the values
        if (!show)
        {
            lblProgressFolderValue.Text = "";
            prgBarFolder.Progress = 0;
            lblProgressFolderPercent.Text = "";
            lblCurrentFolder.Text = "";

            lblProgressFileValue.Text = "";
            prgBar.Progress = 0;
            lblProgressFilePercent.Text = "";
            lblCurrentFile.Text = "";
        }
    }

    // PART III : Logs, Results, DEWITT button and Options 
    private void ShowPartIII(bool show)
    {
        btnLog.IsVisible = show;

        btnDewitt.IsVisible = show;

        slOverwrite.IsVisible = show;
        chkForReal.IsVisible = show;

        btnMenuResults.IsVisible = show;
        lblMenuResults.IsVisible = show;

        if (!show)
            cvResults.ItemsSource = null;
    }

    // PART IV : DEWITT progress bar
    private void ShowPartIV(bool show)
    {
        lblProgressEXIFValue.IsVisible = show;
        prgBarEXIF.IsVisible = show;
        lblProgressEXIFPercent.IsVisible = show;
        lblCurrentEXIF.IsVisible = show;

        // If hidden, clear the values
        if (!show)
        {
            lblProgressEXIFValue.Text = "";
            prgBarEXIF.Progress = 0;
            lblProgressEXIFPercent.Text = "";
            lblCurrentEXIF.Text = "";
        }
    }

    // Search box changed
    private void schFile_TextChanged(object sender, TextChangedEventArgs e)
    {
        // Refresh the view
        List<DriveFile> filteredList = foundFiles.Where(f => f.Name.ToLower().Contains(e.NewTextValue.ToLower())).ToList();
        cvResults.ItemsSource = filteredList.Take(cvFoundFilesDisplay);
        cvResults.Header = $"Filtered {filteredList.Count} {(filteredList.Count > 1 ? "files" : "file")} out of {foundFiles.Count} - showing first {cvFoundFilesDisplay} :";
    }

    // a cvResults Button has been clicked
    private async void btnExportEXIFData_Clicked(object sender, EventArgs e)
    {
        Button theButton = (Button)sender;
        DriveFile theFile = (DriveFile)theButton.BindingContext;

        // Log Part : EXIF Data
        string logFileEXIF = $"{lblRootFolder.Text}/PicturesDateFixer/EXIF_Data.txt";
        if (File.Exists(logFileEXIF))
            File.Delete(logFileEXIF);

        using (var sw = new StreamWriter(logFileEXIF))
        {
            sw.WriteLine($"EXIF Tags for {theFile.FullPath} :");

            IEnumerable<MetadataExtractor.Directory> directories = ImageMetadataReader.ReadMetadata(theFile.FullPath);
            foreach (var directory in directories)
                foreach (var tag in directory.Tags)
                    sw.WriteLine($"{directory.Name} - {tag.Name} = {tag.Description}");
        }
        // Done
        await DisplayAlert("Export Done", $"File Export is done in {logFileEXIF}.", "Kewl");

    }
    #endregion

    // PREFS HANDLING ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    #region PREFS HANDLING
    // New Pref textbox changed : enable or not the btnAdd
    private void txtAddPref_TextChanged(object sender, TextChangedEventArgs e)
    {
        btnAddPref.IsEnabled = (e.NewTextValue.Trim().Length > 0);
    }

    // Read the Prefs and update the collection view
    private void RefreshCvPrefs()
    {
        cvPreferences.ItemsSource = targetFolderList.OrderBy(x => x.Name).ToList();
    }

    // Add Preference Clicked
    private async void btnAddPref_Clicked(object sender, EventArgs e)
    {
        // Close Keyboard
        txtAddPref.IsEnabled = false;
        txtAddPref.IsEnabled = true;

        string toAdd = txtAddPref.Text.Trim();
        if (toAdd.Length > 0)
        {
            // If it exists
            if (targetFolderList.FirstOrDefault(x => x.Name == toAdd) != null)
            {
                await DisplayAlert("Error", $"'{toAdd}' already exists.", "OK");
            }
            else
            {
                targetFolderList.Add(new PrefFolder { Name = toAdd, IsChecked = false });
                txtAddPref.Text = "";

                // Save the Prefs
                string strPrefs = string.Join(";", targetFolderList.Select(x => x.Name).ToList());
                Preferences.Set("prefTargetFolderList", strPrefs);

                // Refresh the Prefs
                RefreshCvPrefs();

                await DisplayAlert("Done", $"'{toAdd}' added.", "OK");
            }
        }
        else
        {
            await DisplayAlert("Error", $"Preference cannot be empty", "OK");
        }
    }

    // Default Prefs
    private async void btnDefaultPref_Clicked(object sender, EventArgs e)
    {
        // Add the target in the SD Card (second part is default)
        Preferences.Set("prefTargetFolderList", "" +
                                            "DCIM/Camera;" +
                                            "DCIM/NBP_2019;" +
                                            "DCIM/Screenshots;" +
                                            "DCIM/Test EXIF;" +
                                            "WhatsApp/Media/Whatsapp Images" +
                                            "");

        // Add the items in targetFolder
        targetFolderList = new List<PrefFolder>();
        foreach (string aPref in Preferences.Get("prefTargetFolderList", "").Split(";"))
            targetFolderList.Add(new PrefFolder { Name = aPref, IsChecked = false });

        // Refresh the prefs
        RefreshCvPrefs();

        await DisplayAlert("Default Prefs", $"Preferences have been set to Default.", "OK");
    }

    // Reset Prefs
    private async void btnResetPrefs_Clicked(object sender, EventArgs e)
    {
        // Set the pref to nothing
        Preferences.Set("prefTargetFolderList", "");

        // Clear the items in targetFolder
        targetFolderList = new List<PrefFolder>();

        // Refresh the prefs
        RefreshCvPrefs();

        await DisplayAlert("Prefs Reset", $"Preferences have been reset.", "OK");
    }


    // A PrefFolder delete button is clicked : : remove it and recalculate the targetFolderList List
    private void btnDeletePref_Clicked(object sender, EventArgs e)
    {
        // Get the imagebutton clicked
        ImageButton theImageButton = (ImageButton)sender;
        string theFolder = theImageButton.ClassId;

        // Remove it from the preferences
        targetFolderList.Remove((PrefFolder)theImageButton.BindingContext);

        // Save the Prefs
        string strPrefs = string.Join(";", targetFolderList.Select(x => x.Name).ToList());
        Preferences.Set("prefTargetFolderList", strPrefs);

        // Refresh the Prefs
        RefreshCvPrefs();
    }

    // Tick everything
    private void btnSelectAllPrefs_Clicked(object sender, EventArgs e)
    {
        foreach (PrefFolder aPref in targetFolderList)
            aPref.IsChecked = true;

        // Refresh the prefs
        RefreshCvPrefs();
    }

    // Untick everything
    private void btnSelectNoPrefs_Clicked(object sender, EventArgs e)
    {
        foreach (PrefFolder aPref in targetFolderList)
            aPref.IsChecked = false;

        // Refresh the prefs
        RefreshCvPrefs();
    }

    // Invert the selection
    private void btnSelectInversePrefs_Clicked(object sender, EventArgs e)
    {
        foreach (PrefFolder aPref in targetFolderList)
            aPref.IsChecked = !aPref.IsChecked;

        // Refresh the prefs
        RefreshCvPrefs();
    }

    // A checkbox has been ticked/unticked
    private void chkPref_Changed(object sender, CheckedChangedEventArgs e)
    {
        PrefFolder theFolder;
        if (sender is CheckBox)
        {
            CheckBox theCheckbox = (CheckBox)sender;
            theFolder = (PrefFolder)theCheckbox.BindingContext;
        }
        else
        {
            Label theLabel = (Label)sender;
            theFolder = (PrefFolder)theLabel.BindingContext;
        }

        PrefFolder aPref = targetFolderList.FirstOrDefault(x => x.Name == theFolder.Name);
        if (aPref != null)
            aPref.IsChecked = theFolder.IsChecked;
    }
    #endregion

    // FIND SD CARD NAME ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    #region FIND SD CARD NAME
    private void btnFindSDPath(object sender, EventArgs e)
    {
        // Drives which will be found -- /storage/emulated/0/Android/data is the internal storage
        List<DriveFile> targetDrives = new List<DriveFile>();

        // Get mounting points
        string[] logicalDrives = Environment.GetLogicalDrives();

        // Exceptions
        List<LogLine> exceptions = new List<LogLine>();

        // For each mounting points
        foreach (string drv in logicalDrives)
        {
            try
            {
                // Get the mounting points root directories
                string[] theDirectories = System.IO.Directory.GetDirectories(drv, "*");
                // For each directory, match the searched one if found
                foreach (var aDirectory in theDirectories)
                    if (aDirectory.ToLower().EndsWith("/" + txtSearchFolder.Text.ToLower().Trim()))
                        targetDrives.Add(new DriveFile { Name = drv });
            }
            catch (Exception ex)
            {
                //if (drv.Contains("/storage/emulated/0/Android/data"))
                exceptions.Add(new LogLine { Drive = drv, ExceptionMessage = ex.Message });
            }
        }

        // Feed the found drives in the collection view
        cvDrives.ItemsSource = targetDrives;
        lblFolder.Text = $"'{txtSearchFolder.Text}' found in {targetDrives.Count} drive{(targetDrives.Count > 1 ? "s" : "")}";

        // Hide the rest of the forms
        ShowPartII(false);
        ShowPartIII(false);
        ShowPartIV(false);
    }
    #endregion

    // FIND FOLDERS CONTENTS ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    #region FIND FOLDERS CONTENTS
    // A Mount Name button has been clicked, search it
    private async void cvButton_Clicked(object sender, EventArgs e)
    {
        Button button = sender as Button;
        DriveFile selectedDrive = button.BindingContext as DriveFile;

        // Assign the invisible value
        lblRootFolder.Text = selectedDrive.Name;

        // File extensions to search for
        //string[] validExtensions = { ".jpg", ".jpeg", ".png", ".gif" };
        List<string> validExtensions = new List<string>();
        if (chkJpg.IsChecked) validExtensions.Add(lblchkJpg.Text);
        if (chkJpeg.IsChecked) validExtensions.Add(lblchkJpeg.Text);
        if (chkPng.IsChecked) validExtensions.Add(lblchkPng.Text);
        if (chkGif.IsChecked) validExtensions.Add(lblchkGif.Text);
        if (chkMp4.IsChecked) validExtensions.Add(lblchkMp4.Text);

        // See Android/MainActivity.cs for Write Authorization
        // Directory.CreateDirectory("/storage/3439-3532/BEB");
        // using FileStream outputStream = File.OpenWrite("/storage/3439-3532/BEB/test.txt");
        // using StreamWriter streamWriter = new StreamWriter(outputStream);
        // await streamWriter.WriteAsync("OKAY");

        // Files found will be stored there
        foundFiles = new List<DriveFile>();

        // New batch of Exception
        exceptList = new List<LogLine>();

        List<PrefFolder> targetFolderListChecked = targetFolderList.Where(x => x.IsChecked).ToList();
        int cntFolderChecked = targetFolderListChecked.Count();

        ShowPartII(cntFolderChecked > 0);
        ShowPartIII(false);
        ShowPartIV(false);

        if (targetFolderListChecked.Count == 0)
            await DisplayAlert("Alert", "No target folders set. Check in the Settings Panel to add some.", "OK");

        int cntFolder = 0;

        // For each Target Folder specified in the main
        foreach (PrefFolder aTargetFolder in targetFolderListChecked)
        {
            if (aTargetFolder.IsChecked)
            {
                // Target folder
                string targetFolder = $"{lblRootFolder.Text}/{aTargetFolder.Name}";

                // Progress Folder
                cntFolder++;
                lblCurrentFolder.Text = aTargetFolder.Name;
                lblProgressFolderValue.Text = $"{cntFolder} / {cntFolderChecked}";
                lblProgressFolderPercent.Text = $"{Math.Round((double)cntFolder / cntFolderChecked * 100)}%";
                await prgBarFolder.ProgressTo((double)cntFolder / cntFolderChecked, 10, Easing.Linear);

                try
                {
                    // Get the files
                    //string[] theFiles = Directory.GetFiles("/storage/3439-3532/DCIM/Camera", "*");
                    string[] theFiles = System.IO.Directory.GetFiles(targetFolder, "*");

                    int cnt = 0;
                    // For each directory, match the searched one if found
                    foreach (var aFile in theFiles)
                    {
                        // Progress File
                        cnt++;
                        lblCurrentFile.Text = System.IO.Path.GetFileName(aFile);
                        lblProgressFileValue.Text = $"{cnt} / {theFiles.Count()}";
                        lblProgressFilePercent.Text = $"{Math.Round((double)cnt / theFiles.Count() * 100)}%";
                        await prgBar.ProgressTo((double)cnt / theFiles.Count(), 10, Easing.Linear);

                        // Only take the files in scope
                        if (validExtensions.Contains(new FileInfo(aFile).Extension.ToLower()))
                        {
                            // Get the file with its data
                            DriveFile aDriveFile = ReadEXIFforFile(aFile);

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
                    exceptList.Add(new LogLine { Drive = aTargetFolder.Name, ExceptionMessage = ex.Message });
                }
            }
        }

        // If files where found...
        if (foundFiles.Count > 0)
        {
            // Sort
            foundFiles = foundFiles.OrderBy(x => x.FullPath).ToList();

            // Feed the found drives in the collection view
            cvResults.Header = $"{foundFiles.Count} {(foundFiles.Count > 1 ? "files" : "file")} found - showing first {cvFoundFilesDisplay} :";
            cvResults.ItemsSource = foundFiles.Take(cvFoundFilesDisplay);

            // Show the rest of the form
            ShowPartIII(true);

            // DO IT !
            // Install Nuget plugin.maui.audio and put the file in the Resource/Raw folder
            IAudioPlayer audioPlayerTick;
            audioPlayerTick = AudioManager.Current.CreatePlayer(await FileSystem.OpenAppPackageFileAsync("doit.mp3"));
            audioPlayerTick.Play();
        }
    }

    // Log the results of the search
    private async void btnLog_Clicked(object sender, EventArgs e)
    {
        // Log Part : Files
        string logFile = $"{lblRootFolder.Text}/PicturesDateFixer/Files.txt";
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
        string logFileError = $"{lblRootFolder.Text}/PicturesDateFixer/Files_Errors.txt";
        if (File.Exists(logFileError))
            File.Delete(logFileError);

        using (var sw = new StreamWriter(logFileError))
        {
            sw.WriteLine("Realm|Error");
            foreach (LogLine anException in exceptList)
            {
                sw.WriteLine($"{anException.Drive}|{anException.ExceptionMessage}");
            }
        }

        await DisplayAlert("Export Done", $"File Export is done in {logFile}.\r\n Errors in {logFileError} ", "Kewl");
    }
    #endregion

    // ADD EXIF DATA ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    #region ADD EXIF DATA
    // Batch version, with Dewitt button clicked
    private async void btnDewitt_Clicked(object sender, EventArgs e)
    {
        var result = await DisplayAlert("DEWITT ?", "DEWITT ?", "DEWITT !!", "Nah.");
        if (result)
        {
            ShowPartIV(true);

            // Start new bash of logs
            List<LogLine> successEXIFList = new List<LogLine>();
            List<LogLine> exceptEXIFList = new List<LogLine>();

            int cnt = 0;
            int cntTotal = 0;

            if (foundFiles.Where(x => x.Extension != ".mp4").Any())
                await DisplayAlert("Warning : mp4 files", "mp4 where found but are not supported (yet) :\r\n\r\n" +
                                   "A way to get/set the 'Media Created' field of the files have to be found.", "OK");

            if (foundFiles.Where(x => x.Extension != ".png").Any())
                await DisplayAlert("Warning : png files", "png where found but are not fully supported (yet) :\r\n\r\n" +
                                   "ExifTag.PNGCreationTime can be fetched/set, but is not the field used to group pictures.\r\n\r\n" +
                                   "Modifying it in the Gallery App to a specific date then parsing all the ExifTags yields no result.\r\n\r\n" +
                                   "Loading the modified png file in exif.tools shows the updated value as DateTimeOriginal though, like the jpg, " +
                                   "but ExifTag.DateTimeOriginal yields nothing.", "OK");

            foreach (DriveFile aDriveFile in foundFiles)
            {
                // Progress EXIF
                cntTotal++;
                lblProgressEXIFValue.Text = $"{cntTotal} / {foundFiles.Count()}";
                lblProgressEXIFPercent.Text = $"{Math.Round((double)cntTotal / foundFiles.Count() * 100)}%";
                lblCurrentEXIF.Text = $"{aDriveFile.Folder}";
                await prgBarEXIF.ProgressTo((double)cntTotal / foundFiles.Count(), 10, Easing.Linear);

                // Do the Work
                WriteEXIFforFile(aDriveFile, true);
                cnt++;
            }

            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            // Log Part : Success
            string logFileSuccess = $"{lblRootFolder.Text}/PicturesDateFixer/Files_Success_EXIF.txt";
            if (File.Exists(logFileSuccess))
                File.Delete(logFileSuccess);

            using (var sw = new StreamWriter(logFileSuccess))
            {
                sw.WriteLine("Realm|Result");
                foreach (LogLine aSuccess in successEXIFList)
                {
                    sw.WriteLine($"{aSuccess.Drive}|{aSuccess.ExceptionMessage}");
                }
            }

            // Log Part : Errors
            string logFileError = $"{lblRootFolder.Text}/PicturesDateFixer/Files_Errors_EXIF.txt";
            if (File.Exists(logFileError))
                File.Delete(logFileError);

            using (var sw = new StreamWriter(logFileError))
            {
                sw.WriteLine("Realm|Error");
                foreach (LogLine anException in exceptEXIFList)
                {
                    sw.WriteLine($"{anException.Drive}|{anException.ExceptionMessage}");
                }
            }

            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            // Display Alert
            if (chkForReal.IsChecked)
            {
                await DisplayAlert("EXIF Tagging Done", $"{cnt} out of {cntTotal} done.\r\n" +
                                                        $"\r\n" +
                                                        $"Results loggued in :\r\n" +
                                                        $"{logFileSuccess}\r\n" +
                                                        $"\r\n" +
                                                        $"{exceptEXIFList.Count} errors loggued in :\r\n" +
                                                        $"{logFileError}\r\n" +
                                                        $"\r\n" +
                                                        $"Changes may take some time to reflect. Reboot device if needed.", "Kewl");
            }
            else
            {
                await DisplayAlert("EXIF *SIMULATION* Done", $"{cnt} out of {cntTotal} done.\r\n" +
                                                           $"\r\n" +
                                                           $"Results loggued in :\r\n" +
                                                           $"{logFileSuccess}\r\n" +
                                                           $"\r\n" +
                                                           $"Possible errors ({exceptEXIFList.Count} ) loggued in :\r\n" +
                                                           $"{logFileError}\r\n" +
                                                           $"\r\n" +
                                                           $"Review the results and relaunch with the 'Do It For Real !' checkbox ticked", "Kewl");
            }
        }
    }
 
    // From the Result page, just for one file
    private void btnRewriteEXIFforFile_Clicked(object sender, EventArgs e)
    {
        // Fetch the clicked file
        Button theButton = (Button)sender;
        DriveFile aDriveFile = (DriveFile)theButton.BindingContext;

        // Do the Work for on file
        WriteEXIFforFile(aDriveFile, false);

        // refresh the item in foundFiles
        foreach (DriveFile aFile in foundFiles)
        {
            if (aFile == aDriveFile)
            {
                // Fetch the new file info
                DriveFile newInfo = ReadEXIFforFile(aFile.FullPath);
                aFile.DateTimeOriginal = newInfo.DateTimeOriginal;
            }
        }

        // Update the cvResults
        schFile.Text = "";
        cvResults.Header = $"{foundFiles.Count} {(foundFiles.Count > 1 ? "files" : "file")} found - showing first {cvFoundFilesDisplay} :";
        cvResults.ItemsSource = foundFiles.Take(cvFoundFilesDisplay);
    }
    #endregion
}


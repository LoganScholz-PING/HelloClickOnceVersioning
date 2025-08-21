using System.IO;
using System.Text;
using System.Windows;
using System.Xml;
using System.Xml.Serialization;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace HelloClickOnceVersioning
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        // example path:
        // C:\Users\logans\AppData\Local\Apps\2.0\4M4RDP3A.HRP\5KQ5NXXO.1XO\info..tion_0000000000000000_0001.0000_583f9c8cb22d0c4f\
        // exe names for testing:
        // InforJSONDataExposer.exe
        // LengthGaugeFrontEnd_InforWPF.exe


        string clickOnceAdditionalAppDataPath = "\\Apps\\2.0\\";
        string programStartExeName = "LengthGaugeFrontEnd_InforWPF.exe";

        public class AllAppInfo
        {
            public IList<AppInfo> clickOnceAppInfo = new List<AppInfo>();
        }


        public class AppInfo
        {
            public string exeName = "";
            public string exePath = "";
            public string directoryPathNoExe = "";
            public string exeManifestFilePath = "";
            public string clickOnceVersion = "";
        }


        private void btnACTION1_Click(object sender, RoutedEventArgs e)
        {
            string exeName = txtEXENAME.Text.Trim();

            if(string.IsNullOrEmpty(exeName))
            {
                txtOUTPUT.Clear();
                txtOUTPUT.AppendText("ERROR: Please enter a valid ClickOnce .exe file name into the text box");
                return;
            }

            string folder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            folder += clickOnceAdditionalAppDataPath;
            AllAppInfo allApps = GetAllClickOnceAppData(folder, exeName);
            PrintAllRelevantInfo(allApps);
        }


        public void PrintAllRelevantInfo(AllAppInfo allApps)
        {
            txtOUTPUT.Clear();
            txtOUTPUT.AppendText($" ========== PRINTING ALL INFO FOR {allApps.clickOnceAppInfo[0].exeName} ========== \r\n");

            int count = 1;
            foreach (AppInfo appInfo in allApps.clickOnceAppInfo)
            {
                txtOUTPUT.AppendText($" - Installation #{count}:\r\n");
                txtOUTPUT.AppendText($" - .exe Name: {appInfo.exeName}\r\n");
                txtOUTPUT.AppendText($" - .exe Path: {appInfo.exePath}\r\n");
                txtOUTPUT.AppendText($" - Directory Path (No .exe): {appInfo.directoryPathNoExe}\r\n");
                txtOUTPUT.AppendText($" - .exe Manifest File: {appInfo.exeManifestFilePath}\r\n");
                txtOUTPUT.AppendText($" - Application Version: {appInfo.clickOnceVersion}\r\n\r\n");
                count++;
            }
        }


        public MainWindow()
        {
            InitializeComponent();

            txtEXENAME.Text = programStartExeName;
        }


        private AllAppInfo GetAllClickOnceAppData(string folder, string _exeName)
        {
            AllAppInfo allAppInfo = new AllAppInfo();

            IList<string> exeLocations = FindAllExeLocationsInAppDataFolder(folder, _exeName);
            IList<string> cleanDirs = cleanUpDirectoryNames(exeLocations, _exeName);
            IList<string> exeManifestLocations = FindAllExeManifestPaths(exeLocations, _exeName);
            IList<string> exeManifestVersions = ReadAllAppVersions(exeManifestLocations);

            allAppInfo = PackageAllClickOnceInstalledApplicationData(_exeName, exeLocations, cleanDirs, exeManifestLocations, exeManifestVersions);

            return allAppInfo;
        }


        private IList<string> FindAllExeLocationsInAppDataFolder(string folder, string exe)
        {
            string[] exeLocations = Directory.GetFiles(folder, exe, SearchOption.AllDirectories);
            IList<string> cleanExeLocations = new List<string>(); // only folders that contain both .exe and .exe.manifest make the cut

            foreach (string exeLocation in exeLocations)
            {
                string exeLocationJustFolder = exeLocation.Replace(exe, "");
                string[] tmp = Directory.GetFiles(exeLocationJustFolder, exe + ".manifest");
                if (tmp.Length > 0)
                {
                    cleanExeLocations.Add(exeLocation);
                }
            }
            return cleanExeLocations;
        }


        private IList<string> cleanUpDirectoryNames(IList<string> exePaths, string _exeName)
        {
            IList<string> cleanDirs = new List<string>();
            foreach (string path in exePaths)
            {
                cleanDirs.Add(path.Replace(_exeName, ""));
            }
            return cleanDirs;
        }


        private IList<string> FindAllExeManifestPaths(IList<string> exeLocations, string _exeName)
        {
            IList<string> allManifestPaths = new List<string>();

            foreach (string dir in exeLocations)
            {
                allManifestPaths.Add(dir + ".manifest");
            }

            return allManifestPaths;
        }


        private IList<string> ReadAllAppVersions(IList<string> exeManifestLocations)
        {
            IList<string> allAppVersions = new List<string>();

            // Example string to match:
            // <asmv1:assemblyIdentity name="InforJSONDataExposer.exe" version="1.0.0.55" publicKeyToken="0000000000000000" language="en" processorArchitecture="msil" type="win32" />
            Regex majorPattern = new Regex(@"<asmv1:assemblyIdentity (.)+>");

            // Example string to match:
            // version="1.0.0.55"
            Regex minorPattern = new Regex(@"version=""(.*?)""");

            foreach (string manifestPath in exeManifestLocations)
            {
                string manifestText = System.IO.File.ReadAllText(manifestPath);

                MatchCollection majorFindings = majorPattern.Matches(manifestText);

                if(majorFindings.Count > 0)
                {
                    string majorFinding = majorFindings[0].Value;
                    MatchCollection minorFindings = minorPattern.Matches(majorFinding);

                    if (minorFindings.Count > 0)
                    {
                        string final = minorFindings[0].Value.Replace("version=\"", "");
                        final = final.Replace("\"", "");
                        allAppVersions.Add(final);
                    }
                    else
                    {
                        // Error: malformed or empty exe.manifest file?
                    }
                }
                else
                {
                    // Error: malformed or empty exe.manifest file?
                }
            }
            return allAppVersions;
        }


        public AllAppInfo PackageAllClickOnceInstalledApplicationData(string exeName, IList<string> exeLocations, IList<string> cleanDirs, IList<string> exeManifestLocations, IList<string> exeManifestVersions)
        {
            AllAppInfo allAppInfo = new AllAppInfo();

            // TODO: Possibly get the .Count of all the parameters and make sure they are the same?
            for (int i = 0; i < exeLocations.Count; i++)
            {
                AppInfo appInfo = new AppInfo();

                appInfo.exeName = exeName;
                appInfo.exePath = exeLocations[i];
                appInfo.directoryPathNoExe = cleanDirs[i];
                appInfo.exeManifestFilePath = exeManifestLocations[i];
                appInfo.clickOnceVersion = exeManifestVersions[i];

                allAppInfo.clickOnceAppInfo.Add(appInfo);
            }
            return allAppInfo;
        }


    }
}
using System;
using System.IO;
using System.IO.Compression;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using CmlLib.Core;
using CmlLib.Core.Auth;
using System.Windows.Threading;
using System.Threading;
using System.Net;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Threading.Tasks;
using CmlLib.Core.Version;

namespace LauncherWPF
{
    public partial class MainWindow : Window
    {
        string appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string nickname;
        int MaxRamMb;

        public MainWindow()
        {
            TextBox1.Text = Settings.Default.User;
            SliderRam.Value = Convert.ToDouble(Settings.Default.Ram);
            LabelRam.Text = Settings.Default.Ram;
            CheckBox1.IsChecked = Settings.Default.Close;

            InitializeComponent();

            DispatcherTimer timer = new DispatcherTimer();
            timer.Tick += new EventHandler(changeImage);
            timer.Interval = new TimeSpan(0, 0, 5);
            timer.Start();

            if (Directory.Exists(appdata + @"\.minecraft\versions\1.16.5-forge-36.2.34") == true) VersionText.Text = "1.16.5 Forge (36.2.34)";
            else VersionText.Text = "1.16.5";
        }

        //Main Button
        private async void BtnPlay_MouseDown(object sender, RoutedEventArgs e)
        {
            UIEnabled(false);

            Settings.Default.User = TextBox1.Text;
            Settings.Default.Ram = LabelRam.Text;
            Settings.Default.Save();
            MaxRamMb = Convert.ToInt32(Settings.Default.Ram);
            nickname = Settings.Default.User;
            var session = MSession.GetOfflineSession(nickname);

            ServicePointManager.DefaultConnectionLimit = 256;

            var path = new MinecraftPath();

            var launcher = new CMLauncher(path);

            ProgressBar1.Value = 0;
            launcher.FileChanged += (e) =>
            {
                Label1.Content = (e.FileKind.ToString(), e.FileName, e.ProgressedFileCount.ToString(), e.TotalFileCount.ToString());
                if (ProgressBar1.Value < 327) ProgressBar1.Value++;
                else ProgressBar1.Value = 0;
            };

            var versions = await launcher.GetAllVersionsAsync();

            string version;
            if (VersionText.Text == "1.16.5") version = "1.16.5";
            else version = "1.16.5-forge-36.2.34";

            if (Directory.Exists(appdata + @"\.minecraft\versions\1.16.5-forge-36.2.34") == false)
            {
                await FilesDownloadZIP(@"https://drive.google.com/uc?export=download&confirm=no_antivirus&id=1yHZLjLcMrTohpeZAEmdAIwpwyfzPIdAK", 
                    appdata + @"\.minecraft\versions\1.16.5-forge-36.2.34.zip", 
                    appdata + @"\.minecraft\versions\", 
                    "Installing Forge 36.2.34");

                await FilesDownloadZIP(@"https://drive.google.com/uc?export=download&confirm=no_antivirus&id=1RPGMu9VZX0GQnbMKx5ySP4Qo8ff3NefP",
                    appdata + @"\.minecraft\libraries.zip",
                    appdata + @"\.minecraft\", 
                    "Installing libraries");
            }

            if (File.Exists(appdata + @"\.minecraft\usercache.json")) File.Copy(appdata + @"\.minecraft\usercache.json", appdata + @"\.minecraft\launcher_profiles.json", true);

            int Swidth, Sheight;
            bool FullScr;
            if(CheckBox2.IsChecked == true)
            {
                FullScr = false;
                Swidth = 800;
                Sheight = 450;
            }
            else
            {
                FullScr = true;
                Swidth = 1920;
                Sheight = 1080;
                
            }
            var launchOption = new MLaunchOption
            {
                MaximumRamMb = MaxRamMb,
                Session = session,
                FullScreen = FullScr,
                ScreenWidth = Swidth,
                ScreenHeight = Sheight
            };

            var process = await launcher.CreateProcessAsync(version, launchOption);
            process.Start();
            if (CheckBox1.IsChecked == true) 
            {
                Settings.Default.Close = true;
                Settings.Default.Save();
                this.Close();
            }
            else 
            {
                Settings.Default.Close = false;
                UIEnabled(true);
                Label1.Content = "";
                ProgressBar1.Value = 0;
                Settings.Default.Save();
            }
        }



        //Methods  
        private void changeImage(object sender, EventArgs e)
        {
            if (FlipView1.SelectedIndex > 2) FlipView1.SelectedIndex = 0;
            else FlipView1.SelectedIndex++;
        }
        private async Task FilesDownloadZIP(string link, string zipPath, string extractPath, string name)
        {
            UIEnabled(false);
            WebClient webClient = new WebClient();
            Thread thread = new Thread(() => {
                webClient.DownloadFileAsync(new Uri(link), zipPath);
                while (webClient.IsBusy == true)
                {
                    int i = 0;
                    i += 100;
                    Thread.Sleep(i);
                    this.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate ()
                    {
                        if (ProgressBar1.Value < 327) ProgressBar1.Value++;
                        else ProgressBar1.Value = 0;
                        UIEnabled(false);
                        if (Label1.Content == "") Label1.Content = name;
                    }));
                }
                this.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate () { UIEnabled(true); }));
                ZipFile.ExtractToDirectory(zipPath, extractPath, true);
                File.Delete(zipPath);
                this.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate ()
                {
                    ProgressBar1.Value = 0;
                    Label1.Content = "";
                }));
            });
            thread.Start();
        }
        private void UIEnabled(bool b)
        {
            BtnPlay.IsEnabled = b;
            TextBox1.IsEnabled = b;
            CheckBox1.IsEnabled = b;
            CheckBox2.IsEnabled = b;
            MenuButton.IsEnabled = b;
            BtnRam.IsEnabled = b;
            VersionBtn.IsEnabled = b;
            if (b == false)
            {
                RamBar.Visibility = Visibility.Hidden;
                MenuBar.Visibility = Visibility.Hidden;
                BtnRamEnabled.Visibility = Visibility.Visible;
                BtnPlayEnabled.Visibility = Visibility.Visible;
                MenuButtonRectangle1.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom("#FFD0D0D0");
                MenuButtonRectangle2.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom("#FFD0D0D0");
                MenuButtonRectangle3.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom("#FFD0D0D0");
            }
            else
            {
                MenuButtonRectangle1.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom("#FFFFFFFF");
                MenuButtonRectangle2.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom("#FFFFFFFF");
                MenuButtonRectangle3.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom("#FFFFFFFF");
                BtnRamEnabled.Visibility = Visibility.Hidden;
                BtnPlayEnabled.Visibility = Visibility.Hidden;
            }
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Settings.Default.Close = (bool)CheckBox1.IsChecked;
            Settings.Default.Ram = LabelRam.Text;
            Settings.Default.Save();
            Settings.Default.Upgrade();
        }



        //UI Useful 
        private void Btn1_Click(object sender, RoutedEventArgs e)
        {
            var patch = Path.Combine(appdata, ".minecraft");
            Process.Start("explorer.exe", patch);
        }
        private async void Btn2_Click(object sender, RoutedEventArgs e)
        {
            if (Directory.Exists(appdata + @"\.minecraft\mods") == false) Directory.CreateDirectory(appdata + @"\.minecraft\mods");
            await FilesDownloadZIP(@"https://drive.google.com/uc?export=download&confirm=no_antivirus&id=1-DSe6ngUrZL8Hvd68CciKK-5_cHcfFUW",
                appdata + @"\.minecraft\mods.zip",
                appdata + @"\.minecraft\",
                "Installing modifications");
        }
        private void Btn3_Click(object sender, RoutedEventArgs e) => Process.Start(new ProcessStartInfo("https://drive.google.com/drive/folders/17gxJBkI9vvVO7pwR1oNu-sWXuyGWZ3L9") { UseShellExecute = true });
        private void Btn4_Click(object sender, RoutedEventArgs e) => Process.Start(new ProcessStartInfo("https://discord.com/") { UseShellExecute = true });
        
        private void VersionBtn_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (VerisonBar.IsVisible == true) VerisonBar.Visibility = Visibility.Hidden;
            else VerisonBar.Visibility = Visibility.Visible;

            if (Directory.Exists(appdata + @"\.minecraft\versions\1.16.5-forge-36.2.34") == true)
            {
                VersionLabel1.IsEnabled = true;
                VersionLabel1.Foreground = (SolidColorBrush)new BrushConverter().ConvertFrom("#FFFFFFFF");
            }
            else
            {
                VersionLabel1.IsEnabled = false;
                VersionLabel1.Foreground = (SolidColorBrush)new BrushConverter().ConvertFrom("#FF565656");
            }
            MenuBar.Visibility = Visibility.Hidden;
            RamBar.Visibility = Visibility.Hidden;
        }



        //UI Animations etc 
        private void BtnRam_MouseEnter(object sender, MouseEventArgs e) => BtnRamRectangle.Stroke = (SolidColorBrush)new BrushConverter().ConvertFrom("#7EB4EA");
        private void BtnRam_MouseLeave(object sender, MouseEventArgs e) => BtnRamRectangle.Stroke = (SolidColorBrush)new BrushConverter().ConvertFrom("#FF202225");

        private void VersionBtn_MouseEnter(object sender, MouseEventArgs e) => BtnVersionRectangle.Stroke = (SolidColorBrush)new BrushConverter().ConvertFrom("#7EB4EA");
        private void VersionBtn_MouseLeave(object sender, MouseEventArgs e) => BtnVersionRectangle.Stroke = (SolidColorBrush)new BrushConverter().ConvertFrom("#FF202225");


        private void VersionLabel1_MouseEnter(object sender, MouseEventArgs e) => VersionLabel1.Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#33000000");
        private void VersionLabel1_MouseLeave(object sender, MouseEventArgs e) => VersionLabel1.Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#00000000");
        private void VersionLabel1_MouseDown(object sender, MouseButtonEventArgs e)
        {
            VersionText.Text = "Forge 1.16.5 (36.2.34)";
            VerisonBar.Visibility = Visibility.Hidden;
        }
        private void Versionlabel2_MouseEnter(object sender, MouseEventArgs e) => Versionlabel2.Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#33000000");
        private void Versionlabel2_MouseLeave(object sender, MouseEventArgs e) => Versionlabel2.Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#00000000");
        private void Versionlabel2_MouseDown(object sender, MouseButtonEventArgs e)
        {
            VersionText.Text = "1.16.5";
            VerisonBar.Visibility = Visibility.Hidden;
        }

        private void PlayLabel_MouseEnter(object sender, MouseEventArgs e) => BtnPlayRectangle.Stroke = (SolidColorBrush)new BrushConverter().ConvertFrom("#7EB4EA");
        private void PlayLabel_MouseLeave(object sender, MouseEventArgs e) => BtnPlayRectangle.Stroke = (SolidColorBrush)new BrushConverter().ConvertFrom("#FF1E714C");

        private void BtnPlay_MouseEnter(object sender, MouseEventArgs e) => BtnPlayRectangle.Stroke = (SolidColorBrush)new BrushConverter().ConvertFrom("#7EB4EA");
        private void BtnPlay_MouseLeave(object sender, MouseEventArgs e) => BtnPlayRectangle.Stroke = (SolidColorBrush)new BrushConverter().ConvertFrom("#FF1E714C");

        private void BtnClose_MouseDown(object sender, MouseButtonEventArgs e) => this.Close();
        private void BtnClose_MouseEnter(object sender, MouseEventArgs e) => BtnClose.Source = new BitmapImage(new Uri(@"/Resources/CloseRed.png", UriKind.Relative));
        private void BtnClose_MouseLeave(object sender, MouseEventArgs e) => BtnClose.Source = new BitmapImage(new Uri(@"/Resources/Close.png", UriKind.Relative));

        private void BtnMinimize_MouseDown(object sender, MouseButtonEventArgs e) => this.WindowState = WindowState.Minimized;
        private void BtnMinimize_MouseEnter(object sender, MouseEventArgs e) => BtnMinimize.Source = new BitmapImage(new Uri(@"/Resources/MinimizeGray.png", UriKind.Relative));
        private void BtnMinimize_MouseLeave(object sender, MouseEventArgs e) => BtnMinimize.Source = new BitmapImage(new Uri(@"/Resources/Minimize.png", UriKind.Relative));

        private void Bar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (MenuBar.IsVisible == true) MenuBar.Visibility = Visibility.Hidden;
            else MenuBar.Visibility = Visibility.Visible;
            VerisonBar.Visibility = Visibility.Hidden;
            RamBar.Visibility = Visibility.Hidden;
        }
        private void Bar_MouseEnter(object sender, MouseEventArgs e)
        {
            MenuButtonRectangle1.Stroke = (SolidColorBrush)new BrushConverter().ConvertFrom("#7EB4EA");
            MenuButtonRectangle2.Stroke = (SolidColorBrush)new BrushConverter().ConvertFrom("#7EB4EA");
            MenuButtonRectangle3.Stroke = (SolidColorBrush)new BrushConverter().ConvertFrom("#7EB4EA");
        }
        private void Bar_MouseLeave(object sender, MouseEventArgs e)
        {
            MenuButtonRectangle1.Stroke = (SolidColorBrush)new BrushConverter().ConvertFrom("#FF202225");
            MenuButtonRectangle2.Stroke = (SolidColorBrush)new BrushConverter().ConvertFrom("#FF202225");
            MenuButtonRectangle3.Stroke = (SolidColorBrush)new BrushConverter().ConvertFrom("#FF202225");
        }

        private void TextBox1_GotFocus(object sender, RoutedEventArgs e)
        {
            if (TextBox1.Text != Settings.Default.User || TextBox1.Text == "NickName") TextBox1.Text = "";
        }
        private void TextBox1_LostFocus(object sender, RoutedEventArgs e)
        {
            if (TextBox1.Text == "") TextBox1.Text = "NickName";
            Settings.Default.User = TextBox1.Text;
            Settings.Default.Save();
            nickname = Settings.Default.User;
        }

        private void BtnRam_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (RamBar.IsVisible == true) RamBar.Visibility = Visibility.Hidden;
            else RamBar.Visibility = Visibility.Visible;
            MenuBar.Visibility = Visibility.Hidden;
            VerisonBar.Visibility = Visibility.Hidden;
        }
        private void SliderRam_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Settings.Default.Ram = LabelRam.Text;
            Settings.Default.Save();
            MaxRamMb = Convert.ToInt32(Settings.Default.Ram);
        }

        private void FlipView1_MouseDown(object sender, MouseButtonEventArgs e) => MenuBar.Visibility = Visibility.Hidden;

        private void Header_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) this.DragMove();
        }
    }
}

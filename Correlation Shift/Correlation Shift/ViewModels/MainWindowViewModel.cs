namespace Correlation_Shift.ViewModels
{
    using Catel.Configuration;
    using Catel.Data;
    using Catel.IoC;
    using Catel.MVVM;
    using Catel.Services;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using Views;
    public class MainWindowViewModel : ViewModelBase
    {
        public MainWindowViewModel()
        {
            ShowAbout = new Command(OnShowAboutExecute);
            CalculateImageShift = new Command(OnCalculateImageShiftExecute, OnCalculateImageShiftCanExecute);
            HandleFileDropOne = new Command<DragEventArgs>(OnHandleFileDropOneExecute);
            HandleFileDropTwo = new Command<DragEventArgs>(OnHandleFileDropTwoExecute);

            ChannelOneFiles = new ObservableCollection<string>();
            ChannelTwoFiles = new ObservableCollection<string>();

            shifter = new BackgroundWorker();
            shifter.WorkerSupportsCancellation = true;
            shifter.WorkerReportsProgress = true;
            shifter.RunWorkerCompleted += Shifter_RunWorkerCompleted;
            shifter.ProgressChanged += Shifter_ProgressChanged;

            shifter.DoWork += Shifter_DoWork;
        }

        private void Shifter_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            Progress = e.ProgressPercentage;
        }

        private void Shifter_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            RunCancelButton = "Run";

            var dependencyResolver = this.GetDependencyResolver();
            var messageService = dependencyResolver.Resolve<IMessageService>();

            messageService.ShowInformationAsync("Shifting finished");
        }

        // Performs the shift logic
        private void Shifter_DoWork(object sender, DoWorkEventArgs e)
        {
            IList<string> channelOneFilesCopy = new List<string>(ChannelOneFiles); // We dont block the UI so we work on a copy in case
            IList<string> channelTwoFilesCopy = new List<string>(ChannelTwoFiles); // some does add or remove files

            var dependencyResolver = this.GetDependencyResolver();
            var configService = dependencyResolver.Resolve<ConfigurationService>();

            Shifter.Shifter s = new Shifter.Shifter();

            s.MinX = configService.GetValue(ConfigurationContainer.Local, "MinX", -20);
            s.MinY = configService.GetValue(ConfigurationContainer.Local, "MinY", -20);
            s.MaxX = configService.GetValue(ConfigurationContainer.Local, "MaxX", 20);
            s.MaxY = configService.GetValue(ConfigurationContainer.Local, "MaxY", 20);

            for (int i = 0; (i < channelOneFilesCopy.Count && !shifter.CancellationPending); i++)
            {
                shifter.ReportProgress((i * 100) / ChannelOneFiles.Count);
                var offset = s.DetermineBestShift(channelOneFilesCopy[i], channelTwoFilesCopy[i]);
                Shifter.Shifter.SaveShiftedImage(channelOneFilesCopy[i], e.Argument.ToString(), offset);
            }
        }

        public override string Title { get { return "Correlation Shift"; } }

        private BackgroundWorker shifter;

        // TODO: Register models with the vmpropmodel codesnippet
        // TODO: Register view model properties with the vmprop or vmpropviewmodeltomodel codesnippets
        // TODO: Register commands with the vmcommand or vmcommandwithcanexecute codesnippets

        /// <summary>
        /// Gets or sets the property value.
        /// </summary>
        public string RunCancelButton
        {
            get { return GetValue<string>(RunCancelButtonProperty); }
            set { SetValue(RunCancelButtonProperty, value); }
        }

        /// <summary>
        /// Register the RunCancelButton property so it is known in the class.
        /// </summary>
        public static readonly PropertyData RunCancelButtonProperty = RegisterProperty("RunCancelButton", typeof(string), "Run");

        /// <summary>
        /// Gets or sets the property value.
        /// </summary>
        public int Progress
        {
            get { return GetValue<int>(ProgressProperty); }
            set { SetValue(ProgressProperty, value); }
        }

        /// <summary>
        /// Register the Progress property so it is known in the class.
        /// </summary>
        public static readonly PropertyData ProgressProperty = RegisterProperty("Progress", typeof(int), null);

        /// <summary>
        /// Gets the ShowAbout command.
        /// </summary>
        public Command ShowAbout { get; private set; }

        /// <summary>
        /// Displays the About window as modal dialog
        /// </summary>
        private void OnShowAboutExecute()
        {
            AboutWindow about = new AboutWindow();
            about.ShowDialog();
        }

        /// <summary>
        /// Gets the CalculateImageShift command.
        /// </summary>
        public Command CalculateImageShift { get; private set; }

        /// <summary>
        /// Method to check whether the CalculateImageShift command can be executed.
        /// </summary>
        /// <returns><c>true</c> if the command can be executed; otherwise <c>false</c></returns>
        private bool OnCalculateImageShiftCanExecute()
        {
            return (ChannelOneFiles.Count == ChannelTwoFiles.Count) && ChannelOneFiles.Count > 0;
        }

        /// <summary>
        /// Start the calculation of image shifts
        /// </summary>
        private void OnCalculateImageShiftExecute()
        {
            if (shifter.IsBusy)
            {
                shifter.CancelAsync();
                return;
            }

            var dependencyResolver = this.GetDependencyResolver();
            var selectDirectoryService = dependencyResolver.Resolve<ISelectDirectoryService>();
            selectDirectoryService.ShowNewFolderButton = true;
            selectDirectoryService.Title = "Select a folder where the shifted images should be saved...";

            if (selectDirectoryService.DetermineDirectory())
            {
                string path = selectDirectoryService.DirectoryName;
#if DEBUG
                if (true) // For debug purposes all directories are fine
#else
                    if (isDirectoryEmpty(path))
#endif
                {
                    RunCancelButton = "Cancel";
                    shifter.RunWorkerAsync(path);
                }
                else
                {
#if DEBUG
#pragma warning disable 0162 //Disable unreachable warning for DEBUG settings
#endif
                    var messageService = dependencyResolver.Resolve<IMessageService>();
                    messageService.ShowErrorAsync("The specified folder is not empty. Please select an empty folder");
#if DEBUG
#pragma warning restore 0162
#endif
                }

            }
        }

        private bool isDirectoryEmpty(string path)
        {
            return !Directory.EnumerateFileSystemEntries(path).Any();
        }

        /// <summary>
        /// Adds dropped files to the first channels collections
        /// </summary>
        public Command<DragEventArgs> HandleFileDropOne { get; private set; }

        /// <summary>
        /// Method to invoke when the HandleFileDrop command is executed.
        /// </summary>
        private void OnHandleFileDropOneExecute(DragEventArgs e)
        {
            handleDragData(e, ChannelOneFiles, item => !ChannelTwoFiles.Contains(item));
        }

        /// <summary>
        /// Gets the HandleFileDropTwo command.
        /// </summary>
        public Command<DragEventArgs> HandleFileDropTwo { get; private set; }

        /// <summary>
        /// Method to invoke when the HandleFileDropTwo command is executed.
        /// </summary>
        private void OnHandleFileDropTwoExecute(DragEventArgs e)
        {
            handleDragData(e, ChannelTwoFiles, item => !ChannelOneFiles.Contains(item));
        }

        private void handleDragData(DragEventArgs e, ObservableCollection<string> collection, Func<string, bool> validate_addition)
        {
            var d = (IEnumerable<string>)e.Data.GetData(DataFormats.FileDrop);

            var filtered = d.Where(i => Path.GetExtension(i).ToLower().Equals(".tif") || Path.GetExtension(i).ToLower().Equals(".tiff"));

            var newItems = filtered.Where(x => !collection.Any(y => string.Equals(x, y)));

            foreach (var item in newItems)
                collection.Add(item);

            var ordered = collection.OrderBy(s => Path.GetFileName(s)).ToList();

            collection.Clear();

            bool addition_failed = false;

            foreach (var item in ordered)
            {
                if (validate_addition(item))
                {
                    collection.Add(item);
                }
                else
                {
                    addition_failed = true;
                }
            }

            if (addition_failed)
            {
                var dependencyResolver = this.GetDependencyResolver();
                var messageService = dependencyResolver.Resolve<IMessageService>();
                messageService.ShowWarningAsync("At least on image could not be added as it was already present in the list for the other channel");
            }

            CalculateImageShift.RaiseCanExecuteChanged();
        }

        /// <summary>
        /// Contains the locations of the files of channel 1
        /// </summary>
        public ObservableCollection<string> ChannelOneFiles
        {
            get { return GetValue<ObservableCollection<string>>(ChannelOneFilesProperty); }
            set { SetValue(ChannelOneFilesProperty, value); }
        }

        /// <summary>
        /// Register the ChannelOneFiles property so it is known in the class.
        /// </summary>
        public static readonly PropertyData ChannelOneFilesProperty = RegisterProperty("ChannelOneFiles", typeof(ObservableCollection<string>), null);

        /// <summary>
        /// .Contains the files of the second channel
        /// </summary>
        public ObservableCollection<string> ChannelTwoFiles
        {
            get { return GetValue<ObservableCollection<string>>(ChannelTwoFilesProperty); }
            set { SetValue(ChannelTwoFilesProperty, value); }
        }

        /// <summary>
        /// Register the ChannelTwoFiles property so it is known in the class.
        /// </summary>
        public static readonly PropertyData ChannelTwoFilesProperty = RegisterProperty("ChannelTwoFiles", typeof(ObservableCollection<string>), null);

        protected override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            // TODO: subscribe to events here
        }

        protected override async Task CloseAsync()
        {
            // TODO: unsubscribe from events here

            await base.CloseAsync();
        }
    }
}

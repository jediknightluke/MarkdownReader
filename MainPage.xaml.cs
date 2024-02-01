using Markdig;
using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http;
using Microsoft.Maui.Layouts;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using Microsoft.Maui.Controls;



namespace MarkdownReader
{


    public class TabItem
    {
        public string Title { get; set; }
        public View Content { get; set; }
    }
    public class TabControl : StackLayout
    {
        private StackLayout tabHeaders = new StackLayout
        {
            Orientation = StackOrientation.Horizontal,
            Spacing = 10 // Adjust spacing as needed
        };
        private ContentView tabContent = new ContentView();

        public TabControl()
        {
            this.Orientation = StackOrientation.Vertical;
            this.Children.Add(tabHeaders);
            this.Children.Add(tabContent);
        }

        public void AddTab(TabItem tabItem, string imagePath)
        {
            var tabButton = new TabButton(tabItem.Title, imagePath);
            tabHeaders.Children.Add(tabButton);
        }
    }
    public class TabButton : AbsoluteLayout
    {
        public Image TabImage { get; private set; }
        public Label TabLabel { get; private set; }

        public TabButton(string title, string imagePath)
        {
            this.WidthRequest = 400;
            // Check if the image exists at the given path
            if (File.Exists(imagePath))
            {
                var frame = new Frame
                {
                    CornerRadius = 10, // Adjust the corner radius as needed
                    Padding = 0, // Adjust padding if needed

                    Content = new Frame
                    {
                        // Set the frame properties
                        Padding = 0, // No padding inside the frame
                        CornerRadius = 5, // Adjust corner radius as needed
                        BorderColor = Color.FromHex("#9A7ECC"), // Set your border color
                        HasShadow = false, // Set to true if you want shadow
                        BackgroundColor = Color.FromHex("#362D46") // Set the background color
                    }
                };

                // Add the frame (with the image) to the stack layout
                this.Children.Add(frame);

                AbsoluteLayout.SetLayoutBounds(TabImage, new Rect(0, 0, 1, 1));
                AbsoluteLayout.SetLayoutFlags(TabImage, AbsoluteLayoutFlags.All);
            }
            else
            {

                TabImage = new Image
                {
                    BackgroundColor = Color.FromHex("#362D46"),
                    HeightRequest = 30, // Set as needed
                    WidthRequest = 400, // Set as needed
                    Aspect = Aspect.AspectFill
                };
                AbsoluteLayout.SetLayoutBounds(TabImage, new Rect(0, 0, 1, 1));
                AbsoluteLayout.SetLayoutFlags(TabImage, AbsoluteLayoutFlags.All);
            }


            // Create and set up the Label
            TabLabel = new Label
            {
                Text = title,
                TextColor = Colors.White,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center
            };
            AbsoluteLayout.SetLayoutBounds(TabLabel, new Rect(0.5, 0.5, AbsoluteLayout.AutoSize, AbsoluteLayout.AutoSize));
            AbsoluteLayout.SetLayoutFlags(TabLabel, AbsoluteLayoutFlags.PositionProportional);

            // Add both the Image and Label to the AbsoluteLayout
            Children.Add(TabImage);
            Children.Add(TabLabel);
        }


    }

    public partial class MainPage : ContentPage, INotifyPropertyChanged
    {
        private List<string> GetMarkdownLines(string markdown)
        {
            // Simple split by line breaks. This might need to be more complex depending on your markdown structure.
            return markdown.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None).ToList();
        }
        private Dictionary<string, string> FileDictionary = new Dictionary<string, string>();
        private bool isDragging = false;

        private string currentFileName = null;
        private bool _isEditing;
        public ICommand TogglePanelCommand { get; private set; }
        public ICommand ToggleEditModeCommand { get; private set; }
        private int _currentEditingLineIndex;
        public string ToggleFilesPanelButtonText { get; set; } = "→"; // Default to right arrow
        public int CurrentEditingLineIndex
        {
            get => _currentEditingLineIndex;
            set
            {
                _currentEditingLineIndex = value;
                OnPropertyChanged(nameof(CurrentEditingLineIndex));
            }
        }
        private GridLength _filePanelWidth = new GridLength(.1, GridUnitType.Star);
        private GridLength _readingPanelWidth = new GridLength(.9, GridUnitType.Star);
        private double _filePanelProportion = 0.2; // Proportion of file panel width to total width

        public double FilePanelProportion
        {
            get => _filePanelProportion;
            set
            {
                if (_filePanelProportion != value)
                {
                    _filePanelProportion = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(FilePanelWidth));
                    OnPropertyChanged(nameof(ReadingPanelWidth));
                }
            }
        }

        public GridLength FilePanelWidth => new GridLength(_filePanelProportion, GridUnitType.Star);
        public GridLength ReadingPanelWidth => new GridLength(1 - _filePanelProportion, GridUnitType.Star);


        public MainPage()
        {
            InitializeComponent();
            BindingContext = this;

            // Set initial proportion for file panel (20% of available space)
            FilePanelProportion = 0.2; // This will automatically update FilePanelWidth and ReadingPanelWidth

            // Initialize commands
            TogglePanelCommand = new Command(TogglePanel);
            ToggleEditModeCommand = new Command(ToggleEditMode);

            // Set initial state of the side panel and visibility of components
            IsSidePanelHidden = true;
            IsEditing = false;
            MarkdownEditor.IsVisible = false; // Initially hide the Markdown editor
            MarkdownWebView.IsVisible = true; // Initially show the WebView

            // Load files into FileListView
            FileListView.ItemsSource = FileDictionary.Keys.ToList();
        }


        private const double DragSensitivity = 5; // Adjust this threshold as needed
        private double _initialDragX;


        private void OnPanUpdated(object sender, PanUpdatedEventArgs e)
        {
            switch (e.StatusType)
            {
                case GestureStatus.Started:
                    // Record the initial X position at the start of the drag
                    _initialDragX = e.TotalX;
                    break;

                case GestureStatus.Running:
                    // Calculate the drag distance relative to the initial drag position
                    double dragDistance = e.TotalX - _initialDragX;

                    if (Math.Abs(dragDistance) > DragSensitivity)
                    {
                        // Update the proportion based on the drag distance
                        var newProportion = _filePanelProportion + dragDistance / MainGrid.Width;
                        newProportion = Math.Clamp(newProportion, 0.1, 0.9); // Keep within bounds
                        FilePanelProportion = newProportion;
                        _initialDragX = e.TotalX;
                    }


                    break;

                case GestureStatus.Completed:
                case GestureStatus.Canceled:
                    // Reset the initial X position at the end of the drag
                    _initialDragX = 0;
                    break;
            }
        }





        private void TogglePanel()
        {
            if (!isDragging) // Only toggle panel if not dragging
            {
                if (IsSidePanelHidden)
                {
                    FilePanelProportion = 0; // Hide file panel
                }
                else
                {
                    FilePanelProportion = 0.2; // Set file panel to 20%
                }

                // OnPropertyChanged calls are not needed here as setting FilePanelProportion will trigger them
            }
        }



        public new event PropertyChangedEventHandler? PropertyChanged;


        protected override void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            base.OnPropertyChanged(propertyName);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        // Properties
        public bool IsSidePanelHidden { get; set; }
        public bool IsPreviewModeEnabled => !IsEditing;

        public string MarkdownContent
        {
            get => (string)GetValue(MarkdownContentProperty);
            set => SetValue(MarkdownContentProperty, value);
        }

        // Using a DependencyProperty as the backing store for MarkdownContent. This enables animation, styling, binding, etc...
        public static readonly BindableProperty MarkdownContentProperty =
            BindableProperty.Create(nameof(MarkdownContent), typeof(string), typeof(MainPage), default(string), BindingMode.TwoWay, propertyChanged: OnMarkdownContentChanged);

        private static void OnMarkdownContentChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var page = bindable as MainPage;
            if (newValue is string markdownText)
            {
                page?.UpdateWebViewContent(markdownText);
            }
        }

        public bool IsEditing
        {
            get => _isEditing;
            set
            {
                _isEditing = value;
                OnPropertyChanged(nameof(IsEditing));
                OnPropertyChanged(nameof(IsPreviewModeEnabled));
            }
        }

        private void ToggleEditMode()
        {
            IsEditing = !IsEditing;
        }

        private void UpdateWebViewContent(string markdownText)
        {
            var html = ConvertMarkdownToHtml(markdownText);
            MarkdownWebView.Source = new HtmlWebViewSource { Html = html };

            // Re-inject JavaScript after updating HTML content
            string javascript = GetHtmlWithJavaScript();
            MarkdownWebView.EvaluateJavaScriptAsync(javascript);
        }


        public void UpdateEditedLine(string newLineContent)
        {
            UpdateMarkdownLine(CurrentEditingLineIndex, newLineContent);
        }

        private static string ConvertMarkdownToHtml(string markdownText)
        {
            if (string.IsNullOrEmpty(markdownText))
            {
                return "<html><head></head><body></body></html>";
            }

            var pipeline = new MarkdownPipelineBuilder()
                .UseAdvancedExtensions() // Make sure all necessary extensions are enabled
                .Build();
            string htmlContent = Markdown.ToHtml(markdownText, pipeline) + GetHtmlWithCss();

            return htmlContent + GetHtmlWithJavaScript();
        }


        private static string GetHtmlWithJavaScript()
        {
            string javascript = @"
               <script>
                document.addEventListener('click', function(event) {
                    var existingEditable = document.querySelector('.editable');
                    if (event.target !== existingEditable) {
                        if (existingEditable) {
                            updateElementFromEditable(existingEditable);
                        }
                        if (['P', 'H1', 'H2', 'H3', 'H4', 'H5', 'H6', 'LI'].includes(event.target.tagName)) {
                            makeElementEditable(event.target);
                        }
                    }
                });

                function makeElementEditable(element) {
                    element.contentEditable = 'true';
                    element.classList.add('editable');
                    element.focus();
                }

                function updateElementFromEditable(editable) {
                    editable.contentEditable = 'false';
                    editable.classList.remove('editable');
                    var index = Array.from(document.body.children).indexOf(editable);
                    var value = editable.innerHTML; // or use `innerText` based on your formatting needs
                    window.location.href = 'callback://editline/' + index + '/' + encodeURIComponent(value);
                }
                </script>";
            return javascript;
        }




        private static string GetHtmlWithCss()
        {
            string css = @"
        <style>
            body {
                background-color: #333;
                color: white;
                font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
                margin: 0;
                padding: 20px;
            }
            h1, h2, h3, h4, h5, h6 {
                border-bottom: 1px solid #444;
                color: #ccc;
            }
            p {
                margin-bottom: 10px;
            }
            a {
                color: #6db3f2;
                text-decoration: none;
            }
            a:hover {
                text-decoration: underline;
            }
            code {
                background-color: #444;
                border-radius: 3px;
                padding: 2px 6px;
            }
            pre {
                background-color: #444;
                border-radius: 3px;
                padding: 10px;
                overflow-x: auto;
            }
            ul, ol {
                padding-left: 20px;
            }
            blockquote {
                border-left: 3px solid #555;
                margin: 10px 0;
                padding-left: 20px;
                color: #777;
            }
            .editable {
                min-height: 20px; /* Adjust as needed */
                background-color: transparent;
                border: none;
                color: inherit;
                font-size: inherit;
                font-family: inherit;
                width: 100%;
                overflow: auto;
            }
            /* Add other styles as needed */
        </style>";
            return css;
        }



        private async void UpdateMarkdownLine(int lineIndex, string newLineContent)
        {
            var lines = GetMarkdownLines(MarkdownContent);
            if (lineIndex >= 0 && lineIndex < lines.Count)
            {
                lines[lineIndex] = newLineContent;
                MarkdownContent = string.Join("\n", lines);

                await Task.Delay(300); // Short delay for smoother experience

                UpdateWebViewContent(MarkdownContent); // Refresh WebView
            }
            else
            {
                // Handle error: line index out of range
            }
        }




        private async void OpenFile_Click(object sender, EventArgs e)
        {
            try
            {
                var result = await FilePicker.PickAsync();
                if (result != null)
                {
                    string fileName = result.FileName;
                    string filePath = result.FullPath;
                    string markdownText = await ReadMarkdownFile(filePath);
                    string htmlText = MainPage.ConvertMarkdownToHtml(markdownText);

                    // Store the currently opened file name and content
                    currentFileName = fileName;
                    if (!FileDictionary.ContainsKey(fileName))
                    {
                        FileDictionary.Add(fileName, markdownText);
                    }
                    else
                    {
                        FileDictionary[fileName] = markdownText;
                    }

                    // Update the ListView to reflect the new files
                    FileListView.ItemsSource = FileDictionary.Keys.ToList();
                    MarkdownEditor.Text = markdownText; // This makes the content editable
                    MarkdownEditor.IsEnabled = true; // Enable the editor if it was previously disabled
                                                     // Update the WebView with the formatted Markdown content
                    MarkdownContent = markdownText;
                    UpdateWebViewContent(MarkdownContent);

                    // Hide the Editor and show the WebView
                    IsEditing = false;
                    MarkdownEditor.IsVisible = false;
                    MarkdownWebView.IsVisible = true;

                    // Create a WebView for the new document
                    var documentWebView = new WebView
                    {
                        Source = new HtmlWebViewSource { Html = ConvertMarkdownToHtml(markdownText) }
                    };

                    string tabImagePath = System.IO.Path.Combine(Environment.CurrentDirectory, "Resources", "Images", "tab.png");

                    // Create a new tab for the opened document
                    var newTab = new TabItem
                    {
                        Title = fileName,
                        Content = documentWebView
                    };

                    DocumentTabs.AddTab(newTab, tabImagePath);
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Error opening file: {ex.Message}", "OK");
            }

            IsEditing = true;
        }


        private async void SaveFile_Click(object sender, EventArgs e)
        {
            if (currentFileName != null)
            {
                string markdownText = MarkdownEditor.Text;
                await SaveMarkdownToFile(currentFileName, markdownText);

                // Use IsEditing instead of IsEditModeEnabled
                IsEditing = false;
                OnPropertyChanged(nameof(IsEditing));
            }
            IsEditing = false;
        }

        private async Task SaveMarkdownToFile(string fileName, string markdownText)
        {
            if (FileDictionary.ContainsKey(fileName))
            {
                string filePath = FileDictionary[fileName];
                string newFilePath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(filePath) ?? string.Empty, fileName ?? string.Empty);

                try
                {
                    File.Move(filePath, newFilePath);

                    // Update the dictionary with the new file name and path
                    FileDictionary.Remove(fileName);
                    FileDictionary[newFilePath] = newFilePath;

                    // Update the ListView
                    FileListView.ItemsSource = FileDictionary.Keys.ToList();
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", $"Error renaming file: {ex.Message}", "OK");
                }
            }

        }

        private async void FileListView_ItemTapped(object sender, ItemTappedEventArgs e)
        {
            if (e.Item is string fileName && FileDictionary.ContainsKey(fileName))
            {
                string markdownText = FileDictionary[fileName];
                string htmlText = MainPage.ConvertMarkdownToHtml(markdownText);

                // Assign the markdown text to the MarkdownContent property
                MarkdownContent = markdownText;
                OnPropertyChanged(nameof(MarkdownContent)); // Notify the view about the change


            }
        }

        private async void RenameFile_Tapped(object sender, EventArgs e)
        {
            var menuItem = sender as MenuItem;
            if (menuItem != null && menuItem.CommandParameter is string fileName)
            {
                // Show an input dialog to input the new file name
                string? newFileName = await DisplayPromptAsync("Rename File", "Enter a new file name:", "OK", "Cancel", keyboard: Keyboard.Text);
                if (!string.IsNullOrEmpty(newFileName) && !string.IsNullOrWhiteSpace(newFileName))
                {
                    if (FileDictionary.ContainsKey(fileName))
                    {
                        string filePath = FileDictionary[fileName];
                        string newFilePath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(filePath) ?? string.Empty, newFileName);

                        try
                        {
                            File.Move(filePath, newFilePath);

                            // Update the dictionary with the new file name and path
                            FileDictionary.Remove(fileName);
                            FileDictionary[newFileName] = newFilePath;

                            // Update the ListView
                            FileListView.ItemsSource = FileDictionary.Keys.ToList();
                        }
                        catch (Exception ex)
                        {
                            await DisplayAlert("Error", $"Error renaming file: {ex.Message}", "OK");
                        }
                    }
                }
            }
        }


        private async Task<string> ReadMarkdownFile(string filePath)
        {
            try
            {
                using (var streamReader = new StreamReader(filePath))
                {
                    return await streamReader.ReadToEndAsync();
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Error reading file: {ex.Message}", "OK");
                return string.Empty;
            }
        }




        private void ToggleFilesPanelButton_Clicked(object sender, EventArgs e)
        {
            // Toggle the visibility of the panel
            IsSidePanelHidden = !IsSidePanelHidden;
            SidePanel.IsVisible = !IsSidePanelHidden;

            // Swap the image based on the visibility of the panel
            ToggleImage.Source = IsSidePanelHidden ? "right.png" : "left.png";
            //ToggleFilesPanelButtonText = IsSidePanelHidden ? "→" : "←";
            OnPropertyChanged(nameof(ToggleFilesPanelButtonText)); // Notify changes
        }



        private void WebView_OnNavigating(object sender, WebNavigatingEventArgs e)
        {
            if (e.Url.StartsWith("callback://editline/"))
            {
                var parts = e.Url.Substring("callback://editline/".Length).Split('/');
                if (parts.Length == 2)
                {
                    int lineIndex = int.Parse(parts[0]);
                    string newContentHtml = WebUtility.UrlDecode(parts[1]);

                    // Convert HTML to Markdown
                    var converter = new ReverseMarkdown.Converter();
                    string newContentMarkdown = converter.Convert(newContentHtml);

                    // Example of post-processing, if necessary
                    newContentMarkdown = PostProcessMarkdown(newContentMarkdown);

                    UpdateMarkdownLine(lineIndex, newContentMarkdown);
                    UpdateWebViewContent(MarkdownContent);
                }
                e.Cancel = true;
            }
        }

        private string PostProcessMarkdown(string markdown)
        {
            // Here, handle any custom Markdown formatting rules you need
            // For example, wrapping unformatted text in markdown syntax if needed
            return markdown;
        }



    }
}
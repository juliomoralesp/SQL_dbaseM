using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using SqlServerManager.Services;
using SqlServerManager.UI;

namespace SqlServerManager.UI
{
    public class ModernFileBrowser : UserControl
    {
        private SplitContainer splitContainer;
        private Panel leftPanel;
        private Panel rightPanel;
        private TreeView fileTreeView;
        private ListView fileListView;
        private Panel previewPanel;
        private TextBox previewTextBox;
        private Label pathLabel;
        private ToolStrip toolStrip;
        private ToolStripButton upButton;
        private ToolStripButton refreshButton;
        private ToolStripComboBox pathComboBox;
        private ImageList imageList;

        // File type icons
        private Dictionary<string, int> fileTypeIcons;
        private string currentPath = "";
        
        public event EventHandler<FileSelectedEventArgs> FileSelected;
        public event EventHandler<PathChangedEventArgs> PathChanged;
        
        public string CurrentPath 
        { 
            get => currentPath; 
            set => NavigateToPath(value); 
        }
        
        public FileInfo SelectedFile { get; private set; }
        public DirectoryInfo SelectedDirectory { get; private set; }

        public ModernFileBrowser()
        {
            InitializeComponent();
            SetupImageList();
            LoadInitialPath();
        }

        private void InitializeComponent()
        {
            this.Size = new Size(800, 600);
            this.BackColor = ModernThemeManager.CurrentColors.BackgroundPrimary;

            // Create main split container
            splitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                SplitterDistance = 250,
                BackColor = ModernThemeManager.CurrentColors.BackgroundPrimary
            };

            // Left panel for tree view
            leftPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = ModernThemeManager.CurrentColors.BackgroundSecondary
            };

            // Right panel for list and preview
            rightPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = ModernThemeManager.CurrentColors.BackgroundPrimary
            };

            // Create toolbar
            CreateToolbar();

            // Create tree view
            fileTreeView = new TreeView
            {
                Dock = DockStyle.Fill,
                HideSelection = false,
                ShowLines = true,
                ShowPlusMinus = true,
                ShowRootLines = false,
                BackColor = ModernThemeManager.CurrentColors.BackgroundSecondary,
                ForeColor = ModernThemeManager.CurrentColors.ForegroundPrimary,
                BorderStyle = BorderStyle.None,
                Font = ModernThemeManager.GetScaledFont(this.Font)
            };
            fileTreeView.NodeMouseClick += TreeView_NodeMouseClick;
            fileTreeView.BeforeExpand += TreeView_BeforeExpand;

            // Create list view
            fileListView = new ListView
            {
                Dock = DockStyle.Top,
                Height = 300,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                BackColor = ModernThemeManager.CurrentColors.BackgroundPrimary,
                ForeColor = ModernThemeManager.CurrentColors.ForegroundPrimary,
                BorderStyle = BorderStyle.None,
                Font = ModernThemeManager.GetScaledFont(this.Font)
            };
            
            // Add columns
            fileListView.Columns.Add("Name", 200);
            fileListView.Columns.Add("Type", 100);
            fileListView.Columns.Add("Size", 80);
            fileListView.Columns.Add("Modified", 120);
            
            fileListView.SelectedIndexChanged += ListView_SelectedIndexChanged;
            fileListView.DoubleClick += ListView_DoubleClick;

            // Create preview panel
            previewPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = ModernThemeManager.CurrentColors.BackgroundSecondary,
                Padding = new Padding(5)
            };

            previewTextBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Both,
                BackColor = ModernThemeManager.CurrentColors.BackgroundPrimary,
                ForeColor = ModernThemeManager.CurrentColors.ForegroundPrimary,
                BorderStyle = BorderStyle.None,
                Font = new Font("Consolas", 9f)
            };

            // Path label
            pathLabel = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 25,
                BackColor = ModernThemeManager.CurrentColors.BackgroundSecondary,
                ForeColor = ModernThemeManager.CurrentColors.ForegroundSecondary,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(5, 0, 0, 0),
                Font = ModernThemeManager.GetScaledFont(this.Font)
            };

            // Layout controls
            previewPanel.Controls.Add(previewTextBox);
            rightPanel.Controls.Add(previewPanel);
            rightPanel.Controls.Add(fileListView);
            rightPanel.Controls.Add(pathLabel);

            leftPanel.Controls.Add(fileTreeView);
            
            splitContainer.Panel1.Controls.Add(leftPanel);
            splitContainer.Panel2.Controls.Add(rightPanel);

            this.Controls.Add(splitContainer);
            this.Controls.Add(toolStrip);
        }

        private void CreateToolbar()
        {
            toolStrip = new ToolStrip
            {
                Dock = DockStyle.Top,
                GripStyle = ToolStripGripStyle.Hidden,
                BackColor = ModernThemeManager.CurrentColors.BackgroundSecondary,
                ForeColor = ModernThemeManager.CurrentColors.ForegroundPrimary
            };

            upButton = new ToolStripButton
            {
                Text = "↑ Up",
                ToolTipText = "Go up one level"
            };
            upButton.Click += UpButton_Click;

            refreshButton = new ToolStripButton
            {
                Text = "↻ Refresh",
                ToolTipText = "Refresh current directory"
            };
            refreshButton.Click += RefreshButton_Click;

            pathComboBox = new ToolStripComboBox
            {
                AutoSize = false,
                Width = 300,
                DropDownStyle = ComboBoxStyle.DropDown
            };
            pathComboBox.KeyDown += PathComboBox_KeyDown;

            toolStrip.Items.AddRange(new ToolStripItem[]
            {
                upButton,
                refreshButton,
                new ToolStripSeparator(),
                new ToolStripLabel("Path:"),
                pathComboBox
            });
        }

        private void SetupImageList()
        {
            imageList = new ImageList
            {
                ImageSize = new Size(16, 16),
                ColorDepth = ColorDepth.Depth32Bit
            };

            fileTypeIcons = new Dictionary<string, int>();

            try
            {
                // Add default icons (using system icons or creating simple colored rectangles)
                var folderIcon = CreateColorIcon(Color.Gold);
                var fileIcon = CreateColorIcon(Color.LightBlue);
                var sqlIcon = CreateColorIcon(Color.Orange);
                var textIcon = CreateColorIcon(Color.White);
                var xmlIcon = CreateColorIcon(Color.LightGreen);
                
                imageList.Images.Add("folder", folderIcon);
                imageList.Images.Add("file", fileIcon);
                imageList.Images.Add("sql", sqlIcon);
                imageList.Images.Add("text", textIcon);
                imageList.Images.Add("xml", xmlIcon);

                fileTypeIcons[".sql"] = imageList.Images.IndexOfKey("sql");
                fileTypeIcons[".txt"] = imageList.Images.IndexOfKey("text");
                fileTypeIcons[".log"] = imageList.Images.IndexOfKey("text");
                fileTypeIcons[".xml"] = imageList.Images.IndexOfKey("xml");
                fileTypeIcons[".json"] = imageList.Images.IndexOfKey("xml");
                fileTypeIcons[".config"] = imageList.Images.IndexOfKey("xml");
            }
            catch (Exception ex)
            {
                LoggingService.LogWarning("Failed to setup file browser icons: {Message}", ex.Message);
            }

            fileTreeView.ImageList = imageList;
            fileListView.SmallImageList = imageList;
        }

        private Bitmap CreateColorIcon(Color color)
        {
            var bitmap = new Bitmap(16, 16);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.FillRectangle(new SolidBrush(color), 2, 2, 12, 12);
                g.DrawRectangle(Pens.Black, 2, 2, 12, 12);
            }
            return bitmap;
        }

        private void LoadInitialPath()
        {
            try
            {
                var initialPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                NavigateToPath(initialPath);
                LoadDrives();
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Failed to load initial path: {Message}", ex.Message);
            }
        }

        private void LoadDrives()
        {
            try
            {
                fileTreeView.Nodes.Clear();
                
                foreach (var drive in DriveInfo.GetDrives())
                {
                    if (drive.IsReady)
                    {
                        var driveNode = new TreeNode(drive.Name)
                        {
                            Tag = drive.RootDirectory,
                            ImageKey = "folder",
                            SelectedImageKey = "folder"
                        };
                        
                        // Add dummy node for expansion
                        driveNode.Nodes.Add("Loading...");
                        fileTreeView.Nodes.Add(driveNode);
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Failed to load drives: {Message}", ex.Message);
            }
        }

        private void NavigateToPath(string path)
        {
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
                return;

            currentPath = path;
            pathLabel.Text = path;
            pathComboBox.Text = path;
            
            LoadDirectoryContents(path);
            PathChanged?.Invoke(this, new PathChangedEventArgs(path));
        }

        private void LoadDirectoryContents(string path)
        {
            try
            {
                fileListView.Items.Clear();
                var directory = new DirectoryInfo(path);

                // Add directories
                foreach (var dir in directory.GetDirectories().OrderBy(d => d.Name))
                {
                    try
                    {
                        var item = new ListViewItem(dir.Name)
                        {
                            Tag = dir,
                            ImageKey = "folder"
                        };
                        item.SubItems.Add("Folder");
                        item.SubItems.Add("");
                        item.SubItems.Add(dir.LastWriteTime.ToString("yyyy-MM-dd HH:mm"));
                        fileListView.Items.Add(item);
                    }
                    catch
                    {
                        // Skip inaccessible directories
                    }
                }

                // Add files
                foreach (var file in directory.GetFiles().OrderBy(f => f.Name))
                {
                    try
                    {
                        var extension = file.Extension.ToLower();
                        var imageKey = fileTypeIcons.ContainsKey(extension) ? 
                            imageList.Images.Keys[fileTypeIcons[extension]] : "file";

                        var item = new ListViewItem(file.Name)
                        {
                            Tag = file,
                            ImageKey = imageKey
                        };
                        item.SubItems.Add(GetFileTypeDescription(extension));
                        item.SubItems.Add(FormatFileSize(file.Length));
                        item.SubItems.Add(file.LastWriteTime.ToString("yyyy-MM-dd HH:mm"));
                        fileListView.Items.Add(item);
                    }
                    catch
                    {
                        // Skip inaccessible files
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Failed to load directory contents for {Path}: {Message}", path, ex.Message);
            }
        }

        private string GetFileTypeDescription(string extension)
        {
            return extension.ToUpper() switch
            {
                ".SQL" => "SQL Script",
                ".TXT" => "Text File",
                ".LOG" => "Log File",
                ".XML" => "XML File",
                ".JSON" => "JSON File",
                ".CONFIG" => "Configuration",
                ".CS" => "C# Source",
                ".CSPROJ" => "C# Project",
                ".SLN" => "Solution",
                _ => extension.ToUpper() + " File"
            };
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        private void TreeView_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            if (e.Node.Nodes.Count == 1 && e.Node.Nodes[0].Text == "Loading...")
            {
                e.Node.Nodes.Clear();
                LoadTreeNodeChildren(e.Node);
            }
        }

        private void LoadTreeNodeChildren(TreeNode node)
        {
            if (node.Tag is DirectoryInfo directory)
            {
                try
                {
                    foreach (var subDir in directory.GetDirectories().OrderBy(d => d.Name))
                    {
                        try
                        {
                            var childNode = new TreeNode(subDir.Name)
                            {
                                Tag = subDir,
                                ImageKey = "folder",
                                SelectedImageKey = "folder"
                            };
                            
                            // Check if directory has subdirectories
                            if (subDir.GetDirectories().Any())
                            {
                                childNode.Nodes.Add("Loading...");
                            }
                            
                            node.Nodes.Add(childNode);
                        }
                        catch
                        {
                            // Skip inaccessible directories
                        }
                    }
                }
                catch (Exception ex)
                {
                    LoggingService.LogWarning("Failed to load tree node children: {Message}", ex.Message);
                }
            }
        }

        private void TreeView_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Node.Tag is DirectoryInfo directory)
            {
                NavigateToPath(directory.FullName);
            }
        }

        private void ListView_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (fileListView.SelectedItems.Count > 0)
            {
                var selectedItem = fileListView.SelectedItems[0];
                
                if (selectedItem.Tag is FileInfo file)
                {
                    SelectedFile = file;
                    SelectedDirectory = null;
                    _ = LoadFilePreview(file);
                    FileSelected?.Invoke(this, new FileSelectedEventArgs(file, null));
                }
                else if (selectedItem.Tag is DirectoryInfo directory)
                {
                    SelectedFile = null;
                    SelectedDirectory = directory;
                    previewTextBox.Text = $"Directory: {directory.Name}\nItems: {directory.GetFileSystemInfos().Length}";
                    FileSelected?.Invoke(this, new FileSelectedEventArgs(null, directory));
                }
            }
        }

        private void ListView_DoubleClick(object sender, EventArgs e)
        {
            if (fileListView.SelectedItems.Count > 0)
            {
                var selectedItem = fileListView.SelectedItems[0];
                
                if (selectedItem.Tag is DirectoryInfo directory)
                {
                    NavigateToPath(directory.FullName);
                }
            }
        }

        private async Task LoadFilePreview(FileInfo file)
        {
            try
            {
                if (file.Length > 1024 * 1024) // 1MB limit
                {
                    previewTextBox.Text = "File too large for preview";
                    return;
                }

                var extension = file.Extension.ToLower();
                if (new[] { ".txt", ".sql", ".cs", ".xml", ".json", ".config", ".log" }.Contains(extension))
                {
                    var content = await File.ReadAllTextAsync(file.FullName);
                    previewTextBox.Text = content;
                }
                else
                {
                    previewTextBox.Text = $"Preview not available for {extension} files";
                }
            }
            catch (Exception ex)
            {
                previewTextBox.Text = $"Error loading preview: {ex.Message}";
                LoggingService.LogWarning("Failed to load file preview: {Message}", ex.Message);
            }
        }

        private void UpButton_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(currentPath))
            {
                var parent = Directory.GetParent(currentPath);
                if (parent != null)
                {
                    NavigateToPath(parent.FullName);
                }
            }
        }

        private void RefreshButton_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(currentPath))
            {
                LoadDirectoryContents(currentPath);
            }
        }

        private void PathComboBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                var path = pathComboBox.Text;
                if (Directory.Exists(path))
                {
                    NavigateToPath(path);
                }
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            ModernThemeManager.ApplyThemeToControl(this);
        }
    }

    public class FileSelectedEventArgs : EventArgs
    {
        public FileInfo File { get; }
        public DirectoryInfo Directory { get; }

        public FileSelectedEventArgs(FileInfo file, DirectoryInfo directory)
        {
            File = file;
            Directory = directory;
        }
    }

    public class PathChangedEventArgs : EventArgs
    {
        public string Path { get; }

        public PathChangedEventArgs(string path)
        {
            Path = path;
        }
    }
}

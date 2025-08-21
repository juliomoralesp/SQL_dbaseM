using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace SqlServerManager.UI
{
    /// <summary>
    /// Manages keyboard shortcuts and provides accessibility features
    /// </summary>
    public class KeyboardShortcutManager : IDisposable
    {
        private readonly Form _parentForm;
        private readonly Dictionary<Keys, ShortcutAction> _shortcuts = new Dictionary<Keys, ShortcutAction>();
        private readonly Dictionary<string, Keys> _shortcutGroups = new Dictionary<string, Keys>();
        private bool _disposed = false;

        public KeyboardShortcutManager(Form parentForm)
        {
            _parentForm = parentForm ?? throw new ArgumentNullException(nameof(parentForm));
            _parentForm.KeyPreview = true;
            _parentForm.KeyDown += ParentForm_KeyDown;
            
            RegisterDefaultShortcuts();
            LoggingService.LogDebug("Keyboard shortcut manager initialized for {FormType}", _parentForm.GetType().Name);
        }

        /// <summary>
        /// Register a keyboard shortcut with an action
        /// </summary>
        public void RegisterShortcut(Keys keyCombo, Action action, string description, string group = "General")
        {
            if (_shortcuts.ContainsKey(keyCombo))
            {
                LoggingService.LogWarning("Shortcut {Keys} already registered, overwriting", keyCombo);
            }

            _shortcuts[keyCombo] = new ShortcutAction(action, description, group);
            _shortcutGroups[group] = keyCombo;
            
            LoggingService.LogDebug("Registered shortcut {Keys} for {Description} in group {Group}", keyCombo, description, group);
        }

        /// <summary>
        /// Register a keyboard shortcut with a parameterized action
        /// </summary>
        public void RegisterShortcut<T>(Keys keyCombo, Action<T> action, T parameter, string description, string group = "General")
        {
            RegisterShortcut(keyCombo, () => action(parameter), description, group);
        }

        /// <summary>
        /// Unregister a keyboard shortcut
        /// </summary>
        public void UnregisterShortcut(Keys keyCombo)
        {
            if (_shortcuts.Remove(keyCombo))
            {
                LoggingService.LogDebug("Unregistered shortcut {Keys}", keyCombo);
            }
        }

        /// <summary>
        /// Get all registered shortcuts grouped by category
        /// </summary>
        public Dictionary<string, List<(Keys Keys, string Description)>> GetAllShortcuts()
        {
            var grouped = new Dictionary<string, List<(Keys, string)>>();

            foreach (var kvp in _shortcuts)
            {
                var group = kvp.Value.Group;
                if (!grouped.ContainsKey(group))
                {
                    grouped[group] = new List<(Keys, string)>();
                }
                grouped[group].Add((kvp.Key, kvp.Value.Description));
            }

            // Sort shortcuts within each group
            foreach (var group in grouped.Values)
            {
                group.Sort((a, b) => a.Item2.CompareTo(b.Item2));
            }

            return grouped;
        }

        /// <summary>
        /// Show a help dialog with all available shortcuts
        /// </summary>
        public void ShowShortcutHelp()
        {
            var helpForm = new ShortcutHelpForm(GetAllShortcuts());
            helpForm.ShowDialog(_parentForm);
        }

        /// <summary>
        /// Enable or disable all shortcuts
        /// </summary>
        public void SetShortcutsEnabled(bool enabled)
        {
            _parentForm.KeyPreview = enabled;
            LoggingService.LogDebug("Keyboard shortcuts {Status} for {FormType}", 
                enabled ? "enabled" : "disabled", _parentForm.GetType().Name);
        }

        private void RegisterDefaultShortcuts()
        {
            try
            {
                // Application shortcuts
                RegisterShortcut(Keys.F1, () => ShowShortcutHelp(), "Show Help", "Help");
                RegisterShortcut(Keys.Alt | Keys.F4, () => _parentForm.Close(), "Exit Application", "Application");
                RegisterShortcut(Keys.Escape, () => HandleEscape(), "Cancel/Close", "Navigation");

                // Connection shortcuts
                RegisterShortcut(Keys.Control | Keys.N, () => TriggerMenuAction("connect"), "New Connection", "Connection");
                RegisterShortcut(Keys.Control | Keys.D, () => TriggerMenuAction("disconnect"), "Disconnect", "Connection");
                RegisterShortcut(Keys.F5, () => TriggerMenuAction("refresh"), "Refresh", "Navigation");

                // Query/SQL Editor shortcuts
                RegisterShortcut(Keys.Control | Keys.E, () => TriggerMenuAction("execute"), "Execute Query", "SQL");
                RegisterShortcut(Keys.F9, () => TriggerMenuAction("execute"), "Execute Query (Alt)", "SQL");
                RegisterShortcut(Keys.Control | Keys.Shift | Keys.E, () => TriggerMenuAction("explain"), "Explain Query Plan", "SQL");

                // Data manipulation shortcuts
                RegisterShortcut(Keys.Control | Keys.S, () => TriggerMenuAction("save"), "Save Changes", "Data");
                RegisterShortcut(Keys.Control | Keys.Z, () => TriggerMenuAction("undo"), "Undo", "Data");
                RegisterShortcut(Keys.Control | Keys.Y, () => TriggerMenuAction("redo"), "Redo", "Data");

                // View shortcuts
                RegisterShortcut(Keys.Control | Keys.T, () => TriggerMenuAction("newtab"), "New Tab", "View");
                RegisterShortcut(Keys.Control | Keys.W, () => TriggerMenuAction("closetab"), "Close Tab", "View");
                RegisterShortcut(Keys.Control | Keys.Tab, () => TriggerMenuAction("nexttab"), "Next Tab", "View");
                RegisterShortcut(Keys.Control | Keys.Shift | Keys.Tab, () => TriggerMenuAction("prevtab"), "Previous Tab", "View");

                // Tools shortcuts
                RegisterShortcut(Keys.Control | Keys.I, () => TriggerMenuAction("import"), "Import Data", "Tools");
                RegisterShortcut(Keys.Control | Keys.Shift | Keys.I, () => TriggerMenuAction("importwizard"), "Import Wizard", "Tools");
                RegisterShortcut(Keys.Control | Keys.Shift | Keys.E, () => TriggerMenuAction("export"), "Export Data", "Tools");
                RegisterShortcut(Keys.Control | Keys.B, () => TriggerMenuAction("backup"), "Backup Database", "Tools");

                // Search and navigation
                RegisterShortcut(Keys.Control | Keys.F, () => TriggerMenuAction("find"), "Find", "Search");
                RegisterShortcut(Keys.F3, () => TriggerMenuAction("findnext"), "Find Next", "Search");
                RegisterShortcut(Keys.Shift | Keys.F3, () => TriggerMenuAction("findprev"), "Find Previous", "Search");
                RegisterShortcut(Keys.Control | Keys.H, () => TriggerMenuAction("replace"), "Replace", "Search");
                RegisterShortcut(Keys.Control | Keys.G, () => TriggerMenuAction("goto"), "Go to Line", "Navigation");

                LoggingService.LogInformation("Default keyboard shortcuts registered successfully");
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, "Error registering default shortcuts");
            }
        }

        private void ParentForm_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                var keyCombo = e.KeyData;
                
                if (_shortcuts.TryGetValue(keyCombo, out var shortcutAction))
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    
                    LoggingService.LogDebug("Executing shortcut {Keys}: {Description}", keyCombo, shortcutAction.Description);
                    
                    try
                    {
                        shortcutAction.Action?.Invoke();
                        LoggingService.LogUserAction($"Keyboard shortcut executed", shortcutAction.Description);
                    }
                    catch (Exception ex)
                    {
                        LoggingService.LogError(ex, "Error executing shortcut {Keys}: {Description}", keyCombo, shortcutAction.Description);
                        Services.ExceptionHandler.Handle(ex, $"executing keyboard shortcut '{shortcutAction.Description}'", _parentForm);
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, "Error in keyboard shortcut handler");
            }
        }

        private void TriggerMenuAction(string action)
        {
            try
            {
                // Try to find and trigger corresponding menu items or toolbar buttons
                var mainForm = _parentForm as MainForm;
                if (mainForm == null) return;

                switch (action.ToLowerInvariant())
                {
                    case "connect":
                        TriggerControlClick(mainForm, "connectButton", "connectionMenuItem");
                        break;
                    case "disconnect":
                        TriggerControlClick(mainForm, "disconnectButton", "disconnectMenuItem");
                        break;
                    case "refresh":
                        TriggerControlClick(mainForm, "refreshButton", "refreshMenuItem");
                        break;
                    case "execute":
                        TriggerControlClick(mainForm, "executeButton", "executeMenuItem");
                        break;
                    case "save":
                        TriggerControlClick(mainForm, "saveButton", "saveMenuItem");
                        break;
                    case "import":
                        TriggerControlClick(mainForm, "importButton", "importMenuItem");
                        break;
                    case "importwizard":
                        TriggerControlClick(mainForm, "importWizardButton", "importWizardMenuItem");
                        break;
                    case "export":
                        TriggerControlClick(mainForm, "exportButton", "exportMenuItem");
                        break;
                    case "backup":
                        TriggerControlClick(mainForm, "backupButton", "backupMenuItem");
                        break;
                    case "newtab":
                        if (mainForm.TabControl != null && mainForm.TabControl.TabPages.Count > 0)
                        {
                            // Add new tab logic would go here
                            LoggingService.LogInformation("New tab shortcut triggered");
                        }
                        break;
                    case "closetab":
                        if (mainForm.TabControl != null && mainForm.TabControl.SelectedTab != null)
                        {
                            // Close current tab logic would go here
                            LoggingService.LogInformation("Close tab shortcut triggered");
                        }
                        break;
                    case "nexttab":
                        NavigateTab(mainForm, 1);
                        break;
                    case "prevtab":
                        NavigateTab(mainForm, -1);
                        break;
                    default:
                        LoggingService.LogDebug("No handler found for action: {Action}", action);
                        break;
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, "Error triggering menu action: {Action}", action);
            }
        }

        private void TriggerControlClick(Form form, params string[] controlNames)
        {
            foreach (var controlName in controlNames)
            {
                // Try to find regular control first
                var control = FindControl(form, controlName);
                if (control is Button button && button.Enabled)
                {
                    button.PerformClick();
                    return;
                }
                
                // Try to find ToolStrip items
                var toolStripItem = FindToolStripItem(form, controlName);
                if (toolStripItem is ToolStripMenuItem menuItem && menuItem.Enabled)
                {
                    menuItem.PerformClick();
                    return;
                }
                else if (toolStripItem is ToolStripButton toolButton && toolButton.Enabled)
                {
                    toolButton.PerformClick();
                    return;
                }
            }
        }

        private Control FindControl(Control parent, string name)
        {
            if (parent.Name == name) return parent;

            foreach (Control child in parent.Controls)
            {
                var found = FindControl(child, name);
                if (found != null) return found;
            }

            return null;
        }

        private ToolStripItem FindToolStripItem(Control parent, string name)
        {
            // Check if parent is a ToolStrip
            if (parent is ToolStrip toolStrip)
            {
                foreach (ToolStripItem item in toolStrip.Items)
                {
                    if (item.Name == name) return item;
                    
                    // Check for sub-items in dropdown menus
                    if (item is ToolStripDropDownItem dropDown)
                    {
                        var found = FindToolStripItemRecursive(dropDown.DropDownItems, name);
                        if (found != null) return found;
                    }
                }
            }

            // Recursively search child controls
            foreach (Control child in parent.Controls)
            {
                var found = FindToolStripItem(child, name);
                if (found != null) return found;
            }

            return null;
        }

        private ToolStripItem FindToolStripItemRecursive(ToolStripItemCollection items, string name)
        {
            foreach (ToolStripItem item in items)
            {
                if (item.Name == name) return item;
                
                if (item is ToolStripDropDownItem dropDown)
                {
                    var found = FindToolStripItemRecursive(dropDown.DropDownItems, name);
                    if (found != null) return found;
                }
            }
            return null;
        }

        private void NavigateTab(MainForm mainForm, int direction)
        {
            if (mainForm.TabControl == null || mainForm.TabControl.TabPages.Count <= 1) return;

            var currentIndex = mainForm.TabControl.SelectedIndex;
            var newIndex = currentIndex + direction;

            if (newIndex < 0)
                newIndex = mainForm.TabControl.TabPages.Count - 1;
            else if (newIndex >= mainForm.TabControl.TabPages.Count)
                newIndex = 0;

            mainForm.TabControl.SelectedIndex = newIndex;
            LoggingService.LogDebug("Navigated to tab {Index}", newIndex);
        }

        private void HandleEscape()
        {
            // Close active dialogs or cancel operations
            var activeControl = _parentForm.ActiveControl;
            
            if (_parentForm.Modal)
            {
                _parentForm.DialogResult = DialogResult.Cancel;
                _parentForm.Close();
            }
            else
            {
                // Check for active search boxes, combo boxes, etc.
                if (activeControl is TextBox textBox)
                {
                    textBox.SelectAll();
                }
                else if (activeControl is ComboBox comboBox && comboBox.DroppedDown)
                {
                    comboBox.DroppedDown = false;
                }
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                if (_parentForm != null)
                {
                    _parentForm.KeyDown -= ParentForm_KeyDown;
                }
                
                _shortcuts.Clear();
                _shortcutGroups.Clear();
                _disposed = true;
                
                LoggingService.LogDebug("Keyboard shortcut manager disposed for {FormType}", _parentForm?.GetType().Name);
            }
        }

        private class ShortcutAction
        {
            public Action Action { get; }
            public string Description { get; }
            public string Group { get; }

            public ShortcutAction(Action action, string description, string group)
            {
                Action = action;
                Description = description;
                Group = group;
            }
        }
    }

    /// <summary>
    /// Help dialog showing all available keyboard shortcuts
    /// </summary>
    internal class ShortcutHelpForm : Form
    {
        public ShortcutHelpForm(Dictionary<string, List<(Keys Keys, string Description)>> shortcuts)
        {
            InitializeComponent(shortcuts);
        }

        private void InitializeComponent(Dictionary<string, List<(Keys Keys, string Description)>> shortcuts)
        {
            Text = "Keyboard Shortcuts";
            Size = new System.Drawing.Size(600, 500);
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;
            MaximizeBox = false;
            MinimizeBox = false;

            var tabControl = new TabControl
            {
                Dock = DockStyle.Fill
            };

            foreach (var group in shortcuts.OrderBy(g => g.Key))
            {
                var tabPage = new TabPage(group.Key);
                var listView = new ListView
                {
                    Dock = DockStyle.Fill,
                    View = View.Details,
                    FullRowSelect = true,
                    GridLines = true
                };

                listView.Columns.Add("Shortcut", 150);
                listView.Columns.Add("Action", 400);

                foreach (var shortcut in group.Value)
                {
                    var item = new ListViewItem(GetKeyString(shortcut.Keys));
                    item.SubItems.Add(shortcut.Description);
                    listView.Items.Add(item);
                }

                tabPage.Controls.Add(listView);
                tabControl.TabPages.Add(tabPage);
            }

            var buttonPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 50
            };

            var closeButton = new Button
            {
                Text = "Close",
                DialogResult = DialogResult.OK,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                Size = new System.Drawing.Size(75, 23)
            };
            closeButton.Location = new System.Drawing.Point(buttonPanel.Width - closeButton.Width - 10, 
                (buttonPanel.Height - closeButton.Height) / 2);

            buttonPanel.Controls.Add(closeButton);

            Controls.Add(tabControl);
            Controls.Add(buttonPanel);

            AcceptButton = closeButton;
        }

        private string GetKeyString(Keys keys)
        {
            var result = new List<string>();

            if ((keys & Keys.Control) == Keys.Control)
                result.Add("Ctrl");
            if ((keys & Keys.Alt) == Keys.Alt)
                result.Add("Alt");
            if ((keys & Keys.Shift) == Keys.Shift)
                result.Add("Shift");

            var keyCode = keys & ~Keys.Modifiers;
            if (keyCode != Keys.None)
                result.Add(GetKeyName(keyCode));

            return string.Join(" + ", result);
        }

        private string GetKeyName(Keys key)
        {
            return key switch
            {
                Keys.F1 => "F1",
                Keys.F2 => "F2",
                Keys.F3 => "F3",
                Keys.F4 => "F4",
                Keys.F5 => "F5",
                Keys.F9 => "F9",
                Keys.Escape => "Esc",
                Keys.Tab => "Tab",
                Keys.Enter => "Enter",
                Keys.Space => "Space",
                _ => key.ToString()
            };
        }
    }
}

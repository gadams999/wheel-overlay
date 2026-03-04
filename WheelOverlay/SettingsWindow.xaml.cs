using System;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Linq;
using WheelOverlay.Models;
using WheelOverlay.ViewModels;

namespace WheelOverlay
{
    public partial class SettingsWindow : Window
    {
        private AppSettings? _settings;
        public event EventHandler? SettingsChanged;

        // UI Controls
        private System.Windows.Controls.ComboBox? _profileComboBox;
        private System.Windows.Controls.ComboBox? _layoutComboBox;
        private System.Windows.Controls.ComboBox? _deviceComboBox;
        private Slider? _fontSizeSlider;
        private System.Windows.Controls.TextBox? _selectedColorTextBox;
        private System.Windows.Controls.TextBox? _nonSelectedColorTextBox;
        private Slider? _spacingSlider;
        private Slider? _opacitySlider;
        private System.Windows.Controls.TextBox[]? _labelTextBoxes;
        
        // New v0.5.0 controls
        private System.Windows.Controls.ComboBox? _positionCountComboBox;
        private System.Windows.Controls.ComboBox? _gridRowsComboBox;
        private System.Windows.Controls.ComboBox? _gridColumnsComboBox;
        private ItemsControl? _gridPreviewControl;
        private TextBlock? _gridCapacityText;
        private ItemsControl? _suggestedDimensionsControl;
        
        // Conditional visibility controls (overlay-visibility-and-ui-improvements)
        private TextBlock? _targetExeDisplay;
        private System.Windows.Controls.Button? _browseButton;
        private System.Windows.Controls.Button? _clearButton;
        
        // Dial layout controls (v0.6.0)
        private Slider? _dialKnobScaleSlider;
        private Slider? _dialLabelGapSlider;
        
        // Theme preference combo (v0.6.0)
        private System.Windows.Controls.ComboBox? _themePreferenceComboBox;
        
        // Grid-specific controls container (v0.6.0 - hidden for non-Grid layouts)
        private StackPanel? _gridControlsPanel;
        // Dial-specific controls container (v0.6.0 - hidden for non-Dial layouts)
        private StackPanel? _dialControlsPanel;

        private StackPanel? _settingsPanel;
        private SettingsViewModel? _viewModel;

        public SettingsWindow(AppSettings settings)
        {
            InitializeComponent();
            _settings = settings;
            _viewModel = new SettingsViewModel();
            Loaded += SettingsWindow_Loaded;
        }

        private void ScrollViewer_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            if (sender is ScrollViewer scrollViewer)
            {
                scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - e.Delta / 3.0);
                e.Handled = true;
            }
        }

        private Profile? GetCurrentProfile()
        {
            if (_settings == null) return null;
            
            var profile = _settings.Profiles.FirstOrDefault(p => p.Id == _settings.SelectedProfileId);
            if (profile == null)
            {
                // Fallback / Self-healing
                var validProfiles = GetProfilesForCurrentDevice();
                if (!validProfiles.Any())
                {
                    profile = CreateNewProfile("Default", _settings.SelectedDeviceName);
                }
                else
                {
                    profile = validProfiles.First();
                }
                _settings.SelectedProfileId = profile.Id;
            }
            return profile;
        }

        private List<Profile> GetProfilesForCurrentDevice()
        {
            if (_settings == null) return new List<Profile>();
            
            return _settings.Profiles
                .Where(p => p.DeviceName == _settings.SelectedDeviceName)
                .ToList();
        }

        private Profile CreateNewProfile(string name, string deviceName)
        {
            if (_settings == null) throw new InvalidOperationException("Settings not initialized");
            
            var wheelDef = WheelDefinition.SupportedWheels.FirstOrDefault(w => w.DeviceName == deviceName) 
                           ?? WheelDefinition.SupportedWheels[0];

            var profile = new Profile
            {
                Name = name,
                DeviceName = deviceName,
                TextLabels = Enumerable.Repeat("DASH", wheelDef.TextFieldCount).ToList() // Default filler
            };
            
            // If it's the Alpha, give it the nice defaults
            if (deviceName == "BavarianSimTec Alpha")
            {
               profile.TextLabels = new List<string> { "DASH", "TC2", "MAP", "FUEL", "BRGT", "VOL", "BOX", "DIFF" };
            }

            _settings.Profiles.Add(profile);
            return profile;
        }



        private void SettingsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Find SettingsPanel manually since XAML binding isn't working
            _settingsPanel = FindName("SettingsPanel") as StackPanel;
            
            if (_settingsPanel == null)
            {
                System.Windows.MessageBox.Show("Could not find SettingsPanel control!", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            ShowDisplaySettings();
        }

        private void CategoryListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CategoryListBox.SelectedItem is System.Windows.Controls.ListBoxItem selectedItem && selectedItem.Tag != null)
            {
                // Save current category values before switching
                SaveCurrentCategoryValues();
                
                string category = selectedItem.Tag.ToString()!;
                switch (category)
                {
                    case "Display":
                        ShowDisplaySettings();
                        break;
                    case "Appearance":
                        ShowAppearanceSettings();
                        break;
                    case "Advanced":
                        ShowAdvancedSettings();
                        break;
                }
            }
        }

        private void SaveCurrentCategoryValues()
        {
            if (_settings == null) return;
            
            var profile = GetCurrentProfile();
            if (profile == null) return;

            // Save layout
            if (_layoutComboBox?.SelectedItem is System.Windows.Controls.ComboBoxItem selectedItem && selectedItem.Tag != null)
            {
                profile.Layout = Enum.Parse<DisplayLayout>(selectedItem.Tag.ToString()!);
            }

            // Save device selection
            if (_deviceComboBox?.SelectedItem is string selectedDevice)
            {
                _settings.SelectedDeviceName = selectedDevice;
            }

            // Save position count
            if (_positionCountComboBox?.SelectedItem is int positionCount)
            {
                profile.PositionCount = positionCount;
            }

            // Save grid dimensions
            if (_gridRowsComboBox?.SelectedItem is int gridRows)
            {
                profile.GridRows = gridRows;
            }
            if (_gridColumnsComboBox?.SelectedItem is int gridColumns)
            {
                profile.GridColumns = gridColumns;
            }

            // Save text labels
            if (_labelTextBoxes != null)
            {
                for (int i = 0; i < _labelTextBoxes.Length; i++)
                {
                    if (_labelTextBoxes[i] != null)
                    {
                        // Ensure list is big enough
                        while (profile.TextLabels.Count <= i) profile.TextLabels.Add("");
                        
                        profile.TextLabels[i] = _labelTextBoxes[i].Text;
                    }
                }
            }

            // Save other values
            if (_fontSizeSlider != null) _settings.FontSize = (int)_fontSizeSlider.Value;
            if (_selectedColorTextBox != null) _settings.SelectedTextColor = _selectedColorTextBox.Text;
            if (_nonSelectedColorTextBox != null) _settings.NonSelectedTextColor = _nonSelectedColorTextBox.Text;
            if (_spacingSlider != null) _settings.ItemSpacing = (int)_spacingSlider.Value;
            if (_opacitySlider != null) _settings.MoveOverlayOpacity = (int)_opacitySlider.Value;
            
            // Save dial knob scale (round to nearest 0.5 to avoid float drift)
            if (_dialKnobScaleSlider != null) profile.DialKnobScale = Math.Round(_dialKnobScaleSlider.Value * 2) / 2;
            if (_dialLabelGapSlider != null) profile.DialLabelGapPercent = (int)_dialLabelGapSlider.Value;

            // Save theme preference
            if (_themePreferenceComboBox?.SelectedItem is System.Windows.Controls.ComboBoxItem themeItem && themeItem.Tag != null)
            {
                _settings.ThemePreference = Enum.Parse<ThemePreference>(themeItem.Tag.ToString()!);
            }
        }

        private void ShowDisplaySettings()
        {
            if (_settingsPanel == null || _settings == null) return;
            _settingsPanel.Children.Clear();

            var currentProfile = GetCurrentProfile();
            if (currentProfile == null) return;

            var wheelDef = WheelDefinition.SupportedWheels.FirstOrDefault(w => w.DeviceName == currentProfile.DeviceName)
                           ?? WheelDefinition.SupportedWheels[0];

            // Title
            var title = new TextBlock { Text = "Display & Device Settings", FontSize = 20, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 20) };
            title.SetResourceReference(TextBlock.ForegroundProperty, "ThemeForeground");
            _settingsPanel.Children.Add(title);

            // --- 1. Device Selection ---
            AddLabel("Wheel Device");
            _deviceComboBox = new System.Windows.Controls.ComboBox { Margin = new Thickness(0, 0, 0, 15) };
            
            foreach (var wheel in WheelDefinition.SupportedWheels)
            {
                _deviceComboBox.Items.Add(wheel.DeviceName);
            }
            
            _deviceComboBox.SelectedItem = _settings.SelectedDeviceName;
            
            _deviceComboBox.SelectionChanged += (s, e) =>
            {
                if (_deviceComboBox.SelectedItem is string newDeviceName && newDeviceName != _settings.SelectedDeviceName)
                {
                    _settings.SelectedDeviceName = newDeviceName;
                    
                    // Force profile switch to a valid one for the new device
                    _settings.SelectedProfileId = Guid.Empty; 
                    GetCurrentProfile(); // Self-healing will create/select a profile
                    
                    // Rebuild UI to reflect new device and profile
                    ShowDisplaySettings();
                }
            };
            
            _settingsPanel.Children.Add(_deviceComboBox);

            // --- 2. Profile Section ---
            AddLabel("Profile (for this device)");
            var profilePanel = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 15) };
            
            _profileComboBox = new System.Windows.Controls.ComboBox { Width = 200, Margin = new Thickness(0, 0, 10, 0) };
            var profiles = GetProfilesForCurrentDevice();
            foreach (var p in profiles)
            {
                var item = new ComboBoxItem { Content = p.Name, Tag = p.Id };
                _profileComboBox.Items.Add(item);
                if (p.Id == currentProfile.Id) _profileComboBox.SelectedItem = item;
            }
            
            // Handle Profile Switching
            _profileComboBox.SelectionChanged += (s, e) =>
            {
                if (_profileComboBox.SelectedItem is ComboBoxItem selected && (Guid)selected.Tag != _settings.SelectedProfileId)
                {
                    SaveCurrentCategoryValues(); // Save old profile first
                    _settings.SelectedProfileId = (Guid)selected.Tag;
                    ShowDisplaySettings(); // Rebuild UI
                }
            };

            var newBtn = new System.Windows.Controls.Button { Content = "New", Width = 60, Margin = new Thickness(0, 0, 5, 0) };
            newBtn.Click += (s, e) => 
            {
                SaveCurrentCategoryValues();
                var newProfile = CreateNewProfile($"New Profile", currentProfile.DeviceName);
                // Copy values
                newProfile.Layout = currentProfile.Layout;
                newProfile.TextLabels = new List<string>(currentProfile.TextLabels);
                _settings.SelectedProfileId = newProfile.Id;
                ShowDisplaySettings();
            };

            var renameBtn = new System.Windows.Controls.Button { Content = "Rename", Width = 60, Margin = new Thickness(0, 0, 5, 0) };
            renameBtn.Click += (s, e) =>
            {
                var inputDialog = new System.Windows.Window
                {
                    Title = "Rename Profile",
                    Width = 350,
                    Height = 150,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = this
                };

                var panel = new StackPanel { Margin = new Thickness(10) };
                panel.Children.Add(new TextBlock { Text = "Enter new profile name:", Margin = new Thickness(0, 0, 0, 10) });
                
                var textBox = new System.Windows.Controls.TextBox { Text = currentProfile.Name, Margin = new Thickness(0, 0, 0, 10) };
                panel.Children.Add(textBox);

                var buttonPanel = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, HorizontalAlignment = System.Windows.HorizontalAlignment.Right };
                var okBtn = new System.Windows.Controls.Button { Content = "OK", Width = 70, Margin = new Thickness(0, 0, 5, 0) };
                okBtn.Click += (sender, args) => { inputDialog.DialogResult = true; inputDialog.Close(); };
                var cancelBtn = new System.Windows.Controls.Button { Content = "Cancel", Width = 70 };
                cancelBtn.Click += (sender, args) => { inputDialog.DialogResult = false; inputDialog.Close(); };
                
                buttonPanel.Children.Add(okBtn);
                buttonPanel.Children.Add(cancelBtn);
                panel.Children.Add(buttonPanel);

                inputDialog.Content = panel;

                if (inputDialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(textBox.Text))
                {
                    currentProfile.Name = textBox.Text.Trim();
                    ShowDisplaySettings(); // Refresh
                }
            };

            var delBtn = new System.Windows.Controls.Button { Content = "Delete", Width = 60 };
            delBtn.Click += (s, e) =>
            {
                if (profiles.Count <= 1)
                {
                    System.Windows.MessageBox.Show("Cannot delete the only profile.", "Warning");
                    return;
                }
                
                if (System.Windows.MessageBox.Show($"Delete profile '{currentProfile.Name}'?", "Confirm", System.Windows.MessageBoxButton.YesNo) == System.Windows.MessageBoxResult.Yes)
                {
                    _settings.Profiles.Remove(currentProfile);
                    _settings.SelectedProfileId = _settings.Profiles.First(p => p.DeviceName == currentProfile.DeviceName).Id;
                    ShowDisplaySettings();
                }
            };

            profilePanel.Children.Add(_profileComboBox);
            profilePanel.Children.Add(newBtn);
            profilePanel.Children.Add(renameBtn);
            profilePanel.Children.Add(delBtn);
            _settingsPanel.Children.Add(profilePanel);

            // Set ViewModel's selected profile (currentProfile is guaranteed non-null due to early return above)
            _viewModel!.SelectedProfile = currentProfile!;
            
            // --- Conditional Visibility Section (overlay-visibility-and-ui-improvements) ---
            AddLabel("Conditional Visibility", "Show overlay only when this application is running");
            
            var filePanel = new StackPanel 
            { 
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 15)
            };
            
            _targetExeDisplay = new TextBlock 
            { 
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0),
                MinWidth = 200
            };
            _targetExeDisplay.SetResourceReference(TextBlock.ForegroundProperty, "ThemeForeground");
            UpdateTargetDisplay(currentProfile.TargetExecutablePath);
            
            _browseButton = new System.Windows.Controls.Button 
            { 
                Content = "Browse...",
                Width = 80,
                Margin = new Thickness(0, 0, 5, 0)
            };
            _browseButton.Click += OnBrowseClick;
            
            _clearButton = new System.Windows.Controls.Button 
            { 
                Content = "Clear",
                Width = 60
            };
            _clearButton.Click += OnClearClick;
            _clearButton.IsEnabled = !string.IsNullOrEmpty(currentProfile.TargetExecutablePath);
            
            filePanel.Children.Add(_targetExeDisplay);
            filePanel.Children.Add(_browseButton);
            filePanel.Children.Add(_clearButton);
            _settingsPanel.Children.Add(filePanel);
            
            // --- NEW v0.5.0: Position Count Configuration ---
            AddLabel("Number of Positions", "Configure how many positions your wheel has (2-20)");
            _positionCountComboBox = new System.Windows.Controls.ComboBox { Margin = new Thickness(0, 0, 0, 15) };
            foreach (int count in _viewModel.AvailablePositionCounts)
            {
                _positionCountComboBox.Items.Add(count);
            }
            _positionCountComboBox.SelectedItem = _viewModel.SelectedProfile.PositionCount;
            _positionCountComboBox.SelectionChanged += PositionCount_Changed;
            _settingsPanel.Children.Add(_positionCountComboBox);

            // --- NEW v0.5.0: Grid Dimensions Configuration (wrapped for visibility toggle) ---
            _gridControlsPanel = new StackPanel();
            _gridControlsPanel.Visibility = currentProfile.Layout == DisplayLayout.Grid 
                ? Visibility.Visible 
                : Visibility.Collapsed;
            
            var gridDimLabel = new TextBlock { Text = "Grid Layout Dimensions", FontSize = 14, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 10, 0, 5) };
            gridDimLabel.SetResourceReference(TextBlock.ForegroundProperty, "ThemeForeground");
            _gridControlsPanel.Children.Add(gridDimLabel);
            
            var gridDimensionsPanel = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, Margin = new Thickness(0, 5, 0, 5) };
            
            var rowsLabel = new TextBlock { Text = "Rows:", Width = 50, VerticalAlignment = VerticalAlignment.Center };
            rowsLabel.SetResourceReference(TextBlock.ForegroundProperty, "ThemeForeground");
            _gridRowsComboBox = new System.Windows.Controls.ComboBox { Width = 60, Margin = new Thickness(0, 0, 10, 0) };
            foreach (int row in _viewModel.AvailableRows)
            {
                _gridRowsComboBox.Items.Add(row);
            }
            _gridRowsComboBox.SelectedItem = _viewModel.SelectedProfile.GridRows;
            _gridRowsComboBox.SelectionChanged += GridDimensions_Changed;
            
            var timesLabel = new TextBlock { Text = "×", Margin = new Thickness(0, 0, 10, 0), VerticalAlignment = VerticalAlignment.Center };
            timesLabel.SetResourceReference(TextBlock.ForegroundProperty, "ThemeForeground");
            
            var columnsLabel = new TextBlock { Text = "Columns:", Width = 70, VerticalAlignment = VerticalAlignment.Center };
            columnsLabel.SetResourceReference(TextBlock.ForegroundProperty, "ThemeForeground");
            _gridColumnsComboBox = new System.Windows.Controls.ComboBox { Width = 60 };
            foreach (int col in _viewModel.AvailableColumns)
            {
                _gridColumnsComboBox.Items.Add(col);
            }
            _gridColumnsComboBox.SelectedItem = _viewModel.SelectedProfile.GridColumns;
            _gridColumnsComboBox.SelectionChanged += GridDimensions_Changed;
            
            gridDimensionsPanel.Children.Add(rowsLabel);
            gridDimensionsPanel.Children.Add(_gridRowsComboBox);
            gridDimensionsPanel.Children.Add(timesLabel);
            gridDimensionsPanel.Children.Add(columnsLabel);
            gridDimensionsPanel.Children.Add(_gridColumnsComboBox);
            _gridControlsPanel.Children.Add(gridDimensionsPanel);

            // --- Grid Preview ---
            var gridPreviewBorder = new Border 
            { 
                BorderThickness = new Thickness(1), 
                Padding = new Thickness(10), 
                Margin = new Thickness(0, 5, 0, 10) 
            };
            gridPreviewBorder.SetResourceReference(Border.BorderBrushProperty, "ThemeControlBorder");
            
            var gridPreviewPanel = new StackPanel();
            
            _gridCapacityText = new TextBlock 
            { 
                Text = _viewModel.GridCapacityDisplay, 
                FontSize = 11, 
                Margin = new Thickness(0, 0, 0, 5) 
            };
            _gridCapacityText.SetResourceReference(TextBlock.ForegroundProperty, "ThemeSubtext");
            gridPreviewPanel.Children.Add(_gridCapacityText);
            
            _gridPreviewControl = new ItemsControl { Margin = new Thickness(0, 5, 0, 0) };
            _gridPreviewControl.ItemsSource = _viewModel.GridPreviewCells;
            
            var gridPreviewTemplate = new ItemsPanelTemplate();
            var uniformGridFactory = new FrameworkElementFactory(typeof(System.Windows.Controls.Primitives.UniformGrid));
            uniformGridFactory.SetValue(System.Windows.Controls.Primitives.UniformGrid.RowsProperty, _viewModel.SelectedProfile.GridRows);
            uniformGridFactory.SetValue(System.Windows.Controls.Primitives.UniformGrid.ColumnsProperty, _viewModel.SelectedProfile.GridColumns);
            gridPreviewTemplate.VisualTree = uniformGridFactory;
            _gridPreviewControl.ItemsPanel = gridPreviewTemplate;
            
            var cellTemplate = new DataTemplate();
            var borderFactory = new FrameworkElementFactory(typeof(Border));
            borderFactory.SetResourceReference(Border.BorderBrushProperty, "ThemeControlBorder");
            borderFactory.SetValue(Border.BorderThicknessProperty, new Thickness(1));
            borderFactory.SetValue(Border.WidthProperty, 30.0);
            borderFactory.SetValue(Border.HeightProperty, 30.0);
            borderFactory.SetValue(Border.MarginProperty, new Thickness(2));
            
            var textBlockFactory = new FrameworkElementFactory(typeof(TextBlock));
            textBlockFactory.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding());
            textBlockFactory.SetValue(TextBlock.HorizontalAlignmentProperty, System.Windows.HorizontalAlignment.Center);
            textBlockFactory.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
            textBlockFactory.SetValue(TextBlock.FontSizeProperty, 10.0);
            textBlockFactory.SetResourceReference(TextBlock.ForegroundProperty, "ThemeForeground");
            
            borderFactory.AppendChild(textBlockFactory);
            cellTemplate.VisualTree = borderFactory;
            _gridPreviewControl.ItemTemplate = cellTemplate;
            
            gridPreviewPanel.Children.Add(_gridPreviewControl);
            gridPreviewBorder.Child = gridPreviewPanel;
            _gridControlsPanel.Children.Add(gridPreviewBorder);

            // --- Suggested Dimensions ---
            var sugDimLabel = new TextBlock { Text = "Suggested Dimensions", FontSize = 14, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 10, 0, 5) };
            sugDimLabel.SetResourceReference(TextBlock.ForegroundProperty, "ThemeForeground");
            _gridControlsPanel.Children.Add(sugDimLabel);
            
            _suggestedDimensionsControl = new ItemsControl { Margin = new Thickness(0, 5, 0, 15) };
            _suggestedDimensionsControl.ItemsSource = _viewModel.SuggestedDimensions;
            
            var wrapPanelTemplate = new ItemsPanelTemplate();
            var wrapPanelFactory = new FrameworkElementFactory(typeof(WrapPanel));
            wrapPanelTemplate.VisualTree = wrapPanelFactory;
            _suggestedDimensionsControl.ItemsPanel = wrapPanelTemplate;
            
            var buttonTemplate = new DataTemplate();
            var buttonFactory = new FrameworkElementFactory(typeof(System.Windows.Controls.Button));
            buttonFactory.SetBinding(System.Windows.Controls.Button.ContentProperty, new System.Windows.Data.Binding("DisplayText"));
            buttonFactory.SetValue(System.Windows.Controls.Button.MarginProperty, new Thickness(2));
            buttonFactory.AddHandler(System.Windows.Controls.Button.ClickEvent, new RoutedEventHandler(SuggestedDimension_Click));
            buttonTemplate.VisualTree = buttonFactory;
            _suggestedDimensionsControl.ItemTemplate = buttonTemplate;
            
            _gridControlsPanel.Children.Add(_suggestedDimensionsControl);
            _settingsPanel.Children.Add(_gridControlsPanel);

            // --- 3. Dynamic Text Labels ---
            AddLabel($"Position Labels");
            
            // Ensure lists are synced with PositionCount
            while (currentProfile.TextLabels.Count < currentProfile.PositionCount) currentProfile.TextLabels.Add("");
            if (currentProfile.TextLabels.Count > currentProfile.PositionCount)
            {
                currentProfile.TextLabels.RemoveRange(currentProfile.PositionCount, currentProfile.TextLabels.Count - currentProfile.PositionCount);
            }

            // Add ScrollViewer for text labels
            var scrollViewer = new ScrollViewer 
            { 
                MaxHeight = 300, 
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Margin = new Thickness(0, 5, 0, 15)
            };
            
            var labelsPanel = new StackPanel();
            _labelTextBoxes = new System.Windows.Controls.TextBox[currentProfile.PositionCount];
            for (int i = 0; i < currentProfile.PositionCount; i++)
            {
                var panel = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, Margin = new Thickness(0, 5, 0, 0) };
                var label = new TextBlock { Text = $"Position {i + 1}:", Width = 80, VerticalAlignment = VerticalAlignment.Center };
                label.SetResourceReference(TextBlock.ForegroundProperty, "ThemeForeground");
                var val = (i < currentProfile.TextLabels.Count) ? currentProfile.TextLabels[i] : "";
                var textBox = new System.Windows.Controls.TextBox { Width = 200, Text = val };
                _labelTextBoxes[i] = textBox;
                panel.Children.Add(label);
                panel.Children.Add(textBox);
                labelsPanel.Children.Add(panel);
            }
            
            scrollViewer.Content = labelsPanel;
            _settingsPanel.Children.Add(scrollViewer);

            // --- 4. Layout Section ---
            AddLabel("Display Layout");
            _layoutComboBox = new System.Windows.Controls.ComboBox { Margin = new Thickness(0, 0, 0, 15) };
            _layoutComboBox.Items.Add(CreateComboBoxItem("Single Text", "Single"));
            _layoutComboBox.Items.Add(CreateComboBoxItem("Vertical List", "Vertical"));
            _layoutComboBox.Items.Add(CreateComboBoxItem("Horizontal List", "Horizontal"));
            _layoutComboBox.Items.Add(CreateComboBoxItem("Grid", "Grid"));
            _layoutComboBox.Items.Add(CreateComboBoxItem("Dial", "Dial"));
            
            foreach (System.Windows.Controls.ComboBoxItem item in _layoutComboBox.Items)
            {
                if (item.Tag.ToString() == currentProfile.Layout.ToString())
                {
                    _layoutComboBox.SelectedItem = item;
                    break;
                }
            }
            _settingsPanel.Children.Add(_layoutComboBox);
            
            // Update grid/dial controls visibility when layout changes
            _layoutComboBox.SelectionChanged += (s, e) =>
            {
                if (_layoutComboBox.SelectedItem is ComboBoxItem selectedItem)
                {
                    var layoutTag = selectedItem.Tag?.ToString();
                    if (_gridControlsPanel != null)
                        _gridControlsPanel.Visibility = layoutTag == "Grid" ? Visibility.Visible : Visibility.Collapsed;
                    if (_dialControlsPanel != null)
                        _dialControlsPanel.Visibility = layoutTag == "Dial" ? Visibility.Visible : Visibility.Collapsed;
                }
            };

            // --- 4b. Dial-specific controls (always built, visibility toggled) ---
            _dialControlsPanel = new StackPanel();
            _dialControlsPanel.Visibility = currentProfile.Layout == DisplayLayout.Dial
                ? Visibility.Visible
                : Visibility.Collapsed;

            // Labeled separator: --- Dial Settings ---
            var dialSepContainer = new Grid { Margin = new Thickness(0, 15, 0, 10) };
            dialSepContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            dialSepContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            dialSepContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var dialSepLeft = new Separator { VerticalAlignment = VerticalAlignment.Center };
            dialSepLeft.SetResourceReference(Separator.BackgroundProperty, "ThemeControlBorder");
            Grid.SetColumn(dialSepLeft, 0);

            var dialSepText = new TextBlock { Text = "Dial Settings", FontSize = 12, Margin = new Thickness(10, 0, 10, 0) };
            dialSepText.SetResourceReference(TextBlock.ForegroundProperty, "ThemeSubtext");
            Grid.SetColumn(dialSepText, 1);

            var dialSepRight = new Separator { VerticalAlignment = VerticalAlignment.Center };
            dialSepRight.SetResourceReference(Separator.BackgroundProperty, "ThemeControlBorder");
            Grid.SetColumn(dialSepRight, 2);

            dialSepContainer.Children.Add(dialSepLeft);
            dialSepContainer.Children.Add(dialSepText);
            dialSepContainer.Children.Add(dialSepRight);
            _dialControlsPanel.Children.Add(dialSepContainer);

            var dialKnobLabel = new TextBlock { Text = "Dial Knob Size", FontSize = 14, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 10, 0, 5) };
            dialKnobLabel.SetResourceReference(TextBlock.ForegroundProperty, "ThemeForeground");
            var dialKnobInfo = new Border
            {
                Width = 16,
                Height = 16,
                CornerRadius = new CornerRadius(8),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(6, 10, 0, 5),
                VerticalAlignment = VerticalAlignment.Center,
                Cursor = System.Windows.Input.Cursors.Help,
                Background = System.Windows.Media.Brushes.Transparent
            };
            dialKnobInfo.SetResourceReference(Border.BorderBrushProperty, "ThemeSubtext");
            var dialKnobInfoText = new TextBlock
            {
                Text = "i",
                FontSize = 10,
                FontStyle = FontStyles.Italic,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            dialKnobInfoText.SetResourceReference(TextBlock.ForegroundProperty, "ThemeSubtext");
            dialKnobInfo.Child = dialKnobInfoText;
            ToolTipService.SetInitialShowDelay(dialKnobInfo, 0);
            ToolTipService.SetShowDuration(dialKnobInfo, 30000);
            dialKnobInfo.ToolTip = "Scale the knob graphic (1 = smallest, 10 = largest). Text stays the same size.";
            var dialKnobLabelPanel = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal };
            dialKnobLabelPanel.Children.Add(dialKnobLabel);
            dialKnobLabelPanel.Children.Add(dialKnobInfo);
            _dialControlsPanel.Children.Add(dialKnobLabelPanel);
            _dialKnobScaleSlider = AddSlider(1, 10, 0.5, Math.Round(currentProfile.DialKnobScale * 2) / 2);
            _dialKnobScaleSlider.IsSnapToTickEnabled = true;
            // AddSlider adds a wrapper panel to _settingsPanel — move it into _dialControlsPanel
            var knobSliderWrapper = (UIElement)_settingsPanel.Children[_settingsPanel.Children.Count - 1];
            _settingsPanel.Children.Remove(knobSliderWrapper);
            _dialControlsPanel.Children.Add(knobSliderWrapper);

            var dialGapLabel = new TextBlock { Text = "Label Gap", FontSize = 14, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 10, 0, 5) };
            dialGapLabel.SetResourceReference(TextBlock.ForegroundProperty, "ThemeForeground");
            var dialGapInfo = new Border
            {
                Width = 16,
                Height = 16,
                CornerRadius = new CornerRadius(8),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(6, 10, 0, 5),
                VerticalAlignment = VerticalAlignment.Center,
                Cursor = System.Windows.Input.Cursors.Help,
                Background = System.Windows.Media.Brushes.Transparent
            };
            dialGapInfo.SetResourceReference(Border.BorderBrushProperty, "ThemeSubtext");
            var dialGapInfoText = new TextBlock
            {
                Text = "i",
                FontSize = 10,
                FontStyle = FontStyles.Italic,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            dialGapInfoText.SetResourceReference(TextBlock.ForegroundProperty, "ThemeSubtext");
            dialGapInfo.Child = dialGapInfoText;
            ToolTipService.SetInitialShowDelay(dialGapInfo, 0);
            ToolTipService.SetShowDuration(dialGapInfo, 30000);
            dialGapInfo.ToolTip = "Gap between cog edge and text (% of knob radius)";
            var dialGapLabelPanel = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal };
            dialGapLabelPanel.Children.Add(dialGapLabel);
            dialGapLabelPanel.Children.Add(dialGapInfo);
            _dialControlsPanel.Children.Add(dialGapLabelPanel);
            _dialLabelGapSlider = AddSlider(10, 20, 1, currentProfile.DialLabelGapPercent);
            _dialLabelGapSlider.IsSnapToTickEnabled = true;
            var gapSliderWrapper = (UIElement)_settingsPanel.Children[_settingsPanel.Children.Count - 1];
            _settingsPanel.Children.Remove(gapSliderWrapper);
            _dialControlsPanel.Children.Add(gapSliderWrapper);
            _settingsPanel.Children.Add(_dialControlsPanel);

            // --- 5. Font Size & Spacing ---
            var fontSepContainer = new Grid { Margin = new Thickness(0, 15, 0, 10) };
            fontSepContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            fontSepContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            fontSepContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var fontSepLeft = new Separator { VerticalAlignment = VerticalAlignment.Center };
            fontSepLeft.SetResourceReference(Separator.BackgroundProperty, "ThemeControlBorder");
            Grid.SetColumn(fontSepLeft, 0);

            var fontSepText = new TextBlock { Text = "Font Settings", FontSize = 12, Margin = new Thickness(10, 0, 10, 0) };
            fontSepText.SetResourceReference(TextBlock.ForegroundProperty, "ThemeSubtext");
            Grid.SetColumn(fontSepText, 1);

            var fontSepRight = new Separator { VerticalAlignment = VerticalAlignment.Center };
            fontSepRight.SetResourceReference(Separator.BackgroundProperty, "ThemeControlBorder");
            Grid.SetColumn(fontSepRight, 2);

            fontSepContainer.Children.Add(fontSepLeft);
            fontSepContainer.Children.Add(fontSepText);
            fontSepContainer.Children.Add(fontSepRight);
            _settingsPanel.Children.Add(fontSepContainer);

            AddLabel("Font Size", "Text size for overlay labels (10-80 pt)");
            _fontSizeSlider = AddSlider(10, 80, 1, _settings.FontSize);

            AddLabel("Item Spacing", "Space between items in pixels");
            _spacingSlider = AddSlider(0, 20, 1, _settings.ItemSpacing);
        }

        private void ShowAppearanceSettings()
        {
            if (_settingsPanel == null || _settings == null) return;
            _settingsPanel.Children.Clear();

            var title = new TextBlock { Text = "Appearance Settings", FontSize = 20, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 20) };
            title.SetResourceReference(TextBlock.ForegroundProperty, "ThemeForeground");
            _settingsPanel.Children.Add(title);

            AddLabel("Theme");
            _themePreferenceComboBox = new System.Windows.Controls.ComboBox { Width = 200, HorizontalAlignment = System.Windows.HorizontalAlignment.Left, Margin = new Thickness(0, 0, 0, 15) };
            _themePreferenceComboBox.SetResourceReference(System.Windows.Controls.ComboBox.BackgroundProperty, "ThemeControlBackground");
            _themePreferenceComboBox.SetResourceReference(System.Windows.Controls.ComboBox.ForegroundProperty, "ThemeControlForeground");
            _themePreferenceComboBox.SetResourceReference(System.Windows.Controls.ComboBox.BorderBrushProperty, "ThemeControlBorder");
            _themePreferenceComboBox.Items.Add(CreateComboBoxItem("System Default", "System"));
            _themePreferenceComboBox.Items.Add(CreateComboBoxItem("Light", "Light"));
            _themePreferenceComboBox.Items.Add(CreateComboBoxItem("Dark", "Dark"));
            // Select current preference
            foreach (System.Windows.Controls.ComboBoxItem item in _themePreferenceComboBox.Items)
            {
                if (item.Tag?.ToString() == _settings.ThemePreference.ToString())
                {
                    _themePreferenceComboBox.SelectedItem = item;
                    break;
                }
            }
            _settingsPanel.Children.Add(_themePreferenceComboBox);

            AddLabel("Selected Text Color");
            _selectedColorTextBox = AddColorPicker(_settings.SelectedTextColor);

            AddLabel("Non-Selected Text Color");
            _nonSelectedColorTextBox = AddColorPicker(_settings.NonSelectedTextColor);
        }

        private System.Windows.Controls.TextBox AddColorPicker(string initialColor)
        {
            var panel = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 15) };
            var textBox = new System.Windows.Controls.TextBox { Text = initialColor, Width = 100, VerticalAlignment = VerticalAlignment.Center };
            var pickButton = new System.Windows.Controls.Button { Content = "Pick", Width = 50, Margin = new Thickness(10, 0, 0, 0) };
            
            pickButton.Click += (s, e) =>
            {
                var dialog = new System.Windows.Forms.ColorDialog();
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    var color = dialog.Color;
                    textBox.Text = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
                }
            };

            panel.Children.Add(textBox);
            panel.Children.Add(pickButton);
            _settingsPanel?.Children.Add(panel);
            return textBox;
        }

        private void ShowAdvancedSettings()
        {
            if (_settingsPanel == null || _settings == null) return;
            _settingsPanel.Children.Clear();

            var title = new TextBlock { Text = "Advanced Settings", FontSize = 20, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 20) };
            title.SetResourceReference(TextBlock.ForegroundProperty, "ThemeForeground");
            _settingsPanel.Children.Add(title);

            AddLabel("Move Overlay Opacity", "Overlay transparency when repositioning (0 = invisible, 100 = fully opaque)");
            _opacitySlider = AddSlider(0, 100, 10, _settings.MoveOverlayOpacity);

            // --- Review or Reset Settings section ---
            var separator = new Border
            {
                BorderThickness = new Thickness(0, 1, 0, 0),
                Margin = new Thickness(0, 20, 0, 0)
            };
            separator.SetResourceReference(Border.BorderBrushProperty, "ThemeControlBorder");
            _settingsPanel.Children.Add(separator);

            var sectionLabel = new TextBlock
            {
                Text = "Review or Reset Settings",
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 20, 0, 10)
            };
            sectionLabel.SetResourceReference(TextBlock.ForegroundProperty, "ThemeForeground");
            _settingsPanel.Children.Add(sectionLabel);

            var openFolderButton = new System.Windows.Controls.Button
            {
                Content = "Open Settings Folder",
                Width = 180,
                Margin = new Thickness(0, 0, 0, 8),
                HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                ToolTip = "Opens the folder containing your settings and log files in File Explorer"
            };
            openFolderButton.SetResourceReference(System.Windows.Controls.Control.BackgroundProperty, "ThemeControlBackground");
            openFolderButton.SetResourceReference(System.Windows.Controls.Control.ForegroundProperty, "ThemeControlForeground");
            openFolderButton.SetResourceReference(System.Windows.Controls.Control.BorderBrushProperty, "ThemeControlBorder");
            openFolderButton.Click += OpenSettingsFolder_Click;
            _settingsPanel.Children.Add(openFolderButton);

            var resetButton = new System.Windows.Controls.Button
            {
                Content = "Reset to Defaults",
                Width = 180,
                Margin = new Thickness(0, 0, 0, 8),
                HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                ToolTip = "Deletes your settings file and restores all options to their default values"
            };
            resetButton.SetResourceReference(System.Windows.Controls.Control.BackgroundProperty, "ThemeControlBackground");
            resetButton.SetResourceReference(System.Windows.Controls.Control.ForegroundProperty, "ThemeControlForeground");
            resetButton.SetResourceReference(System.Windows.Controls.Control.BorderBrushProperty, "ThemeControlBorder");
            resetButton.Click += ResetSettings_Click;
            _settingsPanel.Children.Add(resetButton);
        }

        private void AddLabel(string text, string? tooltip = null)
        {
            if (_settingsPanel == null) return;

            var container = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 5) };

            var label = new TextBlock { Text = text, FontSize = 14, FontWeight = FontWeights.SemiBold };
            label.SetResourceReference(TextBlock.ForegroundProperty, "ThemeForeground");
            container.Children.Add(label);

            if (tooltip != null)
            {
                var infoBorder = new Border
                {
                    Width = 16,
                    Height = 16,
                    CornerRadius = new CornerRadius(8),
                    BorderThickness = new Thickness(1),
                    Margin = new Thickness(6, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    Cursor = System.Windows.Input.Cursors.Help,
                    Background = System.Windows.Media.Brushes.Transparent
                };
                infoBorder.SetResourceReference(Border.BorderBrushProperty, "ThemeSubtext");

                var infoText = new TextBlock
                {
                    Text = "i",
                    FontSize = 10,
                    FontStyle = FontStyles.Italic,
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                infoText.SetResourceReference(TextBlock.ForegroundProperty, "ThemeSubtext");
                infoBorder.Child = infoText;

                ToolTipService.SetInitialShowDelay(infoBorder, 0);
                ToolTipService.SetShowDuration(infoBorder, 30000);
                infoBorder.ToolTip = tooltip;

                container.Children.Add(infoBorder);
            }

            _settingsPanel.Children.Add(container);
        }


        private Slider AddSlider(double min, double max, double tickFreq, double value)
        {
            if (_settingsPanel == null) return new Slider();
            var panel = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 15) };
            var slider = new Slider { Minimum = min, Maximum = max, Width = 200, TickFrequency = tickFreq, IsSnapToTickEnabled = true, Value = value };
            var valueText = new TextBlock { Margin = new Thickness(10, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
            valueText.SetResourceReference(TextBlock.ForegroundProperty, "ThemeForeground");
            valueText.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding("Value") { Source = slider });
            panel.Children.Add(slider);
            panel.Children.Add(valueText);
            _settingsPanel.Children.Add(panel);
            return slider;
        }

        private System.Windows.Controls.ComboBoxItem CreateComboBoxItem(string content, string tag)
        {
            return new System.Windows.Controls.ComboBoxItem { Content = content, Tag = tag };
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            if (_settings == null) return;

            // Save current category values
            SaveCurrentCategoryValues();

            _settings.Save();
            
            // Notify that settings changed
            SettingsChanged?.Invoke(this, EventArgs.Empty);
        }

        private void PositionCount_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_positionCountComboBox == null || _viewModel?.SelectedProfile == null) return;
            
            int newCount = (int)_positionCountComboBox.SelectedItem;
            int oldCount = _viewModel.SelectedProfile.TextLabels.Count;
            
            if (newCount < oldCount)
            {
                // Check if any positions being removed have text
                bool hasPopulatedPositions = _viewModel.SelectedProfile.TextLabels
                    .Skip(newCount)
                    .Any(label => !string.IsNullOrWhiteSpace(label));
                
                if (hasPopulatedPositions)
                {
                    var result = System.Windows.MessageBox.Show(
                        $"Reducing position count will remove labels for positions {newCount + 1}-{oldCount}. Continue?",
                        "Confirm Position Count Change",
                        System.Windows.MessageBoxButton.YesNo,
                        System.Windows.MessageBoxImage.Warning);
                    
                    if (result == System.Windows.MessageBoxResult.No)
                    {
                        _positionCountComboBox.SelectedItem = oldCount;
                        return;
                    }
                }
            }
            
            _viewModel.UpdatePositionCount(newCount);
            ValidateAndAdjustGridDimensions();
            
            // Refresh the display to show updated text label inputs
            ShowDisplaySettings();
        }

        private void GridDimensions_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_gridRowsComboBox == null || _gridColumnsComboBox == null || _viewModel?.SelectedProfile == null) return;
            
            // Update profile with new dimensions
            _viewModel.SelectedProfile.GridRows = (int)_gridRowsComboBox.SelectedItem;
            _viewModel.SelectedProfile.GridColumns = (int)_gridColumnsComboBox.SelectedItem;
            
            ValidateGridDimensions();
        }

        private void ValidateAndAdjustGridDimensions()
        {
            var profile = _viewModel?.SelectedProfile;
            if (profile == null) return;
            
            if (!profile.IsValidGridConfiguration())
            {
                profile.AdjustGridToDefault();
                _viewModel?.RefreshGridPreview();
                
                // Update the combo boxes
                if (_gridRowsComboBox != null)
                    _gridRowsComboBox.SelectedItem = profile.GridRows;
                if (_gridColumnsComboBox != null)
                    _gridColumnsComboBox.SelectedItem = profile.GridColumns;
                
                System.Windows.MessageBox.Show(
                    $"Grid dimensions adjusted to {profile.GridRows}×{profile.GridColumns} to accommodate {profile.PositionCount} positions.",
                    "Grid Adjusted",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
            }
        }

        private void ValidateGridDimensions()
        {
            var profile = _viewModel?.SelectedProfile;
            if (profile == null) return;
            
            var result = ProfileValidator.ValidateGridDimensions(profile);
            
            if (!result.IsValid)
            {
                System.Windows.MessageBox.Show(result.Message, "Invalid Grid Configuration", 
                          System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                profile.AdjustGridToDefault();
                
                // Update the combo boxes
                if (_gridRowsComboBox != null)
                    _gridRowsComboBox.SelectedItem = profile.GridRows;
                if (_gridColumnsComboBox != null)
                    _gridColumnsComboBox.SelectedItem = profile.GridColumns;
            }
            
            _viewModel?.RefreshGridPreview();
            
            // Update grid preview panel
            if (_gridPreviewControl != null)
            {
                var gridPreviewTemplate = new ItemsPanelTemplate();
                var uniformGridFactory = new FrameworkElementFactory(typeof(System.Windows.Controls.Primitives.UniformGrid));
                uniformGridFactory.SetValue(System.Windows.Controls.Primitives.UniformGrid.RowsProperty, profile.GridRows);
                uniformGridFactory.SetValue(System.Windows.Controls.Primitives.UniformGrid.ColumnsProperty, profile.GridColumns);
                gridPreviewTemplate.VisualTree = uniformGridFactory;
                _gridPreviewControl.ItemsPanel = gridPreviewTemplate;
            }
            
            // Update capacity display
            if (_gridCapacityText != null && _viewModel != null)
            {
                _gridCapacityText.Text = _viewModel.GridCapacityDisplay;
            }
        }

        private void SuggestedDimension_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.DataContext is SuggestedDimension dimension)
            {
                if (_viewModel?.SelectedProfile == null) return;
                
                _viewModel.SelectedProfile.GridRows = dimension.Rows;
                _viewModel.SelectedProfile.GridColumns = dimension.Columns;
                
                // Update the combo boxes
                if (_gridRowsComboBox != null)
                    _gridRowsComboBox.SelectedItem = dimension.Rows;
                if (_gridColumnsComboBox != null)
                    _gridColumnsComboBox.SelectedItem = dimension.Columns;
                
                _viewModel.RefreshGridPreview();
                
                // Update grid preview panel
                if (_gridPreviewControl != null)
                {
                    var gridPreviewTemplate = new ItemsPanelTemplate();
                    var uniformGridFactory = new FrameworkElementFactory(typeof(System.Windows.Controls.Primitives.UniformGrid));
                    uniformGridFactory.SetValue(System.Windows.Controls.Primitives.UniformGrid.RowsProperty, dimension.Rows);
                    uniformGridFactory.SetValue(System.Windows.Controls.Primitives.UniformGrid.ColumnsProperty, dimension.Columns);
                    gridPreviewTemplate.VisualTree = uniformGridFactory;
                    _gridPreviewControl.ItemsPanel = gridPreviewTemplate;
                }
                
                // Update capacity display
                if (_gridCapacityText != null)
                {
                    _gridCapacityText.Text = _viewModel.GridCapacityDisplay;
                }
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        // --- Settings File Management ---

        private void OpenSettingsFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var directory = AppSettings.GetSettingsDirectory();
                if (System.IO.Directory.Exists(directory))
                {
                    System.Diagnostics.Process.Start("explorer.exe", directory);
                }
                else
                {
                    System.Windows.MessageBox.Show(
                        "Settings folder does not exist yet. It will be created when settings are first saved.",
                        "Settings Folder",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                Services.LogService.Error("Failed to open settings folder", ex);
            }
        }

        private void ResetSettings_Click(object sender, RoutedEventArgs e)
        {
            var result = System.Windows.MessageBox.Show(
                "This will reset all settings to their default values.\n\nAre you sure?",
                "Reset Settings",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                var settingsPath = AppSettings.GetSettingsPath();
                if (System.IO.File.Exists(settingsPath))
                {
                    System.IO.File.Delete(settingsPath);
                    Services.LogService.Info("Settings file deleted by user request (reset to defaults)");
                }

                // Reload fresh defaults
                _settings = AppSettings.Load();
                _settings.Save();

                // Notify the main app so the overlay picks up the new settings
                SettingsChanged?.Invoke(this, EventArgs.Empty);

                // Close settings window — user can reopen to see defaults
                Close();
            }
            catch (Exception ex)
            {
                Services.LogService.Error("Failed to reset settings", ex);
                System.Windows.MessageBox.Show(
                    "Failed to reset settings. You can manually delete the file from the settings folder.",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        
        // --- Conditional Visibility UI Methods (overlay-visibility-and-ui-improvements) ---
        
        private void OnBrowseClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "Select Target Application",
                    Filter = "Executable Files (*.exe)|*.exe|All Files (*.*)|*.*",
                    FilterIndex = 1,
                    CheckFileExists = true,
                    CheckPathExists = true
                };
                
                if (dialog.ShowDialog() == true)
                {
                    var selectedPath = dialog.FileName;
                    
                    // Validate file exists
                    if (!System.IO.File.Exists(selectedPath))
                    {
                        Services.LogService.Info($"Selected file does not exist: {selectedPath}");
                        System.Windows.MessageBox.Show(
                            "The selected file does not exist. Please select a valid executable file.",
                            "File Not Found",
                            System.Windows.MessageBoxButton.OK,
                            System.Windows.MessageBoxImage.Warning);
                        return;
                    }
                    
                    // Validate file extension
                    var extension = System.IO.Path.GetExtension(selectedPath);
                    if (!string.Equals(extension, ".exe", StringComparison.OrdinalIgnoreCase))
                    {
                        Services.LogService.Info($"Selected file is not an executable: {selectedPath}");
                        System.Windows.MessageBox.Show(
                            "Please select an executable file (.exe).",
                            "Invalid File Type",
                            System.Windows.MessageBoxButton.OK,
                            System.Windows.MessageBoxImage.Warning);
                        return;
                    }
                    
                    if (_viewModel?.SelectedProfile != null)
                    {
                        _viewModel.SelectedProfile.TargetExecutablePath = selectedPath;
                        UpdateTargetDisplay(selectedPath);
                        Services.LogService.Info($"Target executable set to: {selectedPath}");
                    }
                }
            }
            catch (Exception ex)
            {
                Services.LogService.Error("Error selecting target executable", ex);
                System.Windows.MessageBox.Show(
                    "An error occurred while selecting the file. Please try again.",
                    "Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }
        
        private void OnClearClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_viewModel?.SelectedProfile != null)
                {
                    _viewModel.SelectedProfile.TargetExecutablePath = null;
                    UpdateTargetDisplay(null);
                    Services.LogService.Info("Target executable cleared - overlay will be always visible");
                }
            }
            catch (Exception ex)
            {
                Services.LogService.Error("Error clearing target executable", ex);
                System.Windows.MessageBox.Show(
                    "An error occurred while clearing the selection. Please try again.",
                    "Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }
        
        private void UpdateTargetDisplay(string? path)
        {
            try
            {
                if (_targetExeDisplay == null) return;
                
                if (string.IsNullOrEmpty(path))
                {
                    _targetExeDisplay.Text = "(None - always visible)";
                    if (_clearButton != null)
                        _clearButton.IsEnabled = false;
                }
                else
                {
                    // Safely extract filename, handling invalid path characters
                    try
                    {
                        _targetExeDisplay.Text = System.IO.Path.GetFileName(path);
                    }
                    catch (ArgumentException)
                    {
                        // Path contains invalid characters, display as-is
                        _targetExeDisplay.Text = path;
                        Services.LogService.Info($"Path contains invalid characters: {path}");
                    }
                    
                    if (_clearButton != null)
                        _clearButton.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                Services.LogService.Error("Error updating target display", ex);
                // Fail gracefully - just show the path as-is
                if (_targetExeDisplay != null && !string.IsNullOrEmpty(path))
                {
                    _targetExeDisplay.Text = path;
                }
            }
        }
    }
}

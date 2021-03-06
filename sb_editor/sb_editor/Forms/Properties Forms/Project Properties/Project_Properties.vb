Imports System.IO
Imports IniFileFunctions
Imports sb_editor.ReaderClasses
Imports sb_editor.WritersClasses

Partial Public Class Project_Properties
    '*===============================================================================================
    '* GLOBAL VARIABLES
    '*===============================================================================================
    Private ReadOnly textFileReaders As New FileParsers()
    Private ReadOnly textFileWriters As New FileWriters
    Private ReadOnly ratesNames As String() = New String() {"Minimum", "Low", "Medium", "High", "Maximum"}
    Private promptSave As Boolean = True
    Private ratesNamesIndex As Byte = 0
    Private ReadOnly mainform As MainFrame

    '*===============================================================================================
    '* FORM EVENTS
    '*===============================================================================================
    Sub New(mainformObj As MainFrame)
        ' Esta llamada es exigida por el diseñador.
        InitializeComponent()

        ' Agregue cualquier inicialización después de la llamada a InitializeComponent().
        mainform = mainformObj
    End Sub

    Private Sub Project_Properties_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ProjectSettingsFile = textFileReaders.ReadPropertiesFile(SysFileProperties)

        'Put available formats and select the first one
        If ProjectSettingsFile.AvailableFormats.Length > 0 Then
            'Print available formats
            CopyArrayToListView(ListView_Formats, ProjectSettingsFile.AvailableFormats)
            'Select first item
            ListView_Formats.Items(0).Selected = True
            ListView_Formats.Items(0).Focused = True
        End If
        'Select the first item by default - Formats
        If ComboBox_Platform.Items.Count > 0 Then
            ComboBox_Platform.SelectedIndex = 0
        End If
        'Print available formats in the combo
        If ProjectSettingsFile.AvailableReSampleRates.Count > 0 Then
            'Add available rates format to the combobox and select the first one
            If ProjectSettingsFile.AvailableFormats.Length > 0 Then
                ComboBox_RatesFormat.Items.AddRange(GetColumn(ProjectSettingsFile.AvailableFormats, 0))
                ComboBox_RatesFormat.SelectedIndex = 0
            End If
            Dim sampleRatesArray As String() = ProjectSettingsFile.AvailableReSampleRates.ToArray
            'Add available rates to the listbox
            ListBox_SampleRates.Items.AddRange(sampleRatesArray)
            'Add available rates to the combobox
            ComboBox_DefaultSampleRate.Items.AddRange(sampleRatesArray)
            ComboBox_DefaultSampleRate.SelectedIndex = ProjectSettingsFile.MiscProps.DefaultRate
        End If

        'Misc properties
        Textbox_Master_Path.Text = ProjectSettingsFile.MiscProps.SampleFileFolder
        Textbox_SonixFolder.Text = ProjectSettingsFile.MiscProps.HashCodeFileFolder
        Textbox_EngineXFolder.Text = ProjectSettingsFile.MiscProps.EngineXFolder
        Textbox_EuroLandServer.Text = ProjectSettingsFile.MiscProps.EuroLandHashCodeServerPath
        TextBox_UserName.Text = EuroSoundUser
        TextBox_EditWavs.Text = ProjAudioEditor
        TextBox_TextEditor.Text = ProjTextEditor

        'SoundBank Sizes
        Numeric_PlayStationMaxSize.Value = SoundBankMaxPlayStation
        Numeric_PcMaxSize.Value = SoundBankMaxPC
        Numeric_GameCubeMaxSize.Value = SoundBankMaxGameCube
        Numeric_XboxMaxSize.Value = SoundBankMaxXbox

        'Others
        CheckBox_PrefixHashCodes.Checked = ProjectSettingsFile.MiscProps.PrefixHtSound
        Checkbox_ViewPrePostOutputCommands.Checked = ProjectSettingsFile.MiscProps.ViewOutputDos
    End Sub

    Private Sub Project_Properties_Shown(sender As Object, e As EventArgs) Handles MyBase.Shown
        'Ensure that we have an username
        If StrComp(EuroSoundUser, "") = 0 Then
            TextBox_UserName.Text = AskForUserName("MyName")
            RestartEuroSound()
        End If
    End Sub

    Private Sub Project_Properties_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        'Check closing reason
        If e.CloseReason = CloseReason.UserClosing Then
            'Check if we have to show the message
            If promptSave Then
                'Ask user what wants to do
                My.Computer.Audio.PlaySystemSound(Media.SystemSounds.Exclamation)
                Dim save As MsgBoxResult = MsgBox("Are you sure you wish to Quit Properties without saving?", vbOKCancel + vbQuestion, "Confirm Quit")
                'Cancel close if user not want to quit
                If save = MsgBoxResult.Cancel Then
                    e.Cancel = True
                Else
                    'Load Project Data
                    LoadProjectData(mainform, Path.Combine(WorkingDirectory, "Project.txt"))
                End If
            Else
                'Load Project Data
                LoadProjectData(mainform, Path.Combine(WorkingDirectory, "Project.txt"))
            End If
        End If

    End Sub

    '*===============================================================================================
    '* FORM BUTTONS
    '*===============================================================================================
    Private Sub Button_OK_Click(sender As Object, e As EventArgs) Handles Button_OK.Click
        'Disable save file message
        promptSave = False
        'Update variables
        ProjectSettingsFile.AvailableFormats = ParseListViewToMatrix()
        ProjectSettingsFile.MiscProps.SampleFileFolder = Textbox_Master_Path.Text
        ProjectSettingsFile.MiscProps.HashCodeFileFolder = Textbox_SonixFolder.Text
        ProjectSettingsFile.MiscProps.EngineXFolder = Textbox_EngineXFolder.Text
        ProjectSettingsFile.MiscProps.EuroLandHashCodeServerPath = Textbox_EuroLandServer.Text
        ProjectSettingsFile.MiscProps.PrefixHtSound = CheckBox_PrefixHashCodes.Checked
        ProjectSettingsFile.MiscProps.ViewOutputDos = Checkbox_ViewPrePostOutputCommands.Checked

        'SoundBank Max sizes
        SoundBankMaxPlayStation = Numeric_PlayStationMaxSize.Value
        SoundBankMaxPC = Numeric_PcMaxSize.Value
        SoundBankMaxGameCube = Numeric_GameCubeMaxSize.Value
        SoundBankMaxXbox = Numeric_XboxMaxSize.Value

        'Save file
        textFileWriters.SavePropertiesFile(ProjectSettingsFile, SysFileProperties)
        SaveIniFile()
        'Update program ini file
        Dim baseIniFile As New IniFile(EuroSoundIniFilePath)
        ProjAudioEditor = TextBox_EditWavs.Text
        ProjTextEditor = TextBox_TextEditor.Text
        baseIniFile.Write("Edit_Wavs_With", ProjAudioEditor, "Form7_Misc")
        baseIniFile.Write("TextEditor", ProjTextEditor, "PropertiesForm")
        'Close form
        Close()
    End Sub

    Private Sub Button_Cancel_Click(sender As Object, e As EventArgs) Handles Button_Cancel.Click
        'Close form
        Close()
    End Sub

    '*===============================================================================================
    '* FOLDER PATHS
    '*===============================================================================================
    Private Sub Button_Master_Path_Click(sender As Object, e As EventArgs) Handles Button_Master_Path.Click
        'Update desc
        FolderBrowserDialog.Description = "Set Folder for Sample Files."
        'Set results
        If FolderBrowserDialog.ShowDialog = DialogResult.OK Then
            'Ensure that the master folder exists
            If Directory.Exists(Path.Combine(FolderBrowserDialog.SelectedPath, "Master")) Then
                Textbox_Master_Path.Text = FolderBrowserDialog.SelectedPath
            Else
                My.Computer.Audio.PlaySystemSound(Media.SystemSounds.Exclamation)
                MsgBox("Master folder not found, please choose another path.", vbOKOnly + vbCritical, "EuroSound")
            End If
        End If
    End Sub

    Private Sub Button_SonixFolder_Click(sender As Object, e As EventArgs) Handles Button_SonixFolder.Click
        'Update desc
        FolderBrowserDialog.Description = "Set Folder for Hashcodes Files."
        'Set results
        If FolderBrowserDialog.ShowDialog = DialogResult.OK Then
            Textbox_SonixFolder.Text = FolderBrowserDialog.SelectedPath
        End If
    End Sub

    Private Sub Button_EngineXFolder_Click(sender As Object, e As EventArgs) Handles Button_EngineXFolder.Click
        'Update desc
        FolderBrowserDialog.Description = "Set Folder for Hashcodes Files."
        'Set results
        If FolderBrowserDialog.ShowDialog = DialogResult.OK Then
            Textbox_EngineXFolder.Text = FolderBrowserDialog.SelectedPath
        End If
    End Sub

    Private Sub Button_EuroLandServer_Click(sender As Object, e As EventArgs) Handles Button_EuroLandServer.Click
        'Update desc
        FolderBrowserDialog.Description = "Set Folder to the EuroLand HashCodes folder."
        'Set results
        If FolderBrowserDialog.ShowDialog = DialogResult.OK Then
            Textbox_EuroLandServer.Text = FolderBrowserDialog.SelectedPath
        End If
    End Sub

    Private Sub Button_BrowseOutput_Click(sender As Object, e As EventArgs) Handles Button_BrowseOutput.Click
        'Ensure that we have an item selected
        If ListView_Formats.SelectedItems.Count > 0 Then
            'Update desc
            FolderBrowserDialog.Description = "Set Folder for the Output Files."
            'Set results
            If FolderBrowserDialog.ShowDialog = DialogResult.OK Then
                ListView_Formats.SelectedItems(0).SubItems(1).Text = FolderBrowserDialog.SelectedPath
            End If
        End If
    End Sub

    '*===============================================================================================
    '* AVAILABLE FORMATS SECTION
    '*===============================================================================================
    Private Sub Button_AddFormat_Click(sender As Object, e As EventArgs) Handles Button_AddFormat.Click
        'Ensure that there is an item selected in the combobox
        If ComboBox_Platform.SelectedItem IsNot Nothing Then
            Dim selectedPlatform = ComboBox_Platform.SelectedItem
            'Check if the platform has been added to the dictionary
            If Not ProjectSettingsFile.sampleRateFormats.ContainsKey(selectedPlatform) Then
                'Add platform to dictionary
                ProjectSettingsFile.sampleRateFormats.Add(selectedPlatform, New Dictionary(Of String, UInteger))
                ProjectSettingsFile.sampleRateFormats(selectedPlatform).Add("Default", 22050)
                If ProjectSettingsFile.sampleRateFormats.Count > 0 Then
                    If ComboBox_RatesFormat.Items.Count > 0 Then
                        For Each ReSampleRate As String In ProjectSettingsFile.sampleRateFormats(ComboBox_RatesFormat.Items(0)).Keys
                            If Not ProjectSettingsFile.sampleRateFormats(selectedPlatform).ContainsKey(ReSampleRate) Then
                                ProjectSettingsFile.sampleRateFormats(selectedPlatform).Add(ReSampleRate, 22050)
                            End If
                        Next
                    End If
                End If

                'Add item to list
                Dim formatitem As New ListViewItem(New String() {selectedPlatform, "Set Output Folder.", "On"})
                ListView_Formats.Items.Add(formatitem)

                'Add item to combobox
                If Not ComboBox_RatesFormat.Items.Contains(selectedPlatform) Then
                    ComboBox_RatesFormat.Items.Add(selectedPlatform)
                End If
            End If
        End If
    End Sub

    Private Sub Button_Resample_On_Click(sender As Object, e As EventArgs) Handles Button_Resample_On.Click
        For itemIdex As Integer = 0 To ListView_Formats.SelectedIndices.Count - 1
            Dim currentIndex As Integer = ListView_Formats.SelectedIndices(itemIdex)
            ProjectSettingsFile.AvailableFormats(currentIndex, 2) = "On"
            ListView_Formats.Items(currentIndex).SubItems(2).Text = "On"
        Next
    End Sub

    Private Sub Button_ResampleOff_Click(sender As Object, e As EventArgs) Handles Button_ResampleOff.Click
        For itemIdex As Integer = 0 To ListView_Formats.SelectedIndices.Count - 1
            Dim currentIndex As Integer = ListView_Formats.SelectedIndices(itemIdex)
            ProjectSettingsFile.AvailableFormats(currentIndex, 2) = "Off"
            ListView_Formats.Items(currentIndex).SubItems(2).Text = "Off"
        Next
    End Sub

    '*===============================================================================================
    '* SAMPLE RATES SECTION
    '*===============================================================================================
    Private Sub Button_AddSampleRate_Click(sender As Object, e As EventArgs) Handles Button_AddSampleRate.Click
        'Get name
        Dim defaultName
        If ratesNamesIndex < ratesNames.Length Then
            defaultName = ratesNames(ratesNamesIndex)
        Else
            defaultName = ratesNames(4) & " " & (ratesNamesIndex - 4)
        End If

        'Ask user for a new resample rate name
        Dim resampleName = InputBox("Enter New Re-sample Rate Name", "New Re-sample Name", defaultName)
        If resampleName IsNot "" Then
            'Ensure that does not exists
            If Not ProjectSettingsFile.AvailableReSampleRates.Contains(resampleName) Then
                'Add item to list and to the combobox
                ListBox_SampleRates.Items.Add(resampleName)
                ComboBox_DefaultSampleRate.Items.Add(resampleName)
                ProjectSettingsFile.AvailableReSampleRates.Add(resampleName)

                'Update platform rates
                For Each platform In ProjectSettingsFile.sampleRateFormats
                    Dim formatRatesList As Dictionary(Of String, UInteger) = platform.Value
                    If Not formatRatesList.ContainsKey(resampleName) Then
                        formatRatesList.Add(resampleName, 22050)
                    End If
                Next

                'Print rates
                If ComboBox_RatesFormat.SelectedItem IsNot Nothing Then
                    'Get selected platform
                    Dim selectedPlatform As String = ComboBox_RatesFormat.SelectedItem
                    PrintFormatRates(selectedPlatform)
                End If
            End If
            'Update counter
            ratesNamesIndex += 1
        End If
    End Sub

    Private Sub ComboBox_RatesFormat_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ComboBox_RatesFormat.SelectedIndexChanged
        'Ensure that the combobox has a valid item selected
        If ComboBox_RatesFormat.SelectedItem IsNot Nothing AndAlso ProjectSettingsFile.sampleRateFormats.Count > 0 Then
            PrintFormatRates(ComboBox_RatesFormat.SelectedItem)
        End If
    End Sub

    Private Sub ListView_SampleRateValues_DoubleClick(sender As Object, e As EventArgs) Handles ListView_SampleRateValues.DoubleClick
        Dim selectedLabel As String = ListView_SampleRateValues.SelectedItems(0).Text

        'Ensure that we have selected an item
        If ListView_SampleRateValues.SelectedItems.Count > 0 AndAlso ProjectSettingsFile.sampleRateFormats(ComboBox_RatesFormat.SelectedItem).ContainsKey(selectedLabel) Then
            'Ask user for a value
            Dim currentSampleRate = ListView_SampleRateValues.SelectedItems(0).SubItems(1).Text
            Dim InputValue As String = InputBox("Enter New Re-sample Rate", "New Sample Rate", currentSampleRate)
            'Ensure that is valid
            If IsNumeric(InputValue) Then
                'Update subitem
                ListView_SampleRateValues.SelectedItems(0).SubItems(1).Text = InputValue
                'Update dictionary
                ProjectSettingsFile.sampleRateFormats(ComboBox_RatesFormat.SelectedItem)(selectedLabel) = CInt(InputValue)
            Else
                'Inform user
                MsgBox("Invalid sample rate value", vbOKOnly + vbExclamation, "Error")
            End If
        End If
    End Sub

    '*===============================================================================================
    '* MISC SECTION
    '*===============================================================================================
    Private Sub TextBox_EditWavs_MouseDoubleClick(sender As Object, e As MouseEventArgs) Handles TextBox_EditWavs.MouseDoubleClick
        'Put selected path to the textbox
        If OpenFileDialog.ShowDialog = DialogResult.OK Then
            'Update textbox and variables
            TextBox_EditWavs.Text = OpenFileDialog.FileName
        End If
    End Sub

    Private Sub TextBox_UserName_MouseDoubleClick(sender As Object, e As MouseEventArgs) Handles TextBox_UserName.MouseDoubleClick
        'Ask user for a new username
        TextBox_UserName.Text = AskForUserName(TextBox_UserName.Text)
        EuroSoundUser = TextBox_UserName.Text
    End Sub

    Private Sub TextBox_TextEditor_MouseDoubleClick(sender As Object, e As MouseEventArgs) Handles TextBox_TextEditor.MouseDoubleClick
        'Put selected path to the textbox
        If OpenFileDialog.ShowDialog = DialogResult.OK Then
            'Update textbox and variables
            TextBox_TextEditor.Text = OpenFileDialog.FileName
        End If
    End Sub
End Class
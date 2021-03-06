Imports System.IO
Imports System.Media
Imports sb_editor.ParsersObjects
Imports sb_editor.ReaderClasses
Imports sb_editor.WritersClasses

Partial Public Class Frm_SfxEditor
    '*===============================================================================================
    '* GLOBAL VARIABLES
    '*===============================================================================================
    Private ReadOnly sfxFileName As String
    Private ReadOnly writers As New FileWriters
    Private ReadOnly reader As New FileParsers
    Private ReadOnly sfxFilesData As New Dictionary(Of String, SfxFile)
    Private StreamSamplesList As String()
    Private promptSave As Boolean = True

    Sub New(fileName As String)
        ' Esta llamada es exigida por el diseñador.
        InitializeComponent()
        ' Agregue cualquier inicialización después de la llamada a InitializeComponent().
        sfxFileName = fileName
    End Sub

    '*===============================================================================================
    '* FORM EVENTS
    '*===============================================================================================
    Private Sub Frm_SfxEditor_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        'Hide mainframe
        Dim MainFrame As Form = CType(Application.OpenForms("MainFrame"), MainFrame)
        MainFrame.Hide()

        Text = sfxFileName
        Label_SFX_Name.Text = ">Name: " & sfxFileName
        SfxParamsAndSamplePool.Textbox_SfxName.Text = sfxFileName

        'Add formats to combobox
        ComboBox_AvailableFormats.BeginUpdate()
        ComboBox_AvailableFormats.Items.AddRange(ProjectSettingsFile.sampleRateFormats.Keys.ToArray)
        If ComboBox_AvailableFormats.Items.Count > 0 Then
            ComboBox_AvailableFormats.SelectedIndex = 0
        End If
        ComboBox_AvailableFormats.EndUpdate()

        'Get stream sounds list
        If File.Exists(SysFileSamples) Then
            StreamSamplesList = reader.GetAllStreamSamples(reader.SamplesFileToDatatable(SysFileSamples))
        End If

        'Check if the misc folder exists
        Dim miscFolder As String = Path.Combine(WorkingDirectory, "SFXs", "Misc")
        Directory.CreateDirectory(miscFolder)

        'Add Common Tab Page
        Dim baseFilePath = Path.Combine(WorkingDirectory, "SFXs", sfxFileName & ".txt")
        If File.Exists(baseFilePath) Then
            Dim commonTextFile As String = Path.Combine(WorkingDirectory, "SFXs", "Misc", "Common.txt")
            File.Copy(baseFilePath, commonTextFile, True)
            sfxFilesData.Add("Common", reader.ReadSFXFile(commonTextFile))
        End If

        'Add Specific Formats Tab Pages
        Dim availablePlatforms As String() = ProjectSettingsFile.sampleRateFormats.Keys.ToArray
        For index As Integer = 0 To availablePlatforms.Length - 1
            'Create folder if not exists
            Dim folderPath As String = Path.Combine(WorkingDirectory, "SFXs", availablePlatforms(index))
            Directory.CreateDirectory(folderPath)

            'Check if the request file exists
            baseFilePath = Path.Combine(folderPath, sfxFileName & ".txt")
            If File.Exists(baseFilePath) Then
                Dim platformTextFile As String = Path.Combine(WorkingDirectory, "SFXs", "Misc", availablePlatforms(index) & ".txt")
                File.Copy(baseFilePath, platformTextFile, True)
                sfxFilesData.Add(CreateTab(availablePlatforms(index)).Text, reader.ReadSFXFile(platformTextFile))

                'Disable Button
                Select Case availablePlatforms(index)
                    Case "PlayStation2"
                        Button_SpecVersion_PlayStation2.Enabled = False
                    Case "GameCube"
                        Button_SpecVersion_GameCube.Enabled = False
                    Case "X Box"
                        Button_SpecVersion_Xbox.Enabled = False
                    Case "Xbox"
                        Button_SpecVersion_Xbox.Enabled = False
                    Case "PC"
                        Button_SpecVersion_PC.Enabled = False
                End Select
            Else
                'Disable Button
                Select Case availablePlatforms(index)
                    Case "PlayStation2"
                        Button_SpecVersion_PlayStation2.Enabled = True
                    Case "GameCube"
                        Button_SpecVersion_GameCube.Enabled = True
                    Case "X Box"
                        Button_SpecVersion_Xbox.Enabled = True
                    Case "Xbox"
                        Button_SpecVersion_Xbox.Enabled = True
                    Case "PC"
                        Button_SpecVersion_PC.Enabled = True
                End Select
            End If
        Next

        'Inherited from common file and applied to all other formats
        ShowSfxParameters(TabControl_Platforms.SelectedTab.Text)

        'Show common file
        ShowSfxSamplePoolControl(TabControl_Platforms.SelectedTab.Text)
        ShowSfxSamplePool(TabControl_Platforms.SelectedTab.Text)

        'Enable clipboard
        If File.Exists(Path.Combine(WorkingDirectory, "SFXs", "Misc", "ClipBoard.txt")) Then
            Button_ClipboardPaste.Enabled = True
        End If
    End Sub

    Private Sub Frm_SfxEditor_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        'Check closing reason
        If e.CloseReason = CloseReason.UserClosing Then
            'Check if we have to show the message
            If promptSave Then
                'Ask user what wants to do
                My.Computer.Audio.PlaySystemSound(SystemSounds.Exclamation)
                Dim save As MsgBoxResult = MsgBox("Are you sure you wish to quit without saving?", vbOKCancel + vbQuestion, "Confirm Quit")
                If save = MsgBoxResult.Cancel Then
                    e.Cancel = True
                End If
            End If
        End If
    End Sub

    Private Sub Frm_SfxEditor_FormClosed(sender As Object, e As FormClosedEventArgs) Handles MyBase.FormClosed
        'Close hashcodes list form
        Dim subSfxListForm = CType(Application.OpenForms("HashCodesList"), HashCodesList)
        If subSfxListForm IsNot Nothing Then
            subSfxListForm.Close()
        End If

        'Stop audio playing
        My.Computer.Audio.Stop()

        'Show mainframe again
        Dim MainFrame As Form = CType(Application.OpenForms("MainFrame"), MainFrame)
        MainFrame.Show()
    End Sub

    '*===============================================================================================
    '* TAB CONTROL EVENTS
    '*===============================================================================================
    Private Sub TabControl_Platforms_Selected(sender As Object, e As TabControlEventArgs) Handles TabControl_Platforms.Selected
        'Show common file
        ShowSfxSamplePoolControl(TabControl_Platforms.SelectedTab.Text)
        ShowSfxSamplePool(TabControl_Platforms.SelectedTab.Text)

        'Disable remove specific format if the current tab is "Common"
        If StrComp(e.TabPage.Text, "Common") = 0 Then
            Button_RemoveSpecificVersion.Enabled = False
        Else
            Button_RemoveSpecificVersion.Enabled = True
        End If
    End Sub

    '*===============================================================================================
    '* SAMPLE POOL CONTROL EVENTS
    '*===============================================================================================
    Private Sub SfxParamsAndSamplePool_MaxDelayValidating(sender As Object, e As EventArgs) Handles SfxParamsAndSamplePool.SfxControl_MaxDelayValidating
        If SfxParamsAndSamplePool.RadioButton_Single.Checked Then
            If SfxParamsAndSamplePool.Numeric_MaxDelay.Value < 0 Then
                SfxParamsAndSamplePool.Numeric_MaxDelay.Value = 0
            End If
        End If
        Dim selectedFileData As SfxFile = sfxFilesData(TabControl_Platforms.SelectedTab.Text)
        selectedFileData.SamplePool.MaxDelay = SfxParamsAndSamplePool.Numeric_MaxDelay.Value
    End Sub

    Private Sub SfxParamsAndSamplePool_MinDelayValidating(sender As Object, e As EventArgs) Handles SfxParamsAndSamplePool.SfxControl_MinDelayValidating
        If SfxParamsAndSamplePool.RadioButton_Single.Checked Then
            If SfxParamsAndSamplePool.Numeric_MinDelay.Value < 0 Then
                SfxParamsAndSamplePool.Numeric_MinDelay.Value = 0
            End If
        End If
        Dim selectedFileData As SfxFile = sfxFilesData(TabControl_Platforms.SelectedTab.Text)
        selectedFileData.SamplePool.MinDelay = SfxParamsAndSamplePool.Numeric_MinDelay.Value
    End Sub

    Private Sub SfxParamsAndSamplePool_LoopChecked(sender As Object, e As EventArgs) Handles SfxParamsAndSamplePool.SfxControl_LoopChecked
        Dim selectedFileData As SfxFile = sfxFilesData(TabControl_Platforms.SelectedTab.Text)
        selectedFileData.SamplePool.isLooped = SfxParamsAndSamplePool.CheckBox_SamplePoolLoop.Checked
    End Sub

    Private Sub SfxParamsAndSamplePool_SingleChecked(sender As Object, e As EventArgs) Handles SfxParamsAndSamplePool.SfxControl_SingleChecked
        Dim selectedFileData As SfxFile = sfxFilesData(TabControl_Platforms.SelectedTab.Text)
        If SfxParamsAndSamplePool.RadioButton_Single.Checked Then
            selectedFileData.SamplePool.Action1 = 0
        End If
    End Sub

    Private Sub SfxParamsAndSamplePool_SfxControl_RandomPickChecked(sender As Object, e As EventArgs) Handles SfxParamsAndSamplePool.SfxControl_RandomPickChecked
        Dim selectedFileData As SfxFile = sfxFilesData(TabControl_Platforms.SelectedTab.Text)
        If SfxParamsAndSamplePool.RadioButton_Single.Checked Then
            selectedFileData.SamplePool.RandomPick = SfxParamsAndSamplePool.CheckBox_RandomPick.Checked
        End If
    End Sub

    Private Sub SfxParamsAndSamplePool_MultiSampleChecked(sender As Object, e As EventArgs) Handles SfxParamsAndSamplePool.SfxControl_MultiSampleChecked
        Dim selectedFileData As SfxFile = sfxFilesData(TabControl_Platforms.SelectedTab.Text)
        If SfxParamsAndSamplePool.RadioButton_MultiSample.Checked Then
            selectedFileData.SamplePool.Action1 = 1
        End If
    End Sub

    Private Sub SfxParamsAndSamplePool_ShuffledChecked(sender As Object, e As EventArgs) Handles SfxParamsAndSamplePool.SfxControl_ShuffledChecked
        Dim selectedFileData As SfxFile = sfxFilesData(TabControl_Platforms.SelectedTab.Text)
        selectedFileData.SamplePool.Shuffled = SfxParamsAndSamplePool.CheckBox_Shuffled.Checked
    End Sub

    Private Sub SfxParamsAndSamplePool_PolyphonicChecked(sender As Object, e As EventArgs) Handles SfxParamsAndSamplePool.SfxControl_PolyphonicChecked
        Dim selectedFileData As SfxFile = sfxFilesData(TabControl_Platforms.SelectedTab.Text)
        selectedFileData.SamplePool.Polyphonic = SfxParamsAndSamplePool.CheckBox_Polyphonic.Checked
    End Sub

    Private Sub SfxParamsAndSamplePool_SfxControl_StealOnLouderChecked(sender As Object, e As EventArgs) Handles SfxParamsAndSamplePool.SfxControl_StealOnLouderChecked
        If SfxParamsAndSamplePool.CheckBox_StealOnLouder.Checked Then
            If Numeric_RandomVolume.Value <> 0 Then
                'Inform user
                MsgBox("Steal On Louder & Random Volume NOT allowed.", vbOKOnly + vbExclamation, "EuroSound")
                SfxParamsAndSamplePool.CheckBox_StealOnLouder.Checked = False
            End If
        End If
    End Sub

    '*===============================================================================================
    '* SAMPLE POOL EVENTS
    '*===============================================================================================
    Private Sub CheckBox_EnableSubSFX_Click(sender As Object, e As EventArgs) Handles CheckBox_EnableSubSFX.Click
        'Ensure that the sample pool list is empty
        If ListBox_SamplePool.Items.Count > 0 Then
            'Cancel check
            Dim r As CheckBox = sender
            r.Checked = Not r.Checked

            'Inform user
            MsgBox("Sample Pool File List Must be empty!", vbOKOnly + vbCritical, "Error")
        Else
            If CheckBox_EnableSubSFX.Checked Then
                EnableSubSfxSection()
            Else
                DisableSubSfxSection()
            End If
            Dim sfxData As SfxFile = sfxFilesData(TabControl_Platforms.SelectedTab.Text)
            sfxData.SamplePool.EnableSubSFX = CheckBox_EnableSubSFX.Checked
        End If
    End Sub

    Private Sub ListBox_SamplePool_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ListBox_SamplePool.SelectedIndexChanged
        If CheckBox_EnableSubSFX.Checked = False Then
            Dim lbls As List(Of Label) = GroupBox_SampleProps.Controls.OfType(Of Label)().ToList()
            'Show Samples info
            If ListBox_SamplePool.SelectedItems.Count > 1 Then
                Label_SampleInfo_FreqValue.Text = ".."
                Label_SampleInfo_SizeValue.Text = ".."
                Label_SampleInfo_LengthValue.Text = ".."
                Label_SampleInfo_LoopValue.Text = ".."
                Label_SampleInfo_StreamedValue.Text = ".."
                ShowSampleData(ListBox_SamplePool.SelectedItem)

                'Change label colors
                For Each lbl In lbls
                    lbl.BackColor = Color.Black
                    lbl.ForeColor = Color.White
                Next
            Else
                ShowSampleInfo(ListBox_SamplePool.SelectedItem)
                ShowSampleData(ListBox_SamplePool.SelectedItem)

                'Change label colors
                For Each lbl In lbls
                    lbl.BackColor = SystemColors.Control
                    lbl.ForeColor = Color.Black
                Next
            End If
        End If
    End Sub

    Private Sub ListBox_SamplePool_Click(sender As Object, e As EventArgs) Handles ListBox_SamplePool.Click
        If CheckBox_EnableSubSFX.Checked = False Then
            If ListBox_SamplePool.SelectedItems.Count = 1 Then
                Dim selectedSample As Sample = ListBox_SamplePool.SelectedItem
                Dim waveFullPath As String = Path.Combine(WorkingDirectory, "Master", selectedSample.FilePath)
                If Not File.Exists(waveFullPath) Then
                    MsgBox("File Not Found: " & waveFullPath, vbOKOnly & vbCritical, "EuroSound")
                End If
            End If
        End If
    End Sub

    '*===============================================================================================
    '* SAMPLE POOL LISTBOX EVENTS
    '*===============================================================================================
    Private Sub ListBox_SamplePool_DragOver(sender As Object, e As DragEventArgs) Handles ListBox_SamplePool.DragOver
        e.Effect = DragDropEffects.Copy
    End Sub

    Private Sub ListBox_SamplePool_DragDrop(sender As Object, e As DragEventArgs) Handles ListBox_SamplePool.DragDrop
        'Ensure that the drag drop effect is correct
        If e.Effect = DragDropEffects.Copy Then
            'Ensure that the data type is correct
            If e.Data.GetDataPresent(GetType(ListBox.SelectedObjectCollection)) AndAlso CheckBox_EnableSubSFX.Checked = True Then
                'Add items
                Dim sfxFileData As SfxFile = sfxFilesData(TabControl_Platforms.SelectedTab.Text)
                Dim itemsData As ListBox.SelectedObjectCollection = e.Data.GetData(GetType(ListBox.SelectedObjectCollection))
                For Each data As String In itemsData
                    'Create a new object with default settings
                    Dim sampleObj As New Sample With {
                        .FilePath = data,
                        .RandomPitchOffset = 0,
                        .BaseVolume = 0
                    }
                    'Add object to list (binding list automatically updates the listbox control)
                    sfxFileData.Samples.Add(sampleObj)
                Next
            Else
                My.Computer.Audio.PlaySystemSound(SystemSounds.Beep)
            End If
        End If
    End Sub

    Private Sub Button_MoveUp_Click(sender As Object, e As EventArgs) Handles Button_MoveUp.Click
        ListBox_SamplePool.BeginUpdate()
        Dim indexes As Integer() = ListBox_SamplePool.SelectedIndices.Cast(Of Integer)().ToArray()
        If indexes.Length > 0 AndAlso indexes(0) > 0 Then
            Dim itemsToSelect As New Collection

            'Move items
            Dim sfxFileData As SfxFile = sfxFilesData(TabControl_Platforms.SelectedTab.Text)
            For i As Integer = 0 To sfxFileData.Samples.Count - 1
                If indexes.Contains(i) Then
                    Dim moveItem As Sample = sfxFileData.Samples(i)
                    sfxFileData.Samples.Remove(moveItem)
                    sfxFileData.Samples.Insert(i - 1, moveItem)
                    itemsToSelect.Add(i - 1)
                End If
            Next

            'Update selection
            ListBox_SamplePool.SelectedIndices.Clear()
            For index As Integer = 1 To itemsToSelect.Count
                ListBox_SamplePool.SelectedIndex = itemsToSelect(index)
            Next
        End If
        ListBox_SamplePool.EndUpdate()
    End Sub

    Private Sub Button_MoveDown_Click(sender As Object, e As EventArgs) Handles Button_MoveDown.Click
        ListBox_SamplePool.BeginUpdate()
        Dim indexes As Integer() = ListBox_SamplePool.SelectedIndices.Cast(Of Integer)().ToArray()
        Dim sfxFileData As SfxFile = sfxFilesData(TabControl_Platforms.SelectedTab.Text)
        If indexes.Length > 0 AndAlso indexes(indexes.Length - 1) < sfxFileData.Samples.Count - 1 Then
            Dim itemsToSelect As New Collection
            'Move items
            For i As Integer = sfxFileData.Samples.Count - 1 To 0 Step -1
                If indexes.Contains(i) Then
                    Dim moveItem As Sample = sfxFileData.Samples(i)
                    sfxFileData.Samples.Remove(moveItem)
                    sfxFileData.Samples.Insert(i + 1, moveItem)
                    itemsToSelect.Add(i + 1)
                End If
            Next

            'Update selection
            ListBox_SamplePool.SelectedIndices.Clear()
            For index As Integer = 1 To itemsToSelect.Count
                ListBox_SamplePool.SelectedIndex = itemsToSelect(index)
            Next
        End If
        ListBox_SamplePool.EndUpdate()
    End Sub

    Private Sub Button_AddSample_Click(sender As Object, e As EventArgs) Handles Button_AddSample.Click
        AddNewSample(TabControl_Platforms.SelectedTab.Text)
    End Sub

    Private Sub MenuItem_SamplePool_Add_Click(sender As Object, e As EventArgs) Handles MenuItem_SamplePool_Add.Click
        AddNewSample(TabControl_Platforms.SelectedTab.Text)
    End Sub

    Private Sub Button_RemoveSample_Click(sender As Object, e As EventArgs) Handles Button_RemoveSample.Click
        RemoveSelectedSample(TabControl_Platforms.SelectedTab.Text)
    End Sub

    Private Sub MenuItem_SamplePool_Remove_Click(sender As Object, e As EventArgs) Handles MenuItem_SamplePool_Remove.Click
        RemoveSelectedSample(TabControl_Platforms.SelectedTab.Text)
    End Sub

    Private Sub Button_CopySample_Click(sender As Object, e As EventArgs) Handles Button_CopySample.Click
        CopySelectedSample(TabControl_Platforms.SelectedTab.Text)
    End Sub

    Private Sub MenuItem_SamplePool_Copy_Click(sender As Object, e As EventArgs) Handles MenuItem_SamplePool_Copy.Click
        CopySelectedSample(TabControl_Platforms.SelectedTab.Text)
    End Sub

    Private Sub Button_OpenSampleFolder_Click(sender As Object, e As EventArgs) Handles Button_OpenSampleFolder.Click
        OpenSelectedSampleFolder(TabControl_Platforms.SelectedTab.Text)
    End Sub

    Private Sub MenuItem_SamplePool_Open_Click(sender As Object, e As EventArgs) Handles MenuItem_SamplePool_Open.Click
        OpenSelectedSampleFolder(TabControl_Platforms.SelectedTab.Text)
    End Sub

    Private Sub Button_EditSample_Click(sender As Object, e As EventArgs) Handles Button_EditSample.Click
        EditSelectedSample(TabControl_Platforms.SelectedTab.Text)
    End Sub

    Private Sub MenuItem_SamplePool_Edit_Click(sender As Object, e As EventArgs) Handles MenuItem_SamplePool_Edit.Click
        EditSelectedSample(TabControl_Platforms.SelectedTab.Text)
    End Sub

    Private Sub Button_PlaySample_Click(sender As Object, e As EventArgs) Handles Button_PlaySample.Click
        PlaySelectedSample(TabControl_Platforms.SelectedTab.Text)
    End Sub

    Private Sub MenuItem_SamplePool_Play_Click(sender As Object, e As EventArgs) Handles MenuItem_SamplePool_Play.Click
        PlaySelectedSample(TabControl_Platforms.SelectedTab.Text)
    End Sub

    Private Sub Button_StopSample_Click(sender As Object, e As EventArgs) Handles Button_StopSample.Click
        'Stop audio player
        My.Computer.Audio.Stop()
    End Sub

    Private Sub MenuItem_SamplePool_Stop_Click(sender As Object, e As EventArgs) Handles MenuItem_SamplePool_Stop.Click
        'Stop audio player
        My.Computer.Audio.Stop()
    End Sub

    '*===============================================================================================
    '* SAMPLE PROPERTIES EVENTS
    '*===============================================================================================
    Private Sub Numeric_PitchOffset_ValueChanged(sender As Object, e As EventArgs) Handles Numeric_PitchOffset.ValueChanged
        Dim sfxElement As SfxFile = sfxFilesData(TabControl_Platforms.SelectedTab.Text)
        'Ensure that we have selected an item
        If ListBox_SamplePool.SelectedItems.Count > 0 Then
            'Update property
            For Each sampleObject As Sample In ListBox_SamplePool.SelectedItems
                sampleObject.PitchOffset = Numeric_PitchOffset.Value
            Next
        End If
    End Sub

    Private Sub Numeric_RandomPitch_ValueChanged(sender As Object, e As EventArgs) Handles Numeric_RandomPitch.ValueChanged
        'Ensure that we have selected an item
        If ListBox_SamplePool.SelectedItems.Count > 0 Then
            For Each sampleObject As Sample In ListBox_SamplePool.SelectedItems
                sampleObject.RandomPitchOffset = Numeric_RandomPitch.Value
            Next
        End If
    End Sub

    Private Sub Numeric_BaseVolume_ValueChanged(sender As Object, e As EventArgs) Handles Numeric_BaseVolume.ValueChanged
        'Ensure that we have selected an item
        If ListBox_SamplePool.SelectedItems.Count > 0 Then
            For Each sampleObject As Sample In ListBox_SamplePool.SelectedItems
                sampleObject.BaseVolume = Numeric_BaseVolume.Value
            Next
        End If
    End Sub

    Private Sub Numeric_RandomVolume_ValueChanged(sender As Object, e As EventArgs) Handles Numeric_RandomVolume.ValueChanged
        'Ensure that we have selected an item
        If ListBox_SamplePool.SelectedItems.Count > 0 Then
            If SfxParamsAndSamplePool.CheckBox_StealOnLouder.Checked Then
                If Numeric_RandomVolume.Value <> 0 Then
                    MsgBox("Steal On Louder & Random Volume NOT allowed.", vbOKOnly + vbExclamation, "EuroSound")
                    Numeric_RandomVolume.Value = 0
                End If
            Else
                'Update property
                For Each sampleObject As Sample In ListBox_SamplePool.SelectedItems
                    sampleObject.RandomVolumeOffset = Numeric_RandomVolume.Value
                Next
            End If
        End If
    End Sub

    Private Sub Numeric_Pan_ValueChanged(sender As Object, e As EventArgs) Handles Numeric_Pan.ValueChanged
        'Ensure that we have selected an item
        If ListBox_SamplePool.SelectedItems.Count > 0 Then
            'Update property
            For Each sampleObject As Sample In ListBox_SamplePool.SelectedItems
                sampleObject.Pan = Numeric_Pan.Value
            Next
        End If
    End Sub

    Private Sub Numeric_RandomPan_ValueChanged(sender As Object, e As EventArgs) Handles Numeric_RandomPan.ValueChanged
        'Ensure that we have selected an item
        If ListBox_SamplePool.SelectedItems.Count > 0 Then
            For Each sampleObject As Sample In ListBox_SamplePool.SelectedItems
                sampleObject.RandomPan = Numeric_RandomPan.Value
            Next
        End If
    End Sub

    Private Sub Button_TestSfx_Click(sender As Object, e As EventArgs) Handles Button_TestSfx.Click
        'Start watcher
        Dim watch As New Stopwatch
        watch.Start()

        'Create Test SoundBank
        Dim filePath As String = sfxFilesData(TabControl_Platforms.SelectedTab.Text).filePath
        CreateTestSFX(filePath)

        'Stop watcher
        watch.Stop()
        TextBox_ScriptTime.Text = "ES Time " & watch.ElapsedMilliseconds

        'Start Process
        TextBox_ScriptDebug.Clear()
        Dim procStartInfo As New ProcessStartInfo("SystemFiles\testSounds.cmd") With {
            .RedirectStandardOutput = True,
            .RedirectStandardError = True,
            .UseShellExecute = False,
            .CreateNoWindow = True
        }
        Dim p As New Process With {
            .EnableRaisingEvents = True,
            .StartInfo = procStartInfo
        }
        p.StartInfo.CreateNoWindow = True
        p.StartInfo.UseShellExecute = False
        AddHandler p.OutputDataReceived, AddressOf OutputHandler
        AddHandler p.ErrorDataReceived, AddressOf OutputHandler
        p.Start()
        p.BeginOutputReadLine()
    End Sub

    Private Sub Button_ReverbTester_Click(sender As Object, e As EventArgs) Handles Button_ReverbTester.Click
        Dim reverbTest As New Frm_ReverbTester()
        reverbTest.ShowDialog()
    End Sub

    Private Sub OutputHandler(sendingProcess As Object, outLine As DataReceivedEventArgs)
        If Not String.IsNullOrEmpty(outLine.Data) Then
            TextBox_ScriptDebug.Invoke(Sub() TextBox_ScriptDebug.Text += Trim(outLine.Data & vbCrLf))
        End If
    End Sub

    Private Sub TextBox_EuroSoundTime_Click(sender As Object, e As EventArgs) Handles TextBox_ScriptDebug.Click
        Dim debugForm As New Frm_DebugData(TextBox_ScriptDebug.Text.Split(vbCrLf))
        debugForm.ShowDialog()
    End Sub

    '*===============================================================================================
    '* BUTTON EVENTS
    '*===============================================================================================
    Private Sub Button_SpecVersion_PlayStation2_Click(sender As Object, e As EventArgs) Handles Button_SpecVersion_PlayStation2.Click
        CreateSpecificFormat("PlayStation2")
        Button_SpecVersion_PlayStation2.Enabled = False
    End Sub

    Private Sub Button_SpecVersion_GameCube_Click(sender As Object, e As EventArgs) Handles Button_SpecVersion_GameCube.Click
        CreateSpecificFormat("GameCube")
        Button_SpecVersion_GameCube.Enabled = False
    End Sub

    Private Sub Button_SpecVersion_Xbox_Click(sender As Object, e As EventArgs) Handles Button_SpecVersion_Xbox.Click
        CreateSpecificFormat("X Box")
        Button_SpecVersion_Xbox.Enabled = False
    End Sub

    Private Sub Button_SpecVersion_PC_Click(sender As Object, e As EventArgs) Handles Button_SpecVersion_PC.Click
        CreateSpecificFormat("PC")
        Button_SpecVersion_PC.Enabled = False
    End Sub

    Private Sub Button_Clipboard_Copy_Click(sender As Object, e As EventArgs) Handles Button_Clipboard_Copy.Click
        If sfxFilesData.ContainsKey(TabControl_Platforms.SelectedTab.Text) Then
            writers.WriteSfxFile(sfxFilesData(TabControl_Platforms.SelectedTab.Text), Path.Combine(WorkingDirectory, "SFXs", "Misc", "ClipBoard.txt"))
            Button_ClipboardPaste.Enabled = True
        End If
    End Sub

    Private Sub Button_ClipboardPaste_Click(sender As Object, e As EventArgs) Handles Button_ClipboardPaste.Click
        Dim clipboardFilePath As String = Path.Combine(WorkingDirectory, "SFXs", "Misc", "ClipBoard.txt")
        If File.Exists(clipboardFilePath) Then
            Dim sfxFile As SfxFile = reader.ReadSFXFile(clipboardFilePath)
            sfxFilesData(TabControl_Platforms.SelectedTab.Text) = sfxFile

            'Inherited from common file and applied to all other formats
            ShowSfxParameters(TabControl_Platforms.SelectedTab.Text)

            'Show common file
            ShowSfxSamplePoolControl(TabControl_Platforms.SelectedTab.Text)
            ShowSfxSamplePool(TabControl_Platforms.SelectedTab.Text)
        End If
    End Sub

    Private Sub Button_RemoveSpecificVersion_Click(sender As Object, e As EventArgs) Handles Button_RemoveSpecificVersion.Click
        'Enable Button
        Select Case TabControl_Platforms.SelectedTab.Text
            Case "PC"
                Button_SpecVersion_PC.Enabled = True
            Case "X Box"
                Button_SpecVersion_Xbox.Enabled = True
            Case "Xbox"
                Button_SpecVersion_Xbox.Enabled = True
            Case "GameCube"
                Button_SpecVersion_GameCube.Enabled = True
            Case "PlayStation2"
                Button_SpecVersion_PlayStation2.Enabled = True
        End Select

        'Delete tab
        sfxFilesData.Remove(TabControl_Platforms.SelectedTab.Text)
        TabControl_Platforms.TabPages.Remove(TabControl_Platforms.SelectedTab)
    End Sub

    '*===============================================================================================
    '* FORM BUTTONS
    '*===============================================================================================
    Private Sub Button_Cancel_Click(sender As Object, e As EventArgs) Handles Button_Cancel.Click
        'Close form
        Close()
    End Sub

    Private Sub Button_OK_Click(sender As Object, e As EventArgs) Handles Button_OK.Click
        'Disable save file message
        promptSave = False

        'Iterate over all available tabs
        For Each sfxFile As KeyValuePair(Of String, SfxFile) In sfxFilesData
            UpdateFilesParameters()

            'Write file
            Dim tempFilePath As String = Path.Combine(WorkingDirectory, "SFXs", "Misc", sfxFile.Key & ".txt")
            writers.WriteSfxFile(sfxFile.Value, tempFilePath)

            'Move file to the final folder
            If StrComp(sfxFile.Key, "Common") = 0 Then
                File.Copy(tempFilePath, Path.Combine(WorkingDirectory, "SFXs", sfxFileName & ".txt"), True)
            Else
                File.Copy(tempFilePath, Path.Combine(WorkingDirectory, "SFXs", sfxFile.Key, sfxFileName & ".txt"), True)
            End If
        Next

        'Close form
        Close()
    End Sub
End Class
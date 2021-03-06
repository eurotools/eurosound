Imports System.IO
Imports sb_editor.ParsersObjects
Imports sb_editor.ReaderClasses
Imports sb_editor.WritersClasses

Public Class UserControl_SFXs
    '*===============================================================================================
    '* GLOBAL VARS
    '*===============================================================================================
    Private ReadOnly textFileReaders As New FileParsers
    Private ReadOnly writers As New FileWriters
    Public Property AllowDoubleClick As Boolean = True

    '*===============================================================================================
    '* FORM EVENTS
    '*===============================================================================================
    Public Function LoadHashCodes() As String()
        Dim hashCodesList As New List(Of String)
        Dim hashCodesArray As String() = Nothing

        'Get files from directory and add it to list
        Dim sfxDir As String = Path.Combine(WorkingDirectory, "SFXs")
        If Directory.Exists(sfxDir) Then
            Dim filesToInspect As String() = Directory.GetFiles(sfxDir, "*.txt", SearchOption.TopDirectoryOnly)
            For fileIndex As Integer = 0 To filesToInspect.Length - 1
                Dim currentFile As String = filesToInspect(fileIndex)
                hashCodesList.Add(Path.GetFileNameWithoutExtension(currentFile))
            Next

            'Get array
            hashCodesArray = hashCodesList.ToArray
            Array.Sort(hashCodesArray)

            'Add item to listbox
            ListBox_SFXs.BeginUpdate()
            ListBox_SFXs.Items.Clear()
            ListBox_SFXs.Items.AddRange(hashCodesArray)
            ListBox_SFXs.EndUpdate()

            'Update counter
            Label_TotalSfx.Text = "Total: " & hashCodesArray.Length

            'Update Project file
            Dim projectFile As String = Path.Combine(WorkingDirectory, "Project.txt")
            Dim temporalProjFile As ProjectFile = textFileReaders.ReadProjectFile(Path.Combine(WorkingDirectory, "System", "TempFileName.txt"))
            writers.CreateProjectFile(projectFile, temporalProjFile.SoundBankList, temporalProjFile.DataBaseList, hashCodesArray)
        End If

        Return hashCodesArray
    End Function

    Public Sub LoadRefineList()
        'Get arrays item
        Dim refineFilePath As String = Path.Combine(WorkingDirectory, "System", "RefineSearch.txt")
        If File.Exists(refineFilePath) Then
            Dim keywords As String() = textFileReaders.ReadRefineList(refineFilePath)

            'Update combobox
            ComboBox_SFX_Section.BeginUpdate()
            ComboBox_SFX_Section.Items.Clear()
            ComboBox_SFX_Section.Items.AddRange(keywords)
            ComboBox_SFX_Section.EndUpdate()

            'Select first item
            If ComboBox_SFX_Section.Items.Count > 0 Then
                ComboBox_SFX_Section.SelectedIndex = 0
            End If

            'Erase Array
            Erase keywords
        End If
    End Sub

    '*===============================================================================================
    '* BUTTON EVENTS
    '*===============================================================================================
    Private Sub Button_UpdateList_Click(sender As Object, e As EventArgs) Handles Button_UpdateList.Click
        Dim updaterForm As New Frm_RefineSearch(Me)
        updaterForm.ShowDialog()
    End Sub

    Private Sub Button_ShowAll_Click(sender As Object, e As EventArgs) Handles Button_ShowAll.Click
        LoadHashCodes()
        If ComboBox_SFX_Section.Items.Count > 0 Then
            ComboBox_SFX_Section.SelectedIndex = 0
        End If
    End Sub

    Private Sub CheckBox_SortByDate_CheckStateChanged(sender As Object, e As EventArgs) Handles CheckBox_SortByDate.CheckStateChanged
        If CheckBox_SortByDate.Checked Then
            Dim sfxFolderPath As String = Path.Combine(WorkingDirectory, "SFXs")
            If Directory.Exists(sfxFolderPath) Then
                ListBox_SFXs.Sorted = False

                Dim filesToAdd As New List(Of KeyValuePair(Of String, String))
                Dim filesToCheck As String() = Directory.GetFiles(sfxFolderPath, "*.txt", SearchOption.TopDirectoryOnly)
                For fileIndex As Integer = 0 To filesToCheck.Length - 1
                    Dim currentFilePath As String = filesToCheck(fileIndex)
                    Dim headerFileInfo = textFileReaders.GetFileHeaderInfo(currentFilePath)
                    filesToAdd.Add(New KeyValuePair(Of String, String)(headerFileInfo.LastModify, Path.GetFileNameWithoutExtension(currentFilePath)))
                Next

                Dim sortedList As String() = filesToAdd.OrderBy(Function(x) x.Key).ToList.Select(Function(x) x.Value).ToArray

                'Add items to listbox
                ListBox_SFXs.BeginUpdate()
                ListBox_SFXs.Items.Clear()
                ListBox_SFXs.Items.AddRange(sortedList)
                ListBox_SFXs.EndUpdate()
            End If
        Else
            ListBox_SFXs.Sorted = True
            LoadHashCodes()
        End If
    End Sub

    Private Sub Button_UnUsedHashCodes_Click(sender As Object, e As EventArgs) Handles Button_UnUsedHashCodes.Click
        'Check SFXs that are not used in the databases
        Dim usedSfxs As New HashSet(Of String)
        Dim allSfxs As New HashSet(Of String)

        'Get all used SFX
        Dim databasesFilePath As String = Path.Combine(WorkingDirectory, "DataBases")
        If Directory.Exists(databasesFilePath) Then
            'Get all available files
            Dim availableDatabases As String() = Directory.GetFiles(databasesFilePath, "*.txt", SearchOption.TopDirectoryOnly)

            'Inspect files
            For databaseIndex As Integer = 0 To availableDatabases.Length - 1
                Dim databaseFileData As DataBaseFile = textFileReaders.ReadDataBaseFile(availableDatabases(databaseIndex))
                usedSfxs.UnionWith(databaseFileData.Dependencies)
            Next

            'Get all SFXs from the folder
            Dim sfxDirectory As String = Path.Combine(WorkingDirectory, "SFXs")
            If Directory.Exists(sfxDirectory) Then
                Dim availableSfxs As String() = Directory.GetFiles(sfxDirectory, "*.txt", SearchOption.TopDirectoryOnly)
                For index As Integer = 0 To availableSfxs.Length - 1
                    allSfxs.Add(Path.GetFileNameWithoutExtension(availableSfxs(index)))
                Next
            End If

            'Add results
            ListBox_SFXs.BeginUpdate()
            ListBox_SFXs.Items.Clear()
            ListBox_SFXs.Items.AddRange(allSfxs.Except(usedSfxs).ToArray)
            ListBox_SFXs.EndUpdate()

            'Update counter
            Label_TotalSfx.Text = "Total: " & ListBox_SFXs.Items.Count
        End If
    End Sub

    '*===============================================================================================
    '* COMBOBOX EVENTS
    '*===============================================================================================
    Private Sub ComboBox_SFX_Section_SelectionChangeCommitted(sender As Object, e As EventArgs) Handles ComboBox_SFX_Section.SelectionChangeCommitted
        'Clear items
        If ComboBox_SFX_Section.SelectedIndex = 0 Then 'All
            LoadHashCodes()
        ElseIf ComboBox_SFX_Section.SelectedIndex = 1 Then 'Highlighted
            Dim selectedIndices As String() = ListBox_SFXs.SelectedItems.Cast(Of String).ToArray
            ListBox_SFXs.BeginUpdate()
            ListBox_SFXs.Items.Clear()
            ListBox_SFXs.Items.AddRange(selectedIndices)
            ListBox_SFXs.EndUpdate()

            'Update counter
            Label_TotalSfx.Text = "Total: " & ListBox_SFXs.Items.Count
        Else 'Selected Keyword
            LoadHashCodes()

            'Remove items that doesn't match 
            ListBox_SFXs.BeginUpdate()
            Dim totalItemsCount As Integer = ListBox_SFXs.Items.Count - 1
            For itemIndex As Integer = totalItemsCount To 0 Step -1
                Dim currentItem As String = ListBox_SFXs.Items(itemIndex)
                If InStr(1, currentItem, ComboBox_SFX_Section.SelectedItem, CompareMethod.Binary) Then
                    Continue For
                Else
                    ListBox_SFXs.Items.Remove(currentItem)
                End If
            Next
            ListBox_SFXs.EndUpdate()

            'Update counter
            Label_TotalSfx.Text = "Total: " & ListBox_SFXs.Items.Count
        End If
    End Sub

    '*===============================================================================================
    '* CONTEXT MENU
    '*===============================================================================================
    Private Sub ContextMenuSfx_AddToDb_Click(sender As Object, e As EventArgs) Handles ContextMenuSfx_AddToDb.Click
        'Get mainframe and clear selection
        Dim mainForm As MainFrame = CType(Application.OpenForms("MainFrame"), MainFrame)
        If mainForm IsNot Nothing Then
            'Ensure that we have selected an item
            If ListBox_SFXs.SelectedItems.Count > 0 AndAlso mainForm.ListBox_DataBases.SelectedItems.Count = 1 Then
                'Get the items that we have to add
                Dim itemsInDataBase As String() = mainForm.ListBox_DataBaseSFX.Items.Cast(Of String).ToArray
                Dim selectedSfxItems As String() = ListBox_SFXs.SelectedItems.Cast(Of String).ToArray
                Dim itemsToAdd As String() = selectedSfxItems.Except(itemsInDataBase)

                'Sort items and add it to the listbox
                Array.Sort(itemsToAdd)
                mainForm.ListBox_DataBases.BeginUpdate()
                mainForm.ListBox_DataBaseSFX.Items.AddRange(itemsToAdd)
                mainForm.ListBox_DataBases.EndUpdate()

                'Update text file
                Dim databaseTxt As String = Path.Combine(WorkingDirectory, "DataBases", mainForm.ListBox_DataBases.SelectedItem & ".txt")
                writers.UpdateDataBaseText(databaseTxt, mainForm.ListBox_DataBaseSFX.Items.Cast(Of String).ToArray, textFileReaders)
            Else
                My.Computer.Audio.PlaySystemSound(Media.SystemSounds.Asterisk)
            End If
        End If
    End Sub

    Private Sub ContextMenuSfx_Properties_Click(sender As Object, e As EventArgs) Handles ContextMenuSfx_Properties.Click
        'Ensure that we have selected an item
        If ListBox_SFXs.SelectedItems.Count = 1 Then
            'Get item name and full path
            Dim selectedSFX As String = ListBox_SFXs.SelectedItem
            Dim sfxFullPath As String = Path.Combine(WorkingDirectory, "SFXs", selectedSFX & ".txt")

            'Ensure that the file exists 
            If File.Exists(sfxFullPath) Then
                'Show form
                Dim sfxProps As New SFX_Properties(selectedSFX, sfxFullPath)
                sfxProps.ShowDialog()
            Else
                'Inform user about this error
                MsgBox("The specified file has not been found: " & sfxFullPath, vbOKOnly + vbCritical, "Error")
            End If
        Else
            My.Computer.Audio.PlaySystemSound(Media.SystemSounds.Asterisk)
        End If
    End Sub

    Private Sub ContextMenuSfx_EditSfx_Click(sender As Object, e As EventArgs) Handles ContextMenuSfx_EditSfx.Click
        'Get selected item
        If ListBox_SFXs.SelectedItems.Count = 1 AndAlso AllowDoubleClick Then
            'Get item and file path
            Dim selectedSFX As String = ListBox_SFXs.SelectedItem
            Dim SelectedSfxPath = Path.Combine(WorkingDirectory, "SFXs", selectedSFX & ".txt")

            'Ensure that the file exists and open editor
            If File.Exists(SelectedSfxPath) Then
                Using sfxEditor As New Frm_SfxEditor(selectedSFX)
                    sfxEditor.ShowDialog()
                End Using
            End If
        Else
            My.Computer.Audio.PlaySystemSound(Media.SystemSounds.Asterisk)
        End If
    End Sub

    Private Sub ContextMenuSfx_AddNewSfx_Click(sender As Object, e As EventArgs) Handles ContextMenuSfx_AddNewSfx.Click
        'Ensure that the default file exists
        If File.Exists(SysFileSfxDefaults) AndAlso File.ReadAllLines(SysFileSfxDefaults).Length > 7 Then
            Dim folderToCheck As String = Path.Combine(WorkingDirectory, "SFXs")
            Dim sfxFileName As String = NewFile(GetNextAvailableFileName(folderToCheck, "SFX_Label"), folderToCheck)
            If sfxFileName > "" Then
                'Create and save file
                Dim fileData As SfxFile = textFileReaders.ReadSFXFile(SysFileSfxDefaults)
                fileData.HashCode = SFXHashCodeNumber

                'Get a new hashcode
                writers.WriteSfxFile(fileData, Path.Combine(folderToCheck, sfxFileName & ".txt"))

                'Add item to list
                Dim itemIndex As Integer = ListBox_SFXs.Items.Add(sfxFileName)
                ListBox_SFXs.SelectedIndices.Clear()
                ListBox_SFXs.SelectedIndex = itemIndex

                'Check if we need to realloc
                If SFXHashCodeNumber = 0 Then
                    MsgBox("Please Re-Alloc Hashcodes under Advanced Menu", vbOKOnly + vbExclamation, "EuroSound")
                End If

                'Update Global Variable
                SFXHashCodeNumber += 1
                writers.UpdateMiscFile(Path.Combine(WorkingDirectory, "System", "Misc.txt"))

                'Update Project file
                Dim temporalFile As String = Path.Combine(WorkingDirectory, "System", "TempFileName.txt")
                Dim projectFile As String = Path.Combine(WorkingDirectory, "Project.txt")
                Dim temporalProjFile As ProjectFile = textFileReaders.ReadProjectFile(temporalFile)
                writers.MergeFiles(temporalFile, temporalFile, textFileReaders.ReadProjectFile(projectFile), "#SFXList")
                writers.CreateProjectFile(projectFile, temporalProjFile.SoundBankList, temporalProjFile.DataBaseList, ListBox_SFXs.Items.Cast(Of String).ToArray)
            End If
        Else
            'Inform user about this
            MsgBox("Must Setup Default SFX file first!", vbOKOnly + vbCritical, "Setup SFX Defaults.")

            'Open SFX Default Form
            Using defaultSettingsForm As New SfxDefault
                defaultSettingsForm.ShowDialog()
            End Using
        End If
    End Sub

    Private Sub ContextMenuSfx_Copy_Click(sender As Object, e As EventArgs) Handles ContextMenuSfx_Copy.Click
        If ListBox_SFXs.SelectedItems.Count = 1 Then
            'Ask user
            Dim sfxCopyName As String = CopyFile(ListBox_SFXs.SelectedItem, "SFX", Path.Combine(WorkingDirectory, "SFXs"))
            If sfxCopyName IsNot "" Then
                'Read original file content
                Dim originalFilePath As String = Path.Combine(WorkingDirectory, "SFXs", ListBox_SFXs.SelectedItem & ".txt")
                Dim fileContent As String() = File.ReadAllLines(originalFilePath)
                'Update HashCode
                Dim hashCodePosition As Integer = Array.FindIndex(fileContent, Function(t) t.Equals("#HASHCODE", StringComparison.OrdinalIgnoreCase)) + 1
                If (hashCodePosition < fileContent.Length) Then
                    fileContent(hashCodePosition) = "HashCodeNumber " & SFXHashCodeNumber
                    SFXHashCodeNumber += 1
                    writers.UpdateMiscFile(Path.Combine(WorkingDirectory, "System", "Misc.txt"))

                    'Write new file
                    File.WriteAllLines(Path.Combine(WorkingDirectory, "SFXs", sfxCopyName & ".txt"), fileContent)
                    ListBox_SFXs.Items.Add(sfxCopyName)
                End If
                Erase fileContent
            End If
        Else
            My.Computer.Audio.PlaySystemSound(Media.SystemSounds.Asterisk)
        End If
    End Sub

    Private Sub ContextMenuSfx_Delete_Click(sender As Object, e As EventArgs) Handles ContextMenuSfx_Delete.Click
        'Create a list with the items that we have to remove
        Dim itemsToDelete As String() = ListBox_SFXs.SelectedItems.Cast(Of String).ToArray

        'Ask user what he wants to do
        Dim answerQuestion As MsgBoxResult = MsgBox(MultipleDeletionMessage("Are you sure you want to delete SFX(s)", itemsToDelete), vbInformation + vbYesNo, "Confirm SFX Deletion")
        If answerQuestion = MsgBoxResult.Yes Then
            'Get mainframe and clear selection
            Dim mainForm As MainFrame = CType(Application.OpenForms("MainFrame"), MainFrame)
            mainForm.ListBox_DataBases.SelectedItems.Clear()
            mainForm.ListBox_DataBaseSFX.Items.Clear()

            'Ensure that the Trash folder exists
            Dim sfxsTrash As String = Path.Combine(WorkingDirectory, "SFXs_Trash")
            Directory.CreateDirectory(sfxsTrash)

            'Update Database files
            Dim databaseFiles As String() = Directory.GetFiles(Path.Combine(WorkingDirectory, "DataBases"), "*.txt", SearchOption.TopDirectoryOnly)
            For i As Integer = 0 To databaseFiles.Length - 1
                Dim databaseData As String() = File.ReadAllLines(databaseFiles(i))
                File.WriteAllLines(databaseFiles(i), databaseData.Except(itemsToDelete).ToArray)
            Next
            Erase databaseFiles

            'Remove SFXs
            ListBox_SFXs.BeginUpdate()
            For i As Integer = 0 To itemsToDelete.Count - 1
                ListBox_SFXs.Items.Remove(itemsToDelete(i))
                'Remove text file
                Dim filesToDelete As IEnumerable(Of String) = Directory.GetFiles(WorkingDirectory & "\SFXs", itemsToDelete(i) & ".txt", SearchOption.AllDirectories)
                Using enumerator As IEnumerator(Of String) = filesToDelete.GetEnumerator
                    While enumerator.MoveNext
                        File.Copy(enumerator.Current, Path.Combine(sfxsTrash, itemsToDelete(i) & ".txt"), True)
                        File.Delete(enumerator.Current)
                    End While
                End Using
            Next
            ListBox_SFXs.EndUpdate()

            'Update counter
            Label_TotalSfx.Text = "Total: " & ListBox_SFXs.Items.Count

            'Update Project file
            Dim temporalFile As String = Path.Combine(WorkingDirectory, "System", "TempFileName.txt")
            Dim projectFile As String = Path.Combine(WorkingDirectory, "Project.txt")
            Dim temporalProjFile As ProjectFile = textFileReaders.ReadProjectFile(temporalFile)
            writers.MergeFiles(temporalFile, temporalFile, textFileReaders.ReadProjectFile(projectFile), "#SFXList")
            writers.CreateProjectFile(projectFile, temporalProjFile.SoundBankList, temporalProjFile.DataBaseList, ListBox_SFXs.Items.Cast(Of String).ToArray)
        End If
    End Sub

    Private Sub ContextMenuSfx_Rename_Click(sender As Object, e As EventArgs) Handles ContextMenuSfx_Rename.Click
        If ListBox_SFXs.SelectedItems.Count = 1 Then
            'Get current fileName
            Dim selectedName As String = ListBox_SFXs.SelectedItem
            Dim currentFileName As String = Path.Combine(WorkingDirectory, "SFXs", selectedName & ".txt")

            'Ask for a new name
            Dim diagResult As String = RenameFile(selectedName, "SFX", Path.Combine(WorkingDirectory, "SFXs"))
            If diagResult IsNot "" Then
                Dim mainForm As MainFrame = CType(Application.OpenForms("MainFrame"), MainFrame)

                'Update UI and text file
                File.Move(currentFileName, Path.Combine(WorkingDirectory, "SFXs", diagResult & ".txt"))
                ListBox_SFXs.Items(ListBox_SFXs.SelectedIndex) = diagResult

                'Clear Selection
                mainForm.TreeView_SoundBanks.SelectedNode = Nothing
                mainForm.ListBox_DataBases.SelectedItems.Clear()
                mainForm.ListBox_DataBaseSFX.Items.Clear()

                'Update project file
                Dim databasesToWrite As String() = mainForm.ListBox_DataBases.Items.Cast(Of String).ToArray
                Dim sfxsToWriter As String() = mainForm.UserControl_SFXs.ListBox_SFXs.Items.Cast(Of String).ToArray
                writers.CreateProjectFile(Path.Combine(WorkingDirectory, "Project.txt"), Nothing, databasesToWrite, sfxsToWriter)

                'Update databases
                Dim databaseFiles As String() = Directory.GetFiles(Path.Combine(WorkingDirectory, "Databases"), "*.txt", SearchOption.TopDirectoryOnly)
                For index As Integer = 0 To databaseFiles.Length - 1
                    'Read file
                    Dim fileLines As String() = File.ReadAllLines(databaseFiles(index))

                    'Update file and save changes
                    Dim sfxItemIndex = Array.FindIndex(fileLines, Function(t) t.Equals(selectedName, StringComparison.OrdinalIgnoreCase))
                    If sfxItemIndex >= 0 Then
                        fileLines(sfxItemIndex) = diagResult
                        File.WriteAllLines(databaseFiles(index), fileLines)
                    End If
                Next

                'Liberate Memmory
                Erase databaseFiles
            End If
        Else
            My.Computer.Audio.PlaySystemSound(Media.SystemSounds.Asterisk)
        End If
    End Sub

    Private Sub RemoveSfxAllFolders(filename As String, trashFolder As String)
        Dim fileList As String() = Directory.GetFiles(Path.Combine(WorkingDirectory, "SFXs"), filename & ".txt", SearchOption.AllDirectories)
        For index As Integer = 0 To fileList.Length - 1
            If File.Exists(fileList(index)) Then
                File.Copy(fileList(index), trashFolder, True)
                File.Delete(fileList(index))
            End If
        Next
        Erase fileList
    End Sub

    Private Sub ContextMenuSfx_NewMultiple_Click(sender As Object, e As EventArgs) Handles ContextMenuSfx_NewMultiple.Click
        'Ensure that the default file exists
        If File.Exists(SysFileSfxDefaults) AndAlso File.ReadAllLines(SysFileSfxDefaults).Length > 7 Then
            'Check if we need to realloc
            If SFXHashCodeNumber = 0 Then
                MsgBox("Please Re-Alloc Hashcodes under Advanced Menu", vbOKOnly + vbExclamation, "EuroSound")
            Else
                'Start form
                Dim newMultiple As New SfxNewMultiple
                newMultiple.ShowDialog()
            End If
        Else
            'Inform user about this
            MsgBox("Must Setup Default SFX file first!", vbOKOnly + vbCritical, "Setup SFX Defaults.")

            'Open SFX Default Form
            Using defaultSettingsForm As New SfxDefault
                defaultSettingsForm.ShowDialog()
            End Using
        End If
    End Sub

    Private Sub ContextMenuSfx_MultiEditor_Click(sender As Object, e As EventArgs) Handles ContextMenuSfx_MultiEditor.Click
        Dim listOfSFXs As New List(Of String)
        For itemIndex As Integer = 0 To ListBox_SFXs.SelectedItems.Count - 1
            listOfSFXs.Add(Path.Combine(WorkingDirectory, "SFXs", ListBox_SFXs.SelectedItems(itemIndex) & ".txt"))
        Next

        Dim multiEditor As New SfxMultiEditor(listOfSFXs.ToArray)
        multiEditor.ShowDialog()
    End Sub

    '*===============================================================================================
    '* LISTBOX EVENTS
    '*===============================================================================================
    Private Sub ListBox_SFXs_DragOver(sender As Object, e As DragEventArgs) Handles ListBox_SFXs.DragOver
        e.Effect = DragDropEffects.Move
    End Sub

    Private Sub ListBox_SFXs_DragDrop(sender As Object, e As DragEventArgs) Handles ListBox_SFXs.DragDrop
        If e.Effect = DragDropEffects.Move Then
            If e.Data.GetDataPresent(GetType(ListBox.SelectedObjectCollection)) Then
                Dim itemsData As ListBox.SelectedObjectCollection = e.Data.GetData(GetType(ListBox.SelectedObjectCollection))
                If itemsData.Count > 0 Then
                    'Get mainframe
                    Dim mainForm As MainFrame = CType(Application.OpenForms("MainFrame"), MainFrame)

                    'Remove items
                    While mainForm.ListBox_DataBaseSFX.SelectedItems.Count > 0
                        mainForm.ListBox_DataBaseSFX.Items.Remove(mainForm.ListBox_DataBaseSFX.SelectedItems(0))
                    End While

                    'Update text
                    Dim databaseTxt As String = Path.Combine(WorkingDirectory, "DataBases", mainForm.ListBox_DataBases.SelectedItem & ".txt")
                    Dim databaseDependencies As String() = mainForm.ListBox_DataBaseSFX.Items.Cast(Of String).ToArray
                    writers.UpdateDataBaseText(databaseTxt, databaseDependencies, textFileReaders)
                End If
            End If
        End If
    End Sub

    Private Sub ListBox_SFXs_DoubleClick(sender As Object, e As EventArgs) Handles ListBox_SFXs.DoubleClick
        'Ensure that we have selected an item
        If ListBox_SFXs.SelectedItems.Count > 0 AndAlso AllowDoubleClick Then
            'Get item and file path
            Dim selectedSFX As String = ListBox_SFXs.SelectedItem
            Dim SelectedSfxPath = Path.Combine(WorkingDirectory, "SFXs", selectedSFX & ".txt")

            'Ensure that the file exists
            If File.Exists(SelectedSfxPath) Then
                'Open editor
                Dim sfxEditor As New Frm_SfxEditor(selectedSFX)
                sfxEditor.ShowDialog()
            End If
        End If
    End Sub

    '*===============================================================================================
    '* FUNCTIONS
    '*===============================================================================================
    Private Function GetSfxNamesArray() As String()
        Dim listOfNames As New List(Of String)
        For index As Integer = 0 To ListBox_SFXs.Items.Count - 1
            listOfNames.Add(ListBox_SFXs.Items(index))
        Next

        Return listOfNames.ToArray
    End Function
End Class

Imports System.IO
Imports sb_editor.WritersClasses

Public Class Frm_RefineSearch
    Inherits Frm_TimerForm
    '*===============================================================================================
    '* GLOBAL VARS
    '*===============================================================================================
    Private ReadOnly writers As New FileWriters
    Private ReadOnly mainframe As UserControl_SFXs
    Private canCloseForm As Boolean = False

    '*===============================================================================================
    '* FORM EVENTS
    '*===============================================================================================
    Sub New(parentControl As UserControl_SFXs)
        ' Esta llamada es exigida por el diseñador.
        InitializeComponent()
        ' Agregue cualquier inicialización después de la llamada a InitializeComponent().
        mainframe = parentControl
    End Sub

    Private Sub Frm_RefineSearch_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        'Start process
        If Not BackgroundWorker.IsBusy Then
            BackgroundWorker.RunWorkerAsync()
        End If
    End Sub

    Private Sub Frm_RefineSearch_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        If Not canCloseForm Then
            e.Cancel = True
        End If
    End Sub

    '*===============================================================================================
    '* BACKGROUND WORKER EVENTS
    '*===============================================================================================
    Private Sub BackgroundWorker_DoWork(sender As Object, e As System.ComponentModel.DoWorkEventArgs) Handles BackgroundWorker.DoWork
        'Update list
        Dim sfxFilesToCheck As New List(Of String)
        Dim filesToCheck As String() = Directory.GetFiles(Path.Combine(WorkingDirectory, "SFXs"), "*.txt", SearchOption.TopDirectoryOnly)
        For fileIndex As Integer = 0 To filesToCheck.Length - 1
            sfxFilesToCheck.Add(Path.GetFileNameWithoutExtension(filesToCheck(fileIndex)))
        Next

        'Add item to listbox
        Invoke(Sub() mainframe.ListBox_SFXs.BeginUpdate())
        Invoke(Sub() mainframe.ListBox_SFXs.Items.Clear())
        Invoke(Sub() mainframe.ListBox_SFXs.Items.AddRange(sfxFilesToCheck.ToArray))
        Invoke(Sub() mainframe.ListBox_SFXs.EndUpdate())

        'Update counter
        Invoke(Sub() mainframe.Label_TotalSfx.Text = "Total: " & mainframe.ListBox_SFXs.Items.Count)

        'Start refine list words search
        If sfxFilesToCheck.Count > 0 Then
            'Clear comboboxes
            Invoke(Sub() mainframe.ComboBox_Temporal.Items.Clear())
            Invoke(Sub() mainframe.ComboBox_SFX_Section.Items.Clear())
            'Split only six words
            For wordIndexCount As Integer = 0 To 5
                'Iterate listbox items
                Dim listboxItemsCount As Integer = sfxFilesToCheck.Count - 1
                For sfxItemIndex As Integer = 0 To listboxItemsCount
                    'Calculate and report progress
                    Dim totalItems = listboxItemsCount * 6
                    Dim progFromPrevIterations = listboxItemsCount * wordIndexCount
                    BackgroundWorker.ReportProgress(Decimal.Divide(sfxItemIndex + progFromPrevIterations, totalItems) * 100.0)
                    'Iterate listbox items to find matches
                    For sfxItemIndexSub As Integer = 0 To listboxItemsCount
                        'Skip the line that we are checking in the previus loop
                        If sfxItemIndex = sfxItemIndexSub Then
                            Continue For
                        End If
                        'Get item from listbox
                        Dim currentSfx As String = sfxFilesToCheck(sfxItemIndex)
                        Dim wordToCheck As String = currentSfx
                        'Split words
                        If wordIndexCount > 0 Then
                            For wordIndex = 1 To wordIndexCount
                                If InStr(1, wordToCheck, "_", CompareMethod.Binary) Then
                                    Dim wordLength As Integer = Len(wordToCheck) - InStr(1, wordToCheck, "_", CompareMethod.Binary)
                                    wordToCheck = Microsoft.VisualBasic.Right(wordToCheck, wordLength)
                                End If
                            Next
                        End If
                        If InStr(1, wordToCheck, "_", CompareMethod.Binary) Then
                            Dim wordLength As Integer = InStr(1, wordToCheck, "_", CompareMethod.Binary) - 1
                            wordToCheck = Microsoft.VisualBasic.Left(wordToCheck, wordLength)
                        End If
                        'Find matches
                        If StrComp("SFX", wordToCheck) <> 0 Then
                            If Len(wordToCheck) > 2 Then
                                currentSfx = sfxFilesToCheck(sfxItemIndexSub)
                                If InStr(1, currentSfx, wordToCheck, CompareMethod.Binary) Then
                                    'Get combo items count
                                    Dim addNewItem As Boolean = True
                                    For comboboxIndex As Integer = 0 To mainframe.ComboBox_Temporal.Items.Count - 1
                                        Dim comboWordItem As String = CType(mainframe.ComboBox_Temporal.Items(comboboxIndex), ComboItemData).Name
                                        'Check for duplicated
                                        If InStr(1, comboWordItem, wordToCheck, CompareMethod.Binary) = 0 Then
                                            Continue For
                                        End If
                                        'Add appearance to dictionary
                                        currentSfx = CType(mainframe.ComboBox_Temporal.Items(comboboxIndex), ComboItemData).Name
                                        If StrComp(currentSfx, wordToCheck) = 0 Then
                                            'Get current item data
                                            Dim currentItemData As Integer = CType(mainframe.ComboBox_Temporal.Items(comboboxIndex), ComboItemData).ItemData
                                            'Update value
                                            currentItemData += 1
                                            CType(mainframe.ComboBox_Temporal.Items(comboboxIndex), ComboItemData).ItemData = currentItemData
                                        End If
                                        'Don't add items in the combobox and quit loop
                                        addNewItem = False
                                        Exit For
                                    Next
                                    'Check if we have to add the new item
                                    If addNewItem Then
                                        Invoke(Sub() mainframe.ComboBox_Temporal.Items.Add(New ComboItemData(wordToCheck, 0)))
                                    End If
                                End If
                            End If
                        End If
                    Next
                Next
            Next

            'Get final words
            Invoke(Sub() mainframe.ComboBox_SFX_Section.Items.Add("All"))
            Invoke(Sub() mainframe.ComboBox_SFX_Section.Items.Add("HighLighted"))
            If mainframe.ComboBox_Temporal.Items.Count > 0 Then
                Dim quitLoop As Boolean = False
                Do
                    Dim itemToRemove As Integer = -1
                    'Get max value from the remaining words
                    Dim maxWordAppearances As Integer = 0
                    For itemIndex As Integer = 0 To mainframe.ComboBox_Temporal.Items.Count - 1
                        Dim itemData As Integer = CType(mainframe.ComboBox_Temporal.Items(itemIndex), ComboItemData).ItemData
                        maxWordAppearances = Math.Max(maxWordAppearances, itemData)
                    Next
                    'Get the item with the max value
                    For index As Integer = 0 To mainframe.ComboBox_Temporal.Items.Count - 1
                        Dim itemData As Integer = CType(mainframe.ComboBox_Temporal.Items(index), ComboItemData).ItemData
                        If itemData = maxWordAppearances And itemToRemove = -1 Then
                            itemToRemove = index
                        End If
                    Next
                    'Remove and add items
                    Dim itemStringName As String = CType(mainframe.ComboBox_Temporal.Items(itemToRemove), ComboItemData).Name
                    Invoke(Sub() mainframe.ComboBox_SFX_Section.Items.Add(itemStringName))
                    Invoke(Sub() mainframe.ComboBox_Temporal.Items.RemoveAt(itemToRemove))
                    'Check if we have to skip this loop
                    If maxWordAppearances <= 5 Or mainframe.ComboBox_Temporal.Items.Count < 1 Then
                        quitLoop = True
                    End If
                Loop While quitLoop <> True
            End If
            'Select the first item
            Invoke(Sub() mainframe.ComboBox_SFX_Section.SelectedIndex = 0)

            'Update file
            If Directory.Exists(Path.Combine(WorkingDirectory, "System")) Then
                Dim refineList() As String = (From item As String In mainframe.ComboBox_SFX_Section.Items Select item).ToArray
                writers.CreateRefineList(Path.Combine(WorkingDirectory, "System\RefineSearch.txt"), refineList)
            End If
        End If
    End Sub

    Private Sub BackgroundWorker_ProgressChanged(sender As Object, e As System.ComponentModel.ProgressChangedEventArgs) Handles BackgroundWorker.ProgressChanged
        ProgressBar1.Value = e.ProgressPercentage
    End Sub

    Private Sub BackgroundWorker_RunWorkerCompleted(sender As Object, e As System.ComponentModel.RunWorkerCompletedEventArgs) Handles BackgroundWorker.RunWorkerCompleted
        canCloseForm = True
        Close()
    End Sub
End Class
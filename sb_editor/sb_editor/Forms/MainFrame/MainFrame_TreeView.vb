﻿Partial Public Class MainFrame
    Private Sub DeleteDatabaseFromSoundbank(soundbankNode As TreeNode, fileName As String)
        'Remove node
        For Each sb_database As TreeNode In soundbankNode.Nodes
            If sb_database IsNot Nothing Then
                If StrComp(sb_database.Text, fileName) = 0 Then
                    sb_database.Remove()
                End If
            End If
        Next
        'Add Empty Node
        If soundbankNode.Nodes.Count = 0 Then
            soundbankNode.Nodes.Add("Empty", "Empty Sound Bank", 3, 3)
        End If
    End Sub

    Private Sub CreateNewSoundbank(soundbankName As String)
        'Create node
        Dim soundbankNode = TreeView_SoundBanks.Nodes.Add(CStr(SoundBankHashCodeNumber), soundbankName, 0, 0)
        soundbankNode.Nodes.Add("Empty", "Empty Sound Bank", 3, 3)
        'Create SoundbankObject
        Dim soundbankObj As New SoundbankFile With {
            .HashCode = SoundBankHashCodeNumber
        }
        writers.UpdateSoundbankFile(soundbankObj, fso.BuildPath(WorkingDirectory, "Soundbanks\" & soundbankName & ".txt"), textFileReaders)
        'Update global var
        SoundBankHashCodeNumber += 1
        'Sort control
        TreeView_SoundBanks.Sort()
        'Expand node
        soundbankNode.Expand()
        'Seect node
        TreeView_SoundBanks.SelectedNode = soundbankNode
    End Sub

    Private Sub CopySoundbank(soundbankName As String, sourceSoundbankNode As TreeNode)
        'Node Clonation 
        Dim nodeToAdd As TreeNode = sourceSoundbankNode.Clone
        nodeToAdd.Text = soundbankName
        nodeToAdd.Name = SoundBankHashCodeNumber
        TreeView_SoundBanks.Nodes.Add(nodeToAdd)
        'Create text file
        Dim soundbankFileData As SoundbankFile = textFileReaders.ReadSoundBankFile(fso.BuildPath(WorkingDirectory, "Soundbanks\" & sourceSoundbankNode.Text & ".txt"))
        soundbankFileData.HashCode = SoundBankHashCodeNumber
        writers.UpdateSoundbankFile(soundbankFileData, fso.BuildPath(WorkingDirectory, "Soundbanks\" & soundbankName & ".txt"), textFileReaders)
        'Update global var
        SoundBankHashCodeNumber += 1
        'Sort control
        TreeView_SoundBanks.Sort()
        'Expand node
        nodeToAdd.Expand()
        'Seect node
        TreeView_SoundBanks.SelectedNode = nodeToAdd
    End Sub

    Private Sub AddDatabaseToSoundbank(dataBasesToAdd As List(Of String), soundBank As TreeNode)
        Dim soundbankFilePath As String = fso.BuildPath(WorkingDirectory, "Soundbanks\" & soundBank.Text & ".txt")
        If fso.FileExists(soundbankFilePath) Then
            'Read file data 
            Dim soundbankData As SoundbankFile = textFileReaders.ReadSoundBankFile(soundbankFilePath)
            If soundbankData IsNot Nothing Then
                'Delete empty node if exists
                If soundBank.Nodes.Count = 1 Then
                    If StrComp(soundBank.Nodes(0).Name, "Empty") = 0 Then
                        soundBank.Nodes(0).Remove()
                    End If
                End If
                'Get new items to add
                Dim itemsToAdd As List(Of String) = dataBasesToAdd.Except(soundbankData.Dependencies).ToList
                If itemsToAdd.Count > 0 Then
                    'Add new nodes
                    For Each database In dataBasesToAdd
                        'Add databases
                        soundBank.Nodes.Add(database, database, 2, 2)
                    Next
                    'Sort control
                    TreeView_SoundBanks.Sort()
                    'Select soundbank again
                    TreeView_SoundBanks.SelectedNode = soundBank
                    'Add items to file data
                    itemsToAdd.AddRange(soundbankData.Dependencies)
                    itemsToAdd.Sort()
                    soundbankData.Dependencies = itemsToAdd.ToArray
                    'Update text file
                    writers.UpdateSoundbankFile(soundbankData, soundbankFilePath, textFileReaders)
                End If
            End If
        End If
    End Sub
End Class

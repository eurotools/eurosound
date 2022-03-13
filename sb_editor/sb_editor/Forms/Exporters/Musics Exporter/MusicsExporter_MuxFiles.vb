﻿Imports ESUtils.MusXBuild_MusicFile

Partial Public Class MusicsExporter
    Private Sub CreateMusXFiles(outputPlatforms As String())
        'Update listview
        For fileIndex As Integer = 0 To outputQueue.Rows.Count - 1
            Dim musicItem As DataRow = outputQueue.Rows(fileIndex)
            'Update Title bar
            Invoke(Sub() Text = "Binding Files: " & musicItem.ItemArray(0))
            For platformIndex As Integer = 0 To outputPlatforms.Length - 1
                'Update progress bar
                BackgroundWorker.ReportProgress(Decimal.Divide(platformIndex + (fileIndex * outputPlatforms.Length), outputQueue.Rows.Count * outputPlatforms.Length) * 100.0)
                'Get the current platform
                Dim musicHashCode As Integer = musicItem.ItemArray(2)
                Dim currentPlatform As String = outputPlatforms(platformIndex)

                'Get file data and output path
                Dim outputFilePath As String = GetOutputFolder(musicHashCode, currentPlatform)

                'Get output file paths
                Dim soundMarkerFile As String = fso.BuildPath(outputFilePath, "MFX_" & musicHashCode & ".smf")
                Dim soundSampleData As String = fso.BuildPath(outputFilePath, "MFX_" & musicHashCode & ".ssd")
                Dim musxFilename As String = "HCE" & Hex(musicHashCode).PadLeft(5, "0"c) & ".SFX"
                Dim folderPath = fso.BuildPath(ProjectSettingsFile.MiscProps.EngineXFolder, "Binary\" & GetEngineXFolder(currentPlatform) & "\music")
                CreateFolderIfRequired(folderPath)

                'Build file
                Dim fullDirPath = fso.BuildPath(folderPath, musxFilename)
                If StrComp(outputPlatforms(platformIndex), "GameCube") = 0 Then
                    BuildMusicFile(soundMarkerFile, soundSampleData, fullDirPath, musicHashCode, True)
                Else
                    BuildMusicFile(soundMarkerFile, soundSampleData, fullDirPath, musicHashCode, False)
                End If
                'Update progress bar
                'BackgroundWorker.ReportProgress(counter)
            Next
        Next
    End Sub
End Class
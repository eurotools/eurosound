Imports System.ComponentModel
Imports System.IO
Imports NAudio.Wave
Imports sb_editor.ParsersObjects

Partial Public Class ExporterForm
    Private Function CreateSfxDataTempFolder(samplesDt As DataTable, outputLanguages As String()) As Integer
        Dim maxHashCode As Integer = 0
        'Ensure that we have files to resample
        If samplesDt.Rows.Count > 0 Then
            'Reset progress bar
            Invoke(Sub() ProgressBar1.Value = 0)

            'Get output folder
            Dim tempSfxDataFolder As String = Path.Combine(WorkingDirectory, "TempSfxData")
            Directory.CreateDirectory(tempSfxDataFolder)
            Dim StreamSamplesList As String() = textFileReaders.GetAllStreamSamples(samplesDt)

            'Start resample
            Dim sfxFiles As String() = Directory.GetFiles(Path.Combine(WorkingDirectory, "SFXs"), "*.txt", SearchOption.TopDirectoryOnly)
            Dim samplesCount As Integer = sfxFiles.Length - 1
            For index As Integer = 0 To samplesCount
                'Update title bar
                Dim currentSampleName As String = Path.GetFileNameWithoutExtension(sfxFiles(index))

                'Calculate progress and update title bar
                BackgroundWorker.ReportProgress(Decimal.Divide(index, samplesCount) * 100.0, "Creating SFX_Data.h " & currentSampleName)

                'Read file data
                If File.Exists(sfxFiles(index)) Then
                    Dim sfxFile As SfxFile = textFileReaders.ReadSFXFile(sfxFiles(index))
                    If sfxFile.Samples.Count > 0 Then
                        'Get file path
                        Dim waveRelPath As String = sfxFile.Samples(0).FilePath.TrimStart("\")
                        Dim waveFilePath As String
                        If waveRelPath.IndexOf("SPEECH\ENGLISH", StringComparison.OrdinalIgnoreCase) >= 0 Then
                            waveFilePath = Path.Combine(WorkingDirectory, "Master", "Speech", outputLanguages(outputLanguages.Length - 1), waveRelPath.Substring(15))
                        Else
                            waveFilePath = Path.Combine(WorkingDirectory, "Master", waveRelPath)
                        End If

                        'Get duration
                        Dim waveDuration As Single = 0.00002267574F
                        Dim waveLooping As Integer() = New Integer() {0, 0, 0}
                        If File.Exists(waveFilePath) Then
                            Using waveReader As New WaveFileReader(waveFilePath)
                                waveDuration = waveReader.Length / waveReader.WaveFormat.AverageBytesPerSecond
                                waveLooping = ReadWaveSampleChunk(waveReader)
                            End Using
                        Else
                            sfxFile.Parameters.TrackingType = 4
                        End If

                        'Check if is streamed or not
                        Dim sampleIsStreamed = 0
                        If Array.FindIndex(StreamSamplesList, Function(t) t.Equals(waveRelPath, StringComparison.OrdinalIgnoreCase)) <> -1 Then
                            sampleIsStreamed = 1
                        End If

                        'Create File
                        Dim textFilePath As String = Path.Combine(tempSfxDataFolder, currentSampleName & ".txt")
                        Using sw As StreamWriter = File.CreateText(textFilePath)
                            sw.WriteLine(waveFilePath.ToUpper)
                            sw.WriteLine("{ " & sfxFile.HashCode & " ,  " & sfxFile.Parameters.InnerRadius & ".0f ,  " & sfxFile.Parameters.OuterRadius & ".0f ,  " & sfxFile.Parameters.Alertness & ".0f ,  " & GetWaveDurationFormatted(waveDuration) & "f , " & Math.Max(waveLooping(0), Convert.ToByte(sfxFile.SamplePool.isLooped)) & ", " & sfxFile.Parameters.TrackingType & ", " & sampleIsStreamed & " } ,  // " & currentSampleName)
                        End Using

                        'Get Max HashCode
                        maxHashCode = Math.Max(maxHashCode, sfxFile.HashCode)
                    End If
                End If
            Next

            'Liberate Memmory
            Erase sfxFiles
        End If
        Return maxHashCode
    End Function

    Friend Sub CreateSFXDataBinaryFiles(sfxDataFilePath As String, outPlatforms As String(), outputLanguages As String())
        Dim currentLanguage As String = outputLanguages(0)
        For platformIndex As Integer = 0 To outPlatforms.Length - 1
            Dim currentPlatform As String = outPlatforms(platformIndex)

            'Output File Path
            Dim outputFolder As String = Path.Combine(ProjectSettingsFile.MiscProps.EngineXFolder, "Binary", GetEngineXFolder(currentPlatform), "music")
            Directory.CreateDirectory(outputFolder)

            'Create bin file
            RunConsoleProcess("SystemFiles\CreateSFXDataBin.exe", """" & sfxDataFilePath & """ """ & Path.Combine(outputFolder, "SFX_Data.bin") & """")
        Next
    End Sub

    Private Function GetWaveDurationFormatted(waveDuration As Single) As String
        'Get the number of decimal places
        Dim stringValue As String = waveDuration.ToString(numericProvider)
        Dim decimalPointIndex As Integer = stringValue.IndexOf(".")

        'Format String
        If decimalPointIndex > -1 AndAlso stringValue.Length - decimalPointIndex > 8 Then
            Return waveDuration.ToString("0.######E+00", numericProvider)
        End If

        Return waveDuration.ToString(".#######", numericProvider)
    End Function
End Class

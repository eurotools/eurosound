﻿Imports System.ComponentModel
Imports System.IO
Imports NAudio.Wave

Partial Public Class ExporterForm
    '*===============================================================================================
    '* MAIN METHOD
    '*===============================================================================================
    Private Sub ResampleWaves(soundsTable As DataTable, outputPlatforms As String())
        Dim waveFunctions As New WaveFunctions

        'Get Wave files to include
        Dim samplesCount As Integer = soundsTable.Rows.Count - 1
        If samplesCount > 0 Then
            'Reset progress bar
            Invoke(Sub() ProgressBar1.Value = 0)
            'Start inspecting each line of the datatable
            For rowIndex As Integer = 0 To samplesCount
                If StrComp(soundsTable.Rows(rowIndex).Item(4), "True") = 0 Then
                    'Get paths 
                    Dim sampleRelativePath As String = soundsTable.Rows(rowIndex).ItemArray(0)
                    Dim sourceFilePath As String = fso.BuildPath(ProjectSettingsFile.MiscProps.SampleFileFolder & "\Master", sampleRelativePath)

                    'Calculate and report progress
                    Dim totalProgress As Double = Decimal.Divide(rowIndex, samplesCount) * 100.0
                    BackgroundWorker.ReportProgress(totalProgress)

                    'Resample for each platform 
                    For platformIndex As Integer = 0 To outputPlatforms.Length - 1
                        Dim currentPlatform As String = outputPlatforms(platformIndex)
                        Dim outputFilePath As String = fso.BuildPath(WorkingDirectory & "\" & currentPlatform, sampleRelativePath)

                        'Update title
                        Invoke(Sub() Text = "ReSampling: " & sampleRelativePath & "  " & currentPlatform)

                        'Get wave frequency for the destination format
                        Dim sampleRateLabel As String = soundsTable.Rows(rowIndex).ItemArray(1)
                        Dim sampleRate As Integer = ProjectSettingsFile.sampleRateFormats(currentPlatform)(sampleRateLabel)

                        'Resample the wav for the destination platform
                        CreateFolderIfRequired(fso.GetParentFolderName(outputFilePath))
                        RunProcess("SystemFiles\Sox.exe", """" & sourceFilePath & """ -r " & sampleRate & " """ & outputFilePath & """")

                        'IMA ADPCM For PC and Nintendo GameCube Formats
                        If StrComp(currentPlatform, "PC") = 0 Or StrComp(currentPlatform, "GameCube") = 0 Then
                            If StrComp(soundsTable.Rows(rowIndex).Item(5), "True") = 0 Then
                                Dim ImaOutputFilePath As String = fso.BuildPath(WorkingDirectory & "\" & currentPlatform & "_Software_adpcm", sampleRelativePath)
                                CreateFolderIfRequired(fso.GetParentFolderName(ImaOutputFilePath))
                                'Resampled wav
                                Dim smdFilePath As String = Path.ChangeExtension(ImaOutputFilePath, ".smd")
                                RunProcess("SystemFiles\Sox.exe", """" & sourceFilePath & """ -t raw -r " & sampleRate & " -c 1 -s """ & smdFilePath & """")
                                'Wave to ima
                                RunProcess("SystemFiles\Sox.exe", "-t raw -w -s -r " & sampleRate & " -c 1 """ & smdFilePath & """ -t ima """ & Path.ChangeExtension(ImaOutputFilePath, ".ssp") & """")
                            End If
                        End If

                        'DSP for Nintendo GameCube
                        If StrComp(currentPlatform, "GameCube") Then
                            Dim dspOutputFilePath As String = Path.ChangeExtension(fso.BuildPath(WorkingDirectory & "\GameCube_dsp_adpcm", sampleRelativePath), ".dsp")
                            CreateFolderIfRequired(fso.GetParentFolderName(dspOutputFilePath))
                            'Default arguments
                            Dim dspToolArgs As String = "Encode """ & outputFilePath & """ """ & dspOutputFilePath & """"
                            'Get loop info
                            Using waveReader As New WaveFileReader(sourceFilePath)
                                Dim loopInfo As Integer() = WaveFunctions.ReadSampleChunk(waveReader)
                                If loopInfo(0) = 1 And StrComp(soundsTable.Rows(rowIndex).Item(5), "True") = 0 Then
                                    'Loop offset pos in the resampled wave
                                    Using parsedWaveReader As New WaveFileReader(outputFilePath)
                                        Dim parsedLoop As UInteger = (loopInfo(1) / (waveReader.Length / parsedWaveReader.Length)) * 2
                                        dspToolArgs = "Encode """ & outputFilePath & """ """ & dspOutputFilePath & """ -L " & parsedLoop
                                    End Using
                                End If
                            End Using
                            'Execute Dsp Adpcm Tool
                            RunProcess("SystemFiles\DspCodec.exe", dspToolArgs)
                        End If

                        'Sony VAG for PlayStation 2
                        If StrComp(currentPlatform, "PlayStation2") Then
                            Dim vagOutputFilePath As String = Path.ChangeExtension(fso.BuildPath(WorkingDirectory & "\PlayStation2_VAG", sampleRelativePath), ".vag")
                            CreateFolderIfRequired(fso.GetParentFolderName(vagOutputFilePath))
                            'Default arguments
                            Dim vagToolArgs As String = """" & outputFilePath & """ """ & vagOutputFilePath & """"
                            'Get loop info
                            Using waveReader As New WaveFileReader(sourceFilePath)
                                Dim loopInfo As Integer() = WaveFunctions.ReadSampleChunk(waveReader)
                                If loopInfo(0) = 1 And StrComp(soundsTable.Rows(rowIndex).Item(5), "True") = 0 Then
                                    'Loop offset pos in the resampled wave
                                    Using parsedWaveReader As New WaveFileReader(outputFilePath)
                                        Dim parsedLoop As UInteger = (loopInfo(1) / (waveReader.Length / parsedWaveReader.Length)) * 2
                                        Dim loopOffsetVag As UInteger = ((parsedLoop / 28 + (If(((parsedLoop Mod 28) <> 0), 2, 1))) / 2) - 1
                                        vagToolArgs = """" & outputFilePath & """ """ & vagOutputFilePath & """ -l" & loopOffsetVag
                                    End Using
                                End If
                            End Using
                            'Execute Vag Tool
                            RunProcess("SystemFiles\VagCodec.exe", vagToolArgs)
                        End If

                        'Xbox ADPCM for Xbox
                        If StrComp(currentPlatform, "X Box") = 0 Then
                            Dim xboxOutputFilePath As String = Path.ChangeExtension(fso.BuildPath(WorkingDirectory & "\XBox_adpcm", sampleRelativePath), ".adpcm")
                            CreateFolderIfRequired(fso.GetParentFolderName(xboxOutputFilePath))
                            'Execute Dsp Adpcm Tool
                            RunProcess("SystemFiles\XboxCodec.exe", "Encode """ & outputFilePath & """ """ & xboxOutputFilePath & """")
                        End If
                    Next
                    'Update Property
                    soundsTable.Rows(rowIndex).Item(4) = "False"
                End If
            Next
        End If
    End Sub

End Class

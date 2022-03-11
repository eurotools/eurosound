﻿Imports System.IO
Imports NAudio.Wave
Imports sb_editor.ExporterObjects
Imports sb_editor.ParsersObjects
Imports sb_editor.ReaderClasses

Namespace SoundBanksExporterFunctions
    Friend Module SBExporterModule
        Private ReadOnly textFileReaders As New FileParsers

        Friend Sub GetSFXsDictionary(sfxList As String(), outPlatform As String, SfxDictionary As SortedDictionary(Of String, EXSound), samplesToInclude As HashSet(Of String), streamsList As String(), Optional testMode As Boolean = False)
            'Read all stored SFXs in the DataBases
            For sfxIndex As Integer = 0 To sfxList.Length - 1
                Dim sfxFileName As String = sfxList(sfxIndex)
                Dim sfxFilePath As String = fso.BuildPath(WorkingDirectory & "\SFXs", sfxFileName)
                If fso.FileExists(sfxFilePath) Then
                    'Check for specific formats
                    Dim sfxPlatformPath As String = fso.BuildPath(WorkingDirectory & "\SFXs\" & outPlatform, sfxFileName)
                    If fso.FileExists(sfxPlatformPath) Then
                        sfxFilePath = sfxPlatformPath
                    End If
                    'Read SFX
                    Dim sfxFileData As SfxFile = textFileReaders.ReadSFXFile(sfxFilePath)
                    Dim soundToAdd As New EXSound With {
                        .HashCode = sfxFileData.HashCode,
                        .Ducker = sfxFileData.Parameters.Ducker,
                        .DuckerLength = sfxFileData.Parameters.DuckerLength,
                        .Flags = GetSfxFlags(sfxFileData),
                        .InnerRadius = sfxFileData.Parameters.InnerRadius,
                        .MasterVolume = sfxFileData.Parameters.MasterVolume,
                        .MaxDelay = sfxFileData.SamplePool.MaxDelay,
                        .MaxVoices = sfxFileData.Parameters.MaxVoices,
                        .MinDelay = sfxFileData.SamplePool.MinDelay,
                        .OuterRadius = sfxFileData.Parameters.OuterRadius,
                        .Priority = sfxFileData.Parameters.Priority,
                        .ReverbSend = sfxFileData.Parameters.ReverbSend,
                        .TrackingType = sfxFileData.Parameters.TrackingType,
                        .HasSubSfx = sfxFileData.SamplePool.EnableSubSFX
                    }
                    For sampleIndex As Integer = 0 To sfxFileData.Samples.Count - 1
                        Dim currentSample As Sample = sfxFileData.Samples(sampleIndex)
                        Dim sampleToAdd As New EXSample With {
                            .FilePath = RelativePathToAbs(currentSample.FilePath),
                            .PitchOffset = currentSample.PitchOffset * 1024,
                            .RandomPitchOffset = currentSample.RandomPitchOffset * 1024,
                            .BaseVolume = currentSample.BaseVolume,
                            .RandomVolumeOffset = currentSample.RandomVolumeOffset,
                            .Pan = currentSample.Pan,
                            .RandomPan = currentSample.RandomPan
                        }
                        If Not sfxFileData.SamplePool.EnableSubSFX Then
                            'Check if this sample is streamed or not
                            Dim arraySearchResult As Integer = Array.IndexOf(streamsList, sampleToAdd.FilePath)
                            If arraySearchResult = -1 Then
                                samplesToInclude.Add(currentSample.FilePath)
                            End If
                        End If
                        'Add new sample to the dictionary
                        soundToAdd.Samples.Add(sampleToAdd)
                    Next
                    'Calculate Ducker Length
                    Dim duckerLength As Short = 0
                    If soundToAdd.Ducker > 0 Then
                        duckerLength = GetTotalCents(soundToAdd, outPlatform)
                        If soundToAdd.DuckerLength < 0 Then
                            duckerLength -= Math.Abs(soundToAdd.DuckerLength)
                        Else
                            duckerLength += Math.Abs(soundToAdd.DuckerLength)
                        End If
                    End If
                    soundToAdd.DuckerLength = duckerLength
                    'Check testmode
                    If testMode Then
                        soundToAdd.HashCode = 3
                    End If
                    'Add object to dictionary
                    SfxDictionary.Add(sfxFileName, soundToAdd)
                End If
            Next
        End Sub

        Friend Function GetSFXsList(soundbankData As SoundbankFile) As String()
            Dim SfxList As New HashSet(Of String)
            'Iterate over all databases
            For databaseIndex As Integer = 0 To soundbankData.Dependencies.Length - 1
                Dim databaseFilePath As String = fso.BuildPath(WorkingDirectory & "\DataBases\", soundbankData.Dependencies(databaseIndex) & ".txt")
                If fso.FileExists(databaseFilePath) Then
                    Dim databaseFile As DataBaseFile = textFileReaders.ReadDataBaseFile(databaseFilePath)
                    'Read all stored SFXs in this DataBase
                    For sfxIndex As Integer = 0 To databaseFile.Dependencies.Length - 1
                        SfxList.Add(databaseFile.Dependencies(sfxIndex) & ".txt")
                    Next
                End If
            Next
            Return SfxList.ToArray
        End Function

        Friend Sub GetSamplesDictionary(samplesToInclude As HashSet(Of String), SamplesDictionary As Dictionary(Of String, EXAudio), outPlatform As String, outputLanguage As String, CancelSoundBankOutput As Boolean, Optional testMode As Boolean = False)
            Dim SamplesSortedArray As String() = samplesToInclude.ToArray
            Array.Sort(SamplesSortedArray)
            For sampleIndex As Integer = 0 To SamplesSortedArray.Length - 1
                Dim sampleRelPath As String = SamplesSortedArray(sampleIndex)
                'If starts with speech but doesn't match the current language, get the sample with the right language
                If InStr(1, sampleRelPath, "Speech\", CompareMethod.Binary) Then
                    If StrComp(outputLanguage, "English", CompareMethod.Binary) <> 0 Then
                        Dim multiSamplePath As String = Mid(sampleRelPath, Len("Speech\English\") + 1)
                        sampleRelPath = fso.BuildPath("Speech\" & outputLanguage, multiSamplePath)
                    End If
                End If
                SamplesDictionary.Add(RelativePathToAbs(sampleRelPath), GetEXaudio(sampleRelPath, outPlatform, CancelSoundBankOutput, testMode))
                If CancelSoundBankOutput Then
                    Exit For
                End If
            Next

            Erase SamplesSortedArray
            samplesToInclude = Nothing
        End Sub

        Friend Function GetEXaudio(relativeSampleFilePath As String, outputPlatform As String, ByRef CancelSoundBankOutput As Boolean, testMode As Boolean) As EXAudio
            Dim waveFunctions As New WaveFunctions
            Dim newAudioObj As New EXAudio

            If testMode Then
                Using masterWaveReader As New WaveFileReader(fso.BuildPath(WorkingDirectory & "\Master", relativeSampleFilePath))
                    Dim loopInfo As Integer() = waveFunctions.ReadSampleChunk(masterWaveReader)
                    'Get address and flags
                    newAudioObj.Flags = loopInfo(0)
                    newAudioObj.Frequency = masterWaveReader.WaveFormat.SampleRate
                    newAudioObj.NumberOfChannels = masterWaveReader.WaveFormat.Channels
                    newAudioObj.Bits = 4
                    newAudioObj.FilePath = relativeSampleFilePath
                    newAudioObj.Duration = masterWaveReader.TotalTime.TotalMilliseconds
                    newAudioObj.SampleData = New Byte(ESUtils.BytesFunctions.AlignNumber(masterWaveReader.Length, 4) - 1) {}
                    masterWaveReader.Read(newAudioObj.SampleData, 0, masterWaveReader.Length)
                    'Get Real length
                    newAudioObj.RealSize = masterWaveReader.Length
                    'Loop offset
                    If loopInfo(0) = 1 Then
                        newAudioObj.LoopOffset = ESUtils.BytesFunctions.AlignNumber(loopInfo(1) * 2, 2)
                    End If
                End Using
            Else
                'Get loop info
                Using masterWaveReader As New WaveFileReader(fso.BuildPath(WorkingDirectory & "\Master", relativeSampleFilePath))
                    Dim loopInfo As Integer() = waveFunctions.ReadSampleChunk(masterWaveReader)
                    'Get address and flags
                    newAudioObj.Flags = loopInfo(0)
                    'Get Platform Wave Ffile
                    Dim platformWave As String = fso.BuildPath(WorkingDirectory & "\" & outputPlatform, relativeSampleFilePath)
                    If fso.FileExists(platformWave) Then
                        Using platformWaveReader As New WaveFileReader(platformWave)
                            'Common info
                            newAudioObj.Frequency = platformWaveReader.WaveFormat.SampleRate
                            newAudioObj.NumberOfChannels = platformWaveReader.WaveFormat.Channels
                            newAudioObj.Bits = 4
                            newAudioObj.FilePath = relativeSampleFilePath
                            newAudioObj.Duration = platformWaveReader.TotalTime.TotalMilliseconds
                            'Specific formats
                            If StrComp(outputPlatform, "PC") = 0 Then
                                newAudioObj.SampleData = New Byte(ESUtils.BytesFunctions.AlignNumber(platformWaveReader.Length, 4) - 1) {}
                                platformWaveReader.Read(newAudioObj.SampleData, 0, platformWaveReader.Length)
                                'Get Real length
                                newAudioObj.RealSize = platformWaveReader.Length
                                'Loop offset
                                If loopInfo(0) = 1 Then
                                    newAudioObj.LoopOffset = ESUtils.BytesFunctions.AlignNumber(ESUtils.CalculusLoopOffset.RuleOfThreeLoopOffset(masterWaveReader.WaveFormat.SampleRate, platformWaveReader.WaveFormat.SampleRate, loopInfo(1) * 2), 2)
                                End If
                            ElseIf StrComp(outputPlatform, "PlayStation2") = 0 Then
                                Dim vagFilePath As String = Path.ChangeExtension(fso.BuildPath(WorkingDirectory & "\PlayStation2_VAG", relativeSampleFilePath), ".vag")
                                If fso.FileExists(vagFilePath) Then
                                    Dim vagFile As Byte() = File.ReadAllBytes(vagFilePath)
                                    'Get wave block data aligned
                                    newAudioObj.SampleData = New Byte(ESUtils.BytesFunctions.AlignNumber(vagFile.Length, 64) - 1) {}
                                    Buffer.BlockCopy(vagFile, 0, newAudioObj.SampleData, 0, vagFile.Length)
                                    'Get Real length
                                    newAudioObj.RealSize = vagFile.Length
                                    'Loop offset
                                    If loopInfo(0) = 1 Then
                                        newAudioObj.LoopOffset = Math.Round(ESUtils.CalculusLoopOffset.RuleOfThreeLoopOffset(masterWaveReader.WaveFormat.SampleRate, platformWaveReader.WaveFormat.SampleRate, loopInfo(1) * 2))
                                    End If
                                Else
                                    MsgBox("Output Error: Sample File Missing: UNKNOWN SFX & BANK" & vbCrLf & vagFilePath, vbOKOnly + vbCritical, "EuroSound")
                                    CancelSoundBankOutput = True
                                End If
                            ElseIf StrComp(outputPlatform, "GameCube") = 0 Then
                                Dim dspFilePath As String = Path.ChangeExtension(fso.BuildPath(WorkingDirectory & "\GameCube_dsp_adpcm", relativeSampleFilePath), ".dsp")
                                If fso.FileExists(dspFilePath) Then
                                    Dim dspFile As Byte() = File.ReadAllBytes(dspFilePath)
                                    'Get wave block data aligned
                                    newAudioObj.SampleData = New Byte(ESUtils.BytesFunctions.AlignNumber(dspFile.Length, 32) - 1) {}
                                    Buffer.BlockCopy(dspFile, 0, newAudioObj.SampleData, 0, dspFile.Length)
                                    newAudioObj.DspHeaderData = File.ReadAllBytes(Path.ChangeExtension(dspFilePath, ".dsph"))
                                    'Get Real length
                                    newAudioObj.RealSize = dspFile.Length
                                    'Loop offset
                                    If loopInfo(0) = 1 Then
                                        newAudioObj.LoopOffset = Math.Round(ESUtils.CalculusLoopOffset.RuleOfThreeLoopOffset(masterWaveReader.WaveFormat.SampleRate, platformWaveReader.WaveFormat.SampleRate, loopInfo(1) * 2))
                                    End If
                                Else
                                    MsgBox("Output Error: Sample File Missing: UNKNOWN SFX & BANK" & vbCrLf & dspFilePath, vbOKOnly + vbCritical, "EuroSound")
                                    CancelSoundBankOutput = True
                                End If
                            ElseIf StrComp(outputPlatform, "X Box") = 0 Or StrComp(outputPlatform, "Xbox") = 0 Then
                                Dim xboxFilePath As String = Path.ChangeExtension(fso.BuildPath(WorkingDirectory & "\XBox_adpcm", relativeSampleFilePath), ".adpcm")
                                If fso.FileExists(xboxFilePath) Then
                                    newAudioObj.SampleData = File.ReadAllBytes(xboxFilePath)
                                    'Get Real length
                                    newAudioObj.RealSize = newAudioObj.SampleData.Length
                                    'Loop offset
                                    If loopInfo(0) = 1 Then
                                        newAudioObj.LoopOffset = ESUtils.CalculusLoopOffset.GetXboxAlignedNumber(loopInfo(1))
                                    End If
                                Else
                                    MsgBox("Output Error: Sample File Missing: UNKNOWN SFX & BANK" & vbCrLf & xboxFilePath, vbOKOnly + vbCritical, "EuroSound")
                                    CancelSoundBankOutput = True
                                End If
                            End If
                        End Using
                    Else
                        MsgBox("Output Error: Sample File Missing: UNKNOWN SFX & BANK" & vbCrLf & platformWave, vbOKOnly + vbCritical, "EuroSound")
                        CancelSoundBankOutput = True
                    End If
                End Using
            End If
            Return newAudioObj
        End Function

        Friend Function GetSfxFlags(sfxFileToCheck As SfxFile) As Short
            'Get Flags
            Dim selectedFlags As Short = 0
            'maxReject
            If sfxFileToCheck.Parameters.Action1 = 1 Then
                selectedFlags = selectedFlags Or (1 << 0)
            End If
            'ignoreAge
            If sfxFileToCheck.Parameters.IgnoreAge Then
                selectedFlags = selectedFlags Or (1 << 2)
            End If
            'multiSample
            If sfxFileToCheck.SamplePool.Action1 = 1 Then
                selectedFlags = selectedFlags Or (1 << 3)
            End If
            'randomPick
            If sfxFileToCheck.SamplePool.RandomPick Then
                selectedFlags = selectedFlags Or (1 << 4)
            End If
            'shuffled
            If sfxFileToCheck.SamplePool.Shuffled Then
                selectedFlags = selectedFlags Or (1 << 5)
            End If
            'loop
            If sfxFileToCheck.SamplePool.isLooped Then
                selectedFlags = selectedFlags Or (1 << 6)
            End If
            'polyphonic
            If sfxFileToCheck.SamplePool.Polyphonic Then
                selectedFlags = selectedFlags Or (1 << 7)
            End If
            'underWater
            If sfxFileToCheck.Parameters.Outdoors Then
                selectedFlags = selectedFlags Or (1 << 8)
            End If
            'pauseInNis
            If sfxFileToCheck.Parameters.PauseInNis Then
                selectedFlags = selectedFlags Or (1 << 9)
            End If
            'hasSubSfx
            If sfxFileToCheck.SamplePool.EnableSubSFX Then
                selectedFlags = selectedFlags Or (1 << 10)
            End If
            'stealOnLouder
            If sfxFileToCheck.Parameters.StealOnAge Then
                selectedFlags = selectedFlags Or (1 << 11)
            End If
            'treatLikeMusic
            If sfxFileToCheck.Parameters.MusicType Then
                selectedFlags = selectedFlags Or (1 << 12)
            End If
            Return selectedFlags
        End Function

        Friend Function RelativePathToAbs(sampleRelPath As String) As String
            'Ensure that the string starts with "\"
            Dim relativeSampleFilePath As String = sampleRelPath
            Dim absrelativeSampleFilePath As String = relativeSampleFilePath
            If Not relativeSampleFilePath.StartsWith("\") Then
                absrelativeSampleFilePath = "\" & relativeSampleFilePath
            End If
            Return absrelativeSampleFilePath
        End Function

        Friend Function GetTotalCents(sfxFileToCheck As EXSound, outputPlatform As String) As Short
            Dim totalCents As Short = 0
            For sampleIndex As Integer = 0 To sfxFileToCheck.Samples.Count - 1
                Dim currentSample As EXSample = sfxFileToCheck.Samples(sampleIndex)
                Dim sampleFilePath As String = fso.BuildPath(WorkingDirectory & "\" & outputPlatform, currentSample.FilePath)
                If fso.FileExists(sampleFilePath) Then
                    Using reader As New WaveFileReader(sampleFilePath)
                        Dim cents = reader.TotalTime.TotalMilliseconds / 10
                        totalCents += cents
                    End Using
                End If
            Next
            Return totalCents
        End Function


        Friend Sub WriteSfxFile(binWriter As BinaryWriter, hashCodesList As SortedDictionary(Of String, UInteger), sfxDictionary As SortedDictionary(Of String, EXSound), samplesDictionary As Dictionary(Of String, EXAudio), streamsList As String(), isBigEndian As Boolean)
            binWriter.Write(ESUtils.BytesFunctions.FlipUInt32(sfxDictionary.Count, isBigEndian))
            Dim sfxStartOffsets As New Queue(Of UInteger)
            Dim samplesList As String() = samplesDictionary.Keys.ToArray
            'SFX header
            For Each sfxToWrite As EXSound In sfxDictionary.Values
                binWriter.Write(ESUtils.BytesFunctions.FlipUInt32(sfxToWrite.HashCode, isBigEndian))
                binWriter.Write(ESUtils.BytesFunctions.FlipUInt32(0, isBigEndian))
            Next
            'SFX parameter entry 
            Dim StreamFileRefCheckSum As Integer = 0
            For Each sfxToWrite As EXSound In sfxDictionary.Values
                sfxStartOffsets.Enqueue(binWriter.BaseStream.Position)
                binWriter.Write(ESUtils.BytesFunctions.FlipShort(sfxToWrite.DuckerLength, isBigEndian))
                binWriter.Write(ESUtils.BytesFunctions.FlipShort(sfxToWrite.MinDelay, isBigEndian))
                binWriter.Write(ESUtils.BytesFunctions.FlipShort(sfxToWrite.MaxDelay, isBigEndian))
                binWriter.Write(ESUtils.BytesFunctions.FlipShort(sfxToWrite.InnerRadius, isBigEndian))
                binWriter.Write(ESUtils.BytesFunctions.FlipShort(sfxToWrite.OuterRadius, isBigEndian))
                binWriter.Write(sfxToWrite.ReverbSend)
                binWriter.Write(sfxToWrite.TrackingType)
                binWriter.Write(sfxToWrite.MaxVoices)
                binWriter.Write(sfxToWrite.Priority)
                binWriter.Write(sfxToWrite.Ducker)
                binWriter.Write(sfxToWrite.MasterVolume)
                binWriter.Write(sfxToWrite.Flags)
                binWriter.Write(ESUtils.BytesFunctions.FlipUShort(sfxToWrite.Samples.Count, isBigEndian))
                For sampleIndex As Integer = 0 To sfxToWrite.Samples.Count - 1
                    Dim currentSample As EXSample = sfxToWrite.Samples(sampleIndex)
                    'Find File Ref
                    Dim fileRef As Short
                    If sfxToWrite.HasSubSfx Then
                        If hashCodesList IsNot Nothing Then
                            fileRef = hashCodesList(GetOnlyFileName(currentSample.FilePath.TrimStart("\"c)))
                        Else
                            fileRef = 0
                        End If
                    Else
                        Dim streamFileIndex As Integer = Array.IndexOf(streamsList, currentSample.FilePath)
                        If streamFileIndex = -1 Then
                            fileRef = Array.IndexOf(samplesList, currentSample.FilePath)
                        Else
                            fileRef = (streamFileIndex + 1) * -1
                            PrintLine(1, fileRef & "    " & currentSample.FilePath)
                            StreamFileRefCheckSum += Math.Abs(fileRef)
                        End If
                    End If
                    'Write values
                    binWriter.Write(ESUtils.BytesFunctions.FlipShort(fileRef, isBigEndian))
                    binWriter.Write(ESUtils.BytesFunctions.FlipShort(currentSample.PitchOffset, isBigEndian))
                    binWriter.Write(ESUtils.BytesFunctions.FlipShort(currentSample.RandomPitchOffset, isBigEndian))
                    binWriter.Write(currentSample.BaseVolume)
                    binWriter.Write(currentSample.RandomVolumeOffset)
                    binWriter.Write(currentSample.Pan)
                    binWriter.Write(currentSample.RandomPan)
                    binWriter.Write(CByte(0))
                    binWriter.Write(CByte(0))
                Next
            Next
            'Close debug file
            PrintLine(1, "StreamFileRefCheckSum = " & (StreamFileRefCheckSum * -1))
            FileClose(1)
            'Write start offsets
            binWriter.BaseStream.Seek(8, SeekOrigin.Begin)
            For index As Integer = 0 To sfxStartOffsets.Count - 1
                binWriter.Write(ESUtils.BytesFunctions.FlipUInt32(sfxStartOffsets.Dequeue, isBigEndian))
                binWriter.BaseStream.Seek(4, SeekOrigin.Current)
            Next
        End Sub

        Friend Sub WriteSifFile(binWriter As BinaryWriter, SamplesDictionary As Dictionary(Of String, EXAudio), isBigEndian As Boolean)
            binWriter.Write(ESUtils.BytesFunctions.FlipUInt32(SamplesDictionary.Count, isBigEndian))
            Dim dspHeaderIndex As Integer = 0
            For Each soundToWrite As EXAudio In SamplesDictionary.Values
                binWriter.Write(ESUtils.BytesFunctions.FlipInt32(soundToWrite.Flags, isBigEndian))
                binWriter.Write(ESUtils.BytesFunctions.FlipUInt32(soundToWrite.Address, isBigEndian))
                binWriter.Write(ESUtils.BytesFunctions.FlipUInt32(soundToWrite.SampleData.Length, isBigEndian))
                binWriter.Write(ESUtils.BytesFunctions.FlipUInt32(soundToWrite.Frequency, isBigEndian))
                binWriter.Write(ESUtils.BytesFunctions.FlipUInt32(soundToWrite.RealSize, isBigEndian))
                binWriter.Write(ESUtils.BytesFunctions.FlipUInt32(soundToWrite.NumberOfChannels, isBigEndian))
                binWriter.Write(ESUtils.BytesFunctions.FlipUInt32(soundToWrite.Bits, isBigEndian))
                binWriter.Write(ESUtils.BytesFunctions.FlipUInt32(dspHeaderIndex * 96, isBigEndian))
                binWriter.Write(ESUtils.BytesFunctions.FlipUInt32(soundToWrite.LoopOffset, isBigEndian))
                binWriter.Write(soundToWrite.Duration)
                dspHeaderIndex += 1
            Next
        End Sub

        Friend Sub WriteSsfFile(binWriter As BinaryWriter, SamplesDictionary As Dictionary(Of String, EXAudio))
            For Each soundToWrite As EXAudio In SamplesDictionary.Values
                binWriter.Write(soundToWrite.DspHeaderData)
            Next
        End Sub

        Friend Sub WriteSbfFile(binWriter As BinaryWriter, SamplesDictionary As Dictionary(Of String, EXAudio))
            For Each soundToWrite As EXAudio In SamplesDictionary.Values
                soundToWrite.Address = binWriter.BaseStream.Position
                binWriter.Write(soundToWrite.SampleData)
            Next
        End Sub
    End Module
End Namespace
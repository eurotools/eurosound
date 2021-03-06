Imports System.Globalization
Imports System.IO
Imports System.Text.RegularExpressions
Imports ESUtils.BytesFunctions
Imports IniFileFunctions

Friend Module GenericFunctions
    '*===============================================================================================
    '* FORMAT INFO
    '*===============================================================================================
    Friend ReadOnly numericProvider As New NumberFormatInfo With {
        .NumberDecimalSeparator = "."
    }

    '*===============================================================================================
    '* TOOLS FUNCTIONS
    '*===============================================================================================
    Friend Sub RunConsoleProcess(toolFileName As String, toolArguments As String, Optional ShowWindow As Boolean = False)
        Dim processToExecute As New Process
        processToExecute.StartInfo.FileName = toolFileName
        processToExecute.StartInfo.Arguments = toolArguments
        If ShowWindow Then
            processToExecute.StartInfo.WindowStyle = ProcessWindowStyle.Normal
            processToExecute.StartInfo.CreateNoWindow = False
        Else
            processToExecute.StartInfo.WindowStyle = ProcessWindowStyle.Hidden
            processToExecute.StartInfo.CreateNoWindow = True
        End If
        processToExecute.Start()
        processToExecute.WaitForExit()
    End Sub

    '*===============================================================================================
    '* INPUT DATA
    '*===============================================================================================
    Friend Function AskForUserName(defaultName As String) As String
        'Ask user for a new username
        Dim inputUserName As String = defaultName
        Do
            inputUserName = InputBox("Please Enter Your UserName", "Enter UserName.", inputUserName)
        Loop While inputUserName = ""

        'Put new username
        EuroSoundUser = inputUserName

        'Modify EuroSound Ini
        Dim programIni As New IniFile(EuroSoundIniFilePath)
        programIni.Write("UserName", EuroSoundUser, "Form1_Misc")
        Return EuroSoundUser
    End Function

    '*===============================================================================================
    '* OTHER FUNCTIONS
    '*===============================================================================================
    Friend Sub EditWaveFile(waveFilePath As String)
        'Open tool if files exists
        If File.Exists(waveFilePath) AndAlso File.Exists(ProjAudioEditor) Then
            Try
                Dim procInfo As New ProcessStartInfo With {
                    .FileName = """" & ProjAudioEditor & """",
                    .Arguments = """" & waveFilePath & """"
                }
                Process.Start(procInfo)
            Catch ex As Exception
                MsgBox(ex.Message, vbOKOnly + vbCritical, "EuroSound")
            End Try
        End If
    End Sub

    Friend Function MultipleDeletionMessage(messageToShow As String, itemsToDelete As String()) As String
        Dim maxItemsToShow As Byte = 33
        'Create message to inform user
        Dim filesListToDelete As String = messageToShow & vbNewLine & vbNewLine
        Dim numItems As Integer = Math.Min(maxItemsToShow, itemsToDelete.Length)
        For index As Integer = 0 To numItems - 1
            filesListToDelete += "'" & itemsToDelete(index) & "'" & vbNewLine
        Next
        If itemsToDelete.Count > maxItemsToShow Then
            filesListToDelete += "Plus Some More ....." & vbNewLine
            filesListToDelete += "............" & vbNewLine
        End If
        filesListToDelete += vbNewLine & "Total Files: " & itemsToDelete.Length
        Return filesListToDelete
    End Function

    Friend Sub RestartEuroSound()
        'Restart application
        Process.Start(Application.ExecutablePath)
        Application.Exit()
    End Sub

    Friend Function GetNextAvailableFileName(foldeToCheckIn As String, FileNamePattern As String) As String
        Dim fileNumber As Integer = 0
        While File.Exists(Path.Combine(foldeToCheckIn, FileNamePattern & fileNumber & ".txt"))
            fileNumber += 1
        End While
        Return FileNamePattern & fileNumber
    End Function

    '*===============================================================================================
    '* FILES FUNCTIONS
    '*===============================================================================================
    Friend Function CountFolderFiles(Folder As String, Filter As String) As Integer
        Dim CountFilesDir As Integer = 0
        Dim sFile As String = Dir(Folder & "\" & Filter)
        Do While Len(sFile) > 0
            CountFilesDir += 1
            sFile = Dir()
        Loop
        Return CountFilesDir
    End Function

    Friend Function BytesStringFormat(BytesCaller As Long) As String
        Return FormatBytes(BytesCaller)
    End Function

    Friend Function RenameFile(defaultResponse As String, objectType As String, objectFolder As String) As String
        While True
            Dim inputName As String = InputBox("Enter New Name For " & objectType & " " & defaultResponse, "Rename " & objectType, defaultResponse).Trim
            Dim match As Match = Regex.Match(inputName, namesFormat)
            If (match.Success) Then
                Dim inputFilePath As String = Path.Combine(objectFolder, inputName & ".txt")
                If defaultResponse.Equals(inputName) Then
                    Return ""
                ElseIf File.Exists(inputFilePath) Then
                    MsgBox(objectType & " Label '" & inputName & "' already exists please use another name!", vbOKOnly + vbCritical, "Duplicate " & objectType & " Name")
                Else
                    Return inputName
                End If
            Else
                MsgBox(objectType & " Label '" & inputName & "' uses invalid characters, only numbers, digits and underscore characters are allowed.", vbOKOnly + vbCritical, "EuroSound")
            End If
        End While
        Return ""
    End Function

    Friend Function CopyFile(defaultResponse As String, objectType As String, objectFolder As String) As String
        While True
            Dim inputName As String = InputBox("Enter New Name For " & objectType & " " & defaultResponse, "Copy " & objectType, defaultResponse).Trim
            Dim match As Match = Regex.Match(inputName, namesFormat)
            If (match.Success) Then
                Dim inputFilePath As String = Path.Combine(objectFolder, inputName & ".txt")
                If File.Exists(inputFilePath) Then
                    MsgBox(objectType & " Label '" & inputName & "' already exists please use another name!", vbOKOnly + vbCritical, "Duplicate " & objectType & " Name")
                Else
                    Return inputName
                End If
            Else
                MsgBox(objectType & " Label '" & inputName & "' uses invalid characters, only numbers, digits and underscore characters are allowed.", vbOKOnly + vbCritical, "EuroSound")
            End If
        End While
        Return ""
    End Function

    Friend Function NewFile(objectName As String, objectFolder As String) As String
        While True
            Dim inputName As String = InputBox("Enter Name", "Create New", objectName).Trim
            Dim match As Match = Regex.Match(inputName, namesFormat)
            If (match.Success) Then
                Dim inputFilePath As String = Path.Combine(objectFolder, inputName & ".txt")
                If File.Exists(inputFilePath) Then
                    MsgBox("Label '" & inputName & "' already exists please use another name!", vbOKOnly + vbCritical, "Duplicate Name")
                Else
                    Return inputName
                End If
            Else
                MsgBox("Label '" & inputName & "' uses invalid characters, only numbers, digits and underscore characters are allowed.", vbOKOnly + vbCritical, "EuroSound")
            End If
        End While
        Return ""
    End Function

    '*===============================================================================================
    '* INI FILE FUNCTIONS
    '*===============================================================================================
    Friend Function GetDefaultSampleValues() As Double()
        Dim sampleInfo As Double() = New Double() {0, 0, 0, 0, 0, 0}
        Dim iniFunctions As New IniFile(SysFileProjectIniPath)
        Dim IniPitchOffset As String = iniFunctions.Read("DTextNIndex_0", "SFXForm")
        Dim IniRandomPitch As String = iniFunctions.Read("DTextNIndex_1", "SFXForm")
        Dim IniBaseVolume As String = iniFunctions.Read("DTextNIndex_2", "SFXForm")
        Dim IniRandomVol As String = iniFunctions.Read("DTextNIndex_3", "SFXForm")
        Dim IniPan As String = iniFunctions.Read("DTextNIndex_4", "SFXForm")
        Dim IniRandomPan As String = iniFunctions.Read("DTextNIndex_5", "SFXForm")

        'Pitch Offset
        If IsNumeric(IniPitchOffset) Then
            sampleInfo(0) = Convert.ToDouble(IniPitchOffset, numericProvider)
        End If
        'Random Pitch
        If IsNumeric(IniRandomPitch) Then
            sampleInfo(1) = Convert.ToDouble(IniRandomPitch, numericProvider)
        End If
        'Base Volume
        If IsNumeric(IniBaseVolume) Then
            sampleInfo(2) = CInt(IniBaseVolume)
        End If
        'Random Volume Offset
        If IsNumeric(IniRandomVol) Then
            sampleInfo(3) = CInt(IniRandomVol)
        End If
        'Pan
        If IsNumeric(IniPan) Then
            sampleInfo(4) = CInt(IniPan)
        End If
        'Random Pan
        If IsNumeric(IniRandomPan) Then
            sampleInfo(5) = CInt(IniRandomPan)
        End If

        Return sampleInfo
    End Function

    '*===============================================================================================
    '* STRINGS FUNCTIONS
    '*===============================================================================================
    Friend Function GetEngineXFolder(outputPlatform As String) As String
        Dim FolderName As String = ""
        Select Case outputPlatform
            Case "PC"
                FolderName = "_bin_PC"
            Case "PlayStation2"
                FolderName = "_bin_PS2"
            Case "GameCube"
                FolderName = "_bin_GC"
            Case "X Box"
                FolderName = "_bin_XB"
            Case "Xbox"
                FolderName = "_bin_XB"
        End Select

        GetEngineXFolder = FolderName
    End Function

    Friend Function GetSfxFileName(language As Integer, fileHashCode As Integer) As Integer
        Return ((language And &HF) << 16) Or ((fileHashCode And &HFFFF) << 0)
    End Function

    Friend Function GetEngineXLangFolder(outputLanguage As String) As String
        GetEngineXLangFolder = "_" & Left(outputLanguage, 3)
    End Function

    '*===============================================================================================
    '* DIRECTORIES FUNCTIONS
    '*===============================================================================================
    Friend Sub CopyDirectory(sourceDir As String, destinationDir As String, recursive As Boolean)
        'Get information about the source directory
        Dim dir As New DirectoryInfo(sourceDir)

        'Check if the source directory exists
        If Not dir.Exists Then
            Throw New DirectoryNotFoundException($"Source directory not found: {dir.FullName}")
        End If

        'Cache directories before we start copying
        Dim dirs As DirectoryInfo() = dir.GetDirectories

        'Create the destination directory
        Directory.CreateDirectory(destinationDir)

        'Get the files in the source directory and copy to the destination directory
        'For Each file As FileInfo In dir.GetFiles
        '    Dim targetFilePath = Path.Combine(destinationDir, file.Name)
        '    file.CopyTo(targetFilePath)
        'Next

        'If recursive and copying subdirectories, recursively call this method
        If recursive Then
            For Each subDir As DirectoryInfo In dirs
                Dim newDestinationDir As String = Path.Combine(destinationDir, subDir.Name)
                CopyDirectory(subDir.FullName, newDestinationDir, True)
            Next
        End If
    End Sub

    '*===============================================================================================
    '* HASHCODES FUNCTIONS
    '*===============================================================================================
    Friend Function GetHashCodesDict(sfxFilesPath As String) As SortedDictionary(Of String, UInteger)
        Dim hashCodesDictionary As New SortedDictionary(Of String, UInteger)
        Dim sfxFiles As String() = Directory.GetFiles(sfxFilesPath, "*.txt", SearchOption.TopDirectoryOnly)

        For fileIndex As Integer = 0 To sfxFiles.Length - 1
            Dim currentFilePath As String = sfxFiles(fileIndex)
            Dim sfxLabel As String = Path.GetFileNameWithoutExtension(currentFilePath)
            If Not hashCodesDictionary.ContainsKey(sfxLabel) Then
                'Get HashCode
                Dim sfxFileData As String() = File.ReadAllLines(currentFilePath)
                Dim hashcodeIndex As Integer = Array.FindIndex(sfxFileData, Function(t) t.Equals("#HASHCODE", StringComparison.OrdinalIgnoreCase))
                Dim stringData As String() = sfxFileData(hashcodeIndex + 1).Split(" "c)
                If stringData.Length > 1 AndAlso IsNumeric(stringData(1)) Then
                    hashCodesDictionary.Add(sfxLabel, stringData(1))
                End If
            End If
        Next

        Return hashCodesDictionary
    End Function

    Friend Function GetSoundBanksDict(soundBanksFilePath As String) As SortedDictionary(Of String, UInteger)
        Dim soundBanksDictionary As New SortedDictionary(Of String, UInteger)
        Dim soundbankFiles As String() = Directory.GetFiles(soundBanksFilePath, "*.txt", SearchOption.TopDirectoryOnly)

        For fileIndex As Integer = 0 To soundbankFiles.Length - 1
            Dim currentFilePath As String = soundbankFiles(fileIndex)
            Dim soundBankLabel As String = Path.GetFileNameWithoutExtension(currentFilePath)
            If Not soundBanksDictionary.ContainsKey(soundBankLabel) Then
                'Get HashCode
                Dim soundBankFileData As String() = File.ReadAllLines(currentFilePath)
                Dim hashcodeIndex As Integer = Array.FindIndex(soundBankFileData, Function(t) t.Equals("#HASHCODE", StringComparison.OrdinalIgnoreCase))
                Dim stringData As String() = soundBankFileData(hashcodeIndex + 1).Split(" "c)
                If stringData.Length > 1 AndAlso IsNumeric(stringData(1)) Then
                    soundBanksDictionary.Add(soundBankLabel, stringData(1))
                End If
            End If
        Next

        Return soundBanksDictionary
    End Function

    Friend Function GetReverbsDict(reverbsFilePath As String) As SortedDictionary(Of String, UInteger)
        Dim reverbHashcodesDictionary As New SortedDictionary(Of String, UInteger)
        Dim soundbankFiles As String() = Directory.GetFiles(reverbsFilePath, "*.txt", SearchOption.TopDirectoryOnly)

        For fileIndex As Integer = 0 To soundbankFiles.Length - 1
            Dim currentFilePath As String = soundbankFiles(fileIndex)
            Dim soundBankLabel As String = Path.GetFileNameWithoutExtension(currentFilePath)
            If Not reverbHashcodesDictionary.ContainsKey(soundBankLabel) Then
                'Get HashCode
                Dim reverbFilePath As String() = File.ReadAllLines(currentFilePath)
                Dim hashcodeIndex As Integer = Array.FindIndex(reverbFilePath, Function(t) t.Equals("#MiscData", StringComparison.OrdinalIgnoreCase))
                Dim stringData As String() = reverbFilePath(hashcodeIndex + 1).Split(New Char() {" "c}, StringSplitOptions.RemoveEmptyEntries)
                If stringData.Length > 1 AndAlso IsNumeric(stringData(1)) Then
                    reverbHashcodesDictionary.Add(soundBankLabel, stringData(1))
                End If
            End If
        Next

        Return reverbHashcodesDictionary
    End Function
End Module

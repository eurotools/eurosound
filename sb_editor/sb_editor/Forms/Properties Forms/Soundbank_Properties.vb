Imports System.IO
Imports NAudio.Wave
Imports sb_editor.ParsersObjects
Imports sb_editor.ReaderClasses
Imports sb_editor.SoundBanksExporterFunctions

Public Class Soundbank_Properties
    '*===============================================================================================
    '* GLOBAL VARIABLES
    '*===============================================================================================
    Private ReadOnly SoundbankFilePath As String
    Private ReadOnly OutputPlatform As String
    Private ReadOnly OutputLanguage As String
    Private ReadOnly textFileReaders As New FileParsers()

    Public Sub New(sbFilePath As String, outPlatform As String, outLanguage As String)
        ' Esta llamada es exigida por el diseñador.
        InitializeComponent()

        ' Agregue cualquier inicialización después de la llamada a InitializeComponent().
        SoundbankFilePath = sbFilePath
        OutputPlatform = outPlatform
        OutputLanguage = outLanguage
    End Sub

    '*===============================================================================================
    '* FORM EVENTS
    '*===============================================================================================
    Private Sub Soundbank_Properties_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        'Set cursor as hourglass
        Cursor.Current = Cursors.WaitCursor

        'Get SoundBank info
        Dim availablePlatforms As String() = ProjectSettingsFile.sampleRateFormats.Keys.ToArray
        Dim soundBankData As SoundbankFile = textFileReaders.ReadSoundBankFile(SoundbankFilePath)
        Dim soundBankSfxList As String() = GetSoundBankSFXsList(soundBankData, OutputPlatform)
        Dim soundBankSamplesList As String() = GetSoundBankSamplesList(soundBankSfxList, OutputLanguage)

        'Put soundbank name
        GroupBox_SoundbankData.Text = Path.GetFileNameWithoutExtension(SoundbankFilePath)

        'Calculate Output Filename
        Label_Value_OutFileName.Text = "HC" & Hex(soundBankData.HashCode).PadLeft(6, "0"c) & ".SFX"

        'Show SoundBank Data
        Label_Value_FirstCreated.Text = soundBankData.HeaderInfo.FirstCreated
        Label_CreatedBy_Value.Text = soundBankData.HeaderInfo.CreatedBy
        Label_Value_LastModified.Text = soundBankData.HeaderInfo.LastModify
        Label_ModifiedBy_Value.Text = soundBankData.HeaderInfo.LastModifyBy
        Label_DatabaseCount_Value.Text = soundBankData.Dependencies.Length
        Label_SfxCount_Value.Text = soundBankSfxList.Length
        Label_SampleCount_Value.Text = soundBankSamplesList.Length

        'Add Soundbank data to listboxes and update counters
        If soundBankData.Dependencies.Count > 0 Then
            ListBox_Databases.Items.AddRange(soundBankData.Dependencies)
            Label_DataBasesCount.Text = "DataBases: " & ListBox_Databases.Items.Count
        End If

        If soundBankSfxList.Length > 0 Then
            ListBox_SFXs.Items.AddRange(soundBankSfxList)
            Label_SfxCount.Text = "SFXs: " & soundBankSfxList.Length
        End If

        If soundBankSamplesList.Length > 0 Then
            ListBox_SamplesList.Items.AddRange(soundBankSamplesList)
            Label_TotalSamples.Text = "Samples: " & soundBankSamplesList.Length
        End If

        'Create a new variable to store the total sample size
        Dim totalSampleSize As Integer = 0

        'Get streamed samples 
        Dim soundBankFormatSizes As New List(Of String)
        If File.Exists(SysFileSamples) Then
            Dim samplesTable As DataTable = textFileReaders.SamplesFileToDatatable(SysFileSamples)
            Dim streamSamplesList As String() = textFileReaders.GetAllStreamSamples(samplesTable)
            For formatIndex As Integer = 0 To availablePlatforms.Length - 1
                Dim currentFormat As String = availablePlatforms(formatIndex)
                Dim soundBankSize As String
                Dim formatSamplesList As String() = GetFinalList(soundBankSamplesList, streamSamplesList, currentFormat)
                'Get SoundBank Size
                Dim formatSamplesFolder As String = Path.Combine(WorkingDirectory, "TempOutputFolder", currentFormat, "SoundBanks", OutputLanguage, soundBankData.HashCode & ".sbf")
                If File.Exists(formatSamplesFolder) Then
                    totalSampleSize += GetTotalSampleSize(formatSamplesList)
                    soundBankSize = BytesStringFormat(FileLen(formatSamplesFolder))
                Else
                    If Directory.Exists(Path.Combine(ProjectSettingsFile.MiscProps.SampleFileFolder, "Master")) Then
                        totalSampleSize += GetTotalSampleSize(formatSamplesList)
                        soundBankSize = BytesStringFormat(GetEstimatedPlatformSize(formatSamplesList, currentFormat, samplesTable)) & " - ESTIMATED"
                    Else
                        totalSampleSize += formatSamplesList.Length
                        soundBankSize = BytesStringFormat(GetEstimatedPlatformSizeNoMaster(formatSamplesList, currentFormat)) & " - ESTIMATED"
                    End If
                End If
                'Add format to dictionary
                soundBankFormatSizes.Add(currentFormat & ";" & soundBankSize)
            Next
        Else
            For formatIndex As Integer = 0 To availablePlatforms.Length - 1
                'Add format to dictionary
                Dim currentFormat As String = availablePlatforms(formatIndex)
                soundBankFormatSizes.Add(currentFormat & ";" & BytesStringFormat(0) & " - ESTIMATED")
            Next
        End If

        'Print values
        For itemIndex As Integer = 0 To soundBankFormatSizes.Count - 1
            Dim labelFormatName As Label = GroupBox_SoundbankData.Controls.Find("Label_Format" & (itemIndex + 1) & "Name", False)(0)
            Dim labelFormatValue As Label = GroupBox_SoundbankData.Controls.Find("Label_Format" & (itemIndex + 1) & "Value", False)(0)
            Dim dataToPrint As String() = Split(soundBankFormatSizes.Item(itemIndex), ";")
            labelFormatName.Text = dataToPrint(0)
            labelFormatName.Visible = True
            labelFormatValue.Text = dataToPrint(1)
            labelFormatValue.Visible = True
        Next

        'Total Sample Size
        Label_Value_Size.Text = BytesStringFormat(totalSampleSize)

        'Set cursor as default arrow
        Cursor.Current = Cursors.Default
    End Sub

    Private Sub Soundbank_Properties_Shown(sender As Object, e As EventArgs) Handles MyBase.Shown
        If ActiveForm IsNot Me Then
            FlashWindowAPI(Handle)
        End If
    End Sub

    '*===============================================================================================
    '* FORM BUTTONS
    '*===============================================================================================
    Private Sub Button_OK_Click(sender As Object, e As EventArgs) Handles Button_OK.Click
        'Close Form
        Close()
    End Sub

    '*===============================================================================================
    '* FUNCTIONS TO CALCULATE ESTIMATED SIZE - MASTER FOLDER
    '*===============================================================================================
    Friend Function GetEstimatedPlatformSize(samplesList As String(), outputPlatform As String, samplesTable As DataTable) As Integer
        Dim platformSize As Integer = 0
        For fileIndex As Integer = 0 To samplesList.Length - 1
            Dim sampleFilePath As String = samplesList(fileIndex)
            'Get wave length
            Dim masterWaveSize As Integer
            Dim masterWaveFrequency As Integer
            Using waveReader As New WaveFileReader(sampleFilePath)
                masterWaveSize = waveReader.Length
                masterWaveFrequency = waveReader.WaveFormat.SampleRate
            End Using
            'Get the ReSampled wave size
            For rowIndex As Integer = 0 To samplesTable.Rows.Count - 1
                Dim currentRow As String = samplesTable.Rows(rowIndex).ItemArray(0)
                If InStr(1, sampleFilePath, UCase(currentRow), CompareMethod.Binary) Then
                    Dim frequencyLabel As String = samplesTable.Rows(rowIndex).ItemArray(1)
                    Dim platformFrequency As Integer = ProjectSettingsFile.sampleRateFormats(outputPlatform)(frequencyLabel)
                    Dim compressionFactor As Decimal = Decimal.Divide(masterWaveFrequency, platformFrequency)
                    Select Case outputPlatform
                        Case "PC"
                            platformSize += Decimal.Divide(masterWaveSize, compressionFactor)
                        Case "GameCube"
                            Dim resampledWaveSize As Decimal = Decimal.Divide(masterWaveSize, compressionFactor)
                            Dim gameCubeDSPSize As Decimal = Decimal.Divide(resampledWaveSize, 3.46)
                            platformSize += gameCubeDSPSize
                        Case "PlayStation2"
                            Dim resampledWaveSize As Decimal = Decimal.Divide(masterWaveSize, compressionFactor)
                            Dim sonyVagSize As Decimal = Decimal.Divide(resampledWaveSize, 3.45)
                            platformSize += sonyVagSize
                        Case Else
                            Dim resampledWaveSize As Decimal = Decimal.Divide(masterWaveSize, compressionFactor)
                            Dim xboxAdpcm As Decimal = Decimal.Divide(resampledWaveSize, 3.54)
                            platformSize += xboxAdpcm
                    End Select
                    Exit For
                End If
            Next
        Next
        Return platformSize
    End Function

    Friend Function GetTotalSampleSize(samplesList As String()) As Integer
        Dim sampleSize As Integer = 0
        For sampleIndex As Integer = 0 To samplesList.Length - 1
            Dim filePath As String = samplesList(sampleIndex)
            'Get wave length
            Using waveReader As New WaveFileReader(filePath)
                sampleSize += waveReader.Length
            End Using
        Next
        Return sampleSize
    End Function
End Class
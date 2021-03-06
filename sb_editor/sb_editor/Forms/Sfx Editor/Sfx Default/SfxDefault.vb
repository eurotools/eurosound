Imports System.IO
Imports IniFileFunctions
Imports sb_editor.ParsersObjects
Imports sb_editor.ReaderClasses

Public Class SfxDefault
    '*===============================================================================================
    '* Global Variables
    '*===============================================================================================
    Private ReadOnly textFileReaders As New FileParsers()
    Private promptSave As Boolean = True

    '*===============================================================================================
    '* FORM EVENTS
    '*===============================================================================================
    Private Sub Frm_SfxDefault_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        'Read defaults file 
        If File.Exists(SysFileSfxDefaults) Then
            'Read SFX File
            Dim objSFX As SfxFile = textFileReaders.ReadSFXFile(SysFileSfxDefaults)
            'Set properties
            SfxParamsAndSamplePool1.Textbox_SfxName.Text = "SFX Default Setting"

            PutDataToControls(objSFX)
        End If

        'Try to read the ini file
        If SysFileProjectIniPath IsNot "" Then
            Dim iniFunctions As New IniFile(SysFileProjectIniPath)
            Dim IniPitchOffset As String = iniFunctions.Read("DTextNIndex_0", "SFXForm")
            Dim IniRandomPitch As String = iniFunctions.Read("DTextNIndex_1", "SFXForm")
            Dim IniBaseVolume As String = iniFunctions.Read("DTextNIndex_2", "SFXForm")
            Dim IniRandomVol As String = iniFunctions.Read("DTextNIndex_3", "SFXForm")
            Dim IniPan As String = iniFunctions.Read("DTextNIndex_4", "SFXForm")
            Dim IniRandomPan As String = iniFunctions.Read("DTextNIndex_5", "SFXForm")
            'Pitch Offset
            If IsNumeric(IniPitchOffset) Then
                SfxParamsAndSamplePool1.Numeric_PitchOffset.Value = Convert.ToDouble(IniPitchOffset, numericProvider)
            End If
            'Random Pitch
            If IsNumeric(IniRandomPitch) Then
                SfxParamsAndSamplePool1.Numeric_RandomPitch.Value = Convert.ToDouble(IniRandomPitch, numericProvider)
            End If
            'Base Volume
            If IsNumeric(IniBaseVolume) Then
                SfxParamsAndSamplePool1.Numeric_BaseVolume.Value = CInt(IniBaseVolume)
            End If
            'Random Volume Offset
            If IsNumeric(IniRandomVol) Then
                SfxParamsAndSamplePool1.Numeric_RandomVolume.Value = CInt(IniRandomVol)
            End If
            'Pan
            If IsNumeric(IniPan) Then
                SfxParamsAndSamplePool1.Numeric_Pan.Value = CInt(IniPan)
            End If
            'Random Pan
            If IsNumeric(IniRandomPan) Then
                SfxParamsAndSamplePool1.Numeric_RandomPan.Value = CInt(IniRandomPan)
            End If
        End If
    End Sub

    Private Sub Frm_SfxDefault_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        'Check closing reason
        If e.CloseReason = CloseReason.UserClosing Then
            'Check if we have to show the message
            If promptSave Then
                'Ask user what wants to do
                My.Computer.Audio.PlaySystemSound(Media.SystemSounds.Exclamation)
                Dim save As MsgBoxResult = MsgBox("Are you sure you wish to quit without saving?", vbOKCancel + vbQuestion, "Confirm Quit")
                'Cancel close if user not want to quit
                If save = MsgBoxResult.Cancel Then
                    e.Cancel = True
                End If
            End If
        End If
    End Sub

    '*===============================================================================================
    '* BUTTON EVENTS
    '*===============================================================================================
    Private Sub Button_Cancel_Click(sender As Object, e As EventArgs) Handles Button_Cancel.Click
        'Close form
        Close()
    End Sub

    Private Sub Button_OK_Click(sender As Object, e As EventArgs) Handles Button_OK.Click
        'Disable save file message
        promptSave = False
        'Save file
        SaveFile()
        'Close form
        Close()
    End Sub

    '*===============================================================================================
    '* FUNCTIONS
    '*===============================================================================================
    Private Sub PutDataToControls(objSFX As SfxFile)
        'Parameters
        SfxParamsAndSamplePool1.TrackBar_Reverb.Value = objSFX.Parameters.ReverbSend
        Select Case objSFX.Parameters.TrackingType
            Case 0
                SfxParamsAndSamplePool1.RadioButton_Tracking_2D.Checked = True
            Case 1
                SfxParamsAndSamplePool1.RadioButton_TrackingType_Amb.Checked = True
            Case 2
                SfxParamsAndSamplePool1.RadioButton_TrackingType_3D.Checked = True
            Case 3
                SfxParamsAndSamplePool1.RadioButton_TrackingType_3DRandom.Checked = True
            Case 4
                SfxParamsAndSamplePool1.RadioButton_Tracking_2DPL2.Checked = True
        End Select
        SfxParamsAndSamplePool1.TrackBar_InnerRadius.Value = objSFX.Parameters.InnerRadius
        SfxParamsAndSamplePool1.TrackBar_OuterRadius.Value = objSFX.Parameters.OuterRadius
        SfxParamsAndSamplePool1.Numeric_MaxVoices.Value = objSFX.Parameters.MaxVoices
        Select Case objSFX.Parameters.Action1
            Case 0
                SfxParamsAndSamplePool1.RadioButton_ActionSteal.Checked = True
            Case 1
                SfxParamsAndSamplePool1.RadioButton_ActionReject.Checked = True
        End Select
        SfxParamsAndSamplePool1.Numeric_Priority.Value = objSFX.Parameters.Priority
        SfxParamsAndSamplePool1.Numeric_Alertness.Value = objSFX.Parameters.Alertness
        SfxParamsAndSamplePool1.CheckBox_IgnoreAge.Checked = objSFX.Parameters.IgnoreAge
        SfxParamsAndSamplePool1.Numeric_Ducker.Value = objSFX.Parameters.Ducker
        SfxParamsAndSamplePool1.Numeric_DuckerLength.Value = objSFX.Parameters.DuckerLength
        SfxParamsAndSamplePool1.Numeric_MasterVolume.Value = objSFX.Parameters.MasterVolume
        SfxParamsAndSamplePool1.CheckBox_UnderWater.Checked = objSFX.Parameters.Outdoors
        SfxParamsAndSamplePool1.Checkbox_PauseInNis.Checked = objSFX.Parameters.PauseInNis
        SfxParamsAndSamplePool1.CheckBox_StealOnLouder.Checked = objSFX.Parameters.StealOnAge
        SfxParamsAndSamplePool1.Checkbox_MusicType.Checked = objSFX.Parameters.MusicType
        SfxParamsAndSamplePool1.Checkbox_Doppler.Checked = objSFX.Parameters.Doppler

        'Sample Pool Control
        Select Case objSFX.SamplePool.Action1
            Case 0
                SfxParamsAndSamplePool1.RadioButton_Single.Checked = True
            Case 1
                SfxParamsAndSamplePool1.RadioButton_MultiSample.Checked = True
        End Select
        SfxParamsAndSamplePool1.CheckBox_RandomPick.Checked = objSFX.SamplePool.RandomPick
        SfxParamsAndSamplePool1.CheckBox_Shuffled.Checked = objSFX.SamplePool.Shuffled
        SfxParamsAndSamplePool1.CheckBox_SamplePoolLoop.Checked = objSFX.SamplePool.isLooped
        SfxParamsAndSamplePool1.CheckBox_Polyphonic.Checked = objSFX.SamplePool.Polyphonic
        SfxParamsAndSamplePool1.Numeric_MinDelay.Value = objSFX.SamplePool.MinDelay
        SfxParamsAndSamplePool1.Numeric_MaxDelay.Value = objSFX.SamplePool.MaxDelay
    End Sub

    Private Sub SaveFile()
        'Save data in the Ini File
        If SysFileProjectIniPath IsNot "" Then
            Dim iniFunctions As New IniFile(SysFileProjectIniPath)
            iniFunctions.Write("DTextNIndex_0", SfxParamsAndSamplePool1.Numeric_PitchOffset.Value.ToString(), "SFXForm")
            iniFunctions.Write("DTextNIndex_1", SfxParamsAndSamplePool1.Numeric_RandomPitch.Value.ToString(), "SFXForm")
            iniFunctions.Write("DTextNIndex_2", SfxParamsAndSamplePool1.Numeric_BaseVolume.Value.ToString(), "SFXForm")
            iniFunctions.Write("DTextNIndex_3", SfxParamsAndSamplePool1.Numeric_RandomVolume.Value.ToString(), "SFXForm")
            iniFunctions.Write("DTextNIndex_4", SfxParamsAndSamplePool1.Numeric_Pan.Value.ToString(), "SFXForm")
            iniFunctions.Write("DTextNIndex_5", SfxParamsAndSamplePool1.Numeric_RandomPan.Value.ToString(), "SFXForm")
        End If

        If Directory.Exists(Path.Combine(WorkingDirectory, "System")) Then
            'Get sfx deefault file path
            Dim sfxDefaultsFilePath = Path.Combine(WorkingDirectory, "System", "SFX Defaults.txt")

            'Replace current file   
            Dim headerData As New FileHeader

            'Get creation time if file exists
            Dim created = Date.Now.ToString(filesDateFormat)
            If File.Exists(sfxDefaultsFilePath) Then
                headerData = textFileReaders.GetFileHeaderInfo(sfxDefaultsFilePath)
                headerData.LastModify = created
                headerData.LastModifyBy = EuroSoundUser
            Else
                headerData.FirstCreated = created
                headerData.CreatedBy = EuroSoundUser
                headerData.LastModify = created
                headerData.LastModifyBy = EuroSoundUser
            End If

            'Save other data in the text file
            Using outputFile As New StreamWriter(sfxDefaultsFilePath)
                outputFile.WriteLine("## EuroSound SFX Defaults File File")
                outputFile.WriteLine("## First Created ... " & headerData.FirstCreated)
                outputFile.WriteLine("## Created By ... " & headerData.CreatedBy)
                outputFile.WriteLine("## Last Modified ... " & headerData.LastModify)
                outputFile.WriteLine("## Last Modified By ... " & headerData.LastModifyBy)
                outputFile.WriteLine("")

                'Variables used by radiobuttons
                Dim TrackingType As Byte = 0, action1 As Byte = 0, actionSamplePool As Byte = 0

                'SFX Parameters
                outputFile.WriteLine("#SFXParameters")
                outputFile.WriteLine(String.Format("ReverbSend  {0}", SfxParamsAndSamplePool1.TrackBar_Reverb.Value))
                If SfxParamsAndSamplePool1.RadioButton_Tracking_2D.Checked Then
                    TrackingType = 0
                End If
                If SfxParamsAndSamplePool1.RadioButton_TrackingType_Amb.Checked Then
                    TrackingType = 1
                End If
                If SfxParamsAndSamplePool1.RadioButton_TrackingType_3D.Checked Then
                    TrackingType = 2
                End If
                If SfxParamsAndSamplePool1.RadioButton_TrackingType_3DRandom.Checked Then
                    TrackingType = 3
                End If
                If SfxParamsAndSamplePool1.RadioButton_Tracking_2DPL2.Checked Then
                    TrackingType = 4
                End If
                outputFile.WriteLine(String.Format("TrackingType  {0}", TrackingType))
                outputFile.WriteLine(String.Format("InnerRadius  {0}", SfxParamsAndSamplePool1.TrackBar_InnerRadius.Value))
                outputFile.WriteLine(String.Format("OuterRadius  {0}", SfxParamsAndSamplePool1.TrackBar_OuterRadius.Value))
                outputFile.WriteLine(String.Format("MaxVoices  {0}", SfxParamsAndSamplePool1.Numeric_MaxVoices.Value))
                If SfxParamsAndSamplePool1.RadioButton_ActionSteal.Checked Then
                    action1 = 0
                End If
                If SfxParamsAndSamplePool1.RadioButton_ActionReject.Checked Then
                    action1 = 1
                End If
                outputFile.WriteLine(String.Format("Action1  {0}", action1))
                outputFile.WriteLine(String.Format("Priority  {0}", SfxParamsAndSamplePool1.Numeric_Priority.Value))
                outputFile.WriteLine(String.Format("Group  {0}", 0))
                outputFile.WriteLine(String.Format("Action2  {0}", 0))
                outputFile.WriteLine(String.Format("Alertness  {0}", SfxParamsAndSamplePool1.Numeric_Alertness.Value))
                outputFile.WriteLine(String.Format("IgnoreAge  {0}", If(SfxParamsAndSamplePool1.CheckBox_IgnoreAge.Checked, 1, 0)))
                outputFile.WriteLine(String.Format("Ducker  {0}", SfxParamsAndSamplePool1.Numeric_Ducker.Value))
                outputFile.WriteLine(String.Format("DuckerLenght  {0}", SfxParamsAndSamplePool1.Numeric_DuckerLength.Value))
                outputFile.WriteLine(String.Format("MasterVolume  {0}", SfxParamsAndSamplePool1.Numeric_MasterVolume.Value))
                outputFile.WriteLine(String.Format("Outdoors  {0}", If(SfxParamsAndSamplePool1.CheckBox_UnderWater.Checked, 1, 0)))
                outputFile.WriteLine(String.Format("PauseInNis  {0}", If(SfxParamsAndSamplePool1.Checkbox_PauseInNis.Checked, 1, 0)))
                outputFile.WriteLine(String.Format("StealOnAge  {0}", If(SfxParamsAndSamplePool1.CheckBox_StealOnLouder.Checked, 1, 0)))
                outputFile.WriteLine(String.Format("MusicType  {0}", If(SfxParamsAndSamplePool1.Checkbox_MusicType.Checked, 1, 0)))
                If SfxParamsAndSamplePool1.Checkbox_Doppler.Checked Then
                    outputFile.WriteLine(String.Format("Doppler  {0}", If(SfxParamsAndSamplePool1.Checkbox_Doppler.Checked, 1, 0)))
                End If
                outputFile.WriteLine("#END")
                outputFile.WriteLine("")

                'Write SFXSamplePoolControl
                outputFile.WriteLine("#SFXSamplePoolControl")
                If SfxParamsAndSamplePool1.RadioButton_Single.Checked Then
                    actionSamplePool = 0
                End If
                If SfxParamsAndSamplePool1.RadioButton_MultiSample.Checked Then
                    actionSamplePool = 1
                End If
                outputFile.WriteLine(String.Format("Action1  {0}", actionSamplePool))
                outputFile.WriteLine(String.Format("RandomPick  {0}", If(SfxParamsAndSamplePool1.CheckBox_RandomPick.Checked, 1, 0)))
                outputFile.WriteLine(String.Format("Shuffled  {0}", If(SfxParamsAndSamplePool1.CheckBox_Shuffled.Checked, 1, 0)))
                outputFile.WriteLine(String.Format("Loop  {0}", If(SfxParamsAndSamplePool1.CheckBox_SamplePoolLoop.Checked, 1, 0)))
                outputFile.WriteLine(String.Format("Polyphonic  {0}", If(SfxParamsAndSamplePool1.CheckBox_Polyphonic.Checked, 1, 0)))
                outputFile.WriteLine(String.Format("MinDelay  {0}", SfxParamsAndSamplePool1.Numeric_MinDelay.Value))
                outputFile.WriteLine(String.Format("MaxDelay  {0}", SfxParamsAndSamplePool1.Numeric_MaxDelay.Value))
                outputFile.WriteLine(String.Format("EnableSubSFX  {0}", 0))
                outputFile.WriteLine(String.Format("EnableStereo  {0}", 0))
                outputFile.WriteLine("#END")
                outputFile.WriteLine("")

                'Write HASHCODE
                outputFile.WriteLine("#HASHCODE")
                outputFile.WriteLine("HashCodeNumber " & 0)
                outputFile.WriteLine("#END")
            End Using
        End If
    End Sub
End Class